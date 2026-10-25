param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskOutput=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOutput){throw 'Use a fresh minimum-width evidence directory'}
if(@(Get-CimInstance Win32_Process|Where-Object{$_.Name -in @('MSBuild.exe','XamlCompiler.exe') -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match '(?i)(build|publish|restore)' -and $_.CommandLine -match '(?i)Orchard')}).Count){throw 'Wait for Orchard build or publish operations before the native fixture'}
New-Item -ItemType Directory -Path $taskOutput|Out-Null
$taskIdentity=Join-Path $taskOutput 'app-identity'
New-Item -ItemType Directory -Path $taskIdentity|Out-Null
$taskFiles=@(foreach($taskName in @('Orchard.dll',($HostName+'.dll'),'Microsoft.UI.Xaml.dll','resources.pri')){
    $taskSource=Join-Path $taskApp $taskName
    $taskSha=(Get-FileHash -LiteralPath $taskSource).Hash.ToLowerInvariant()
    Copy-Item -LiteralPath $taskSource -Destination $taskIdentity
    if((Get-FileHash -LiteralPath (Join-Path $taskIdentity $taskName)).Hash.ToLowerInvariant() -ne $taskSha){throw 'Evidence identity copy differs'}
    @{name=$taskName;sha256=$taskSha;mutableOutputHardlinked=$false}
})
$taskFiles|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskIdentity 'identity.json')
$taskReport=Join-Path $taskOutput 'table-minimum.json'
$taskProcess=Start-Process -FilePath (Join-Path $taskApp ($HostName+'.exe')) -ArgumentList ("--probe-hidden-table-minimum --verify-output `"$taskReport`"") -WindowStyle Hidden -PassThru
@{processId=$taskProcess.Id;startedAt=[DateTime]::UtcNow.ToString('o');foregroundAuthorized=$false;hardwareInputSent=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process.json')
# A short observation interval is not a deadline and never terminates the app.
while(-not $taskProcess.WaitForExit(10000)){}
@{exited=$true;exitCode=$taskProcess.ExitCode;timedOut=$false;observationExpired=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process-exit.json')
if($taskProcess.ExitCode -ne 0){throw ('Minimum-width fixture failed; inspect '+$taskReport)}
& node (Join-Path $PSScriptRoot 'verify-table-minimum.mjs') $taskOutput $HostName (Join-Path $taskOutput 'audit.json')
if($LASTEXITCODE -ne 0){throw 'Minimum-width audit failed'}
