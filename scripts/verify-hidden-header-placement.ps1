param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [Parameter(Mandatory)][string]$Package,
    [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference = 'Stop'
$appPath = (Resolve-Path -LiteralPath $AppDirectory).Path
$outputPath = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
if (Test-Path -LiteralPath (Join-Path $outputPath 'header-placement.json')) { throw 'Use a fresh Header placement output.' }
$identityPath = Join-Path $outputPath 'app-identity'
New-Item -ItemType Directory -Path $identityPath -Force | Out-Null
foreach ($name in @('Orchard.dll','PackageConsumer.dll')) { Copy-Item -LiteralPath (Join-Path $appPath $name) -Destination $identityPath }
foreach ($mode in @('margin','rejected')) {
    $folder = if ($mode -eq 'margin') { $outputPath } else { Join-Path $outputPath 'rejected' }
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    $report = Join-Path $folder 'header-placement.json'
    $arguments = "--probe-hidden-header-placement --verify-output `"$report`""
    if ($mode -eq 'rejected') { $arguments += ' --probe-header-zero-flow-clip' }
    $process = Start-Process -FilePath (Join-Path $appPath 'PackageConsumer.exe') -ArgumentList $arguments -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit(20000)) { $process.Kill(); throw 'New hidden Header probe exceeded its timeout.' }
    @{exited=$true;exitCode=$process.ExitCode} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'process-exit.json')
    $expected = if ($mode -eq 'margin') { 0 } else { 1 }
    if ($process.ExitCode -ne $expected) { throw "Header $mode probe exited $($process.ExitCode); expected $expected. See $report" }
}
node (Join-Path $PSScriptRoot 'verify-hidden-header-placement.mjs') --native $outputPath --app $appPath --package (Resolve-Path -LiteralPath $Package).Path --output (Join-Path $outputPath 'header-placement-audit.json')
if ($LASTEXITCODE -ne 0) { throw 'Header placement audit failed.' }
