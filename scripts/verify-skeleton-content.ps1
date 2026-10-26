param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog',[string]$Reference='tests/Reference/Skeleton.json',[string]$ImmutableManifest)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskTarget=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskTarget){throw 'Use a fresh Skeleton output directory'}
$taskBuilds=@(Get-CimInstance Win32_Process|Where-Object{$_.Name -in @('dotnet.exe','MSBuild.exe','XamlCompiler.exe') -and $_.CommandLine -match '(?i)(build|publish|restore|msbuild|xamlcompiler)' -and $_.CommandLine -match '(?i)Orchard'})
if($taskBuilds.Count){throw 'Wait for the Orchard build before native qualification'}
$taskCommands=[Convert]::ToHexString([IO.File]::ReadAllBytes((Join-Path $taskApp "$HostName.dll")))
if(-not $taskCommands.Contains([Convert]::ToHexString([Text.Encoding]::Unicode.GetBytes('--probe-hidden-skeleton-content')))){throw 'Host does not recognize the hidden Skeleton command'}
$taskManifest=if($ImmutableManifest){Get-Content -LiteralPath $ImmutableManifest -Raw|ConvertFrom-Json}else{$null}
if($taskManifest -and (-not $taskManifest.allFilesVerified -or $taskManifest.mutableOutputHardlinked)){throw 'Immutable app manifest is invalid'}
New-Item -ItemType Directory -Path $taskTarget|Out-Null
Copy-Item -LiteralPath $Reference -Destination (Join-Path $taskTarget 'source-reference.json')
if((Get-FileHash -LiteralPath $Reference).Hash -ne (Get-FileHash -LiteralPath (Join-Path $taskTarget 'source-reference.json')).Hash){throw 'Reference copy changed'}
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity|Out-Null
foreach($taskName in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri')){
 $taskFrom=Join-Path $taskApp $taskName
 if($taskManifest){
  $taskFrozenRoot=[IO.Path]::GetFullPath('artifacts/current')+[IO.Path]::DirectorySeparatorChar
  if(-not $taskApp.StartsWith($taskFrozenRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Identity links require an immutable app under artifacts/current'}
  $taskEntry=@($taskManifest.files|Where-Object file -eq $taskName)
  if($taskEntry.Count -ne 1 -or (Get-FileHash -LiteralPath $taskFrom).Hash.ToLowerInvariant() -ne $taskEntry[0].sha256){throw 'App identity differs from manifest'}
  New-Item -ItemType HardLink -Path (Join-Path $taskIdentity $taskName) -Target $taskFrom|Out-Null
 }else{Copy-Item -LiteralPath $taskFrom -Destination $taskIdentity}
}
$taskReport=Join-Path $taskTarget 'skeleton-content.json'
$taskProcess=Start-Process -FilePath (Join-Path $taskApp "$HostName.exe") -ArgumentList "--probe-hidden-skeleton-content --verify-output `"$taskReport`"" -WindowStyle Hidden -RedirectStandardError (Join-Path $taskTarget 'stderr.txt') -RedirectStandardOutput (Join-Path $taskTarget 'stdout.txt') -PassThru
@{processId=$taskProcess.Id;startedAt=[DateTime]::UtcNow.ToString('o');hidden=$true;executable=(Join-Path $taskApp "$HostName.exe")}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
while(-not $taskProcess.WaitForExit(10000)){}
@{exited=$true;exitCode=$taskProcess.ExitCode;timedOut=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if($taskProcess.ExitCode -ne 0){throw ('Skeleton native check failed; inspect '+$taskReport)}
node (Join-Path $PSScriptRoot 'verify-skeleton-content.mjs') --native $taskTarget --reference $Reference --app $taskApp --host-assembly "$HostName.dll" --output (Join-Path $taskTarget 'audit.json')
if($LASTEXITCODE -ne 0){throw 'Skeleton native audit failed'}
node (Join-Path $PSScriptRoot 'test-skeleton-content-audit.mjs') --report $taskReport --reference $Reference --output (Join-Path $taskTarget 'negative-audit.json')
if($LASTEXITCODE -ne 0){throw 'Skeleton negative audit failed'}
exit 0
