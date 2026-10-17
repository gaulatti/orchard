param(
    [string]$Executable='artifacts/catalog/Orchard.Catalog.exe',
    [string]$Output='artifacts/radio-motion',
    [ValidateSet('X64','Arm64')][string]$ExpectedArchitecture
)
. "$PSScriptRoot/windows-animation-preference.ps1"
$original=[OrchardMotionPreference]::Read()
$outputPath=[IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force|Out-Null
try{
    foreach($enabled in @($true,$false)){
        [OrchardMotionPreference]::Set($enabled)
        $mode=if($enabled){'enabled'}else{'reduced'}
        foreach($component in @('radio-motion','radio-continuity','radio-group')){
            $arguments=@{Executable=$Executable;Output=(Join-Path $outputPath "$mode/$component");Component=$component;ClientSize=@(320,640)}
            if($component -eq 'radio-group'){$arguments.Remove('ClientSize')}
            if($ExpectedArchitecture){$arguments.ExpectedArchitecture=$ExpectedArchitecture}
            & "$PSScriptRoot/smoke-catalog.ps1" @arguments
            $report=Get-Content -LiteralPath (Join-Path $arguments.Output 'result.json') -Raw|ConvertFrom-Json
            if($report.environment.animationsEnabled -ne $enabled){throw "WinRT motion preference differs from $mode."}
        }
    }
}finally{
    [OrchardMotionPreference]::Set($original)
    @{original=$original;restored=[OrchardMotionPreference]::Read();passed=([OrchardMotionPreference]::Read() -eq $original)}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $outputPath 'preference-restored.json')
}
