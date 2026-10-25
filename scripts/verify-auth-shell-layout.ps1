param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog',[switch]$ContractsOnly)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
if(@(Get-CimInstance Win32_Process|Where-Object{($_.Name -in @('MSBuild.exe','XamlCompiler.exe') -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match '(?i)(?:^|\s)(?:build|msbuild)(?:\s|$)')) -and $_.CommandLine -match '(?i)(Orchard|PackageConsumer)'}).Count){throw 'Wait for active builds before probing AuthShell'}
$taskTarget=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskTarget){throw 'Use a fresh AuthShell output directory'}
New-Item -ItemType Directory -Path $taskTarget|Out-Null
$taskReport=Join-Path $taskTarget 'auth-shell-layout.json'
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri',"$HostName.exe")){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskReference=Join-Path $PSScriptRoot '../tests/Reference/AuthShellLayout.json'
if((Get-FileHash -Algorithm SHA256 -LiteralPath $taskReference).Hash.ToLower() -ne '33e1c67660f4ff29b82372b3e4e696b2f3bd8b16061b3aa57c05ca0abcfe8cbd'){throw 'AuthShell source reference differs'}
Copy-Item -LiteralPath $taskReference -Destination (Join-Path $taskTarget 'source-reference.json')
$taskExe=Join-Path $taskApp "$HostName.exe"
$taskDiagnostic=if($ContractsOnly){' --auth-shell-layout-contracts-only'}else{''}
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-auth-shell-layout --verify-output `"$taskReport`"$taskDiagnostic" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o');hardwareInputSent=$false;foregroundAuthorized=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$taskProcess.WaitForExit(300000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill();$taskProcess.WaitForExit(10000)|Out-Null}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone);finishedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw 'AuthShell native probe failed; inspect retained report/stage'}
if($ContractsOnly){Write-Output 'Contract-only diagnostic completed; full AuthShell acceptance not verified';return}
node (Join-Path $PSScriptRoot 'verify-auth-shell-layout.mjs') $taskTarget "$HostName.dll" (Join-Path $taskTarget 'auth-shell-layout-audit.json')
if($LASTEXITCODE -ne 0){throw 'AuthShell independent audit failed'}
