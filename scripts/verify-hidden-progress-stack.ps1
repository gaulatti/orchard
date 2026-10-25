param([Parameter(Mandatory)][string]$Package,[ValidateSet('x64','ARM64')][string]$Platform='x64',[string]$Output='artifacts/current/package-hidden-progress-stack')
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'verify-package.ps1') -Package $Package -Platform $Platform -BuildOnly -Output $Output
$outputPath=[IO.Path]::GetFullPath($Output)
$references=Join-Path $outputPath 'reference'
New-Item -ItemType Directory -Path $references -Force|Out-Null
foreach($name in @('ProgressStack.json','ProgressStackText.json','ProgressStackPaint.json')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot "../tests/Reference/$name") -Destination (Join-Path $references $name)}
$report=Join-Path $outputPath 'hidden-progress-stack.json'
if(Test-Path -LiteralPath $report){Remove-Item -LiteralPath $report}
$process=Start-Process -FilePath (Join-Path $outputPath 'app/PackageConsumer.exe') -ArgumentList "--verify-output `"$report`" --verify-hidden-progress-stack `"$references`"" -PassThru -WindowStyle Hidden
if(-not $process.WaitForExit(60000)){$process.Kill();throw 'Hidden ProgressStack layout check timed out.'}
if($process.ExitCode -ne 0){if(Test-Path -LiteralPath $report){Get-Content -LiteralPath $report -Tail 14};throw "Hidden ProgressStack exited with $($process.ExitCode)."}
$result=Get-Content -LiteralPath $report -Raw|ConvertFrom-Json
if(-not $result.passed -or $result.visible -or $result.nativeVisible -or $result.inputSent -or $result.activateCalled -or $result.activationEvents -ne 0){throw 'Hidden layout environment failed.'}
$expected=if($Platform -eq 'x64'){'X64'}else{'Arm64'}
if($result.processArchitecture -ne $expected -or $result.osArchitecture -ne $expected){throw 'Hidden layout requires the selected native architecture and OS.'}
Get-Content -LiteralPath (Join-Path $outputPath 'result.json')
[ordered]@{hiddenLayoutExecuted=$true;frames=$result.frames.Count;visibleRuntimeVerified=$false;report=$report}|ConvertTo-Json
