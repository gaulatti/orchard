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
public static class OrchardEditorialDesktop
{
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left; public int Top; public int Right; public int Bottom; }
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}
'@

$app = (Resolve-Path -LiteralPath $AppDirectory).Path
$exe = Join-Path $app 'Orchard.Catalog.exe'
$library = Join-Path $app 'Orchard.dll'
$folder = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $folder) { throw 'Use a fresh Editorial desktop output directory.' }
foreach ($path in @($exe, $library)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing Editorial desktop input: $path" } }
$hash = (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'The catalog contains different Orchard library bytes.' }
if (@(Get-Process Orchard.Catalog -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count) { throw 'Close this catalog before its isolated Editorial check.' }
New-Item -ItemType Directory -Path $folder | Out-Null
$report = [ordered]@{
    format = 'orchard-editorial-search-desktop-v1'
    passed = $false
    requestedTheme = $Theme
    catalogExeSha256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    orchardDllSha256 = $hash
    physicalPointerInputSent = $false
    physicalKeyboardInputSent = $false
    osSettingsChanged = $false
    steps = [ordered]@{}
}
function Wait-Until([scriptblock]$condition, [string]$failure) {
    $deadline = [DateTime]::UtcNow.AddSeconds(12)
    do {
        try { $value = & $condition; if ($value) { return $value } } catch { }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)
    throw $failure
}
function Find($root, [string]$name, $type) {
    $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    @($all | Where-Object { $_.Current.Name -eq $name -and $_.Current.ControlType -eq $type -and -not $_.Current.IsOffscreen }) | Select-Object -First 1
}
function Click($element, [IntPtr]$window, [double]$verticalFraction = 0.5) {
    if ([OrchardEditorialDesktop]::GetForegroundWindow() -ne $window) { throw 'Catalog lost foreground; no pointer input was sent.' }
    $rect = $element.Current.BoundingRectangle
    if ($element.Current.IsOffscreen -or $rect.IsEmpty) { throw "Editorial target $($element.Current.Name) is not visible." }
    $x = [int][math]::Floor($rect.X + $rect.Width / 2)
    $y = [int][math]::Floor($rect.Y + $rect.Height * $verticalFraction)
    if (-not [OrchardEditorialDesktop]::SetCursorPos($x, $y)) { throw 'Windows rejected pointer position.' }
    Start-Sleep -Milliseconds 80
    [OrchardEditorialDesktop]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardEditorialDesktop]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    $report.physicalPointerInputSent = $true
}
function Key([byte]$code, [IntPtr]$window, [bool]$control = $false) {
    if ([OrchardEditorialDesktop]::GetForegroundWindow() -ne $window) { throw 'Catalog lost foreground; no keyboard input was sent.' }
    if ($control) { [OrchardEditorialDesktop]::keybd_event(0x11, 0, 0, [UIntPtr]::Zero) }
    [OrchardEditorialDesktop]::keybd_event($code, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardEditorialDesktop]::keybd_event($code, 0, 2, [UIntPtr]::Zero)
    if ($control) { [OrchardEditorialDesktop]::keybd_event(0x11, 0, 2, [UIntPtr]::Zero) }
    $report.physicalKeyboardInputSent = $true
}
function Capture([IntPtr]$window, [string]$path) {
    $rect = [OrchardEditorialDesktop+Rect]::new()
    if (-not [OrchardEditorialDesktop]::GetWindowRect($window, [ref]$rect)) { throw 'Could not locate Editorial window.' }
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
$process = $null
try {
    $process = Start-Process -FilePath $exe -WorkingDirectory $app -PassThru
    $root = Wait-Until {
        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero) { return $null }
        [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    } 'The catalog did not expose a native window.'
    $themeSelector = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'ThemeSelector'))
    if (-not $themeSelector) { throw 'Catalog theme selector unavailable.' }
    $themeSelector.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
    $themeOption = Wait-Until { Find $root $Theme ([System.Windows.Automation.ControlType]::ListItem) } "The $Theme theme option was unavailable."
    $themeOption.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $selectedTheme = @($themeSelector.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern).Current.GetSelection())
    if ($selectedTheme.Count -ne 1 -or $selectedTheme[0].Current.Name -ne $Theme) { throw 'Catalog did not select the requested theme.' }
    $report.steps.selectedTheme = $Theme
    (Wait-Until { Find $root 'Find a component' ([System.Windows.Automation.ControlType]::Edit) } 'Catalog search unavailable.').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Editorial')
    $navigation = Wait-Until { Find $root 'Show Editorial demo' ([System.Windows.Automation.ControlType]::Button) } 'Editorial navigation unavailable.'
    [OrchardEditorialDesktop]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
    Wait-Until { [OrchardEditorialDesktop]::GetForegroundWindow() -eq $process.MainWindowHandle } 'Catalog did not become foreground.' | Out-Null
    Click $navigation $process.MainWindowHandle
    Wait-Until { Find $root '4 matching pages · 0 selected · Local sample' ([System.Windows.Automation.ControlType]::Text) } 'Editorial initial state unavailable.' | Out-Null
    $report.steps.initialSummary = '4 matching pages · 0 selected · Local sample'

    $checkbox = Wait-Until { Find $root 'Select fictional page: Harbor reading room' ([System.Windows.Automation.ControlType]::CheckBox) } 'Harbor row unavailable.'
    Click $checkbox $process.MainWindowHandle
    Wait-Until { Find $root '4 matching pages · 1 selected · Local sample' ([System.Windows.Automation.ControlType]::Text) } 'Pointer selection did not update the summary.' | Out-Null
    $report.steps.selectedSummary = '4 matching pages · 1 selected · Local sample'

    $search = Wait-Until { Find $root 'Search fictional pages' ([System.Windows.Automation.ControlType]::Edit) } 'Editorial search unavailable.'
    $report.steps.searchCandidates = @($root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition) | Where-Object { $_.Current.Name -eq 'Search fictional pages' } | ForEach-Object { [ordered]@{ id = $_.Current.AutomationId; type = $_.Current.ControlType.ProgrammaticName; bounds = $_.Current.BoundingRectangle.ToString(); offscreen = $_.Current.IsOffscreen } })
    Click $search $process.MainWindowHandle 0.2
    $focus = [System.Windows.Automation.AutomationElement]::FocusedElement
    $report.steps.focusAfterSearchClick = [ordered]@{ name = $focus.Current.Name; id = $focus.Current.AutomationId; type = $focus.Current.ControlType.ProgrammaticName }
    Wait-Until { [System.Windows.Automation.AutomationElement]::FocusedElement.Current.Name -eq 'Search fictional pages' } 'Pointer click did not focus search.' | Out-Null
    foreach ($code in @(0x47, 0x41, 0x52, 0x44, 0x45, 0x4E)) { Key $code $process.MainWindowHandle }
    $query = $search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
    $report.steps.typedQuery = $query
    if ($query -ne 'garden') { throw "Physical keyboard search produced '$query'." }
    Wait-Until { Find $root '1 matching pages · 1 selected · Local sample' ([System.Windows.Automation.ControlType]::Text) } 'Keyboard search did not filter visible pages while preserving selection.' | Out-Null
    if (Find $root 'Select fictional page: Harbor reading room' ([System.Windows.Automation.ControlType]::CheckBox)) { throw 'Filtered Harbor row remained visible.' }
    if (-not (Find $root 'Select fictional page: Community garden' ([System.Windows.Automation.ControlType]::CheckBox))) { throw 'Garden row was not visible after filtering.' }
    $report.steps.filteredSummary = '1 matching pages · 1 selected · Local sample'
    $image = Join-Path $folder 'editorial-filtered.png'
    Capture $process.MainWindowHandle $image
    $report.steps.filteredScreenshotSha256 = (Get-FileHash -LiteralPath $image -Algorithm SHA256).Hash.ToLowerInvariant()

    Key 0x41 $process.MainWindowHandle $true
    Key 0x08 $process.MainWindowHandle
    Wait-Until { Find $root '4 matching pages · 1 selected · Local sample' ([System.Windows.Automation.ControlType]::Text) } 'Clearing search did not restore rows and selection.' | Out-Null
    $restored = Wait-Until { Find $root 'Select fictional page: Harbor reading room' ([System.Windows.Automation.ControlType]::CheckBox) } 'Selected Harbor row was not restored.'
    if ($restored.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Current.ToggleState -ne [System.Windows.Automation.ToggleState]::On) { throw 'Harbor selection was lost after filtering.' }
    $report.steps.restoredSummary = '4 matching pages · 1 selected · Local sample'
    $report.passed = $true
}
catch {
    $report.error = $_.Exception.Message
    throw
}
finally {
    if ($process -and -not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(5000)) { Stop-Process -Id $process.Id -Force }
    }
    $report.processExited = if ($process) { -not [bool](Get-Process -Id $process.Id -ErrorAction SilentlyContinue) } else { $true }
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'editorial-search-desktop.json') -Encoding utf8
}
