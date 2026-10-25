param([Parameter(Mandatory)][string]$AppDirectory,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog',[Parameter(Mandatory)][string]$Reference,[Parameter(Mandatory)][string]$ReferenceImages,[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskReference=(Resolve-Path -LiteralPath $Reference).Path
$taskImages=(Resolve-Path -LiteralPath $ReferenceImages).Path
$taskOutput=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOutput){throw 'Use a fresh geometry sweep output'}
New-Item -ItemType Directory -Path $taskOutput|Out-Null
$taskIdentity=Join-Path $taskOutput 'app-identity';New-Item -ItemType Directory -Path $taskIdentity|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll","$HostName.exe",'Microsoft.UI.Xaml.dll','resources.pri')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskCopiedReference=Join-Path $taskOutput 'geometry-reference.json';Copy-Item -LiteralPath $taskReference -Destination $taskCopiedReference
$taskReport=Join-Path $taskOutput 'geometry.json'
$taskExe=Join-Path $taskApp "$HostName.exe"
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-overlay-near-geometry --verify-output `"$taskReport`" --geometry-reference `"$taskCopiedReference`"" -WindowStyle Hidden -PassThru
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o');foregroundAuthorized=$false;hardwareInputSent=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process.json')
$taskDone=$taskProcess.WaitForExit(180000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID; refusing termination'};$taskProcess.Kill();if(-not $taskProcess.WaitForExit(10000)){throw 'Owned geometry process did not terminate'}}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone);finishedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw 'Native geometry sweep failed; inspect retained stage/report'}
& node (Join-Path $PSScriptRoot 'verify-overlay-near-geometry.mjs') $taskOutput $taskCopiedReference $taskImages $HostName (Join-Path $taskOutput 'geometry-audit.json')
if($LASTEXITCODE -ne 0){throw 'Independent native geometry audit failed'}
