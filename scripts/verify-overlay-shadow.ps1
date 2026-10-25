param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Reference,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path;$taskReference=(Resolve-Path -LiteralPath $Reference).Path;$taskTarget=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskTarget){throw 'Use a fresh overlay-shadow directory'}
if(@(Get-CimInstance Win32_Process|Where-Object{($_.Name -in @('MSBuild.exe','XamlCompiler.exe') -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match '(?i)(?:^|\s)(?:build|msbuild|publish)(?:\s|$)')) -and $_.CommandLine -match '(?i)(Orchard|PackageConsumer)'}).Count){throw 'Wait for builds before overlay-shadow probe'}
New-Item -ItemType Directory -Path $taskTarget|Out-Null;$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri',"$HostName.exe")){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
Copy-Item -LiteralPath $taskReference -Destination (Join-Path $taskTarget 'shadow-reference.json')
$taskImages=Join-Path $taskTarget 'shadow-reference';New-Item -ItemType Directory -Path $taskImages|Out-Null
$taskInputs=Get-Content -LiteralPath $taskReference -Raw|ConvertFrom-Json
foreach($taskCase in $taskInputs.cases){if([IO.Path]::GetFileName($taskCase.file) -ne $taskCase.file){throw 'Invalid shadow reference filename'};Copy-Item -LiteralPath (Join-Path (Join-Path (Split-Path -Parent $taskReference) 'OverlayShadow') $taskCase.file) -Destination $taskImages}
$taskPaintReference=Join-Path $PSScriptRoot '../tests/Reference/OverlayShadowPaint.json';if((Get-FileHash -LiteralPath $taskPaintReference -Algorithm SHA256).Hash.ToLowerInvariant() -ne 'dfa5066ed00c02ebc22cd13bc9a8764143dc6e53b100926d741536be294e4db9'){throw 'Shadow paint reference differs'};Copy-Item -LiteralPath $taskPaintReference -Destination (Join-Path $taskTarget 'shadow-paint-reference.json');$taskPaintImages=Join-Path $taskTarget 'shadow-paint-reference';New-Item -ItemType Directory -Path $taskPaintImages|Out-Null;foreach($taskCase in (Get-Content -LiteralPath $taskPaintReference -Raw|ConvertFrom-Json).cases){if([IO.Path]::GetFileName($taskCase.file) -ne $taskCase.file){throw 'Invalid shadow paint filename'};Copy-Item -LiteralPath (Join-Path (Join-Path (Split-Path -Parent $taskPaintReference) 'OverlayShadowPaint') $taskCase.file) -Destination $taskPaintImages}
$taskReport=Join-Path $taskTarget 'shadow.json';$taskExe=Join-Path $taskApp "$HostName.exe";$taskCopiedReference=Join-Path $taskTarget 'shadow-reference.json'
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-overlay-shadow --shadow-reference `"$taskCopiedReference`" --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o');foregroundAuthorized=$false;hardwareInputSent=$false}|ConvertTo-Json|Set-Content (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(600000)
if(-not$taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID; refusing termination'};$taskProcess.Kill();$taskProcess.WaitForExit(10000)|Out-Null}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not$taskDone);finishedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content (Join-Path $taskTarget 'process-exit.json')
if(-not$taskDone -or $taskProcess.ExitCode -ne 0){throw 'Native overlay-shadow probe failed; inspect retained stage/report'}
node (Join-Path $PSScriptRoot 'verify-overlay-shadow.mjs') $taskTarget "$HostName.dll" (Join-Path $taskTarget 'shadow-audit.json')
if($LASTEXITCODE -ne 0){throw 'Independent overlay-shadow audit failed'}
