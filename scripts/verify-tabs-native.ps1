param(
 [Parameter(Mandatory)][string]$AppDirectory,
 [Parameter(Mandatory)][string]$ImmutableManifest,
 [Parameter(Mandatory)][string]$Output,
 [ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='PackageConsumer'
)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskBoundary=[IO.Path]::GetFullPath('artifacts/current')+[IO.Path]::DirectorySeparatorChar
if(-not $taskApp.StartsWith($taskBoundary,[StringComparison]::OrdinalIgnoreCase)){throw 'Use a frozen artifact app, not SDK output'}
$taskManifest=Get-Content -LiteralPath $ImmutableManifest -Raw|ConvertFrom-Json
$taskOutput=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOutput){throw 'Use a fresh Tabs output directory'}
New-Item -ItemType Directory -Path $taskOutput|Out-Null
& compact.exe /C /I /Q $taskOutput *> ($taskOutput+'.compression.log')
if($LASTEXITCODE -ne 0){throw 'Could not mark output compressed'}
$taskIdentity=Join-Path $taskOutput 'app-identity'
New-Item -ItemType Directory -Path $taskIdentity|Out-Null
foreach($taskFile in @('Orchard.dll',"$HostName.dll",'resources.pri','Microsoft.UI.Xaml.dll',"$HostName.deps.json")){
 $taskFrom=Join-Path $taskApp $taskFile
 $taskEntry=@($taskManifest.files|Where-Object file -eq $taskFile)
 if($taskEntry.Count -ne 1 -or (Get-FileHash -LiteralPath $taskFrom).Hash.ToLowerInvariant() -ne $taskEntry[0].sha256){throw 'Frozen Tabs identity changed'}
 New-Item -ItemType HardLink -Path (Join-Path $taskIdentity $taskFile) -Target $taskFrom|Out-Null
}
$taskReference=(Resolve-Path -LiteralPath 'tests/Reference/Tabs.json').Path
$taskReport=Join-Path $taskOutput 'tabs-reference.json'
$taskExe=Join-Path $taskApp "$HostName.exe"
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-tabs-border `"$taskReference`" --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content (Join-Path $taskOutput 'process.json')
while(-not $taskProcess.WaitForExit(10000)){}
@{exited=$true;exitCode=$taskProcess.ExitCode;timedOut=$false}|ConvertTo-Json|Set-Content (Join-Path $taskOutput 'process-exit.json')
if($taskProcess.ExitCode -ne 0){throw 'Hidden Tabs capture failed; inspect its saved report'}
& node (Join-Path $PSScriptRoot 'verify-tabs.mjs') --native $taskOutput --source tests/Reference/Tabs --output (Join-Path $taskOutput 'metadata-audit.json')
if($LASTEXITCODE -ne 0){throw 'Tabs source geometry/brush audit failed'}
& node --input-type=module -e "import fs from 'node:fs/promises';import {auditTabsBorderPaint} from './scripts/verify-tabs-border-paint.mjs';const a=await auditTabsBorderPaint(process.argv[1],process.argv[2]);await fs.writeFile(process.argv[1]+'/border-paint-and-layering-audit.json',JSON.stringify(a,null,2)+'\n');console.log(JSON.stringify({nativeBackgroundLayeringVerified:a.nativeBackgroundLayeringVerified,nativeBackgroundLayeringMismatches:a.nativeBackgroundLayeringMismatches,strictSourceBorderPassed:a.passed,strictSourceBorderMismatches:a.mismatchingPixels}));if(!a.nativeBackgroundLayeringVerified)process.exitCode=1;" $taskOutput "$HostName.dll"
if($LASTEXITCODE -ne 0){throw 'Native Tabs background does not paint under its border; source-image diagnostics are recorded separately'}
