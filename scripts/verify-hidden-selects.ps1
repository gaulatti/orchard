param([Parameter(Mandatory)][string]$Package,[ValidateSet('x64','ARM64')][string]$Platform='x64',[Parameter(Mandatory)][string]$Output,[string]$AppDirectory)
$ErrorActionPreference='Stop'
$expected=if($Platform -eq 'x64'){'X64'}else{'Arm64'}
if([Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString() -ne $expected){throw 'Hidden Select checks require the selected native OS architecture.'}
$outputPath=[IO.Path]::GetFullPath($Output);$report=Join-Path $outputPath 'hidden-selects.json'
if(Test-Path -LiteralPath $report){throw 'Preserve previous evidence and choose a fresh output directory.'}
if($AppDirectory){
 $taskExistingApp=(Resolve-Path -LiteralPath $AppDirectory).Path
 if(-not [string]::Equals($taskExistingApp,[IO.Path]::GetFullPath((Join-Path $outputPath 'app')),[StringComparison]::OrdinalIgnoreCase)){throw 'Reused Select app must belong to the supplied consumer output'}
 $taskExistingResult=Get-Content -LiteralPath (Join-Path $outputPath 'result.json') -Raw|ConvertFrom-Json
 $taskExistingCleanup=Get-Content -LiteralPath (Join-Path $outputPath 'cleanup.json') -Raw|ConvertFrom-Json
 if(-not $taskExistingResult.buildPassed -or -not $taskExistingCleanup.removed -or $taskExistingResult.platform -ne $Platform -or $taskExistingResult.packageSha256 -ne (Get-FileHash -LiteralPath $Package).Hash.ToLowerInvariant()){throw 'Reused Select app is not a verified build of the supplied package'}
}else{& (Join-Path $PSScriptRoot 'verify-package.ps1') -Package $Package -Platform $Platform -BuildOnly -Output $Output}
$references=Join-Path $outputPath 'reference';New-Item -ItemType Directory -Path $references -Force|Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/Select.json') -Destination (Join-Path $references 'Select.json')
$process=Start-Process -FilePath (Join-Path $outputPath 'app/PackageConsumer.exe') -ArgumentList "--verify-output `"$report`" --verify-hidden-selects `"$references`"" -PassThru -WindowStyle Hidden
@{processId=$process.Id;startedAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $outputPath 'hidden-selects-process.json')
if(-not $process.WaitForExit(60000)){if($process.Id -in @(27524,32420,32704,33320,33684)){throw 'Protected PID reused; refusing termination'};$process.Kill();throw 'Hidden Select check timed out.'}
@{exited=$true;exitCode=$process.ExitCode}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $outputPath 'hidden-selects-process-exit.json')
if(-not (Test-Path -LiteralPath $report)){throw 'Hidden Select exited without a report.'}
$result=Get-Content -LiteralPath $report -Raw|ConvertFrom-Json
if(-not $result.isolated -or $result.visible -or $result.nativeVisible -or $result.inputSent -or $result.focusCalled -or $result.activateCalled -or $result.showCalled -or $result.activationEvents -ne 0){throw 'Hidden Select isolation failed.'}
if($result.processArchitecture -ne $expected -or $result.osArchitecture -ne $expected){throw 'Hidden Select architecture differs.'}
if($process.ExitCode -ne 0 -or -not $result.passed){$result|Select-Object passed,isolated,failures,failure|ConvertTo-Json -Depth 5;throw 'Hidden Select contract failed.'}
[ordered]@{passed=$true;hiddenContractExecuted=$true;frames=$result.frames.Count;histories=$result.histories.Count;checks=$result.checks.Count;isolated=$true;visibleRuntimeVerified=$false;report=$report}|ConvertTo-Json
