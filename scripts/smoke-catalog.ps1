param(
    [string]$Executable = 'artifacts/catalog/Orchard.Catalog.exe',
    [string]$Output = 'artifacts/smoke'
)
$ErrorActionPreference = 'Stop'
$executablePath = (Resolve-Path $Executable).Path
$outputPath = [System.IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$process = Start-Process -FilePath $executablePath -ArgumentList "--smoke-output `"$outputPath`"" -PassThru
if (-not $process.WaitForExit(60000)) {
    $process.Kill()
    throw 'Catalog runtime check timed out.'
}
if ($process.ExitCode -ne 0) {
    if (Test-Path "$outputPath/result.json") { Get-Content "$outputPath/result.json" }
    throw "Catalog exited with code $($process.ExitCode)."
}
$report = Get-Content "$outputPath/result.json" -Raw | ConvertFrom-Json
if (-not $report.passed) { throw 'Catalog reported a failed runtime check.' }
foreach ($theme in @('light', 'dark')) {
    if (-not (Test-Path "$outputPath/catalog-$theme.png")) { throw "Missing $theme render." }
}
Get-Content "$outputPath/result.json"
