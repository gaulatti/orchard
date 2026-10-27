param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog',[switch]$PagingOnly,[string]$ImmutableManifest,[string]$IdentityDirectory)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskIdentitySource=if($IdentityDirectory){(Resolve-Path -LiteralPath $IdentityDirectory).Path}else{$null}
if($taskIdentitySource -and $ImmutableManifest){throw 'Choose one immutable identity source'}
$taskManifest=$null
if($ImmutableManifest){
 $taskImmutableRoot=[IO.Path]::GetFullPath('artifacts/current')+[IO.Path]::DirectorySeparatorChar
 if(-not $taskApp.StartsWith($taskImmutableRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Immutable identity links require a frozen runtime under artifacts/current'}
 $taskManifest=Get-Content -LiteralPath $ImmutableManifest -Raw|ConvertFrom-Json
}
$taskBuilds=@(Get-CimInstance Win32_Process | Where-Object { $_.Name -in @('dotnet.exe','MSBuild.exe','XamlCompiler.exe') -and $_.CommandLine -match '(?i)(build|msbuild|xamlcompiler)' -and $_.CommandLine -match '(?i)(Orchard\.Catalog|Orchard\.sln|Orchard/Orchard\.csproj|PackageConsumer\.csproj)' })
if($taskBuilds.Count -gt 0){throw 'Wait for the Orchard build to exit before launching a Data Table benchmark'}
$taskHostCommands=[Convert]::ToHexString([IO.File]::ReadAllBytes((Join-Path $taskApp "$HostName.dll")))
if(-not $taskHostCommands.Contains([Convert]::ToHexString([Text.Encoding]::Unicode.GetBytes('--probe-hidden-data-table-performance')))){throw 'Built host does not recognize the hidden Data Table benchmark command'}
if($PagingOnly -and -not $taskHostCommands.Contains([Convert]::ToHexString([Text.Encoding]::Unicode.GetBytes('--paging-only')))){throw 'Built host does not support focused paging; rebuild it before using -PagingOnly'}
$taskHostCommands=$null
$taskTarget=[IO.Path]::GetFullPath($Output);New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
& compact.exe /C /I /Q $taskTarget *> ($taskTarget+'.compression.log')
if($LASTEXITCODE -ne 0){throw 'Could not mark benchmark output compressed'}
$taskReport=Join-Path $taskTarget 'data-table-performance.json';if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh Data Table performance output directory'}
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
$taskIdentityHashes=@{}
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'Microsoft.UI.Xaml.dll','resources.pri')){
 $taskFrom=Join-Path $taskApp $taskFile
 $taskHash=(Get-FileHash -LiteralPath $taskFrom -Algorithm SHA256).Hash.ToLowerInvariant()
 if($taskManifest){
  $taskEntry=@($taskManifest.files|Where-Object file -eq $taskFile)
  if($taskEntry.Count -ne 1 -or $taskHash -ne $taskEntry[0].sha256){throw 'Frozen benchmark identity differs from its manifest'}
 }
 $taskIdentityHashes[$taskFile]=$taskHash
 if($taskIdentitySource){
  $taskFrozen=Join-Path $taskIdentitySource $taskFile
  if((Get-FileHash -LiteralPath $taskFrozen -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskHash){throw "Saved benchmark identity differs from the app: $taskFile"}
  New-Item -ItemType HardLink -Path (Join-Path $taskIdentity $taskFile) -Target $taskFrozen|Out-Null
 }elseif($taskManifest){New-Item -ItemType HardLink -Path (Join-Path $taskIdentity $taskFile) -Target $taskFrom|Out-Null}
 else{Copy-Item -LiteralPath $taskFrom -Destination $taskIdentity}
}
$taskExe=Join-Path $taskApp "$HostName.exe"
$taskScope=if($PagingOnly){' --paging-only'}else{''}
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-data-table-performance$taskScope --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process.json')
$taskDone=$false
while(-not $taskDone){$taskDone=$taskProcess.WaitForExit(10000)}
@{exited=$taskDone;exitCode=if($taskDone){$taskProcess.ExitCode}else{$null};timedOut=$false;observationExpired=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'process-exit.json')
if(-not $taskDone){throw 'Observation expired; inspect the same owned benchmark process before follow-up'}
if($taskProcess.ExitCode -ne 0){throw "Data Table benchmark failed: $taskReport"}
foreach($taskFile in $taskIdentityHashes.Keys){
 $taskHash=(Get-FileHash -LiteralPath (Join-Path $taskApp $taskFile) -Algorithm SHA256).Hash.ToLowerInvariant()
 if($taskHash -ne $taskIdentityHashes[$taskFile]){throw "Data Table benchmark host changed during measurement: $taskFile"}
 if($taskIdentitySource -and (Get-FileHash -LiteralPath (Join-Path $taskIdentitySource $taskFile) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskHash){throw "Saved benchmark identity changed during measurement: $taskFile"}
}
$taskAuditScope=if($PagingOnly){'paging'}else{'full'}
node (Join-Path $PSScriptRoot 'verify-data-table-performance.mjs') --native $taskTarget --host-assembly "$HostName.dll" --output (Join-Path $taskTarget 'data-table-performance-audit.json') --scope $taskAuditScope
if($LASTEXITCODE -ne 0){throw 'Data Table benchmark audit failed'}
