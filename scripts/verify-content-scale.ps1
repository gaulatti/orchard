param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskOutput=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOutput){throw 'Use a fresh Content scale output directory'}
$taskBuilds=@(Get-CimInstance Win32_Process|Where-Object{$_.Name -in @('dotnet.exe','MSBuild.exe','XamlCompiler.exe') -and $_.CommandLine -match '(?i)(build|publish|restore|msbuild|xamlcompiler)' -and $_.CommandLine -match '(?i)Orchard'})
if($taskBuilds.Count){throw 'Wait for the Orchard SDK process before native qualification'}
$taskCommands=[Convert]::ToHexString([IO.File]::ReadAllBytes((Join-Path $taskApp "$HostName.dll")))
if(-not $taskCommands.Contains([Convert]::ToHexString([Text.Encoding]::Unicode.GetBytes('--probe-hidden-content-scale')))){throw 'Host does not recognize the hidden Content scale command'}
New-Item -ItemType Directory -Path $taskOutput|Out-Null
$taskIdentity=Join-Path $taskOutput 'app-identity';New-Item -ItemType Directory -Path $taskIdentity|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll")){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination (Join-Path $taskIdentity $taskFile)}
$taskReport=Join-Path $taskOutput 'content-scale.json'
$taskProcess=Start-Process -FilePath (Join-Path $taskApp "$HostName.exe") -ArgumentList "--probe-hidden-content-scale --verify-output `"$taskReport`"" -WindowStyle Hidden -RedirectStandardError (Join-Path $taskOutput 'stderr.txt') -RedirectStandardOutput (Join-Path $taskOutput 'stdout.txt') -PassThru
@{processId=$taskProcess.Id;startedAt=[DateTime]::UtcNow.ToString('o');hidden=$true;executable=(Join-Path $taskApp "$HostName.exe");hardwareInputSent=$false;osSettingsChanged=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process.json')
while(-not $taskProcess.WaitForExit(10000)){}
@{exited=$true;exitCode=$taskProcess.ExitCode;timedOut=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process-exit.json')
if($taskProcess.ExitCode -ne 0){throw 'Content scale native probe failed; inspect retained report'}
node (Join-Path $PSScriptRoot 'verify-content-scale.mjs') $taskOutput "$HostName.dll" (Join-Path $taskOutput 'audit.json')
if($LASTEXITCODE -ne 0){throw 'Content scale native report or paint audit failed'}
