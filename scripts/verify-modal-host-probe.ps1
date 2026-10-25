param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[string]$Reference='tests/Reference/AlertDialog.json')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskTarget=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
$taskReport=Join-Path $taskTarget 'modal-host-core.json'
if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh modal host probe output directory.'}
$taskReference=(Resolve-Path -LiteralPath $Reference).Path
$taskIdentity=Join-Path $taskTarget 'app-identity'
New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
foreach($taskFile in @('Orchard.dll','Orchard.Catalog.dll','resources.pri','Microsoft.UI.Xaml.dll','Orchard.Catalog.deps.json')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskExe=Join-Path $taskApp 'Orchard.Catalog.exe'
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-modal-host `"$taskReference`" --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(120000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Own process unexpectedly reused a protected PID; refusing termination.'};$taskProcess.Kill()}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone){throw 'Own hidden modal host probe exceeded its watchdog.'}
if($taskProcess.ExitCode -ne 0){throw "Native modal host probe failed; see $taskReport"}
node (Join-Path $PSScriptRoot 'verify-modal-host-probe.mjs') --native $taskTarget --app $taskIdentity --reference $taskReference --output (Join-Path $taskTarget 'modal-host-audit.json')
if($LASTEXITCODE -ne 0){throw 'Independent modal host probe audit failed.'}
