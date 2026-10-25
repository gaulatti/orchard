param(
    [Parameter(Mandatory)][ValidateSet('X64','Arm64')][string]$ExpectedArchitecture,
    [string]$Executable = 'artifacts/catalog/Orchard.Catalog.exe',
    [string]$Output = 'artifacts/glyph-trace'
)
$ErrorActionPreference = 'Stop'
if (-not $IsWindows -or $env:CI -ne 'true' -or $env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted') {
    throw 'Glyph tracing requires an isolated hosted Windows runner.'
}
$folder = [IO.Path]::GetFullPath($Output)
if (Test-Path $folder) { throw 'Use a fresh trace directory; retain the first baseline.' }
New-Item -ItemType Directory -Path $folder | Out-Null
$work = Join-Path $folder 'debugger-work'
$cache = Join-Path $folder 'symbol-cache'
$case = Join-Path $folder 'glyph-case'
New-Item -ItemType Directory -Path $work,$cache,$case | Out-Null
Add-Type -AssemblyName System.Reflection.Metadata
. (Join-Path $PSScriptRoot 'GlyphImageEvidence.ps1')
$report = [ordered]@{
    head=$env:GITHUB_SHA; expectedArchitecture=$ExpectedArchitecture
    diagnosticOnly=$true; componentAcceptance=$false; traceComplete=$false; glyphCaseExecuted=$false
    scope='Invasive owned fictional catalog trace. Target-only bounded glyph/matrix data; no whole memory, heap or environment dump, SDK replacement or repair.'
}
$debugger = $null
$catalog = $null
$nativeLog = Join-Path $work 'native-glyph-trace.log'
$token = [Guid]::NewGuid().ToString('N')
try {
    $exe = (Resolve-Path -LiteralPath $Executable).Path
    $report.executable = Get-ImageEvidence $exe
    $machine = if ($ExpectedArchitecture -eq 'X64') { 'Amd64' } else { 'Arm64' }
    if ($report.executable.machine -ne $machine) { throw 'Catalog architecture mismatch.' }
    $arch = if ($ExpectedArchitecture -eq 'X64') { 'x64' } else { 'arm64' }
    $kit = [Environment]::GetFolderPath([System.Environment+SpecialFolder]::ProgramFilesX86)
    $cdb = Join-Path $kit "Windows Kits/10/Debuggers/$arch/cdb.exe"
    $report.debugger = Get-ImageEvidence $cdb
    if ($report.debugger.machine -ne $machine) { throw 'Debugger architecture mismatch.' }
    $bindings = Get-Content (Join-Path $PSScriptRoot 'glyph-runtime-identities.json') -Raw | ConvertFrom-Json
    $expected = $bindings.images.$ExpectedArchitecture
    $commands = Join-Path $work 'target-trace.cdb'
    & (Join-Path $PSScriptRoot 'New-GlyphTraceCommands.ps1') -Architecture $ExpectedArchitecture -Output $commands
    $report.commandFileSha256 = (Get-FileHash $commands -Algorithm SHA256).Hash.ToLowerInvariant()
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $cdb; $info.WorkingDirectory = $work; $info.UseShellExecute = $false
    foreach ($name in @($info.Environment.Keys)) {
        if ($name -match '(?i)(TOKEN|SECRET|PASSWORD|CREDENTIAL|KEY|^_NT_SYMBOL|^_NT_ALT_SYMBOL|^_NT_SOURCE)') {
            $null = $info.Environment.Remove($name)
        }
    }
    $info.RedirectStandardInput = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    foreach ($argument in @('-G','-hd','-noshell','-y',"srv*$cache*https://msdl.microsoft.com/download/symbols",
        '-logou',$nativeLog,'-cf',$commands,$exe,'--smoke-output',$case,
        '--smoke-error-state-highlight-ink-probe','--smoke-glyph-native-trace','--glyph-trace-token',$token)) {
        $info.ArgumentList.Add($argument)
    }
    $debugger = [Diagnostics.Process]::new(); $debugger.StartInfo = $info
    $null = $debugger.Start()
    $debugger.StandardInput.Close()
    $stdout = $debugger.StandardOutput.ReadToEndAsync()
    $stderr = $debugger.StandardError.ReadToEndAsync()
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $released = $false
    while (-not $debugger.HasExited -and $clock.ElapsedMilliseconds -lt 360000) {
        $pidFile = Join-Path $case 'owned-process.json'
        if (-not $released -and (Test-Path $pidFile)) {
            $owned = Get-Content $pidFile -Raw | ConvertFrom-Json
            if ($owned.token -cne $token -or $owned.executable -ine $exe) { throw 'Foreign process registration.' }
            $candidate = [Diagnostics.Process]::GetProcessById([int]$owned.pid)
            try {
                if ($candidate.MainModule.FileName -ine $exe -or
                    $candidate.StartTime.ToUniversalTime().Ticks.ToString() -cne $owned.startTimeUtcTicks -or
                    $candidate.StartTime.ToUniversalTime() -lt $debugger.StartTime.ToUniversalTime()) {
                    throw 'Owned process identity/start time mismatch; no release or foreign cleanup.'
                }
                $catalog = $candidate
            } finally {
                if ($null -eq $catalog) { $candidate.Dispose() }
            }
            $xaml = @($catalog.Modules | Where-Object { $_.ModuleName -ieq 'Microsoft.UI.Xaml.dll' })
            if ($xaml.Count -ne 1) { throw 'Expected exactly one loaded XAML image before release.' }
            $report.loadedXaml = Get-ImageEvidence $xaml[0].FileName
            if ($report.loadedXaml.machine -ne $machine -or $report.loadedXaml.sha256 -cne $expected.sha256 -or
                $report.loadedXaml.codeView.Count -ne 1 -or $report.loadedXaml.codeView[0].guid -cne $expected.pdbGuid -or
                $report.loadedXaml.codeView[0].age -ne $expected.pdbAge) { throw 'Loaded XAML identity mismatch; target-data breakpoints remain disabled.' }
            $report.runtimeIdentityMatched = $true
            $report.catalogProcessId = $catalog.Id
            $release = Join-Path $case 'identity-verified.release'
            [IO.File]::WriteAllText(($release+'.pending'),$token)
            [IO.File]::Move(($release+'.pending'),$release)
            $released = $true
            $report.glyphCaseExecuted = $true
        }
        Start-Sleep -Milliseconds 100
        $debugger.Refresh()
    }
    if (-not $debugger.HasExited) { $report.debuggerTimedOut = $true; throw 'Native trace exceeded the original360-second process deadline.' }
    $debugger.WaitForExit()
    $report.debuggerExitCode = $debugger.ExitCode
    $report.stdoutBytes = [Text.Encoding]::UTF8.GetByteCount($stdout.GetAwaiter().GetResult())
    $report.stderrBytes = [Text.Encoding]::UTF8.GetByteCount($stderr.GetAwaiter().GetResult())
    if (-not $released) { throw 'Catalog never reached identity-verified trace release.' }
    if (-not (Test-Path $nativeLog)) { throw 'Native trace log missing.' }
    $text = [IO.File]::ReadAllText($nativeLog,[Text.Encoding]::Unicode)
    $report.targetBindings = [regex]::Matches($text,'(?m)^GLYPH_TRACE_BIND old=').Count
    $report.runCaptures = [regex]::Matches($text,'(?m)^GLYPH_TRACE_RUN phase=').Count
    $report.finalMatrixCaptures = [regex]::Matches($text,'(?m)^GLYPH_TRACE_FINAL phase=').Count
    $report.phases = @([regex]::Matches($text,'(?m)^GLYPH_TRACE_PHASE ([0-9a-f]+)\r?$') | ForEach-Object { [Convert]::ToInt32($_.Groups[1].Value,16) })
    $report.limitOrBindingError = [regex]::IsMatch($text,'(?m)^GLYPH_TRACE_(?:.*_LIMIT|NULL_RUN|TARGET_NOT_BOUND)\r?$')
    $report.commandOrMemoryError = [regex]::IsMatch($text,"(?im)syntax error|couldn.t resolve|memory access error|unable to insert breakpoint|unrecognized command")
    $resultFile = Join-Path $case 'result.json'
    if (-not (Test-Path $resultFile)) { throw 'Original glyph case did not finish; no result inferred.' }
    $result = Get-Content $resultFile -Raw | ConvertFrom-Json
    $report.glyphCasePassed = $result.passed
    $report.glyphCaseError = $result.error
    $report.traceComplete = $report.runtimeIdentityMatched -and $report.targetBindings -gt 0 -and
        $report.runCaptures -gt 0 -and $report.finalMatrixCaptures -gt 0 -and
        $report.phases -contains 2 -and $report.phases -contains 8 -and
        -not $report.limitOrBindingError -and -not $report.commandOrMemoryError
    if (-not $report.traceComplete) { throw 'Target trace incomplete; inspect raw evidence without a glyph verdict.' }
    if (-not $result.passed) { throw 'The original glyph case still fails; complete trace is diagnostic evidence only.' }
} catch {
    $report.error = $_.Exception.ToString()
    throw
} finally {
    $cleanup = @()
    if ($null -ne $debugger) {
        try {
            if (-not $debugger.HasExited) { $debugger.Kill($true); $debugger.WaitForExit() }
            $report.ownedDebuggerExited = $debugger.HasExited
            $debugger.Dispose()
        } catch { $cleanup += $_.Exception.ToString() }
    }
    if ($null -ne $catalog) {
        try {
            $catalog.Refresh()
            if (-not $catalog.HasExited) { $catalog.Kill(); $catalog.WaitForExit() }
            $report.ownedCatalogExited = $catalog.HasExited
            $catalog.Dispose()
        } catch { $cleanup += $_.Exception.ToString() }
    }
    $report.cleanupErrors = $cleanup
    if (Test-Path $nativeLog) {
        $raw = [IO.File]::ReadAllBytes($nativeLog)
        $report.nativeLogBytes = $raw.Length
        $report.nativeLogSha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
        if ($raw.Length -le 2097152) {
            $part = 0
            for ($offset=0; $offset -lt $raw.Length; $offset+=24576) {
                $count = [Math]::Min(24576,$raw.Length-$offset)
                $bytes = [byte[]]::new($count); [Array]::Copy($raw,$offset,$bytes,0,$count)
                [ordered]@{part=$part;offset=$offset;bytes=$count
                    sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
                    base64=[Convert]::ToBase64String($bytes)} | ConvertTo-Json -Compress |
                    Set-Content (Join-Path $folder ('trace-log-{0:d3}.json' -f $part)) -Encoding utf8
                $part++
            }
            $report.nativeLogParts = $part
        } else { $report.nativeLogExportLimitExceeded = $true; $report.traceComplete = $false }
    }
    $report | ConvertTo-Json -Depth 20 | Set-Content (Join-Path $folder 'trace.json') -Encoding utf8
    if ($cleanup.Count -gt 0) { throw 'Owned trace process cleanup failed.' }
    if ($report.nativeLogExportLimitExceeded) { throw 'Native log exceeded complete bounded export.' }
}
