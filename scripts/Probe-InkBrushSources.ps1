param(
    [ValidateSet('foreground-color','backdrop-object')][string]$Mode,
    [ValidateSet('X64','Arm64')][string]$ExpectedArchitecture,
    [string]$Executable = 'artifacts/catalog/Orchard.Catalog.exe',
    [string]$Output = 'artifacts/ink-source-probe'
)
$ErrorActionPreference = 'Stop'
if (-not $IsWindows -or $env:CI -ne 'true' -or $env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted') {
    throw 'This diagnostic requires an isolated hosted Windows runner.'
}
$path = [IO.Path]::GetFullPath($Output)
if (Test-Path $path) { throw 'Use a fresh output directory; never overwrite a probe baseline.' }
New-Item -ItemType Directory -Path $path | Out-Null
$exe = (Resolve-Path $Executable).Path
$process = Start-Process -FilePath $exe -ArgumentList "--smoke-output `"$path`" --smoke-error-state-ink-source-probe --ink-probe-mode $Mode" -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(360000)) { $process.Kill(); throw 'Ink source probe exceeded the original bounded smoke deadline.' }
if ($process.ExitCode -ne 0) { throw "Ink source diagnostic exited with code $($process.ExitCode)." }
$result = Get-Content (Join-Path $path 'result.json') -Raw | ConvertFrom-Json
$report = Get-Content (Join-Path $path 'ink-source-probe.json') -Raw | ConvertFrom-Json
if (-not $result.passed -or -not $result.diagnosticOnly -or $result.componentAcceptance -or
    $result.component -ne 'error-state-ink-source-probe' -or -not $report.passed -or
    -not $report.diagnosticOnly -or $report.componentAcceptance -or -not $report.originalBaselineRetained -or
    $report.mode -ne $Mode -or $report.untouched.Count -ne 2 -or $report.phases.Count -ne 2) {
    throw 'Incomplete or non-diagnostic ink result.'
}
if ($result.environment.processArchitecture -ne $ExpectedArchitecture -or $result.environment.osArchitecture -ne $ExpectedArchitecture) {
    throw 'Diagnostic process/OS architecture mismatch.'
}
foreach ($phase in $report.phases) {
    if (-not $phase.stable -or -not $phase.changedRaster -or $phase.newEndpointPixels -le 0 -or
        $phase.originalSolidPixels -le 0 -or $phase.expectedSolidPixels -ne $phase.originalSolidPixels -or
        -not $phase.sourcesRestored -or -not $phase.sharedConsumerSourcesRestored -or
        -not $phase.nativeUnchanged -or -not $phase.originalRasterRestored) { throw 'Exact probe restoration failed.' }
}
foreach ($name in @('baseline','black-painted','black-restored','white-painted','white-restored')) {
    if (-not (Test-Path (Join-Path $path "ink-probe-$name.png"))) { throw "Missing probe raster $name." }
}
Get-Content (Join-Path $path 'ink-source-probe.json')
