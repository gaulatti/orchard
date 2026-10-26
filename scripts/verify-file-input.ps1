param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
if(@(Get-CimInstance Win32_Process|Where-Object{($_.Name -in @('MSBuild.exe','XamlCompiler.exe') -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match '(?i)(?:^|\s)(?:build|msbuild)(?:\s|$)')) -and $_.CommandLine -match '(?i)(Orchard|PackageConsumer)'}).Count){throw 'Wait for active builds before probing File Input'}
$taskTarget=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskTarget){throw 'Use a fresh File Input output directory'}
New-Item -ItemType Directory -Path $taskTarget|Out-Null
$taskReport=Join-Path $taskTarget 'file-input.json'
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri',"$HostName.exe")){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskReference=Join-Path $PSScriptRoot '../tests/Reference/FileInput.json'
if((Get-FileHash -Algorithm SHA256 -LiteralPath $taskReference).Hash.ToLower() -ne 'bc824df8aef0b200cc74bc96a1aac183fe8e75e028f171f8c6abee633b2c7b8b'){throw 'File Input source reference differs'}
Copy-Item -LiteralPath $taskReference -Destination (Join-Path $taskTarget 'source-reference.json')
$taskExe=Join-Path $taskApp "$HostName.exe"
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-file-input --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o');hardwareInputSent=$false;systemPickerAuthorized=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
while(-not $taskProcess.WaitForExit(10000)){}
$taskDone=$true
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone);finishedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw 'File Input native probe failed; inspect retained report/stage'}
node (Join-Path $PSScriptRoot 'verify-file-input.mjs') --native $taskTarget --host-assembly "$HostName.dll" --output (Join-Path $taskTarget 'file-input-audit.json')
if($LASTEXITCODE -ne 0){throw 'File Input independent audit failed'}
