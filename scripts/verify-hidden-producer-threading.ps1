param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [ValidateSet('Catalog','PackageConsumer')][string]$HostKind = 'Catalog',
    [ValidateSet('X64','Arm64')][string]$ExpectedArchitecture = 'X64',
    [string]$Package,
    [string]$Output = 'artifacts/hidden-producer-threading'
)
$ErrorActionPreference = 'Stop'
$appPath = (Resolve-Path -LiteralPath $AppDirectory).Path
$outputPath = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$reportPath = Join-Path $outputPath 'hidden-producer-threading.json'
if (Test-Path -LiteralPath $reportPath) { throw 'Use a fresh producer-threading output directory.' }
$hostName = if ($HostKind -eq 'Catalog') { 'Orchard.Catalog' } else { 'PackageConsumer' }
$identityPath = Join-Path $outputPath 'app-identity'
New-Item -ItemType Directory -Path $identityPath -Force | Out-Null
foreach ($name in @('Orchard.dll', "$hostName.dll")) { Copy-Item -LiteralPath (Join-Path $appPath $name) -Destination $identityPath }
$arguments = if ($HostKind -eq 'Catalog') { "--verify-hidden-producer-threading `"$outputPath`"" } else { "--verify-output `"$reportPath`" --verify-hidden-producer-threading" }
$process = Start-Process -FilePath (Join-Path $appPath "$hostName.exe") -ArgumentList $arguments -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(120000)) { $process.Kill(); throw 'Hidden producer-threading regression timed out.' }
[ordered]@{ exited = $true; exitCode = $process.ExitCode } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'process-exit.json')
if ($process.ExitCode -ne 0) { throw "Hidden producer-threading regression failed: $($process.ExitCode). See $reportPath" }
$auditArgs = @((Join-Path $PSScriptRoot 'verify-hidden-producer-threading.mjs'), '--native', $outputPath, '--app', $appPath, '--host', $hostName, '--architecture', $ExpectedArchitecture, '--output', (Join-Path $outputPath 'producer-threading-audit.json'))
if ($HostKind -eq 'PackageConsumer') {
    if (-not $Package) { throw 'PackageConsumer verification requires the exact package path.' }
    $auditArgs += @('--package', (Resolve-Path -LiteralPath $Package).Path)
}
node @auditArgs
if ($LASTEXITCODE -ne 0) { throw 'Producer-threading receipt verification failed.' }
