param(
    [string]$Executable = 'artifacts/catalog/Orchard.Catalog.exe',
    [string]$Output = 'artifacts/stepper-motion',
    [ValidateSet('X64','Arm64')][string]$ExpectedArchitecture
)
. "$PSScriptRoot/windows-animation-preference.ps1"
$original = [OrchardMotionPreference]::Read()
$outputPath = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
try {
    foreach ($enabled in @($true,$false)) {
        [OrchardMotionPreference]::Set($enabled)
        $mode = if ($enabled) { 'enabled' } else { 'reduced' }
        $arguments = @{ Executable=$Executable; Output=(Join-Path $outputPath $mode); Component='stepper-motion'; ClientSize=@(1024,745) }
        if ($ExpectedArchitecture) { $arguments.ExpectedArchitecture=$ExpectedArchitecture }
        & "$PSScriptRoot/smoke-catalog.ps1" @arguments
        $report = Get-Content (Join-Path $arguments.Output 'result.json') -Raw | ConvertFrom-Json
        if ($report.environment.animationsEnabled -ne $enabled) { throw "WinRT animation preference differs from the $mode test scope." }
    }
}
finally {
    [OrchardMotionPreference]::Set($original)
    [ordered]@{ original=$original; restored=[OrchardMotionPreference]::Read(); passed=([OrchardMotionPreference]::Read() -eq $original) } |
        ConvertTo-Json | Set-Content (Join-Path $outputPath 'preference-restored.json')
}
