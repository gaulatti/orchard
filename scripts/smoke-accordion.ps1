param([string]$Executable='artifacts/catalog/Orchard.Catalog.exe',[string]$Output='artifacts/accordion',[ValidateSet('X64','Arm64')][string]$ExpectedArchitecture)
. "$PSScriptRoot/windows-animation-preference.ps1"
$ErrorActionPreference='Stop'
$accordionOriginal=[OrchardMotionPreference]::Read()
$accordionRoot=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $accordionRoot -Force|Out-Null
try{
 foreach($accordionEnabled in @($true,$false)){
  [OrchardMotionPreference]::Set($accordionEnabled)
  $accordionMode=if($accordionEnabled){'enabled'}else{'reduced'}
  $accordionArguments=@{Executable=$Executable;Output=(Join-Path $accordionRoot $accordionMode);Component='accordion'}
  if($ExpectedArchitecture){$accordionArguments.ExpectedArchitecture=$ExpectedArchitecture}
  & "$PSScriptRoot/smoke-catalog.ps1" @accordionArguments
  $accordionReport=Get-Content -LiteralPath (Join-Path $accordionArguments.Output 'result.json') -Raw|ConvertFrom-Json
  if($accordionReport.environment.animationsEnabled -ne $accordionEnabled){throw 'Accordion Windows animation preference differs.'}
 }
 $accordionReference=@{Executable=$Executable;Output=(Join-Path $accordionRoot 'reference')}
 if($ExpectedArchitecture){$accordionReference.ExpectedArchitecture=$ExpectedArchitecture}
 & "$PSScriptRoot/smoke-accordion-reference.ps1" @accordionReference
}finally{
 [OrchardMotionPreference]::Set($accordionOriginal)
 @{original=$accordionOriginal;restored=[OrchardMotionPreference]::Read();passed=([OrchardMotionPreference]::Read() -eq $accordionOriginal)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $accordionRoot 'preference-restored.json')
}
