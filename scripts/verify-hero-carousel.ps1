param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
if(@(Get-CimInstance Win32_Process|Where-Object{($_.Name -in @('MSBuild.exe','XamlCompiler.exe') -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match '(?i)(?:^|\s)(?:build|msbuild)(?:\s|$)')) -and $_.CommandLine -match '(?i)(Orchard|PackageConsumer)'}).Count){throw 'Wait for active builds before the Hero Carousel probe'}
$taskTarget=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskTarget){throw 'Use a fresh Hero Carousel output directory'}
New-Item -ItemType Directory -Path $taskTarget|Out-Null
$taskReport=Join-Path $taskTarget 'hero-carousel.json'
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri',"$HostName.exe")){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskReference=Join-Path $PSScriptRoot '../tests/Reference/HeroCarousel.json'
if((Get-FileHash -Algorithm SHA256 -LiteralPath $taskReference).Hash.ToLower() -ne '1a16b5f6698507791b3f7a396eafe145a8432cc32f858b0654af698aca261227'){throw 'Hero Carousel frozen reference differs'}
Copy-Item -LiteralPath $taskReference -Destination (Join-Path $taskTarget 'source-reference.json')
$taskSpec=Get-Content -Raw -LiteralPath $taskReference|ConvertFrom-Json
foreach($taskAsset in $taskSpec.assets){if([IO.Path]::GetFileName($taskAsset.file) -ne $taskAsset.file){throw 'Unexpected image asset path'};$taskSource=Join-Path (Join-Path $PSScriptRoot '../tests/Reference/HeroCarousel') $taskAsset.file;if((Get-FileHash -Algorithm SHA256 -LiteralPath $taskSource).Hash.ToLower() -ne $taskAsset.sha256){throw 'Hero Carousel image asset differs'};Copy-Item -LiteralPath $taskSource -Destination $taskTarget}
$taskExe=Join-Path $taskApp "$HostName.exe"
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-hero-carousel --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o');hardwareInputSent=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(240000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill();$taskProcess.WaitForExit(10000)|Out-Null}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone);finishedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw 'Hero Carousel native probe failed; inspect retained report and stage'}
node (Join-Path $PSScriptRoot 'verify-hero-carousel.mjs') --native $taskTarget --host-assembly "$HostName.dll" --output (Join-Path $taskTarget 'hero-carousel-audit.json')
if($LASTEXITCODE -ne 0){throw 'Hero Carousel independent audit failed'}
