param(
    [ValidateSet('X64','Arm64')][string]$ExpectedArchitecture,
    [string]$Executable = 'artifacts/catalog/Orchard.Catalog.exe',
    [string]$Output = 'artifacts/glyph-runtime'
)
$ErrorActionPreference = 'Stop'
if (-not $IsWindows -or $env:CI -ne 'true' -or $env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted') {
    throw 'Glyph runtime inventory requires an isolated hosted Windows runner.'
}
$folder = [IO.Path]::GetFullPath($Output)
if (Test-Path $folder) { throw 'Use a fresh inventory output directory.' }
New-Item -ItemType Directory -Path $folder | Out-Null
Add-Type -AssemblyName System.Reflection.Metadata

function Get-ImageEvidence([string]$Path) {
    $file = Get-Item -LiteralPath $Path
    $stream = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
    $reader = $null
    try {
        $reader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
        $symbols = @($reader.ReadDebugDirectory() | Where-Object { $_.Type.ToString() -eq 'CodeView' } | ForEach-Object {
            $data = $reader.ReadCodeViewDebugDirectoryData($_)
            [ordered]@{ guid=$data.Guid.ToString('N'); age=$data.Age; path=$data.Path }
        })
        [ordered]@{
            path=$file.FullName; bytes=$file.Length
            sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            machine=$reader.PEHeaders.CoffHeader.Machine.ToString()
            fileVersion=$file.VersionInfo.FileVersion; productVersion=$file.VersionInfo.ProductVersion
            codeView=$symbols
        }
    } finally {
        if ($null -ne $reader) { $reader.Dispose() }
        $stream.Dispose()
    }
}

$report = [ordered]@{
    diagnosticOnly=$true; componentAcceptance=$false; inventoryComplete=$false
    head=$env:GITHUB_SHA; expectedArchitecture=$ExpectedArchitecture
    osDescription=[Runtime.InteropServices.RuntimeInformation]::OSDescription
    osArchitecture=[Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    inventoryProcessArchitecture=[Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
    scope='Read-only binary/symbol/tool inventory and bounded own catalog startup; no native glyph case, contrast write, repair, SDK replacement or acceptance claim.'
}
$process = $null
try {
    $exe = (Resolve-Path -LiteralPath $Executable).Path
    $report.executable = Get-ImageEvidence $exe
    $expectedMachine = if ($ExpectedArchitecture -eq 'X64') { 'Amd64' } else { 'Arm64' }
    if ($report.executable.machine -ne $expectedMachine) { throw 'Published executable architecture mismatch.' }
    $kit = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/Debuggers'
    $report.debuggerCandidates = @(
        foreach ($arch in @('x64','arm64')) {
            foreach ($name in @('cdb.exe','symchk.exe')) {
                $candidate = Join-Path $kit "$arch/$name"
                $exists = Test-Path -LiteralPath $candidate
                [ordered]@{ architecture=$arch; name=$name; path=$candidate; exists=$exists
                    image=$(if ($exists) { Get-ImageEvidence $candidate } else { $null }) }
            }
        }
    )
    $report.debuggerCommands = @(Get-Command cdb.exe,ntsd.exe,symchk.exe -ErrorAction SilentlyContinue |
        Select-Object Name,Source)
    $desktop = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Control Panel\Desktop',$false)
    try {
        $valueNames = @($desktop.GetValueNames())
        $report.fontPreferences = @(
            foreach ($name in @('FontSmoothing','FontSmoothingType','FontSmoothingGamma','FontSmoothingOrientation')) {
                $present = $valueNames -contains $name
                [ordered]@{ name=$name; present=$present
                    kind=$(if ($present) { $desktop.GetValueKind($name).ToString() } else { $null })
                    value=$(if ($present) { $desktop.GetValue($name,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames) } else { $null }) }
            }
        )
    } finally { if ($null -ne $desktop) { $desktop.Dispose() } }
    # Start only the published catalog, without a smoke/contrast command. Module
    # inspection records module identities and on-disk public image metadata,
    # not application data or environment. Always close only this PID.
    $process = Start-Process -FilePath $exe -PassThru
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $modules = @()
    do {
        $process.Refresh()
        if ($process.HasExited) { throw 'Catalog exited before native rendering modules were observable.' }
        $modules = @($process.Modules | Where-Object { $_.ModuleName -match '(?i)(dwrite|xaml|d2d|d3d|dxgi)' })
        if (@($modules | Where-Object { $_.ModuleName -ieq 'Microsoft.UI.Xaml.dll' }).Count -gt 0 -and
            @($modules | Where-Object { $_.ModuleName -match '(?i)dwrite' }).Count -gt 0) { break }
        Start-Sleep -Milliseconds 100
    } while ($clock.ElapsedMilliseconds -lt 30000)
    if (@($modules | Where-Object { $_.ModuleName -ieq 'Microsoft.UI.Xaml.dll' }).Count -eq 0 -or
        @($modules | Where-Object { $_.ModuleName -match '(?i)dwrite' }).Count -eq 0) {
        throw 'Pinned XAML and actual DWrite modules were not both observed within the bounded startup window.'
    }
    $report.loadedRenderingModules = @($modules | Sort-Object ModuleName | ForEach-Object {
        [ordered]@{ name=$_.ModuleName; image=(Get-ImageEvidence $_.FileName) }
    })
    $report.catalogProcessId = $process.Id
    $report.inventoryComplete = $true
} catch {
    $report.error = $_.Exception.ToString()
    throw
} finally {
    $cleanupFailure = $null
    try {
        if ($null -ne $process) {
            $process.Refresh()
            if (-not $process.HasExited) {
                $null = $process.CloseMainWindow()
                if (-not $process.WaitForExit(5000)) { $process.Kill($true); $process.WaitForExit() }
            }
            $report.ownedCatalogExited = $process.HasExited
            $process.Dispose()
        }
    } catch {
        $cleanupFailure = $_
        $report.ownedCleanupFailure = $_.Exception.ToString()
    } finally {
        $report | ConvertTo-Json -Depth 20 | Set-Content (Join-Path $folder 'inventory.json') -Encoding utf8
    }
    if ($null -ne $cleanupFailure) { throw $cleanupFailure }
}
Get-Content (Join-Path $folder 'inventory.json')
