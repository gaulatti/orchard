param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[string]$Reference='tests/Reference/Select.json')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path;$taskReference=(Resolve-Path -LiteralPath $Reference).Path
$taskTarget=[IO.Path]::GetFullPath($Output);New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
$taskReport=Join-Path $taskTarget 'select-modal-branch.json';if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh Select branch output directory'}
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
foreach($taskFile in @('Orchard.dll','Orchard.Catalog.dll','Microsoft.UI.Xaml.dll','resources.pri')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskProcess=Start-Process -FilePath (Join-Path $taskApp 'Orchard.Catalog.exe') -ArgumentList "--probe-hidden-select-modal-branch `"$taskReference`" --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(120000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill();$taskProcess.WaitForExit()}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw "Native Select branch failed: $taskReport"}
node (Join-Path $PSScriptRoot 'verify-select-modal-branch.mjs') --native $taskTarget --reference $taskReference --output (Join-Path $taskTarget 'select-branch-audit.json')
if($LASTEXITCODE -ne 0){throw 'Independent Select branch audit failed'}
