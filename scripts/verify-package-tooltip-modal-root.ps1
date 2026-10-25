param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Package,[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path;$taskPackage=(Resolve-Path -LiteralPath $Package).Path
$taskTarget=[IO.Path]::GetFullPath($Output);New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
$taskReport=Join-Path $taskTarget 'package-tooltip-modal-root.json';if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh package Tooltip output directory'}
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
foreach($taskFile in @('Orchard.dll','PackageConsumer.dll','resources.pri','Microsoft.UI.Xaml.dll')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskProcess=Start-Process -FilePath (Join-Path $taskApp 'PackageConsumer.exe') -ArgumentList "--probe-hidden-tooltip-modal-root --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(120000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill();$taskProcess.WaitForExit()}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw "Public package Tooltip failed: $taskReport"}
node (Join-Path $PSScriptRoot 'verify-package-tooltip-modal-root.mjs') --native $taskTarget --package $taskPackage --output (Join-Path $taskTarget 'package-tooltip-audit.json')
if($LASTEXITCODE -ne 0){throw 'Independent public package Tooltip audit failed'}
