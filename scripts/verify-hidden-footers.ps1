param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [ValidateSet('X64','Arm64')][string]$ExpectedArchitecture = 'X64',
    [string]$Output = 'artifacts/hidden-footers'
)
$ErrorActionPreference = 'Stop'
$appPath = (Resolve-Path -LiteralPath $AppDirectory).Path
$outputPath = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$reportPath = Join-Path $outputPath 'hidden-footers.json'
if (Test-Path -LiteralPath $reportPath) { Remove-Item -LiteralPath $reportPath }
$process = Start-Process -FilePath (Join-Path $appPath 'Orchard.Catalog.exe') -ArgumentList "--verify-hidden-footers `"$outputPath`"" -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(180000)) { $process.Kill(); throw 'Hidden Footer diagnostic timed out.' }
[ordered]@{ exited = $true; exitCode = $process.ExitCode } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'process-exit.json')
if ($process.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $reportPath) { Get-Content -LiteralPath $reportPath -Raw }
    throw "Hidden Footer diagnostic failed: $($process.ExitCode)"
}
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if (-not $report.passed -or -not $report.isolated -or -not $report.diagnosticOnly -or $report.processArchitecture -ne $ExpectedArchitecture -or $report.osArchitecture -ne $ExpectedArchitecture -or $report.frames.Count -ne 192 -or $report.checks.Count -ne 7 -or $report.activationEvents -ne 0 -or $report.focusEvents -ne 0) { throw 'Hidden Footer report failed isolation, architecture or completeness.' }
[ordered]@{ passed = $report.passed; diagnosticOnly = $true; frames = $report.frames.Count; guards = $report.guardChecks; activationEvents = $report.activationEvents; focusEvents = $report.focusEvents; isolated = $report.isolated; visibleRuntimeVerified = $false; nativeComponentAccepted = $false } | ConvertTo-Json
