param(
 [Parameter(Mandatory)][string]$AppDirectory,
 [Parameter(Mandatory)][ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName,
 [Parameter(Mandatory)][string]$Output,
 [string]$ImmutableManifest
)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path;$taskOut=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOut){throw 'Fresh label-raster evidence directory required'}
if(@(Get-CimInstance Win32_Process|Where-Object{$_.Name -in @('Orchard.Catalog.exe','PackageConsumer.exe','MSBuild.exe','XamlCompiler.exe') -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match '(?i)(build|restore|publish)' -and $_.CommandLine -match '(?i)Orchard')}).Count){throw 'Wait for owned SDK/native work before replay'}
New-Item -ItemType Directory -Path $taskOut|Out-Null
& compact.exe /C /I /Q $taskOut *> ($taskOut+'.compression.log');if($LASTEXITCODE -ne 0){throw 'Evidence compression setup failed'}
$taskIdentity=Join-Path $taskOut 'app-identity';New-Item -ItemType Directory -Path $taskIdentity|Out-Null
$taskImmutableFiles=@{}
if($ImmutableManifest){
 $taskArtifactRoot=(Resolve-Path -LiteralPath artifacts/current).Path.TrimEnd('\')+'\'
 if(-not $taskApp.StartsWith($taskArtifactRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Immutable identity linking requires an owned artifact runtime'}
 foreach($taskEntry in (Get-Content -LiteralPath $ImmutableManifest -Raw|ConvertFrom-Json).files){$taskImmutableFiles[$taskEntry.file]=$taskEntry.sha256}
}
$taskFiles=@(foreach($taskName in @('Orchard.dll',($HostName+'.dll'),'Microsoft.UI.Xaml.dll','resources.pri')){
 $taskFrom=Join-Path $taskApp $taskName;$taskSha=(Get-FileHash -LiteralPath $taskFrom).Hash.ToLowerInvariant()
 if($ImmutableManifest){
  if(-not $taskImmutableFiles.ContainsKey($taskName) -or $taskImmutableFiles[$taskName] -ne $taskSha){throw 'Immutable runtime manifest differs'}
  New-Item -ItemType HardLink -Path (Join-Path $taskIdentity $taskName) -Target $taskFrom|Out-Null
 }else{Copy-Item -LiteralPath $taskFrom -Destination (Join-Path $taskIdentity $taskName)}
 if((Get-FileHash -LiteralPath (Join-Path $taskIdentity $taskName)).Hash.ToLowerInvariant() -ne $taskSha){throw 'Evidence identity copy differs'}
 @{name=$taskName;sha256=$taskSha;mutableOutputHardlinked=$false}
})
$taskFiles|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskIdentity 'identity.json')
$taskReport=Join-Path $taskOut 'table-label-raster.json'
$taskNative=Start-Process -FilePath (Join-Path $taskApp ($HostName+'.exe')) -ArgumentList ("--probe-hidden-table-label-raster --verify-output `"$taskReport`"") -WindowStyle Hidden -PassThru
@{processId=$taskNative.Id;startedAt=[DateTime]::UtcNow.ToString('o');diagnosticOnly=$true;normalTimingRun=$false;foregroundAuthorized=$false;hardwareInputSent=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOut 'process.json')
while(-not $taskNative.WaitForExit(10000)){}
@{exited=$true;exitCode=$taskNative.ExitCode;timedOut=$false;diagnosticOnly=$true;normalTimingRun=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOut 'process-exit.json')
if($taskNative.ExitCode -ne 0){throw ('Native label-raster observation failed: '+$taskReport)}
& node scripts/verify-table-label-raster.mjs $taskOut $HostName (Join-Path $taskOut 'audit.json')
if($LASTEXITCODE -ne 0){throw 'Label-raster boundary audit failed; retain the report and images'}
