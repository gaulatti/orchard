param([Parameter(Mandatory)][string]$AppDirectory,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog',[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskOutput=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOutput){throw 'Use fresh Toggle Group evidence'}
if(@(Get-CimInstance Win32_Process|Where-Object{$_.Name -eq 'XamlCompiler.exe' -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match ' (?:build|publish|pack) ')}).Count){throw 'Wait for builds before native tests'}
New-Item -ItemType Directory -Path "$taskOutput/app-identity"|Out-Null
$taskIdentity=@()
foreach($taskName in @('Orchard.dll',"$HostName.dll","$HostName.exe",'resources.pri','Microsoft.UI.Xaml.dll')){Copy-Item -LiteralPath "$taskApp/$taskName" -Destination "$taskOutput/app-identity"; $taskIdentity+=@{name=$taskName;sha256=(Get-FileHash -LiteralPath "$taskOutput/app-identity/$taskName").Hash.ToLowerInvariant()}}
$taskProcess=Start-Process -FilePath "$taskApp/$HostName.exe" -ArgumentList "--probe-hidden-toggle-group --verify-output `"$taskOutput/toggle-group.json`"" -WindowStyle Hidden -PassThru
@{processId=$taskProcess.Id;executable="$taskApp/$HostName.exe";startedAt=[datetime]::UtcNow.ToString('o');foregroundAuthorized=$false;hardwareInputSent=$false}|ConvertTo-Json|Set-Content -LiteralPath "$taskOutput/process.json"
while(-not $taskProcess.WaitForExit(10000)){}
@{exited=$true;exitCode=$taskProcess.ExitCode;observationExpired=$false;finishedAt=[datetime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath "$taskOutput/process-exit.json"
foreach($taskEntry in $taskIdentity){$taskPath=Join-Path "$taskOutput/app-identity" $taskEntry.name; & compact.exe /C /I /Q $taskPath *> $null; if($LASTEXITCODE -ne 0 -or (Get-FileHash -LiteralPath $taskPath).Hash.ToLowerInvariant() -ne $taskEntry.sha256){throw 'Identity compression failed or changed bytes'}}
$taskIdentity|ConvertTo-Json|Set-Content -LiteralPath "$taskOutput/app-identity/identity.json"
if($taskProcess.ExitCode -ne 0){throw 'Native Toggle Group fixture failed; inspect retained report'}
node (Join-Path $PSScriptRoot 'verify-toggle-group.mjs') $taskOutput $HostName "$taskOutput/audit.json"
if($LASTEXITCODE -ne 0){throw 'Independent Toggle Group audit failed'}
