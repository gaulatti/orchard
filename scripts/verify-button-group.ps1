param([Parameter(Mandatory)][string]$AppDirectory,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog',[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskOutput=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOutput){throw 'Use fresh Button Group evidence'}
if(@(Get-CimInstance Win32_Process|Where-Object{$_.Name -eq 'XamlCompiler.exe' -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match ' (?:build|publish|pack) ')}).Count){throw 'Wait for builds before native tests'}
New-Item -ItemType Directory -Path "$taskOutput/app-identity"|Out-Null
$taskIdentity=@()
foreach($taskName in @('Orchard.dll',"$HostName.dll","$HostName.exe",'resources.pri','Microsoft.UI.Xaml.dll')){Copy-Item -LiteralPath "$taskApp/$taskName" -Destination "$taskOutput/app-identity"; $taskIdentity+=@{name=$taskName;sha256=(Get-FileHash -LiteralPath "$taskOutput/app-identity/$taskName").Hash.ToLowerInvariant()}}
$taskReport=Join-Path $taskOutput 'button-group.json'
$taskProcess=Start-Process -FilePath "$taskApp/$HostName.exe" -ArgumentList "--probe-hidden-button-group --verify-output `"$taskReport`"" -WindowStyle Hidden -PassThru
@{processId=$taskProcess.Id;executable="$taskApp/$HostName.exe";startedAt=[datetime]::UtcNow.ToString('o');foregroundAuthorized=$false;hardwareInputSent=$false}|ConvertTo-Json|Set-Content -LiteralPath "$taskOutput/process.json"
$taskExited=$taskProcess.WaitForExit(180000)
@{exited=$taskExited;exitCode=if($taskExited){$taskProcess.ExitCode}else{$null};observationExpired=(-not $taskExited);finishedAt=[datetime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath "$taskOutput/process-exit.json"
if(-not $taskExited){throw 'Observation expired; inspect the same owned process before follow-up'}
if($taskProcess.ExitCode -ne 0){throw 'Native Button Group fixture failed; inspect retained report'}
foreach($taskEntry in $taskIdentity){$taskPath=Join-Path "$taskOutput/app-identity" $taskEntry.name; & compact.exe /C /I /Q $taskPath *> $null; if($LASTEXITCODE -ne 0 -or (Get-FileHash -LiteralPath $taskPath).Hash.ToLowerInvariant() -ne $taskEntry.sha256){throw 'Identity compression failed or changed bytes'}}
$taskIdentity|ConvertTo-Json|Set-Content -LiteralPath "$taskOutput/app-identity/identity.json"
node (Join-Path $PSScriptRoot 'verify-button-group.mjs') $taskOutput $HostName "$taskOutput/audit.json"
if($LASTEXITCODE -ne 0){throw 'Independent Button Group audit failed'}
