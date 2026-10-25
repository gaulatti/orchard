param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [ValidateSet('x64', 'ARM64')][string]$Platform = 'x64',
    [string]$Output = 'artifacts/package-hidden-nav-menus'
)
$ErrorActionPreference = 'Stop'
$appPath = (Resolve-Path -LiteralPath $AppDirectory).Path
$referencePath = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference')).Path
$outputPath = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$reportPath = Join-Path $outputPath 'nav-menus.json'
$exitPath = Join-Path $outputPath 'nav-menus-exit.json'
foreach ($receipt in @($reportPath, $exitPath)) { if (Test-Path -LiteralPath $receipt) { Remove-Item -LiteralPath $receipt } }
$process = Start-Process -FilePath (Join-Path $appPath 'PackageConsumer.exe') -ArgumentList "--verify-output `"$reportPath`" --verify-hidden-nav-menus `"$referencePath`"" -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(60000)) { $process.Kill(); throw 'Hidden Nav Menu package verification timed out.' }
@{ exited = $true; exitCode = $process.ExitCode } | ConvertTo-Json | Set-Content -LiteralPath $exitPath
if ($process.ExitCode -ne 0) { if (Test-Path -LiteralPath $reportPath) { Get-Content -LiteralPath $reportPath }; throw "Hidden Nav Menu consumer failed: $($process.ExitCode)" }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if (-not $report.passed -or $report.processArchitecture -ne $Platform -or $report.processArchitecture -ne $report.osArchitecture) { throw 'Hidden Nav Menu package runtime or architecture failed.' }
node (Join-Path $PSScriptRoot 'verify-hidden-nav-menus.mjs') --native $outputPath --app $appPath --output (Join-Path $outputPath 'nav-menus-audit.json')
if ($LASTEXITCODE -ne 0) { throw 'Hidden Nav Menu package audit failed.' }
