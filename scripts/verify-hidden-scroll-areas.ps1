param([Parameter(Mandatory)][string]$AppDirectory,[ValidateSet('Catalog','PackageConsumer')][string]$HostKind='Catalog',[ValidateSet('X64','Arm64')][string]$ExpectedArchitecture='X64',[string]$Package,[Parameter(Mandatory)][string]$Output,[string]$Reference='tests/Reference/ScrollArea.json')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskTarget=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
$taskReport=Join-Path $taskTarget 'scroll-areas.json'
if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh ScrollArea output directory.'}
$taskHost=if($HostKind -eq 'Catalog'){'Orchard.Catalog'}else{'PackageConsumer'}
$taskReference=(Resolve-Path -LiteralPath $Reference).Path
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
foreach($taskFile in @('Orchard.dll',"$taskHost.dll")){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskExe=Join-Path $taskApp "$taskHost.exe"
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--verify-hidden-scroll-areas `"$taskReference`" --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(120000)
if(-not $taskDone){$taskProcess.Kill()}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone){throw 'Own hidden ScrollArea run exceeded its watchdog.'}
if($taskProcess.ExitCode -ne 0){throw "Native ScrollArea failed; see $taskReport"}
$taskAudit=@((Join-Path $PSScriptRoot 'verify-hidden-scroll-areas.mjs'),'--native',$taskTarget,'--app',$taskIdentity,'--host',$taskHost,'--architecture',$ExpectedArchitecture,'--reference',$taskReference,'--output',(Join-Path $taskTarget 'scroll-area-audit.json'))
if($HostKind -eq 'PackageConsumer'){if(-not $Package){throw 'The independent ScrollArea consumer requires its exact package.'};$taskAudit+=@('--package',(Resolve-Path -LiteralPath $Package).Path)}
node @taskAudit
if($LASTEXITCODE -ne 0){throw 'Independent ScrollArea audit failed.'}
