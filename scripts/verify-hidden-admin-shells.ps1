param([Parameter(Mandatory)][string]$Package,[ValidateSet('x64','ARM64')][string]$Platform='x64',[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$expected=if($Platform -eq 'x64'){'X64'}else{'Arm64'}
if([Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString() -ne $expected){throw 'Hidden AdminShell checks require the selected native OS architecture.'}
$outputPath=[IO.Path]::GetFullPath($Output);$report=Join-Path $outputPath 'hidden-admin-shells.json'
if(Test-Path -LiteralPath $report){throw 'Preserve previous evidence and choose a fresh output directory.'}
& (Join-Path $PSScriptRoot 'verify-package.ps1') -Package $Package -Platform $Platform -BuildOnly -Output $Output
$references=Join-Path $outputPath 'reference';New-Item -ItemType Directory -Path $references -Force|Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/AdminShell.json') -Destination (Join-Path $references 'AdminShell.json')
$process=Start-Process -FilePath (Join-Path $outputPath 'app/PackageConsumer.exe') -ArgumentList "--verify-output `"$report`" --verify-hidden-admin-shells `"$references`"" -PassThru -WindowStyle Hidden
if(-not $process.WaitForExit(60000)){$process.Kill();throw 'Hidden AdminShell check timed out.'}
if(-not(Test-Path -LiteralPath $report)){throw 'Hidden AdminShell exited without a report.'}
$result=Get-Content -LiteralPath $report -Raw|ConvertFrom-Json
if(-not $result.isolated -or $result.visible -or $result.nativeVisible -or $result.inputSent -or $result.focusCalled -or $result.activateCalled -or $result.showCalled -or $result.activationEvents -ne 0){throw 'Hidden AdminShell isolation failed.'}
if($result.processArchitecture -ne $expected -or $result.osArchitecture -ne $expected){throw 'Hidden AdminShell architecture differs.'}
if($process.ExitCode -ne 0 -or -not $result.passed){$result|Select-Object passed,isolated,failures,failure|ConvertTo-Json -Depth 5;throw 'Hidden AdminShell contract failed.'}
[ordered]@{passed=$true;hiddenContractExecuted=$true;frames=$result.frames.Count;checks=$result.checks.Count;isolated=$true;visibleRuntimeVerified=$false;report=$report}|ConvertTo-Json
