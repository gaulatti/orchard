param(
    [ValidateSet('X64','Arm64')][string]$ExpectedArchitecture,
    [string]$Executable = 'artifacts/catalog/Orchard.Catalog.exe',
    [string]$Output = 'artifacts/glyph-symbols'
)
$ErrorActionPreference = 'Stop'
if (-not $IsWindows -or $env:CI -ne 'true' -or $env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted') {
    throw 'Glyph symbol probing requires an isolated hosted Windows runner.'
}
$folder = [IO.Path]::GetFullPath($Output)
if (Test-Path $folder) { throw 'Use a fresh symbol output directory.' }
New-Item -ItemType Directory -Path $folder | Out-Null
$work = Join-Path $folder 'debugger-work'
$cache = Join-Path $folder 'symbol-cache'
New-Item -ItemType Directory -Path $work,$cache | Out-Null
Add-Type -AssemblyName System.Reflection.Metadata
. (Join-Path $PSScriptRoot 'GlyphImageEvidence.ps1')

function New-ScopedProcessInfo([string]$File,[string]$Directory) {
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $File
    $info.WorkingDirectory = $Directory
    $info.UseShellExecute = $false
    foreach ($name in @($info.Environment.Keys)) {
        if ($name -match '(?i)(TOKEN|SECRET|PASSWORD|CREDENTIAL|KEY|^_NT_SYMBOL|^_NT_ALT_SYMBOL|^_NT_SOURCE)') {
            $null = $info.Environment.Remove($name)
        }
    }
    return $info
}

