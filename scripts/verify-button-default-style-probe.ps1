param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Package,[Parameter(Mandatory)][string]$Output,[ValidateSet('X64','Arm64')][string]$ExpectedArchitecture='X64')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskPackage=(Resolve-Path -LiteralPath $Package).Path
$taskTarget=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
$taskReport=Join-Path $taskTarget 'button-default-style.json'
if(Test-Path -LiteralPath $taskReport){throw 'Use a fresh button default-style output directory.'}
$taskArchive=[IO.Compression.ZipFile]::OpenRead($taskPackage)
try{$taskEntries=@($taskArchive.Entries|Where-Object{$_.FullName -match '^lib/[^/]+/Orchard\.dll$'});if($taskEntries.Count -ne 1){throw 'Expected exactly one package library'};$taskStream=$taskEntries[0].Open();try{$taskLibraryHash=[Convert]::ToHexStringLower([Security.Cryptography.SHA256]::HashData($taskStream))}finally{$taskStream.Dispose()}}finally{$taskArchive.Dispose()}
if((Get-FileHash -LiteralPath (Join-Path $taskApp 'Orchard.dll')).Hash.ToLowerInvariant() -ne $taskLibraryHash){throw 'Button probe app differs from its package'}
@{package=$taskPackage;packageSha256=(Get-FileHash -LiteralPath $taskPackage).Hash.ToLowerInvariant();librarySha256=$taskLibraryHash}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'button-package.json')
$taskIdentity=Join-Path $taskTarget 'app-identity';New-Item -ItemType Directory -Path $taskIdentity -Force|Out-Null
foreach($taskFile in @('Orchard.dll','PackageConsumer.dll','resources.pri','PackageConsumer.deps.json')){Copy-Item -LiteralPath (Join-Path $taskApp $taskFile) -Destination $taskIdentity}
$taskExe=Join-Path $taskApp 'PackageConsumer.exe'
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList "--probe-hidden-button-default-style --verify-output `"$taskReport`"" -PassThru -WindowStyle Hidden
@{processId=$taskProcess.Id;executable=$taskExe;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'button-process.json')
$taskDone=$taskProcess.WaitForExit(60000)
if(-not $taskDone){if($taskProcess.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$taskProcess.Kill()}
@{exited=$taskDone;exitCode=$taskProcess.ExitCode;timedOut=(-not $taskDone)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskTarget 'button-process-exit.json')
if(-not $taskDone -or $taskProcess.ExitCode -ne 0){throw "Button default-style probe failed: $taskReport"}
node (Join-Path $PSScriptRoot 'verify-button-default-style-probe.mjs') --native $taskTarget --architecture $ExpectedArchitecture --output (Join-Path $taskTarget 'button-default-style-audit.json')
if($LASTEXITCODE -ne 0){throw 'Button default-style independent audit failed'}
