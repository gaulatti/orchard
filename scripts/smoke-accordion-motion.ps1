param([Parameter(Mandatory)][string]$Executable,[string]$Output='artifacts/accordion-motion',[ValidateSet('X64','Arm64')][string]$ExpectedArchitecture)
. "$PSScriptRoot/windows-animation-preference.ps1"
$ErrorActionPreference='Stop'
$motionOriginal=[OrchardMotionPreference]::Read()
$motionRoot=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $motionRoot -Force|Out-Null
try{
 foreach($motionEnabled in @($true,$false)){
  [OrchardMotionPreference]::Set($motionEnabled)
  $motionMode=if($motionEnabled){'enabled'}else{'reduced'}
  $motionArgs=@{Executable=$Executable;Output=(Join-Path $motionRoot $motionMode);Component='accordion-motion';ClientSize=@(320,640)}
  if($ExpectedArchitecture){$motionArgs.ExpectedArchitecture=$ExpectedArchitecture}
  & "$PSScriptRoot/smoke-catalog.ps1" @motionArgs
  $motionReport=Get-Content -LiteralPath (Join-Path $motionArgs.Output 'result.json') -Raw|ConvertFrom-Json
  if($motionReport.environment.animationsEnabled -ne $motionEnabled){throw 'Accordion motion preference differs.'}
 }
}finally{
 [OrchardMotionPreference]::Set($motionOriginal)
 @{original=$motionOriginal;restored=[OrchardMotionPreference]::Read();passed=([OrchardMotionPreference]::Read() -eq $motionOriginal)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $motionRoot 'preference-restored.json')
}
