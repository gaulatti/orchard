param(
    [string]$Executable = 'artifacts/catalog/Orchard.Catalog.exe',
    [string]$Output = 'artifacts/smoke',
    [ValidateSet('all', 'input', 'tooltips')][string]$Component = 'all',
    [ValidateSet('X64', 'Arm64')][string]$ExpectedArchitecture
)
$ErrorActionPreference = 'Stop'
$executablePath = (Resolve-Path $Executable).Path
$outputPath = [System.IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$resultPath = Join-Path $outputPath 'result.json'
if (Test-Path -LiteralPath $resultPath) { Remove-Item -LiteralPath $resultPath }
$componentArgument = if ($Component -eq 'all') { '' } else { " --smoke-$Component" }
$process = Start-Process -FilePath $executablePath -ArgumentList "--smoke-output `"$outputPath`"$componentArgument" -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(360000)) {
    $process.Kill()
    throw 'Catalog runtime check timed out.'
}
if ($process.ExitCode -ne 0) {
    if (Test-Path "$outputPath/result.json") { Get-Content "$outputPath/result.json" }
    throw "Catalog exited with code $($process.ExitCode)."
}
$report = Get-Content "$outputPath/result.json" -Raw | ConvertFrom-Json
if (-not $report.passed) { throw 'Catalog reported a failed runtime check.' }
if ($ExpectedArchitecture -and ($report.environment.processArchitecture -ne $ExpectedArchitecture -or
    $report.environment.osArchitecture -ne $ExpectedArchitecture)) {
    throw "Catalog requires $ExpectedArchitecture process and OS; reported process=$($report.environment.processArchitecture), OS=$($report.environment.osArchitecture)."
}
foreach ($theme in @('light', 'dark')) {
    if (-not (Test-Path "$outputPath/catalog-$theme.png")) { throw "Missing $theme render." }
}
Get-Content "$outputPath/result.json"
