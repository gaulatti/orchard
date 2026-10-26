param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskOutput=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOutput){throw 'Use a fresh keycap output directory'}
$taskBuilds=@(Get-CimInstance Win32_Process|Where-Object{$_.Name -in @('dotnet.exe','MSBuild.exe','XamlCompiler.exe') -and $_.CommandLine -match '(?i)(build|publish|restore|msbuild|xamlcompiler)' -and $_.CommandLine -match '(?i)Orchard'})
if($taskBuilds.Count){throw 'Wait for the Orchard build before native qualification'}
$taskCommands=[Convert]::ToHexString([IO.File]::ReadAllBytes((Join-Path $taskApp "$HostName.dll")))
if(-not $taskCommands.Contains([Convert]::ToHexString([Text.Encoding]::Unicode.GetBytes('--probe-hidden-kbd-content')))){throw 'Host does not recognize the hidden keycap command'}
New-Item -ItemType Directory -Path $taskOutput|Out-Null
$taskReport=Join-Path $taskOutput 'kbd-content.json'
$taskProcess=Start-Process -FilePath (Join-Path $taskApp "$HostName.exe") -ArgumentList "--probe-hidden-kbd-content --verify-output `"$taskReport`"" -WindowStyle Hidden -RedirectStandardError (Join-Path $taskOutput 'stderr.txt') -RedirectStandardOutput (Join-Path $taskOutput 'stdout.txt') -PassThru
@{processId=$taskProcess.Id;startedAt=[DateTime]::UtcNow.ToString('o');hidden=$true;executable=(Join-Path $taskApp "$HostName.exe")}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process.json')
while(-not $taskProcess.WaitForExit(10000)){}
@{exited=$true;exitCode=$taskProcess.ExitCode;timedOut=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process-exit.json')
$taskResult=Get-Content -LiteralPath $taskReport -Raw|ConvertFrom-Json
if($taskProcess.ExitCode -ne 0 -or -not $taskResult.passed -or $taskResult.states.Count -ne 64 -or $taskResult.contracts.Count -ne 16 -or $taskResult.focus -ne 0 -or $taskResult.activations -ne 0){throw ('Keycap content verification failed: '+$taskResult.error)}
foreach($taskState in $taskResult.states){if(-not $taskState.passed -or (Get-FileHash -LiteralPath (Join-Path $taskOutput $taskState.capture)).Hash.ToLowerInvariant() -ne $taskState.captureSha256){throw 'Keycap capture or state changed'}}
if($taskResult.scaleFrames.Count -ne 8 -or $taskResult.metadataContracts.Count -ne 64 -or -not $taskResult.isolatedRasterizationScaleVerified){throw 'Keycap scaling or metadata coverage is missing'}
$taskIndex=0
foreach($taskScheme in @('Light','Dark')){foreach($taskScale in @(1,1.25,1.5,2)){
 $taskFrame=$taskResult.scaleFrames[$taskIndex++];$taskPng=Join-Path $taskOutput $taskFrame.capture
 if($taskFrame.scheme -ne $taskScheme -or $taskFrame.requestedScale -ne $taskScale -or $taskFrame.actualRasterizationScale -ne $taskScale -or -not $taskFrame.matches -or -not $taskFrame.retained -or $taskFrame.singleHeight -ne 20 -or $taskFrame.combinationHeight -ne 20 -or $taskFrame.singleName -ne 'Esc' -or $taskFrame.combinationName -ne 'Ctrl+K' -or (Get-FileHash -LiteralPath $taskPng).Hash.ToLowerInvariant() -ne $taskFrame.captureSha256){throw 'Keycap native scale, geometry, metadata or capture differs'}
 $taskBytes=[IO.File]::ReadAllBytes($taskPng);$taskWidth=[BitConverter]::ToInt32([byte[]]$taskBytes[19..16],0);$taskHeight=[BitConverter]::ToInt32([byte[]]$taskBytes[23..20],0)
 if($taskWidth -ne 180*$taskScale -or $taskHeight -ne 160*$taskScale -or $taskFrame.width -ne $taskWidth -or $taskFrame.height -ne $taskHeight){throw 'Keycap scale capture has wrong physical dimensions'}
}}
$taskIds=@('default','local-empty','empty-after-content-update','bound-empty','bound-name','bound-empty-again','clear-restores-default','basedon-empty','local-over-style','clear-reveals-style','ancestor-implicit-empty','remove-style-restores-default','empty-label','caller-label','remove-label-restores-default','template-remount-empty')
$taskIndex=0
foreach($taskScheme in @('Light','Dark')){foreach($taskMode in @('single','combination')){foreach($taskId in $taskIds){
 $taskCase=$taskResult.metadataContracts[$taskIndex++];$taskExpected=switch($taskId){'default'{if($taskMode -eq 'single'){'Esc'}else{'Ctrl+K'}} 'bound-name'{'Caller shortcut'} 'local-over-style'{'Local shortcut'} 'caller-label'{'Caller shortcut label'} {$_ -in @('clear-restores-default','remove-style-restores-default','remove-label-restores-default')}{if($taskMode -eq 'single'){'Updated'}else{'Alt+K'}} default{''}}
 if($taskCase.scheme -ne $taskScheme -or $taskCase.mode -ne $taskMode -or $taskCase.id -ne $taskId -or $taskCase.actual -cne $taskExpected -or $taskCase.expected -cne $taskExpected -or -not $taskCase.retained -or -not $taskCase.passed){throw 'Keycap caller metadata precedence differs'}
}}}
$taskCatalogCount=if($HostName -eq 'Orchard.Catalog'){2}else{0}
if($taskResult.catalogs.Count -ne $taskCatalogCount -or @($taskResult.catalogs|Where-Object {-not $_.passed -or -not $_.callerEmptyName}).Count){throw 'Registered keycap metadata demo differs'}
if((Get-FileHash -LiteralPath (Join-Path $taskApp 'Orchard.dll')).Hash.ToLowerInvariant() -ne $taskResult.librarySha256 -or (Get-FileHash -LiteralPath (Join-Path $taskApp "$HostName.dll")).Hash.ToLowerInvariant() -ne $taskResult.hostSha256){throw 'Keycap runtime identity changed'}
@{passed=$true;states=64;callerContracts=16;scaleFrames=8;metadataContracts=64;registeredCatalogThemes=$taskCatalogCount;isolatedRasterizationScaleVerified=$true;guards=$taskResult.guards;focus=0;activations=0;librarySha256=$taskResult.librarySha256;hostSha256=$taskResult.hostSha256;reportSha256=(Get-FileHash -LiteralPath $taskReport).Hash.ToLowerInvariant();nativeARM64RuntimeVerified=$false;fullComponentAcceptance=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'audit.json')
Write-Output 'Keycap content runtime passed:64 alignment states,16 caller contracts,8 native scale frames,64 metadata contracts,zero focus/activation'
exit 0

