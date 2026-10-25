param(
 [Parameter(Mandatory)][string]$AppDirectory,
 [Parameter(Mandatory)][string]$ImmutableManifest,
 [Parameter(Mandatory)][ValidateSet('PackageConsumer','Orchard.Catalog')][string]$HostName,
 [Parameter(Mandatory)][string]$Output,
 [switch]$ExpectFailure,
 [switch]$ButtonContent
)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskProbe=if($ButtonContent){'--probe-hidden-table-button-content-foreground'}else{'--probe-hidden-table-navigation-foreground'}
if($ButtonContent -and -not [Convert]::ToHexString([IO.File]::ReadAllBytes((Join-Path $taskApp ($HostName+'.dll')))).Contains([Convert]::ToHexString([Text.Encoding]::Unicode.GetBytes($taskProbe)))){throw 'Built host does not recognize the button-content foreground probe'}
$taskImmutableRoot=[IO.Path]::GetFullPath('artifacts/current')+[IO.Path]::DirectorySeparatorChar
if(-not $taskApp.StartsWith($taskImmutableRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Use a frozen runtime under artifacts/current, never mutable SDK output'}
$taskManifest=Get-Content -LiteralPath $ImmutableManifest -Raw|ConvertFrom-Json
$taskBuilds=@(Get-CimInstance Win32_Process|Where-Object {$_.Name -in @('MSBuild.exe','XamlCompiler.exe','dotnet.exe') -and $_.CommandLine -match '(?i)(build|msbuild|xamlcompiler)' -and $_.CommandLine -match '(?i)Orchard'})
if($taskBuilds.Count){throw 'Wait for the owned SDK build before native tests'}
$taskFolder=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskFolder){throw 'Use a fresh navigation foreground output'}
New-Item -ItemType Directory -Path $taskFolder|Out-Null
& compact.exe /C /I /Q $taskFolder *> ($taskFolder+'.compression.log')
if($LASTEXITCODE -ne 0){throw 'Could not mark native output compressed'}
$taskIdentity=Join-Path $taskFolder app-identity;New-Item -ItemType Directory -Path $taskIdentity|Out-Null
$taskIdentities=@()
foreach($taskName in @('Orchard.dll',($HostName+'.dll'),'Microsoft.UI.Xaml.dll','resources.pri')){
 $taskEntry=@($taskManifest.files|Where-Object {$_.file -eq $taskName});if($taskEntry.Count -ne 1){throw 'Frozen identity entry absent or duplicated'}
 $taskFrom=Join-Path $taskApp $taskName;$taskHash=(Get-FileHash -LiteralPath $taskFrom -Algorithm SHA256).Hash.ToLowerInvariant()
 if($taskHash -ne $taskEntry[0].sha256){throw ('Frozen identity changed: '+$taskName)}
 New-Item -ItemType HardLink -Path (Join-Path $taskIdentity $taskName) -Target $taskFrom|Out-Null
 $taskIdentities+=@{file=$taskName;sha256=$taskHash;immutableSource=$taskFrom}
}
$taskIdentities|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskIdentity identity.json)
$taskReport=Join-Path $taskFolder $(if($ButtonContent){'table-button-content-foreground.json'}else{'table-navigation-foreground.json'})
$taskProcess=Start-Process -FilePath (Join-Path $taskApp ($HostName+'.exe')) -ArgumentList "$taskProbe --verify-output `"$taskReport`"" -WindowStyle Hidden -PassThru
@{processId=$taskProcess.Id;startedAt=[DateTime]::UtcNow.ToString('o');hidden=$true;hardwareInputSent=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskFolder process.json)
while(-not $taskProcess.WaitForExit(10000)){}
@{exited=$true;exitCode=$taskProcess.ExitCode;timedOut=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskFolder process-exit.json)
$taskNative=Get-Content -LiteralPath $taskReport -Raw|ConvertFrom-Json
if(-not $taskNative.executionCompleted){throw ('Native fixture did not finish: '+$taskNative.stage+' '+$taskNative.error)}
$taskExpectedExit=if($ExpectFailure){1}else{0}
if($taskProcess.ExitCode -ne $taskExpectedExit){throw ('Unexpected native exit: '+$taskProcess.ExitCode)}
$taskNode='C:/Users/gaulatti/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe'
$taskVerifier=if($ButtonContent){'scripts/verify-table-button-content-foreground.mjs'}else{'scripts/verify-table-navigation-foreground.mjs'}
& $taskNode $taskVerifier --native $taskFolder --host-assembly ($HostName+'.dll') --output (Join-Path $taskFolder audit.json)
$taskAuditExit=$LASTEXITCODE
$taskAudit=Get-Content -LiteralPath (Join-Path $taskFolder audit.json) -Raw|ConvertFrom-Json
if($ExpectFailure){if($taskAuditExit -ne 1 -or $taskAudit.passed -or -not $taskAudit.captureValid -or $taskAudit.brushFailures -le 0 -or $taskAudit.paintFailures -le 0){throw 'Expected failing baseline was not proved'}}
elseif($taskAuditExit -ne 0 -or -not $taskAudit.passed){throw 'Owned button foreground regression failed'}
@{expectedFailure=[bool]$ExpectFailure;expectationVerified=$true;nativeExit=$taskProcess.ExitCode;auditExit=$taskAuditExit;librarySha256=$taskNative.librarySha256}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $taskFolder expectation.json)
Write-Output ('Owned button foreground expectation verified; expectedFailure='+[bool]$ExpectFailure)
