param([Parameter(Mandatory)][string]$Executable,[string]$Output='artifacts/current/accordion-reference',[ValidateSet('X64','Arm64')][string]$ExpectedArchitecture)
. "$PSScriptRoot/windows-animation-preference.ps1"
$ErrorActionPreference='Stop'
$accordionOriginal=[OrchardMotionPreference]::Read()
$accordionOutput=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $accordionOutput -Force|Out-Null
try{
 [OrchardMotionPreference]::Set($false)
 foreach($accordionWidth in @(320,639,640)){
  $accordionArgs=@{Executable=$Executable;Output=(Join-Path $accordionOutput $accordionWidth);Component='accordion-reference';ClientSize=@($accordionWidth,640)}
  if($ExpectedArchitecture){$accordionArgs.ExpectedArchitecture=$ExpectedArchitecture}
  & "$PSScriptRoot/smoke-catalog.ps1" @accordionArgs
 }
}finally{
 [OrchardMotionPreference]::Set($accordionOriginal)
 @{original=$accordionOriginal;restored=[OrchardMotionPreference]::Read();passed=([OrchardMotionPreference]::Read() -eq $accordionOriginal)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $accordionOutput 'preference-restored.json')
}
