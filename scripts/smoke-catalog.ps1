param(
    [string]$Executable = 'artifacts/catalog/Orchard.Catalog.exe',
    [string]$Output = 'artifacts/smoke',
    [ValidateSet('all', 'cards', 'skeletons', 'footer', 'nav-menu', 'collection-filters', 'pagination', 'pagination-motion', 'buttons', 'button-group', 'icon-button', 'field', 'auth-shell', 'app-shell', 'header-input', 'hover-card-input', 'alert-dialog-input', 'edge-dialog-input', 'admin-shell', 'admin-shell-input', 'admin-shell-diagnostic', 'admin-shell-text-diagnostic', 'page-frame', 'detail-layout', 'panel', 'panel-layout', 'feed-grid', 'feed-item', 'toggles', 'toggle-group', 'slider', 'theme-toggle', 'dialog-close', 'tabs', 'tabs-reference', 'progress-stack', 'progress-stack-text', 'progress-stack-accessibility', 'empty', 'icon-badge', 'accordion-scalars', 'accordion-continuity', 'accordion-motion', 'accordion', 'accordion-reference', 'radio-continuity', 'radio-motion', 'radio-group', 'radio-reference', 'date-controls', 'date-presentation', 'textarea', 'textarea-font', 'textarea-resize', 'textarea-resize-ownership', 'textarea-limits', 'textarea-commands', 'textarea-catalog', 'metric', 'brush-alpha', 'presentation', 'input', 'tooltips', 'breadcrumb', 'contrast', 'contrast-entry', 'alerts', 'brand', 'bauhaus', 'switches', 'checkboxes', 'activity', 'attention', 'notifications', 'chart-theme', 'line-chart', 'bar-chart', 'area-chart', 'pie-chart', 'donut-chart', 'scatter-chart', 'scatter-chart-sequence', 'radar-chart', 'radar-chart-sequence', 'radial-bar-chart', 'radial-bar-chart-sequence', 'funnel-chart', 'funnel-chart-sequence', 'sankey-chart', 'sankey-chart-sequence', 'sunburst-chart', 'sunburst-chart-sequence', 'currency-chart', 'currency-chart-sequence', 'bar-chart-sequence', 'error-state', 'error-state-contrast', 'error-state-after-chart', 'page-header', 'section-header', 'dashboard-section', 'dashboard-grid', 'sparkline', 'stat-card', 'sidebar', 'stepper', 'stepper-motion', 'stepper-raster', 'data-list', 'timeline', 'search-input', 'search-input-states', 'search-input-catalog', 'filter-chip', 'filter-chip-catalog', 'filter-chip-typography', 'filter-chip-accessibility', 'filter-chip-accessibility-session', 'loading-spinner', 'loading-overlay', 'loading-overlay-lifecycle', 'loading-overlay-scroll', 'loading-overlay-accessibility', 'loading-overlay-template', 'loading-overlay-windows', 'loading-overlay-contrast', 'loading-overlay-contrast-entry', 'data-table-filters', 'select-shadow', 'menus', 'data-table-filters-native', 'data-table-filters-live', 'data-table-column-toggle', 'data-table-pagination', 'data-table-pagination-source', 'data-table-pagination-input', 'data-table-pagination-keyboard', 'data-table-pagination-paired', 'data-table-pagination-scroll', 'data-table-toolbar', 'data-table-toolbar-interactions', 'data-table-toolbar-press', 'font-audit', 'chart-load-focus')][string]$Component = 'all',
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
if ($Component -in @('admin-shell-text-diagnostic', 'pagination', 'pagination-motion', 'collection-filters', 'nav-menu', 'footer')) {
    [ordered]@{ component = $Component; exited = $true; exitCode = $process.ExitCode } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'process-exit.json')
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
if ($Component -eq 'footer') {
    $footerReport = Get-Content -LiteralPath (Join-Path $outputPath 'footer.json') -Raw | ConvertFrom-Json
    if (-not $footerReport.passed -or -not $footerReport.diagnosticOnly -or $footerReport.frames.Count -ne 64 -or $footerReport.inputSequencesVerified -or $footerReport.nativeComponentAccepted) { throw 'Incomplete Footer diagnostic capture.' }
} elseif ($Component -eq 'nav-menu') {
    $navReport = Get-Content -LiteralPath (Join-Path $outputPath 'nav-menu.json') -Raw | ConvertFrom-Json
    if (-not $navReport.passed -or $navReport.frames.Count -ne 42 -or $navReport.interactions.Count -ne 8 -or $navReport.nativeComponentAccepted) { throw 'Incomplete Nav Menu foreground run.' }
    Write-Host ($report | ConvertTo-Json -Compress -Depth 8)
} elseif ($Component -eq 'collection-filters') {
    $collectionReport = Get-Content -LiteralPath (Join-Path $outputPath 'collection-filters.json') -Raw | ConvertFrom-Json
    if (-not $collectionReport.passed -or $collectionReport.frames.Count -ne 50 -or $collectionReport.interactions.Count -ne 4 -or $collectionReport.keyOrderInteractions.Count -ne 4 -or $collectionReport.nativeComponentAccepted) { throw 'Incomplete Collection Filters foreground run.' }
    foreach ($collectionFile in $collectionReport.captures.PSObject.Properties.Name) { if (-not (Test-Path -LiteralPath (Join-Path $outputPath $collectionFile))) { throw 'Missing Collection Filters PNG.' } }
} elseif ($Component -eq 'pagination-motion') {
    $motionReport = Get-Content -LiteralPath (Join-Path $outputPath 'pagination-motion.json') -Raw | ConvertFrom-Json
    if (-not $motionReport.passed -or -not $motionReport.diagnosticOnly -or $motionReport.frames.Count -ne 64 -or $motionReport.lifecycle.Count -ne 64 -or $motionReport.normalRealtimeMotionVerified -or $motionReport.nativeComponentAccepted -or @($motionReport.captures.PSObject.Properties).Count -ne 4) { throw 'Incomplete controlled-time Pagination presentation.' }
    foreach ($motionFile in $motionReport.captures.PSObject.Properties.Name) { if (-not (Test-Path -LiteralPath (Join-Path $outputPath $motionFile))) { throw 'Missing controlled-time Pagination PNG.' } }
} elseif ($Component -eq 'pagination') {
    $paginationReference = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/Pagination.json') -Raw | ConvertFrom-Json
    $paginationReport = Get-Content -LiteralPath (Join-Path $outputPath 'pagination.json') -Raw | ConvertFrom-Json
    if (-not $paginationReport.passed -or $paginationReport.frames.Count -ne $paginationReference.cases.Count -or $paginationReport.interactions.Count -ne 4) { throw 'Incomplete Pagination foreground run.' }
    foreach ($paginationFrame in $paginationReference.cases) { if (-not (Test-Path -LiteralPath (Join-Path $outputPath $paginationFrame.file))) { throw 'Missing Pagination foreground PNG.' } }
} elseif ($Component -eq 'admin-shell') {
    $adminReference = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/AdminShell.json') -Raw | ConvertFrom-Json
    $adminReport = Get-Content -LiteralPath (Join-Path $outputPath 'admin-shell.json') -Raw | ConvertFrom-Json
    if (-not $adminReport.passed -or $adminReport.frames.Count -ne $adminReference.cases.Count) { throw 'Incomplete AdminShell foreground render.' }
    foreach ($adminFrame in $adminReference.cases) {
        foreach ($adminFile in @($adminFrame.file, $adminFrame.scrolledFile)) {
            if ($adminFile -and -not (Test-Path -LiteralPath (Join-Path $outputPath $adminFile))) { throw 'Missing AdminShell foreground PNG.' }
        }
    }
} elseif ($Component -eq 'admin-shell-diagnostic') {
    $diagnostic = Get-Content -LiteralPath (Join-Path $outputPath 'admin-shell-diagnostic.json') -Raw | ConvertFrom-Json
    if (-not $diagnostic.passed -or -not $diagnostic.diagnosticOnly -or $diagnostic.scope -ne 'diagnostic-admin-shell-source-capture' -or $diagnostic.frames.Count -ne 2 -or $diagnostic.variants.Count -ne 32 -or @($diagnostic.captures.PSObject.Properties).Count -ne 32 -or $diagnostic.nativeComponentAccepted -or $diagnostic.sourceRasterParityVerified) { throw 'Invalid focused AdminShell diagnostic.' }
    foreach ($variant in $diagnostic.variants) { if (-not (Test-Path -LiteralPath (Join-Path $outputPath $variant.file))) { throw 'Missing focused AdminShell diagnostic PNG.' } }
} elseif ($Component -eq 'admin-shell-text-diagnostic') {
    $textDiagnostic = Get-Content -LiteralPath (Join-Path $outputPath 'admin-shell-text-diagnostic.json') -Raw | ConvertFrom-Json
    if (-not $textDiagnostic.passed -or $textDiagnostic.scope -ne 'diagnostic-admin-shell-text-rendering' -or -not $textDiagnostic.diagnosticOnly -or $textDiagnostic.frames.Count -ne 2 -or $textDiagnostic.variants.Count -ne 68 -or @($textDiagnostic.captures.PSObject.Properties).Count -ne 68 -or $textDiagnostic.nativeComponentAccepted -or $textDiagnostic.sourceRasterParityVerified -or $textDiagnostic.productionLibraryChanged) { throw 'Invalid AdminShell text diagnostic.' }
    foreach ($textVariant in $textDiagnostic.variants) { if (-not (Test-Path -LiteralPath (Join-Path $outputPath $textVariant.file))) { throw 'Missing text diagnostic PNG.' } }
} elseif ($Component -eq 'theme-toggle') {
    $toggleReference = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/ThemeToggle.json') -Raw | ConvertFrom-Json
    $toggleReport = Get-Content -LiteralPath (Join-Path $outputPath 'theme-toggle.json') -Raw | ConvertFrom-Json
    if (-not $toggleReport.passed -or $toggleReport.frames.Count -ne $toggleReference.cases.Count) { throw 'Incomplete Theme Toggle reference render.' }
    foreach ($toggleFrame in $toggleReference.cases) { if (-not (Test-Path -LiteralPath (Join-Path $outputPath $toggleFrame.file))) { throw 'Missing Theme Toggle reference render.' } }
} elseif ($Component -eq 'slider') {
    $sliderReference = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/Slider.json') -Raw | ConvertFrom-Json
    $sliderReport = Get-Content -LiteralPath (Join-Path $outputPath 'slider.json') -Raw | ConvertFrom-Json
    if (-not $sliderReport.passed -or $sliderReport.frames.Count -ne $sliderReference.cases.Count) { throw 'Incomplete Slider reference render.' }
    foreach ($sliderFrame in $sliderReference.cases) { if ($sliderFrame.file -and -not (Test-Path -LiteralPath (Join-Path $outputPath $sliderFrame.file))) { throw 'Missing Slider reference render.' } }
} elseif ($Component -eq 'feed-grid') {
    $feedReference = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/FeedGrid.json') -Raw | ConvertFrom-Json
    $feedReport = Get-Content -LiteralPath (Join-Path $outputPath 'feed-grid.json') -Raw | ConvertFrom-Json
    if (-not $feedReport.passed -or $feedReport.frames.Count -ne $feedReference.cases.Count) { throw 'Incomplete FeedGrid reference render.' }
    foreach ($feedFrame in $feedReference.cases) { if ($feedFrame.file -and -not (Test-Path -LiteralPath (Join-Path $outputPath $feedFrame.file))) { throw 'Missing FeedGrid reference render.' } }
} elseif ($Component -eq 'panel-layout') {
    $layoutReference = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/PanelLayout.json') -Raw | ConvertFrom-Json
    $layoutReport = Get-Content -LiteralPath (Join-Path $outputPath 'panel-layout.json') -Raw | ConvertFrom-Json
    if (-not $layoutReport.passed -or $layoutReport.frames.Count -ne $layoutReference.cases.Count) { throw 'Incomplete PanelLayout reference render.' }
    foreach ($layoutFrame in $layoutReference.cases) { if ($layoutFrame.file -and -not (Test-Path -LiteralPath (Join-Path $outputPath $layoutFrame.file))) { throw 'Missing PanelLayout reference render.' } }
} elseif ($Component -eq 'panel') {
    $panelReference = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/Panel.json') -Raw | ConvertFrom-Json
    $panelReport = Get-Content -LiteralPath (Join-Path $outputPath 'panel.json') -Raw | ConvertFrom-Json
    if (-not $panelReport.passed -or $panelReport.frames.Count -ne $panelReference.cases.Count) { throw 'Incomplete Panel reference render.' }
    foreach ($frame in $panelReference.cases) { if ($frame.file -and -not (Test-Path -LiteralPath (Join-Path $outputPath $frame.file))) { throw 'Missing Panel reference render.' } }
} elseif ($Component -eq 'detail-layout') {
    $detailReference = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/DetailLayout.json') -Raw | ConvertFrom-Json
    $detailFrames = Get-Content -LiteralPath (Join-Path $outputPath 'detail-layout.json') -Raw | ConvertFrom-Json
    if (-not $detailFrames.passed -or $detailFrames.frames.Count -ne $detailReference.cases.Count) { throw 'Incomplete DetailLayout reference render.' }
    foreach ($detailFrame in $detailReference.cases) {
        if ($detailFrame.file -and -not (Test-Path -LiteralPath (Join-Path $outputPath $detailFrame.file))) { throw 'Missing DetailLayout render.' }
    }
} elseif ($Component -eq 'page-frame') {
    $pageReference = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/PageFrame.json') -Raw | ConvertFrom-Json
    $pageFrames = Get-Content -LiteralPath (Join-Path $outputPath 'page-frame.json') -Raw | ConvertFrom-Json
    if (-not $pageFrames.passed -or $pageFrames.frames.Count -ne $pageReference.cases.Count) { throw 'Incomplete PageFrame reference render.' }
    foreach ($pageFrame in $pageReference.cases) {
        if ($pageFrame.file -and -not (Test-Path -LiteralPath (Join-Path $outputPath $pageFrame.file))) { throw 'Missing PageFrame render.' }
    }
} elseif ($Component -eq 'field') {
    $fieldReference = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/Field.json') -Raw | ConvertFrom-Json
    $fieldFrames = Get-Content -LiteralPath (Join-Path $outputPath 'field.json') -Raw | ConvertFrom-Json
    if (-not $fieldFrames.passed -or $fieldFrames.frames.Count -ne $fieldReference.cases.Count) { throw 'Incomplete Field reference render.' }
    foreach ($fieldFrame in $fieldReference.cases) {
        if (-not (Test-Path -LiteralPath (Join-Path $outputPath $fieldFrame.file))) { throw 'Missing Field render.' }
    }
} elseif ($Component -eq 'dialog-close') {
    $closeReference = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/DialogCloseButton.json') -Raw | ConvertFrom-Json
    $closeFrames = Get-Content -LiteralPath (Join-Path $outputPath 'dialog-close.json') -Raw | ConvertFrom-Json
    if (-not $closeFrames.passed -or $closeFrames.frames.Count -ne $closeReference.cases.Count) { throw 'Incomplete Dialog Close Button reference render.' }
    foreach ($closeFrame in $closeReference.cases) {
        if (-not (Test-Path -LiteralPath (Join-Path $outputPath $closeFrame.file))) { throw 'Missing Dialog Close Button render.' }
    }
} elseif ($Component -eq 'tabs-reference') {
    $tabsReference = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../tests/Reference/Tabs.json') -Raw | ConvertFrom-Json
    $tabsFrames = Get-Content -LiteralPath (Join-Path $outputPath 'tabs-reference.json') -Raw | ConvertFrom-Json
    if (-not $tabsFrames.passed -or $tabsFrames.frames.Count -ne $tabsReference.cases.Count) { throw 'Incomplete Tabs reference render.' }
    foreach ($tabsFrame in @($tabsReference.cases | Where-Object file)) {
        if (-not (Test-Path -LiteralPath (Join-Path $outputPath $tabsFrame.file))) { throw "Missing Tabs render: $($tabsFrame.file)" }
    }
} elseif ($Component -eq 'textarea-font') {
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
} elseif ($Component -eq 'alert-dialog-input') {
    foreach ($theme in @('Light', 'Dark')) {
        $inputReport = Get-Content -LiteralPath (Join-Path $outputPath "alert-dialog-input-$theme.json") -Raw | ConvertFrom-Json
        if (-not $inputReport.passed -or $inputReport.theme -ne $theme -or
            $inputReport.pointerConfirmRequests -ne 1 -or $inputReport.escapeCancelRequests -ne 1 -or
            -not $inputReport.focusRestored -or -not $inputReport.hardwareInputSent) {
            throw "Incomplete $theme Alert Dialog input check."
        }
    }
} elseif ($Component -eq 'edge-dialog-input') {
    foreach ($theme in @('Light', 'Dark')) {
        $inputReport = Get-Content -LiteralPath (Join-Path $outputPath "edge-dialog-input-$theme.json") -Raw | ConvertFrom-Json
        if (-not $inputReport.passed -or $inputReport.theme -ne $theme -or
            $inputReport.drawerEscapeRequests -ne 1 -or $inputReport.sheetPointerCloseRequests -ne 1 -or
            $inputReport.sheetEscapeRequests -ne 1 -or -not $inputReport.focusRestored -or
            -not $inputReport.hardwareInputSent) {
            throw "Incomplete $theme Drawer/Sheet input check."
        }
    }
} else {
    foreach ($theme in @('light', 'dark')) {
        if (-not (Test-Path "$outputPath/catalog-$theme.png")) { throw "Missing $theme render." }
    }
}
Get-Content "$outputPath/result.json"
