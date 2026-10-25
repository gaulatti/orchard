param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [Parameter(Mandatory)][string]$Package,
    [ValidateSet('X64','Arm64')][string]$ExpectedArchitecture = 'X64',
    [string]$Output = 'artifacts/package-hidden-footer'
)
$ErrorActionPreference = 'Stop'
$appPath = (Resolve-Path -LiteralPath $AppDirectory).Path
$packagePath = (Resolve-Path -LiteralPath $Package).Path
$referencePath = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference')).Path
$outputPath = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$reportPath = Join-Path $outputPath 'hidden-footers.json'
if (Test-Path -LiteralPath $reportPath) { Remove-Item -LiteralPath $reportPath }
$process = Start-Process -FilePath (Join-Path $appPath 'PackageConsumer.exe') -ArgumentList "--verify-output `"$reportPath`" --verify-hidden-footers `"$referencePath`"" -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(180000)) { $process.Kill(); throw 'Hidden Footer package diagnostic timed out.' }
[ordered]@{ exited = $true; exitCode = $process.ExitCode } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'process-exit.json')
if ($process.ExitCode -ne 0) { if (Test-Path -LiteralPath $reportPath) { Get-Content -LiteralPath $reportPath -Raw }; throw "Hidden Footer package failed: $($process.ExitCode)" }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if (-not $report.passed -or -not $report.isolated -or $report.processArchitecture -ne $ExpectedArchitecture -or $report.osArchitecture -ne $ExpectedArchitecture) { throw 'Hidden Footer package runtime or isolation failed.' }
node (Join-Path $PSScriptRoot 'verify-hidden-footers.mjs') --package-consumer --package $packagePath --native $outputPath --app $appPath --output (Join-Path $outputPath 'footer-audit.json')
if ($LASTEXITCODE -ne 0) { throw 'Hidden Footer package source audit failed.' }
