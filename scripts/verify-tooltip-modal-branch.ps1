param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[string]$Reference='tests/Reference/Tooltip.json')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path;$taskReference=(Resolve-Path -LiteralPath $Reference).Path
$taskTarget=[IO.Path]::GetFullPath($Output);New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
$taskReport=Join-Path $taskTarget 'tooltip-modal-branch.json';if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh Tooltip output directory'}
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
foreach($taskFile in @('Orchard.dll','Orchard.Catalog.dll','resources.pri','Microsoft.UI.Xaml.dll')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskProcess=Start-Process -FilePath (Join-Path $taskApp 'Orchard.Catalog.exe') -ArgumentList "--probe-hidden-tooltip-modal-branch `"$taskReference`" --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(180000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill();$taskProcess.WaitForExit()}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw "Tooltip branch failed: $taskReport"}
node (Join-Path $PSScriptRoot 'verify-tooltip-modal-branch.mjs') --native $taskTarget --reference $taskReference --output (Join-Path $taskTarget 'tooltip-branch-audit.json')
if($LASTEXITCODE -ne 0){throw 'Independent Tooltip branch audit failed'}
