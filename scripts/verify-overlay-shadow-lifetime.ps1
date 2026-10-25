param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog',
    [ValidateSet('default','near','far','combined','near-rectangle','combined-rectangle','far-empty','combined-empty')][string]$Mode='default',
    [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskOutput=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOutput){throw 'Use a fresh lifetime probe output'}
$taskBuilds=@(Get-CimInstance Win32_Process | Where-Object { $_.Name -in @('dotnet.exe','MSBuild.exe','csc.exe','XamlCompiler.exe') -and $_.CommandLine -match 'Orchard|PackageConsumer' })
if($taskBuilds.Count){throw 'A relevant build is still running'}
New-Item -ItemType Directory -Path $taskOutput|Out-Null
$taskIdentity=Join-Path $taskOutput 'app-identity';New-Item -ItemType Directory -Path $taskIdentity|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll","$HostName.exe",'Microsoft.UI.Xaml.dll','resources.pri')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskReport=Join-Path $taskOutput 'lifetime.json'
$taskExe=Join-Path $taskApp "$HostName.exe"
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-overlay-lifetime --verify-output `"$taskReport`" --shadow-mode $Mode" -WindowStyle Hidden -PassThru
@{processId=$taskProcess.Id;executable=$taskExe;mode=$Mode;startedAt=[DateTime]::UtcNow.ToString('o');foregroundAuthorized=$false;hardwareInputSent=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process.json')
$taskDeadline=[DateTime]::UtcNow.AddSeconds(180)
while(-not $taskProcess.WaitForExit(1000) -and [DateTime]::UtcNow -lt $taskDeadline){}
$taskDone=$taskProcess.HasExited
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID; refusing termination'};$taskProcess.Kill();if(-not $taskProcess.WaitForExit(10000)){throw 'Owned lifetime probe did not terminate'}}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone);finishedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw 'Native lifetime probe failed; inspect retained stage/report'}
& node (Join-Path $PSScriptRoot 'verify-overlay-shadow-lifetime.mjs') $taskOutput $HostName (Join-Path $taskOutput 'lifetime-audit.json')
if($LASTEXITCODE -ne 0){throw 'Independent lifetime audit failed'}
