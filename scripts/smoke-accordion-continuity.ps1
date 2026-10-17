param([Parameter(Mandatory)][string]$Executable,[string]$Output='artifacts/accordion-continuity',[ValidateSet('X64','Arm64')][string]$ExpectedArchitecture)
. "$PSScriptRoot/windows-animation-preference.ps1"
$ErrorActionPreference='Stop'
$continuityOriginal=[OrchardMotionPreference]::Read()
$continuityRoot=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $continuityRoot -Force|Out-Null
try{
 foreach($continuityEnabled in @($true,$false)){
  [OrchardMotionPreference]::Set($continuityEnabled)
  $continuityMode=if($continuityEnabled){'enabled'}else{'reduced'}
  $continuityArgs=@{Executable=$Executable;Output=(Join-Path $continuityRoot $continuityMode);Component='accordion-continuity';ClientSize=@(320,640)}
  if($ExpectedArchitecture){$continuityArgs.ExpectedArchitecture=$ExpectedArchitecture}
  & "$PSScriptRoot/smoke-catalog.ps1" @continuityArgs
  $continuityReport=Get-Content -LiteralPath (Join-Path $continuityArgs.Output 'result.json') -Raw|ConvertFrom-Json
  if($continuityReport.environment.animationsEnabled -ne $continuityEnabled){throw 'Accordion continuity preference differs.'}
 }
}finally{
 [OrchardMotionPreference]::Set($continuityOriginal)
 @{original=$continuityOriginal;restored=[OrchardMotionPreference]::Read();passed=([OrchardMotionPreference]::Read() -eq $continuityOriginal)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $continuityRoot 'preference-restored.json')
}
