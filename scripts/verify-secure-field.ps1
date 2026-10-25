param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog',[switch]$Catalog)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskTarget=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
if($Catalog -and $HostName -ne 'Orchard.Catalog'){throw 'Actual catalog mode requires the catalog host'}
$taskName=if($Catalog){'secure-field-catalog'}else{'secure-field'}
$taskReport=Join-Path $taskTarget "$taskName.json"
if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh Secure Field output directory'}
$taskIdentity=Join-Path $taskTarget 'app-identity'
New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskExe=Join-Path $taskApp "$HostName.exe"
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-$taskName --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(120000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill()}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone){throw 'Own Secure Field probe exceeded its watchdog'}
if($taskProcess.ExitCode -ne 0){throw "Secure Field probe failed: $taskReport"}
node (Join-Path $PSScriptRoot "verify-$taskName.mjs") --native $taskTarget --host-assembly "$HostName.dll" --output (Join-Path $taskTarget "$taskName-audit.json")
if($LASTEXITCODE -ne 0){throw 'Secure Field audit failed'}
