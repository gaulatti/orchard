param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
if(@(Get-CimInstance Win32_Process|Where-Object{$_.Name -in @('dotnet.exe','MSBuild.exe','XamlCompiler.exe') -and $_.CommandLine -match '(?i)(build|msbuild|xamlcompiler)' -and $_.CommandLine -match '(?i)(Orchard|PackageConsumer)'}).Count){throw 'Wait for active builds before the Navigation Menu probe'}
$taskTarget=[IO.Path]::GetFullPath($Output);New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
$taskReport=Join-Path $taskTarget 'navigation-menu.json';if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh Navigation Menu output directory'}
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/NavigationMenu.json') -Destination (Join-Path $taskTarget 'source-reference.json')
$taskExe=Join-Path $taskApp "$HostName.exe";$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-navigation-menu --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
while(-not $taskProcess.WaitForExit(10000)){Write-Output ('Navigation Menu probe remains live: '+$taskProcess.Id)}
$taskDone=$true
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw 'Navigation Menu native probe failed'}
node (Join-Path $PSScriptRoot 'verify-navigation-menu.mjs') --native $taskTarget --host-assembly "$HostName.dll" --output (Join-Path $taskTarget 'navigation-menu-audit.json')
if($LASTEXITCODE -ne 0){throw 'Navigation Menu native audit failed'}
