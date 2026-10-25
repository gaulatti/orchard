param(
    [Parameter(Mandatory)][string]$Package,
    [ValidateSet('x64','ARM64')][string]$Platform='x64',
    [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference='Stop'
$expected=if($Platform -eq 'x64'){'X64'}else{'Arm64'}
if([Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString() -ne $expected){throw 'Hidden component checks require the selected native OS architecture.'}
$outputPath=[IO.Path]::GetFullPath($Output)
$report=Join-Path $outputPath 'hidden-component-contracts.json'
if(Test-Path -LiteralPath $report){throw 'Preserve the previous report and choose a fresh output directory.'}
& (Join-Path $PSScriptRoot 'verify-package.ps1') -Package $Package -Platform $Platform -BuildOnly -Output $Output
$references=Join-Path $outputPath 'reference'
New-Item -ItemType Directory -Path $references -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/ThemeToggle.json') -Destination (Join-Path $references 'ThemeToggle.json')
$process=Start-Process -FilePath (Join-Path $outputPath 'app/PackageConsumer.exe') -ArgumentList "--verify-output `"$report`" --verify-hidden-component-contracts `"$references`"" -PassThru -WindowStyle Hidden
if(-not $process.WaitForExit(60000)){$process.Kill();throw 'Hidden component contract check timed out.'}
if(-not (Test-Path -LiteralPath $report)){throw "Hidden component check exited with $($process.ExitCode) without a report."}
$result=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
if(-not $result.isolated -or $result.visible -or $result.nativeVisible -or $result.inputSent -or $result.focusCalled -or $result.activateCalled -or $result.showCalled -or $result.enabledButtonInvoked -or $result.activationEvents -ne 0){throw 'Hidden component isolation failed.'}
if($result.processArchitecture -ne $expected -or $result.osArchitecture -ne $expected){throw 'Hidden component check reported a different native architecture.'}
if($process.ExitCode -ne 0 -or -not $result.passed){$result | Select-Object passed,isolated,failures,failure | ConvertTo-Json -Depth 6;throw 'Hidden component contract failed.'}
[ordered]@{passed=$true;hiddenContractExecuted=$true;normalStateFrames=$result.frames.Count;checks=$result.checks.Count;isolated=$result.isolated;visibleRuntimeVerified=$false;report=$report} | ConvertTo-Json