# Metadata and code-entry queries only: no addresses are supplied to dt, and
# each disassembly is bounded to 0x40 instructions from a named image function.
# Displayed PDB argument lists alone cannot establish calling-convention or
# source differences. Inspect actual layouts/code before assigning capture fields.
$queryCommands = @(
    '.echo GLYPH_SYMBOL_QUERY_BEGIN'
    '.reload /f Microsoft.UI.Xaml.dll'
    'lmvm Microsoft_UI_Xaml'
    'x Microsoft_UI_Xaml!*DWriteTextRenderer*'
    'x Microsoft_UI_Xaml!*D2DTextDrawingContext*'
)
foreach ($type in @('DWriteTextRenderer','DWRITE_GLYPH_RUN','DWRITE_GLYPH_RUN_DESCRIPTION',
    'DWRITE_GLYPH_OFFSET','PALText::GlyphRun','PALText::GlyphOffset','DWriteFontFace','D2DTextDrawingContext',
    'D2DTextDrawingContext::GlyphRunData','D2DTextDrawingContext::GlyphRunRealization',
    'DynamicArray<D2DTextDrawingContext::GlyphRunData>',
    'HWTextRealization','HWRenderParams','CMILMatrix','CLayoutStorage','CTextBlock')) {
    $queryCommands += ".echo TYPE_METADATA $type"
    $queryCommands += "dt -v Microsoft_UI_Xaml!$type"
}
foreach ($function in @('DWriteTextRenderer::DrawGlyphRun',
    'D2DTextDrawingContext::GetGlyphRunTransformAndBounds',
    'D2DTextDrawingContext::RasterizeGlyphRunToD2DOrRawBuffer',
    'D2DTextDrawingContext::HWBuildGlyphRunSingleTexture','D2DTextDrawingContext::HWRender')) {
    $queryCommands += ".echo IMAGE_ENTRY_CODE $function"
    $queryCommands += "u Microsoft_UI_Xaml!$function L40"
}
$queryCommands += @('.echo GLYPH_SYMBOL_QUERY_END','qd')
$commands = $queryCommands -join '; '
$nativeLog = Join-Path $work 'native-symbols.log'
$report = [ordered]@{
    diagnosticOnly=$true; componentAcceptance=$false; glyphCaseExecuted=$false; probeComplete=$false
    probeKind='binary-type-and-entry-metadata'
    head=$env:GITHUB_SHA; expectedArchitecture=$ExpectedArchitecture
    scope='Owned catalog startup and noninvasive non-suspending type metadata plus bounded image-code queries only. No data-memory/environment/heap dumps, glyph case, repair or SDK replacement.'
    commands=$commands; symbolServer='https://msdl.microsoft.com/download/symbols'
    nativeLogEncoding='UTF-16LE from explicit CDB -logou'
}
$catalog = $null
$debugger = $null
try {
    $exe = (Resolve-Path -LiteralPath $Executable).Path
    $report.executable = Get-ImageEvidence $exe
    $expectedMachine = if ($ExpectedArchitecture -eq 'X64') { 'Amd64' } else { 'Arm64' }
    if ($report.executable.machine -ne $expectedMachine) { throw 'Published executable architecture mismatch.' }
    $arch = if ($ExpectedArchitecture -eq 'X64') { 'x64' } else { 'arm64' }
    $programFilesX86 = [Environment]::GetFolderPath([System.Environment+SpecialFolder]::ProgramFilesX86)
    $cdb = Join-Path $programFilesX86 "Windows Kits/10/Debuggers/$arch/cdb.exe"
    $report.debugger = Get-ImageEvidence $cdb
    if ($report.debugger.machine -ne $expectedMachine) { throw 'Native debugger architecture mismatch.' }
    $catalog = [Diagnostics.Process]::new()
    $catalog.StartInfo = New-ScopedProcessInfo $exe (Split-Path $exe)
    $null = $catalog.Start()
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $xaml = $null
    do {
        $catalog.Refresh()
        if ($catalog.HasExited) { throw 'Owned catalog exited before pinned XAML was observable.' }
        $xaml = @($catalog.Modules | Where-Object { $_.ModuleName -ieq 'Microsoft.UI.Xaml.dll' })
        if ($xaml.Count -eq 1) { break }
        Start-Sleep -Milliseconds 100
    } while ($clock.ElapsedMilliseconds -lt 30000)
    if ($xaml.Count -ne 1) { throw 'Expected one pinned XAML module in the owned catalog.' }
    $report.loadedXaml = Get-ImageEvidence $xaml[0].FileName
    $bindings = Get-Content (Join-Path $PSScriptRoot 'glyph-runtime-identities.json') -Raw | ConvertFrom-Json
    $expected = $bindings.images.$ExpectedArchitecture
    if ($report.loadedXaml.sha256 -cne $expected.sha256 -or $report.loadedXaml.codeView.Count -ne 1 -or
        $report.loadedXaml.codeView[0].guid -cne $expected.pdbGuid -or $report.loadedXaml.codeView[0].age -ne $expected.pdbAge) {
        throw 'Loaded XAML differs from the captured runtime identity; inspect new evidence before querying symbols.'
    }
    $report.runtimeIdentityMatched = $true
    $report.identityInventoryRun = $bindings.inventoryRun
    $report.catalogProcessId = $catalog.Id
    $info = New-ScopedProcessInfo $cdb $work
    $info.RedirectStandardInput = $true
    # Use structured argv, the empty owned working directory and explicit symbols.
    foreach ($argument in @('-pvr','-noshell','-y',
        "srv*$cache*https://msdl.microsoft.com/download/symbols",'-logou',$nativeLog,'-c',$commands,'-p',$catalog.Id.ToString())) {
        $info.ArgumentList.Add($argument)
    }
    $debugger = [Diagnostics.Process]::new()
    $debugger.StartInfo = $info
    $null = $debugger.Start()
    $debugger.StandardInput.Close()
    if (-not $debugger.WaitForExit(180000)) {
        $report.debuggerTimedOut = $true
        $debugger.Kill($true)
        $debugger.WaitForExit()
        throw 'Bounded native symbol query timed out; do not infer symbol absence.'
    }
    $report.debuggerExitCode = $debugger.ExitCode
    if (-not (Test-Path -LiteralPath $nativeLog)) { throw 'CDB produced no native log.' }
    $lines = [IO.File]::ReadAllLines($nativeLog,[Text.Encoding]::Unicode)
    $begin = -1
    $end = -1
    for ($index = 0; $index -lt $lines.Length; $index++) {
        if ($lines[$index].Trim() -ceq 'GLYPH_SYMBOL_QUERY_BEGIN' -and $begin -lt 0) { $begin = $index }
        if ($lines[$index].Trim() -ceq 'GLYPH_SYMBOL_QUERY_END' -and $begin -ge 0) { $end = $index; break }
    }
    $report.standaloneQueryMarkers = $begin -ge 0 -and $end -gt $begin
    if ($debugger.ExitCode -ne 0 -or -not $report.standaloneQueryMarkers) {
        throw 'Native debugger transport/command sequence incomplete; retain its log, not a symbol verdict.'
    }
    if ($end -le $begin + 1) { throw 'Native query emitted no diagnostic output; no symbol verdict.' }
    $queries = $lines[($begin + 1)..($end - 1)]
    $addressLines = @($queries | Where-Object {
        $_ -match '^\s*[0-9a-fA-F]{8}\x60[0-9a-fA-F]{8}\s+Microsoft_UI_Xaml!'
    })
    $report.addressSymbolLineCount = $addressLines.Length
    $report.addressSymbolLineSample = @($addressLines | Select-Object -First 20)
    $report.queryLineCount = $queries.Length
    $report.probeComplete = $true
    # An empty symbol list is data, not a successful native acceptance result.
} catch {
    $report.error = $_.Exception.ToString()
    throw
} finally {
    $cleanupErrors = @()
    if ($null -ne $debugger) {
        try {
            if (-not $debugger.HasExited) { $debugger.Kill($true); $debugger.WaitForExit() }
            $report.ownedDebuggerExited = $debugger.HasExited
            $debugger.Dispose()
        } catch { $cleanupErrors += $_.Exception.ToString() }
    }
    if ($null -ne $catalog) {
        try {
            $catalog.Refresh()
            if (-not $catalog.HasExited) {
                $null = $catalog.CloseMainWindow()
                if (-not $catalog.WaitForExit(5000)) { $catalog.Kill($true); $catalog.WaitForExit() }
            }
            $report.ownedCatalogExited = $catalog.HasExited
            $catalog.Dispose()
        } catch { $cleanupErrors += $_.Exception.ToString() }
    }
    $report.cleanupErrors = $cleanupErrors
    if (Test-Path -LiteralPath $nativeLog) {
        $raw = [IO.File]::ReadAllBytes($nativeLog)
        $report.nativeLogBytes = $raw.Length
        $report.nativeLogSha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
        if ($raw.Length -le 2097152) {
            # Retain the entire raw log in small hash-bound frames. This avoids
            # Actions artifact quota and individual long-line truncation.
            $part = 0
            for ($offset = 0; $offset -lt $raw.Length; $offset += 24576) {
                $count = [Math]::Min(24576,$raw.Length - $offset)
                $bytes = [byte[]]::new($count)
                [Array]::Copy($raw,$offset,$bytes,0,$count)
                [ordered]@{ part=$part; offset=$offset; bytes=$count
                    sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
                    base64=[Convert]::ToBase64String($bytes) } |
                    ConvertTo-Json -Compress | Set-Content (Join-Path $folder ('symbol-log-{0:d3}.json' -f $part)) -Encoding utf8
                $part++
            }
            $report.nativeLogParts = $part
        } else {
            $report.nativeLogExportLimitExceeded = $true
            $report.probeComplete = $false
        }
    }
    $report | ConvertTo-Json -Depth 20 | Set-Content (Join-Path $folder 'symbols.json') -Encoding utf8
    if ($cleanupErrors.Count -gt 0) { throw 'Owned symbol probe cleanup failed; see retained report.' }
    if ($report.nativeLogExportLimitExceeded) { throw 'Native symbol log exceeded bounded export; no complete probe claimed.' }
}
Get-Content (Join-Path $folder 'symbols.json')
