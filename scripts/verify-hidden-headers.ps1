param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [ValidateSet('Catalog','PackageConsumer')][string]$HostKind = 'Catalog',
    [ValidateSet('X64','Arm64')][string]$ExpectedArchitecture = 'X64',
    [string]$Package,
    [Parameter(Mandatory)][string]$Output,
    [string]$Reference = 'tests/Reference/Header.json'
)
$ErrorActionPreference = 'Stop'
$appPath = (Resolve-Path -LiteralPath $AppDirectory).Path
$outputPath = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$report = Join-Path $outputPath 'header.json'
if (Test-Path -LiteralPath $report) { throw 'Use a fresh Header output.' }
$hostName = if ($HostKind -eq 'Catalog') { 'Orchard.Catalog' } else { 'PackageConsumer' }
$identity = Join-Path $outputPath 'app-identity'; New-Item -ItemType Directory -Path $identity -Force | Out-Null
foreach ($name in @('Orchard.dll',"$hostName.dll")) { Copy-Item -LiteralPath (Join-Path $appPath $name) -Destination $identity }
$source = (Resolve-Path -LiteralPath $Reference).Path
$process = Start-Process -FilePath (Join-Path $appPath "$hostName.exe") -ArgumentList "--verify-hidden-headers `"$source`" --verify-output `"$report`"" -PassThru -WindowStyle Hidden
@{processId=$process.Id;executable=(Join-Path $appPath "$hostName.exe");startedAt=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'process.json')
if (-not $process.WaitForExit(120000)) { $process.Kill(); throw 'New hidden Header test exceeded its watchdog.' }
@{exited=$true;exitCode=$process.ExitCode} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'process-exit.json')
if ($process.ExitCode -ne 0) { throw "Header capture failed; see $report" }
$audit = @((Join-Path $PSScriptRoot 'verify-hidden-headers.mjs'),'--native',$outputPath,'--app',$identity,'--host',$hostName,'--architecture',$ExpectedArchitecture,'--reference',$source,'--output',(Join-Path $outputPath 'header-audit.json'))
if ($HostKind -eq 'PackageConsumer') { if (-not $Package) { throw 'The independent Header consumer requires its exact package.' }; $audit += @('--package',(Resolve-Path -LiteralPath $Package).Path) }
node @audit
if ($LASTEXITCODE -ne 0) { throw 'Header source comparison failed.' }
