param([Parameter(Mandatory)][string]$Executable,[string]$Output='artifacts/accordion-scalars',[ValidateSet('X64','Arm64')][string]$ExpectedArchitecture)
. "$PSScriptRoot/windows-animation-preference.ps1"
$ErrorActionPreference='Stop'
$scalarOriginal=[OrchardMotionPreference]::Read()
$scalarRoot=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $scalarRoot -Force|Out-Null
try{
 [OrchardMotionPreference]::Set($false)
 foreach($scalarWidth in @(320,640)){
  $scalarArgs=@{Executable=$Executable;Output=(Join-Path $scalarRoot $scalarWidth);Component='accordion-scalars';ClientSize=@($scalarWidth,640)}
  if($ExpectedArchitecture){$scalarArgs.ExpectedArchitecture=$ExpectedArchitecture}
  & "$PSScriptRoot/smoke-catalog.ps1" @scalarArgs
 }
}finally{
 [OrchardMotionPreference]::Set($scalarOriginal)
 @{original=$scalarOriginal;restored=[OrchardMotionPreference]::Read();passed=([OrchardMotionPreference]::Read() -eq $scalarOriginal)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $scalarRoot 'preference-restored.json')
}
