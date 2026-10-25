param([Parameter(Mandatory)][string]$AppDirectory,[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$taskApp=(Resolve-Path -LiteralPath $AppDirectory).Path
$taskRoot=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $taskRoot){throw 'Use a fresh failure-test output directory'}
$taskHost=Join-Path $taskApp 'Orchard.Catalog.dll'
$taskCommands=[Convert]::ToHexString([IO.File]::ReadAllBytes($taskHost))
if(-not $taskCommands.Contains([Convert]::ToHexString([Text.Encoding]::Unicode.GetBytes('--probe-hidden-data-table-reference')))){throw 'Host does not recognize the hidden reference command'}
New-Item -ItemType Directory -Path $taskRoot|Out-Null
$taskResults=@()
foreach($taskCase in @('missing-reference','unwritable-report')){
    $taskFolder=Join-Path $taskRoot $taskCase
    New-Item -ItemType Directory -Path $taskFolder|Out-Null
    $taskReport=Join-Path $taskFolder 'data-table-reference.json'
    if($taskCase -eq 'unwritable-report'){New-Item -ItemType Directory -Path $taskReport|Out-Null}
    $taskError=Join-Path $taskFolder 'stderr.txt'
    $taskProcess=Start-Process -FilePath (Join-Path $taskApp 'Orchard.Catalog.exe') -ArgumentList "--probe-hidden-data-table-reference --verify-output `"$taskReport`"" -WindowStyle Hidden -RedirectStandardError $taskError -RedirectStandardOutput (Join-Path $taskFolder 'stdout.txt') -PassThru
    while(-not $taskProcess.WaitForExit(10000)){}
    if($taskProcess.ExitCode -ne 1){throw ('Failure case did not exit with code 1: '+$taskCase)}
    $taskErrorText=Get-Content -LiteralPath $taskError -Raw
    if($taskErrorText -notmatch 'FileNotFoundException' -or $taskErrorText -notmatch 'source-reference.json'){throw 'Original reference failure missing from stderr'}
    $taskReportVerified=$false
    if($taskCase -eq 'missing-reference'){
        $taskFailure=Get-Content -LiteralPath $taskReport -Raw|ConvertFrom-Json
        if($taskFailure.passed -ne $false -or $taskFailure.error -notmatch 'FileNotFoundException'){throw 'Small failure report lost the original error'}
        $taskReportVerified=$true
    }elseif(-not (Test-Path -LiteralPath $taskReport -PathType Container)){throw 'Unwritable report fixture changed'}
    $taskResults+=@{case=$taskCase;passed=$true;processId=$taskProcess.Id;exited=$true;exitCode=$taskProcess.ExitCode;timedOut=$false;smallFailureReportVerified=$taskReportVerified;originalErrorOnStderr=$true}
}
$taskResult=@{passed=$true;cases=$taskResults;hostSha256=(Get-FileHash -LiteralPath $taskHost).Hash.ToLowerInvariant();librarySha256=(Get-FileHash -LiteralPath (Join-Path $taskApp 'Orchard.dll')).Hash.ToLowerInvariant();nativeComponentAccepted=$false}
$taskResult|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $taskRoot 'audit.json')
$taskResult|ConvertTo-Json -Depth 5
exit 0
