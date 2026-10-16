param(
    [string]$Executable = 'artifacts/catalog/Orchard.Catalog.exe',
    [string]$Output = 'artifacts/smoke',
    [ValidateSet('all', 'brush-alpha', 'presentation', 'input', 'tooltips', 'breadcrumb', 'contrast', 'contrast-entry', 'alerts', 'brand', 'bauhaus', 'switches', 'checkboxes', 'activity', 'attention', 'notifications', 'chart-theme', 'line-chart', 'bar-chart', 'area-chart', 'pie-chart', 'donut-chart', 'scatter-chart', 'scatter-chart-sequence', 'radar-chart', 'radar-chart-sequence', 'radial-bar-chart', 'radial-bar-chart-sequence', 'funnel-chart', 'funnel-chart-sequence', 'sankey-chart', 'sankey-chart-sequence', 'sunburst-chart', 'sunburst-chart-sequence', 'currency-chart', 'currency-chart-sequence', 'bar-chart-sequence', 'error-state', 'error-state-contrast', 'error-state-after-chart', 'page-header', 'section-header', 'dashboard-section', 'dashboard-grid', 'sparkline', 'stat-card', 'sidebar', 'stepper', 'stepper-motion', 'stepper-raster', 'data-list', 'data-table-filters', 'select-shadow', 'menus', 'data-table-filters-native', 'data-table-filters-live', 'data-table-column-toggle', 'data-table-pagination', 'data-table-pagination-source', 'data-table-pagination-input', 'data-table-pagination-keyboard', 'data-table-pagination-paired', 'data-table-pagination-scroll', 'data-table-toolbar', 'data-table-toolbar-interactions', 'data-table-toolbar-press', 'font-audit', 'chart-load-focus')][string]$Component = 'all',
    [ValidateSet('X64', 'Arm64')][string]$ExpectedArchitecture,
    [ValidateCount(2,2)][int[]]$ClientSize
)
$ErrorActionPreference = 'Stop'
$executablePath = (Resolve-Path $Executable).Path
$outputPath = [System.IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$resultPath = Join-Path $outputPath 'result.json'
if (Test-Path -LiteralPath $resultPath) { Remove-Item -LiteralPath $resultPath }
$componentArgument = if ($Component -eq 'all') { '' } else { " --smoke-$Component" }
$sizeArgument = ''
if ($ClientSize) {
    if ($ClientSize[0] -le 0 -or $ClientSize[1] -le 0) { throw 'ClientSize width and height must be positive.' }
    $sizeArgument = " --smoke-client-size $($ClientSize[0]) $($ClientSize[1])"
}
$process = Start-Process -FilePath $executablePath -ArgumentList "--smoke-output `"$outputPath`"$componentArgument$sizeArgument" -PassThru -WindowStyle Hidden
# The full sequence includes 240 additional required chart pointer actions.
# This watchdog bounds the suite; per-state/input deadlines remain in the app.
$timeoutMilliseconds = if ($Component -eq 'all') { 480000 } else { 360000 }
if (-not $process.WaitForExit($timeoutMilliseconds)) {
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
if ($Component -in @('contrast','error-state-contrast')) {
    foreach ($phase in @('before', 'active', 'restored')) {
        if (-not (Test-Path "$outputPath/contrast-$phase.png")) { throw "Missing contrast $phase render." }
    }
} else {
    foreach ($theme in @('light', 'dark')) {
        if (-not (Test-Path "$outputPath/catalog-$theme.png")) { throw "Missing $theme render." }
    }
}
Get-Content "$outputPath/result.json"
