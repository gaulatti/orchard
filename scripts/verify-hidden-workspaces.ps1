param([Parameter(Mandatory)][string]$AppDirectory,[ValidateSet('x64','ARM64')][string]$Platform='x64',[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$expected=if($Platform -eq 'x64'){'X64'}else{'Arm64'}
if([Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString() -ne $expected){throw 'Hidden demo checks require the selected native OS architecture.'}
$appPath=(Resolve-Path -LiteralPath $AppDirectory).Path
$outputPath=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $outputPath){throw 'Preserve previous evidence and choose a fresh output directory.'}
$exe=Join-Path $appPath 'Orchard.Catalog.exe'
if(-not(Test-Path -LiteralPath $exe -PathType Leaf)){throw 'Build the catalog before running its hidden checks.'}
New-Item -ItemType Directory -Path $outputPath|Out-Null
$process=Start-Process -FilePath $exe -ArgumentList "--verify-hidden-workspaces `"$outputPath`"" -PassThru -WindowStyle Hidden
if(-not $process.WaitForExit(60000)){$process.Kill();throw 'Hidden demo checks timed out.'}
$report=Join-Path $outputPath 'hidden-workspaces.json'
if(-not(Test-Path -LiteralPath $report)){throw 'Hidden catalog exited without its report.'}
$result=Get-Content -LiteralPath $report -Raw|ConvertFrom-Json
if(-not $result.isolated -or $result.visible -or $result.nativeVisible -or $result.inputSent -or $result.focusCalled -or $result.activateCalled -or $result.showCalled -or $result.activationEvents -ne 0){throw 'Hidden demo isolation failed.'}
if($result.processArchitecture -ne $expected -or $result.osArchitecture -ne $expected){throw 'Hidden demo architecture differs.'}
foreach($item in @(@('catalogSha256','Orchard.Catalog.dll'),@('librarySha256','Orchard.dll'))){if($result.($item[0]) -ne (Get-FileHash -LiteralPath (Join-Path $appPath $item[1]) -Algorithm SHA256).Hash.ToLowerInvariant()){throw 'Hidden demo assembly identity differs.'}}
if($process.ExitCode -ne 0 -or -not $result.passed){$result|Select-Object passed,isolated,failure|ConvertTo-Json -Depth 5;throw 'Hidden demo contract failed.'}
[ordered]@{passed=$true;checks=$result.checks.Count;isolated=$true;visibleRuntimeVerified=$false;report=$report}|ConvertTo-Json
