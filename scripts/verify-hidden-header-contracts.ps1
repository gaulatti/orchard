param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [ValidateSet('Catalog','PackageConsumer')][string]$HostKind = 'Catalog',
    [ValidateSet('X64','Arm64')][string]$ExpectedArchitecture = 'X64',
    [string]$Package,
    [string]$Reference = (Join-Path $PSScriptRoot '../tests/Reference/HeaderComposition.json'),
    [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference = 'Stop'
$appPath = (Resolve-Path -LiteralPath $AppDirectory).Path
$referencePath = (Resolve-Path -LiteralPath $Reference).Path
$outputPath = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$report = Join-Path $outputPath 'header-contracts.json'
if (Test-Path -LiteralPath $report) { throw 'Use a fresh Header contracts output.' }
$hostName = if ($HostKind -eq 'Catalog') { 'Orchard.Catalog' } else { 'PackageConsumer' }
$identity = Join-Path $outputPath 'app-identity'; New-Item -ItemType Directory -Path $identity -Force | Out-Null
foreach ($name in @('Orchard.dll',"$hostName.dll")) { Copy-Item -LiteralPath (Join-Path $appPath $name) -Destination $identity }
$arguments = "--verify-hidden-header-contracts `"$referencePath`" --verify-output `"$report`""
$process = Start-Process -FilePath (Join-Path $appPath "$hostName.exe") -ArgumentList $arguments -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(120000)) { $process.Kill(); throw 'New hidden Header contracts exceeded their watchdog.' }
@{exited=$true;exitCode=$process.ExitCode} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'process-exit.json')
if ($process.ExitCode -ne 0) { throw "Header contracts failed; see $report" }
$audit = @((Join-Path $PSScriptRoot 'verify-hidden-header-contracts.mjs'),'--native',$outputPath,'--app',$identity,'--host',$hostName,'--architecture',$ExpectedArchitecture,'--reference',$referencePath,'--output',(Join-Path $outputPath 'header-contracts-audit.json'))
if ($HostKind -eq 'PackageConsumer') { if (-not $Package) { throw 'Header consumer contracts require their exact package.' }; $audit += @('--package',(Resolve-Path -LiteralPath $Package).Path) }
node @audit
if ($LASTEXITCODE -ne 0) { throw 'Header contract audit failed.' }
