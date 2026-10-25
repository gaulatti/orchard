param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[string]$Reference='tests/Reference/AlertDialog.json',[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog',[string]$ImmutableManifest)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskManifest=$null
if($ImmutableManifest){
 $taskBoundary=[IO.Path]::GetFullPath('artifacts/current')+[IO.Path]::DirectorySeparatorChar
 if(-not $taskApp.StartsWith($taskBoundary,[StringComparison]::OrdinalIgnoreCase)){throw 'Immutable links require a frozen app under artifacts/current'}
 $taskManifest=Get-Content -LiteralPath $ImmutableManifest -Raw|ConvertFrom-Json
}
$taskTarget=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
& compact.exe /C /I /Q $taskTarget *> ($taskTarget+'.compression.log')
if($LASTEXITCODE -ne 0){throw 'Could not mark native output compressed'}
$taskReport=Join-Path $taskTarget 'alert-dialog-native.json'
if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh modal host probe output directory.'}
$taskReference=(Resolve-Path -LiteralPath $Reference).Path
$taskIdentity=Join-Path $taskTarget 'app-identity'
New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'resources.pri','Microsoft.UI.Xaml.dll',"$HostName.deps.json")){
 $taskFrom=Join-Path $taskApp $taskFile
 if($taskManifest){$taskEntry=@($taskManifest.files|Where-Object file -eq $taskFile);if($taskEntry.Count -ne 1 -or (Get-FileHash -LiteralPath $taskFrom).Hash.ToLowerInvariant() -ne $taskEntry[0].sha256){throw 'Frozen AlertDialog identity changed'};New-Item -ItemType HardLink -Path (Join-Path $taskIdentity $taskFile) -Target $taskFrom|Out-Null}else{Copy-Item -LiteralPath $taskFrom -Destination $taskIdentity}
}
$taskExe=Join-Path $taskApp "$HostName.exe"
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-alert-dialog `"$taskReference`" --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$false
while(-not $taskDone){$taskDone=$taskProcess.WaitForExit(10000)}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone){throw 'Own hidden modal host probe exceeded its watchdog.'}
if($taskProcess.ExitCode -ne 0){throw "Native modal host probe failed; see $taskReport"}
node (Join-Path $PSScriptRoot 'verify-alert-dialog-native.mjs') --native $taskTarget --reference $taskReference --host-assembly "$HostName.dll" --output (Join-Path $taskTarget 'native-audit.json')
if($LASTEXITCODE -ne 0){throw 'Independent AlertDialog source audit failed.'}

