param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [Parameter(Mandatory)][string]$Output,
    [Parameter(Mandatory)][string]$ExpectedLibrarySha256,
    [Parameter(Mandatory)][ValidateSet('Light', 'Dark')][string]$Theme
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class OrchardMonitoringDesktop
{
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left; public int Top; public int Right; public int Bottom; }
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rect rect);
}
'@

$app = (Resolve-Path -LiteralPath $AppDirectory).Path
$exe = Join-Path $app 'Orchard.Catalog.exe'
$library = Join-Path $app 'Orchard.dll'
$folder = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $folder) { throw 'Use a fresh Monitoring desktop output directory.' }
foreach ($path in @($exe, $library)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing Monitoring desktop input: $path" } }
$libraryHash = (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()
if ($libraryHash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'The catalog contains different Orchard library bytes.' }
if (@(Get-Process Orchard.Catalog -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count) { throw 'Close this catalog before its isolated Monitoring check.' }
New-Item -ItemType Directory -Path $folder | Out-Null

$report = [ordered]@{
    format = 'orchard-monitoring-desktop-v1'
    passed = $false
    requestedTheme = $Theme
    catalogExeSha256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    orchardDllSha256 = $libraryHash
    steps = [ordered]@{}
    physicalPointerInputSent = $false
    physicalKeyboardInputSent = $false
    osSettingsChanged = $false
    nativeArm64RuntimeVerified = $false
    compositionAccepted = $false
}
function Wait-Until([scriptblock]$condition, [string]$failure) {
    $deadline = [DateTime]::UtcNow.AddSeconds(12)
    do {
        try { $observation = & $condition; if ($observation) { return $observation } } catch { }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)
    throw $failure
}
function ById($root, [string]$id) {
    $condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function ByNameType($root, [string]$name, $type) {
    $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    @($all | Where-Object { $_.Current.Name -eq $name -and $_.Current.ControlType -eq $type }) | Select-Object -First 1
}
function Click($element, [IntPtr]$window) {
    if ([OrchardMonitoringDesktop]::GetForegroundWindow() -ne $window) { throw 'Catalog lost foreground; no Monitoring pointer input was sent.' }
    $bounds = $element.Current.BoundingRectangle
    if ($element.Current.IsOffscreen -or $bounds.IsEmpty -or $bounds.Width -le 0 -or $bounds.Height -le 0) { throw "Monitoring target $($element.Current.Name) is not visible." }
    $x = [int][math]::Floor($bounds.X + $bounds.Width / 2)
    $y = [int][math]::Floor($bounds.Y + $bounds.Height / 2)
    if (-not [OrchardMonitoringDesktop]::SetCursorPos($x, $y)) { throw 'Windows rejected Monitoring pointer position.' }
    Start-Sleep -Milliseconds 80
    [OrchardMonitoringDesktop]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardMonitoringDesktop]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    $report.physicalPointerInputSent = $true
    [ordered]@{ x = $x; y = $y; name = $element.Current.Name }
}
function PressKey([byte]$key, [IntPtr]$window) {
    if ([OrchardMonitoringDesktop]::GetForegroundWindow() -ne $window) { throw 'Catalog lost foreground; no Monitoring key was sent.' }
    [OrchardMonitoringDesktop]::keybd_event($key, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [OrchardMonitoringDesktop]::keybd_event($key, 0, 2, [UIntPtr]::Zero)
    $report.physicalKeyboardInputSent = $true
}
function CaptureWindow([IntPtr]$window, [string]$path) {
    $rect = [OrchardMonitoringDesktop+Rect]::new()
    if (-not [OrchardMonitoringDesktop]::GetWindowRect($window, [ref]$rect)) { throw 'Could not locate the Monitoring window for focus evidence.' }
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    $bitmap = [System.Drawing.Bitmap]::new($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, [System.Drawing.Size]::new($width, $height))
        $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}

$originalCursor = [OrchardMonitoringDesktop+Point]::new()
$cursorSaved = [OrchardMonitoringDesktop]::GetCursorPos([ref]$originalCursor)
$process = $null
try {
    $process = Start-Process -FilePath $exe -WorkingDirectory $app -PassThru
    $root = Wait-Until {
        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero) { return $null }
        [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    } 'The catalog did not expose a native window.'
    $themeSelector = ById $root 'ThemeSelector'
    $themeSelector.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
    $themeOption = Wait-Until {
        $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        @($all | Where-Object { $_.Current.Name -eq $Theme -and $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::ListItem -and -not $_.Current.IsOffscreen }) | Select-Object -First 1
    } "The $Theme theme option was unavailable."
    $themeOption.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $selectedTheme = @($themeSelector.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern).Current.GetSelection())
    if ($selectedTheme.Count -ne 1 -or $selectedTheme[0].Current.Name -ne $Theme) { throw 'Catalog did not select the requested theme.' }

    (Wait-Until { ByNameType $root 'Find a component' ([System.Windows.Automation.ControlType]::Edit) } 'Catalog search unavailable.').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Monitoring')
    $navigation = Wait-Until {
        $item = ByNameType $root 'Show Monitoring demo' ([System.Windows.Automation.ControlType]::Button)
        if ($item -and -not $item.Current.IsOffscreen) { $item }
    } 'Monitoring navigation unavailable.'
    [OrchardMonitoringDesktop]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
    Wait-Until { [OrchardMonitoringDesktop]::GetForegroundWindow() -eq $process.MainWindowHandle } 'Catalog did not become foreground.' | Out-Null
    $report.steps.navigationClick = Click $navigation $process.MainWindowHandle
    $initial = '0 selected · 0 local updates · 0 local reviews'
    Wait-Until {
        $summary = ByNameType $root $initial ([System.Windows.Automation.ControlType]::Text)
        if ($summary -and -not $summary.Current.IsOffscreen) { $summary }
    } 'Monitoring demo did not become visibly active.' | Out-Null
    $report.steps.initialSummary = $initial

    $checkboxName = 'Select fictional post: Harbor volunteers open a fictional reading room.'
    $checkbox = Wait-Until {
        $item = ByNameType $root $checkboxName ([System.Windows.Automation.ControlType]::CheckBox)
        if ($item -and -not $item.Current.IsOffscreen) { $item }
    } 'The first fictional post was not visible.'
    $report.steps.selectClick = Click $checkbox $process.MainWindowHandle
    $selected = '1 selected · 0 local updates · 0 local reviews'
    Wait-Until { ByNameType $root $selected ([System.Windows.Automation.ControlType]::Text) } 'Pointer selection did not reach the Monitoring summary.' | Out-Null
    if ($checkbox.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Current.ToggleState -ne [System.Windows.Automation.ToggleState]::On) { throw 'Native checkbox did not retain selection.' }
    $report.steps.selectedSummary = $selected

    $review = Wait-Until {
        $item = ByNameType $root 'Review selected' ([System.Windows.Automation.ControlType]::Button)
        if ($item -and $item.Current.IsEnabled -and -not $item.Current.IsOffscreen) { $item }
    } 'Review action did not enable after selection.'
    $review.SetFocus()
    Wait-Until { $review.Current.HasKeyboardFocus } 'Review action did not acquire keyboard focus.' | Out-Null
    PressKey 0x0D $process.MainWindowHandle
    $record = Wait-Until {
        $item = ByNameType $root 'Record local review' ([System.Windows.Automation.ControlType]::Button)
        if ($item -and -not $item.Current.IsOffscreen) { $item }
    } 'Keyboard Enter did not open the local review overlay.'
    $report.steps.reviewOpenedByEnter = $record.Current.Name
    $focusedReview = Wait-Until {
        $focused = [System.Windows.Automation.AutomationElement]::FocusedElement
        if ($focused.Current.AutomationId -eq 'Instructions') { $focused }
    } 'The visible review did not move focus into its editor.'
    $report.steps.automaticReviewFocus = $focusedReview.Current.Name
    Start-Sleep -Milliseconds 200
    $focusImage = Join-Path $folder 'monitoring-review-focus.png'
    CaptureWindow $process.MainWindowHandle $focusImage
    $report.steps.reviewFocusScreenshotSha256 = (Get-FileHash -LiteralPath $focusImage -Algorithm SHA256).Hash.ToLowerInvariant()
    PressKey 0x1B $process.MainWindowHandle
    Wait-Until {
        $item = ByNameType $root 'Record local review' ([System.Windows.Automation.ControlType]::Button)
        if (-not $item -or $item.Current.IsOffscreen) { $true }
    } 'Keyboard Escape did not close the unchanged local review.' | Out-Null
    Wait-Until { [System.Windows.Automation.AutomationElement]::FocusedElement.Current.Name -eq 'Review selected' } 'Review close did not restore focus to its trigger.' | Out-Null
    if (-not (ByNameType $root $selected ([System.Windows.Automation.ControlType]::Text))) { throw 'Review cancellation lost the selected post.' }
    $report.steps.reviewClosedByEscape = $true
    $report.steps.focusAfterEscape = 'Review selected'

    $update = Wait-Until {
        $item = ByNameType $root 'Simulate local update' ([System.Windows.Automation.ControlType]::Button)
        if ($item -and -not $item.Current.IsOffscreen -and $item.Current.IsEnabled) { $item }
    } 'Local update action unavailable.'
    $report.steps.updateClick = Click $update $process.MainWindowHandle
    $updated = '1 selected · 1 local updates · 0 local reviews'
    Wait-Until { ByNameType $root $updated ([System.Windows.Automation.ControlType]::Text) } 'Physical update did not retain the selected post.' | Out-Null
    $report.steps.updatedSummary = $updated
    $report.passed = $true
}
catch {
    $report.error = $_.Exception.Message
    throw
}
finally {
    if ($process) {
        if (-not $process.HasExited) {
            $process.CloseMainWindow() | Out-Null
            if (-not $process.WaitForExit(5000)) { Stop-Process -Id $process.Id -Force }
        }
        $report.processExited = (Get-Process -Id $process.Id -ErrorAction SilentlyContinue) -eq $null
    }
    if ($cursorSaved) { [OrchardMonitoringDesktop]::SetCursorPos($originalCursor.X, $originalCursor.Y) | Out-Null }
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $folder 'monitoring-desktop.json') -Encoding utf8
}
