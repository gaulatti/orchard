param([Parameter(Mandatory)][string]$AppDirectory,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog',[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
if(@(Get-CimInstance Win32_Process|Where-Object{($_.Name -in @('MSBuild.exe','XamlCompiler.exe') -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match '(?i)(?:^|\s)(?:build|msbuild)(?:\s|$)')) -and $_.CommandLine -match '(?i)(Orchard|PackageConsumer)'}).Count){throw 'Wait for active builds before probing combined shadows'}
$taskOutput=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOutput){throw 'Use a fresh combined-shadow output'}
New-Item -ItemType Directory -Path $taskOutput|Out-Null
$taskIdentity=Join-Path $taskOutput 'app-identity';New-Item -ItemType Directory -Path $taskIdentity|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll","$HostName.exe",'Microsoft.UI.Xaml.dll','resources.pri')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskReference=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/OverlayCombined.json')).Path
$taskCopiedReference=Join-Path $taskOutput 'combined-reference.json';Copy-Item -LiteralPath $taskReference -Destination $taskCopiedReference
$taskReport=Join-Path $taskOutput 'combined.json';$taskExe=Join-Path $taskApp "$HostName.exe"
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-overlay-combined --verify-output `"$taskReport`" --combined-reference `"$taskCopiedReference`"" -WindowStyle Hidden -PassThru
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o');foregroundAuthorized=$false;hardwareInputSent=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process.json')
$taskDone=$taskProcess.WaitForExit(180000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID; refusing termination'};$taskProcess.Kill();if(-not $taskProcess.WaitForExit(10000)){throw 'Owned combined process did not terminate'}}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone);finishedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw 'Native combined-shadow fixture failed; inspect retained stage/report'}
& node (Join-Path $PSScriptRoot 'verify-overlay-combined.mjs') $taskOutput $HostName (Join-Path $taskOutput 'combined-audit.json')
if($LASTEXITCODE -ne 0){throw 'Independent native combined-shadow audit failed; exact raster gate is unchanged'}
