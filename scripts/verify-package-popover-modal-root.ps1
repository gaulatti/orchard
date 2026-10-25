param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Package,[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path;$taskPackage=(Resolve-Path -LiteralPath $Package).Path
$taskTarget=[IO.Path]::GetFullPath($Output);New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
$taskReport=Join-Path $taskTarget 'package-popover-modal-root.json';if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh public package popup output directory'}
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
foreach($taskFile in @('Orchard.dll','PackageConsumer.dll','Microsoft.UI.Xaml.dll','resources.pri')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskProcess=Start-Process -FilePath (Join-Path $taskApp 'PackageConsumer.exe') -ArgumentList "--probe-hidden-popover-modal-root --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(60000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill();$taskProcess.WaitForExit()}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw "Public package popup failed: $taskReport"}
node (Join-Path $PSScriptRoot 'verify-package-popover-modal-root.mjs') --native $taskTarget --package $taskPackage --output (Join-Path $taskTarget 'package-root-audit.json')
if($LASTEXITCODE -ne 0){throw 'Independent public package popup audit failed'}
