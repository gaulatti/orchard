param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
if(@(Get-CimInstance Win32_Process|Where-Object{($_.Name -in @('MSBuild.exe','XamlCompiler.exe') -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match '(?i)(?:^|\s)(?:build|msbuild)(?:\s|$)')) -and $_.CommandLine -match '(?i)(Orchard|PackageConsumer)'}).Count){throw 'Wait for active builds before probing Media Library'}
$taskTarget=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskTarget){throw 'Use a fresh Media Library output directory'}
New-Item -ItemType Directory -Path $taskTarget|Out-Null
$taskReport=Join-Path $taskTarget 'media-library.json'
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri',"$HostName.exe")){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskReference=Join-Path $PSScriptRoot '../tests/Reference/MediaLibrary.json'
if((Get-FileHash -Algorithm SHA256 -LiteralPath $taskReference).Hash.ToLower() -ne '11cbfaa6488702b925f04e498647c59dccce1a3229366c1ed530cd34f56257c5'){throw 'Media Library source reference differs'}
Copy-Item -LiteralPath $taskReference -Destination (Join-Path $taskTarget 'source-reference.json')
foreach($taskAsset in (Get-Content -LiteralPath $taskReference -Raw|ConvertFrom-Json).assets){Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('../tests/Reference/MediaLibrary/'+$taskAsset.file)) -Destination $taskTarget}
$taskExe=Join-Path $taskApp "$HostName.exe"
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-media-library --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o');hardwareInputSent=$false;foregroundAuthorized=$false;systemClipboardAuthorized=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(300000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill();$taskProcess.WaitForExit(10000)|Out-Null}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone);finishedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw 'Media Library native probe failed; inspect retained report/stage'}
node (Join-Path $PSScriptRoot 'verify-media-library.mjs') --native $taskTarget --host-assembly "$HostName.dll" --output (Join-Path $taskTarget 'media-library-audit.json')
if($LASTEXITCODE -ne 0){throw 'Media Library independent audit failed'}
