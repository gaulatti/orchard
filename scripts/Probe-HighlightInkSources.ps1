param(
    [ValidateSet('X64','Arm64')][string]$ExpectedArchitecture,
    [string]$Executable = 'artifacts/catalog/Orchard.Catalog.exe',
    [string]$Output = 'artifacts/highlight-ink-probe'
)
$ErrorActionPreference = 'Stop'
if (-not $IsWindows -or $env:CI -ne 'true' -or $env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted') {
    throw 'This diagnostic requires an isolated hosted Windows runner.'
}
$path = [IO.Path]::GetFullPath($Output)
if (Test-Path $path) { throw 'Use a fresh output directory; never overwrite a probe baseline.' }
New-Item -ItemType Directory -Path $path | Out-Null
$exe = (Resolve-Path $Executable).Path
$process = Start-Process -FilePath $exe -ArgumentList "--smoke-output `"$path`" --smoke-error-state-highlight-ink-probe" -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(360000)) { $process.Kill(); throw 'Highlight source probe exceeded the original bounded smoke deadline.' }
if ($process.ExitCode -ne 0) { throw "Highlight source diagnostic exited with code $($process.ExitCode)." }
$result = Get-Content (Join-Path $path 'result.json') -Raw | ConvertFrom-Json
if (-not $result.passed -or -not $result.diagnosticOnly -or $result.componentAcceptance -or
    $result.component -ne 'error-state-highlight-ink-probe' -or
    $result.environment.processArchitecture -ne $ExpectedArchitecture -or $result.environment.osArchitecture -ne $ExpectedArchitecture) {
    throw 'Incomplete diagnostic result or process/OS architecture mismatch.'
}
foreach ($label in @('error-title','error-description')) {
    $folder = Join-Path $path $label
    $report = Get-Content (Join-Path $folder 'highlight-ink-probe.json') -Raw | ConvertFrom-Json
    if (-not $report.passed -or -not $report.diagnosticOnly -or $report.componentAcceptance -or
        -not $report.originalBaselineRetained -or $report.label -ne $label -or
        -not $report.collectionContentsRestored -or $report.exactIntersectionPixels.Count -le 0 -or
        $report.observations.Count -ne 5 -or $report.phases.Count -ne 2) {
        throw "Incomplete highlight proof for $label."
    }
    foreach ($observation in $report.observations) {
        if (-not $observation.originalRasterRestored -or -not $observation.sourcesRestored -or
            -not $observation.sharedSourcesRestored -or -not $observation.nativeUnchanged -or
            -not $observation.collectionContentsRestored) { throw "Original baseline/source restoration failed for $label." }
    }
    foreach ($phase in $report.phases) {
        if (-not $phase.stable -or -not $phase.restored -or $phase.primaryFailure) { throw "Unstable or failed highlight phase for $label." }
        if (-not $phase.changedRaster -or $phase.actualEndpointPixels -le 0) {
            throw "Actual opaque endpoint not established for $label."
        }
    }
    foreach ($name in @('baseline','untouched-1','untouched-2','getter-only','black-on-white-first','black-on-white-second','black-on-white-restored',
        'white-on-black-first','white-on-black-second','white-on-black-restored')) {
        if (-not (Test-Path (Join-Path $folder "highlight-ink-$name.png"))) { throw "Missing live raster $label/$name." }
    }
    Get-Content (Join-Path $folder 'highlight-ink-probe.json')
}
