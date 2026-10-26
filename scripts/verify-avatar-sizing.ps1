param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output,[ValidateSet('Orchard.Catalog','PackageConsumer')][string]$HostName='Orchard.Catalog')
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskOutput=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskOutput){throw 'Use a fresh keycap output directory'}
$taskBuilds=@(Get-CimInstance Win32_Process|Where-Object{$_.Name -in @('dotnet.exe','MSBuild.exe','XamlCompiler.exe') -and $_.CommandLine -match '(?i)(build|publish|restore|msbuild|xamlcompiler)' -and $_.CommandLine -match '(?i)Orchard'})
if($taskBuilds.Count){throw 'Wait for the Orchard build before native qualification'}
$taskCommands=[Convert]::ToHexString([IO.File]::ReadAllBytes((Join-Path $taskApp "$HostName.dll")))
if(-not $taskCommands.Contains([Convert]::ToHexString([Text.Encoding]::Unicode.GetBytes('--probe-hidden-avatar-sizing')))){throw 'Host does not recognize the hidden avatar command'}
New-Item -ItemType Directory -Path $taskOutput|Out-Null
$taskReport=Join-Path $taskOutput 'avatar-sizing.json'
$taskProcess=Start-Process -FilePath (Join-Path $taskApp "$HostName.exe") -ArgumentList "--probe-hidden-avatar-sizing --verify-output `"$taskReport`"" -WindowStyle Hidden -RedirectStandardError (Join-Path $taskOutput 'stderr.txt') -RedirectStandardOutput (Join-Path $taskOutput 'stdout.txt') -PassThru
@{processId=$taskProcess.Id;startedAt=[DateTime]::UtcNow.ToString('o');hidden=$true;executable=(Join-Path $taskApp "$HostName.exe")}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process.json')
while(-not $taskProcess.WaitForExit(10000)){}
@{exited=$true;exitCode=$taskProcess.ExitCode;timedOut=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'process-exit.json')
$taskResult=Get-Content -LiteralPath $taskReport -Raw|ConvertFrom-Json
if($taskProcess.ExitCode -ne 0 -or -not $taskResult.passed -or $taskResult.states.Count -ne 280 -or $taskResult.contracts.Count -ne 32 -or $taskResult.focus -ne 0 -or $taskResult.activations -ne 0){throw ('Avatar sizing verification failed: '+$taskResult.error)}
foreach($taskState in $taskResult.states){if(-not $taskState.passed){throw 'Avatar state failed'};if($taskState.capture -and (Get-FileHash -LiteralPath (Join-Path $taskOutput $taskState.capture)).Hash.ToLowerInvariant() -ne $taskState.captureSha256){throw 'Avatar capture changed'}}
if((Get-FileHash -LiteralPath (Join-Path $taskApp 'Orchard.dll')).Hash.ToLowerInvariant() -ne $taskResult.librarySha256 -or (Get-FileHash -LiteralPath (Join-Path $taskApp "$HostName.dll")).Hash.ToLowerInvariant() -ne $taskResult.hostSha256){throw 'Avatar runtime identity changed'}
@{passed=$true;states=280;callerContracts=32;guards=$taskResult.guards;focus=0;activations=0;librarySha256=$taskResult.librarySha256;hostSha256=$taskResult.hostSha256;reportSha256=(Get-FileHash -LiteralPath $taskReport).Hash.ToLowerInvariant();nativeARM64RuntimeVerified=$false;fullComponentAcceptance=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskOutput 'audit.json')
Write-Output 'Avatar sizing runtime passed:280 states,32 caller/group contracts, zero focus/activation'
exit 0



