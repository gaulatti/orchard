param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output)
$HostName="Orchard.Catalog"
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path;$taskTarget=[IO.Path]::GetFullPath($Output);New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
$taskReport=Join-Path $taskTarget 'edge-dialog-catalog.json';if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh Drawer/Sheet catalog output directory'}
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskExe=Join-Path $taskApp "$HostName.exe"
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-edge-dialog-catalog --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(120000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill()}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone){throw 'Own Drawer/Sheet catalog probe exceeded its watchdog'}
if($taskProcess.ExitCode -ne 0){throw "Drawer/Sheet catalog probe failed: $taskReport"}
node (Join-Path $PSScriptRoot 'verify-edge-dialog-catalog.mjs') --native $taskTarget --output (Join-Path $taskTarget 'catalog-audit.json')
if($LASTEXITCODE -ne 0){throw 'Actual Drawer/Sheet catalog audit failed'}
