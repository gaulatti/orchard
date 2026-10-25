param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Reference,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path;$taskReference=(Resolve-Path -LiteralPath $Reference).Path;$taskTarget=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskTarget){throw 'Use a fresh selected-fill directory'}
if(@(Get-CimInstance Win32_Process|Where-Object{($_.Name -in @('MSBuild.exe','XamlCompiler.exe') -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match '(?i)(?:^|\s)(?:build|msbuild|publish)(?:\s|$)')) -and $_.CommandLine -match '(?i)(Orchard|PackageConsumer)'}).Count){throw 'Wait for builds before selected-fill probe'}
New-Item -ItemType Directory -Path $taskTarget|Out-Null;$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri',"$HostName.exe")){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
Copy-Item -LiteralPath $taskReference -Destination (Join-Path $taskTarget 'surface-reference.json')
$taskReport=Join-Path $taskTarget 'surface.json';$taskExe=Join-Path $taskApp "$HostName.exe";$taskCopiedReference=Join-Path $taskTarget 'surface-reference.json'
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-command-spotlight-surface --surface-reference `"$taskCopiedReference`" --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o');foregroundAuthorized=$false;hardwareInputSent=$false}|ConvertTo-Json|Set-Content (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(600000)
if(-not$taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID; refusing termination'};$taskProcess.Kill();$taskProcess.WaitForExit(10000)|Out-Null}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not$taskDone);finishedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content (Join-Path $taskTarget 'process-exit.json')
if(-not$taskDone -or $taskProcess.ExitCode -ne 0){throw 'Native selected-fill probe failed; inspect retained stage/report'}
node (Join-Path $PSScriptRoot 'verify-command-spotlight-surface.mjs') $taskTarget "$HostName.dll" (Join-Path $taskTarget 'surface-audit.json')
if($LASTEXITCODE -ne 0){throw 'Independent selected-fill audit failed'}
