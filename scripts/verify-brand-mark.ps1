param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
if(@(Get-CimInstance Win32_Process|Where-Object{$_.Name -in @('dotnet.exe','MSBuild.exe','XamlCompiler.exe') -and $_.CommandLine -match '(?i)(build|msbuild|xamlcompiler)' -and $_.CommandLine -match '(?i)(Orchard|PackageConsumer)'}).Count){throw 'Wait for active builds before the brand mark probe'}
$taskTarget=[IO.Path]::GetFullPath($Output);New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
$taskReport=Join-Path $taskTarget 'brand-mark.json';if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh brand mark output directory'}
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/BrandMark.json') -Destination (Join-Path $taskTarget 'source-reference.json')
$taskExe=Join-Path $taskApp "$HostName.exe";$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-brand-mark --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(120000);if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill()}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw 'Brand mark native probe failed'}
node (Join-Path $PSScriptRoot 'verify-brand-mark.mjs') --native $taskTarget --host-assembly "$HostName.dll" --output (Join-Path $taskTarget 'brand-mark-audit.json')
if($LASTEXITCODE -ne 0){throw 'Brand mark native audit failed'}
