param(
    [string]$Executable = 'artifacts/catalog/Orchard.Catalog.exe',
    [string]$Output = 'artifacts/smoke',
    [ValidateSet('all', 'progress-stack', 'empty', 'icon-badge', 'accordion-scalars', 'accordion-continuity', 'accordion-motion', 'accordion', 'accordion-reference', 'radio-continuity', 'radio-motion', 'radio-group', 'radio-reference', 'date-controls', 'date-presentation', 'textarea', 'textarea-font', 'textarea-resize', 'textarea-resize-ownership', 'textarea-limits', 'textarea-commands', 'textarea-catalog', 'metric', 'brush-alpha', 'presentation', 'input', 'tooltips', 'breadcrumb', 'contrast', 'contrast-entry', 'alerts', 'brand', 'bauhaus', 'switches', 'checkboxes', 'activity', 'attention', 'notifications', 'chart-theme', 'line-chart', 'bar-chart', 'area-chart', 'pie-chart', 'donut-chart', 'scatter-chart', 'scatter-chart-sequence', 'radar-chart', 'radar-chart-sequence', 'radial-bar-chart', 'radial-bar-chart-sequence', 'funnel-chart', 'funnel-chart-sequence', 'sankey-chart', 'sankey-chart-sequence', 'sunburst-chart', 'sunburst-chart-sequence', 'currency-chart', 'currency-chart-sequence', 'bar-chart-sequence', 'error-state', 'error-state-contrast', 'error-state-after-chart', 'page-header', 'section-header', 'dashboard-section', 'dashboard-grid', 'sparkline', 'stat-card', 'sidebar', 'stepper', 'stepper-motion', 'stepper-raster', 'data-list', 'timeline', 'search-input', 'search-input-states', 'search-input-catalog', 'filter-chip', 'filter-chip-catalog', 'filter-chip-typography', 'filter-chip-accessibility', 'filter-chip-accessibility-session', 'loading-overlay', 'loading-overlay-lifecycle', 'loading-overlay-scroll', 'loading-overlay-accessibility', 'loading-overlay-template', 'loading-overlay-windows', 'loading-overlay-contrast', 'loading-overlay-contrast-entry', 'data-table-filters', 'select-shadow', 'menus', 'data-table-filters-native', 'data-table-filters-live', 'data-table-column-toggle', 'data-table-pagination', 'data-table-pagination-source', 'data-table-pagination-input', 'data-table-pagination-keyboard', 'data-table-pagination-paired', 'data-table-pagination-scroll', 'data-table-toolbar', 'data-table-toolbar-interactions', 'data-table-toolbar-press', 'font-audit', 'chart-load-focus')][string]$Component = 'all',
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
if ($ClientSize) { . (Join-Path $PSScriptRoot 'Set-CatalogCaptureWindow.ps1') }
$timeoutMilliseconds = if ($Component -eq 'all') { 480000 } else { 360000 }
$runtimeClock = [Diagnostics.Stopwatch]::StartNew()
$process = Start-Process -FilePath $executablePath -ArgumentList "--smoke-output `"$outputPath`"$componentArgument$sizeArgument" -PassThru -WindowStyle Hidden
if ($ClientSize) {
    try { Set-CatalogCaptureWindow -Process $process -Output $outputPath -RuntimeClock $runtimeClock -TimeoutMilliseconds $timeoutMilliseconds }
    catch { if (-not $process.HasExited) { $process.Kill() }; throw }
}
# The full sequence includes 240 additional required chart pointer actions.
# This watchdog bounds the suite; per-state/input deadlines remain in the app.
if (-not $process.WaitForExit([math]::Max(1, $timeoutMilliseconds - $runtimeClock.ElapsedMilliseconds))) {
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
if ($Component -eq 'textarea-font') {
    foreach ($frame in @('plain-modern','plain-legacy','custom','plain-system','text-box')) {
        if (-not (Test-Path (Join-Path $outputPath ($frame + '.png')))) { throw 'Missing Textarea font diagnostic render.' }
    }
} elseif ($Component -eq 'loading-overlay-contrast') {
    foreach ($theme in @('Light','Dark')) {
        foreach ($phase in @('before','active','restored')) {
            if (-not (Test-Path "$outputPath/contrast-$phase-$theme.png")) { throw "Missing loading contrast $phase $theme render." }
        }
    }
} elseif ($Component -eq 'loading-overlay-scroll') {
    if (-not (Test-Path "$outputPath/scroll-portal.png")) { throw 'Missing scroll portal render.' }
} elseif ($Component -in @('contrast','error-state-contrast')) {
    foreach ($phase in @('before', 'active', 'restored')) {
        if (-not (Test-Path "$outputPath/contrast-$phase.png")) { throw "Missing contrast $phase render." }
    }
} else {
    foreach ($theme in @('light', 'dark')) {
        if (-not (Test-Path "$outputPath/catalog-$theme.png")) { throw "Missing $theme render." }
    }
}
Get-Content "$outputPath/result.json"
