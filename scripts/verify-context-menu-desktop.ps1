param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [Parameter(Mandatory)][string]$Output,
    [Parameter(Mandatory)][string]$ExpectedLibrarySha256,
    [switch]$Nonmodal,
    [switch]$CheckOutside,
    [switch]$Owned
)

$ErrorActionPreference = 'Stop'
if ($CheckOutside -and -not $Nonmodal) { throw 'Outside action check requires nonmodal mode.' }
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class OrchardContextMenuDesktopInput {
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr window, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}
'@

$app = (Resolve-Path -LiteralPath $AppDirectory).Path
$exe = Join-Path $app 'Orchard.Catalog.exe'
$library = Join-Path $app 'Orchard.dll'
$folder = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $folder) { throw 'Use a fresh Context Menu desktop output directory.' }
foreach ($path in @($exe, $library)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing catalog input: $path" } }
$libraryHash = (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()
if ($libraryHash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'Catalog Orchard.dll differs from the expected published package.' }
if (@(Get-Process Orchard.Catalog -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count) { throw 'Close this catalog before its isolated input check.' }
New-Item -ItemType Directory -Path $folder | Out-Null
$report = [ordered]@{
    format = 'orchard-context-menu-desktop-v1'
    catalogExeSha256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    orchardDllSha256 = $libraryHash
    passed = $false
    physicalPointerSent = $false
    physicalKeyboardSent = $false
    osSettingsChanged = $false
    componentAccepted = $false
    nonmodal = [bool]$Nonmodal
    owned = [bool]$Owned
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
function ByName($root, [string]$name, $type) {
    $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    @($all | Where-Object { $_.Current.Name -eq $name -and $_.Current.ControlType -eq $type -and -not $_.Current.IsOffscreen }) | Select-Object -First 1
}
function Ensure-Foreground([IntPtr]$window) {
    if ([OrchardContextMenuDesktopInput]::GetForegroundWindow() -ne $window) { throw 'Catalog lost foreground; no physical input was sent.' }
}
function Pointer($element, [IntPtr]$window, [bool]$right) {
    Ensure-Foreground $window
    $bounds = $element.Current.BoundingRectangle
    if ($bounds.IsEmpty -or $element.Current.IsOffscreen) { throw "Pointer target is not visible: $($element.Current.Name)" }
    $x = [int][math]::Floor($bounds.X + $bounds.Width / 2)
    $y = [int][math]::Floor($bounds.Y + $bounds.Height / 2)
    if (-not [OrchardContextMenuDesktopInput]::SetCursorPos($x, $y)) { throw 'Windows rejected the pointer position.' }
    Start-Sleep -Milliseconds 80
    $down = if ($right) { 8 } else { 2 }
    $up = if ($right) { 16 } else { 4 }
    [OrchardContextMenuDesktopInput]::mouse_event($down, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardContextMenuDesktopInput]::mouse_event($up, 0, 0, 0, [UIntPtr]::Zero)
    $report.physicalPointerSent = $true
    [ordered]@{ x = $x; y = $y; button = if ($right) { 'right' } else { 'left' }; target = $element.Current.Name }
}
function Key([byte]$code) {
    [OrchardContextMenuDesktopInput]::keybd_event($code, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardContextMenuDesktopInput]::keybd_event($code, 0, 2, [UIntPtr]::Zero)
    $report.physicalKeyboardSent = $true
}

$process = $null
try {
    $process = Start-Process -FilePath $exe -WorkingDirectory $app -PassThru
    $root = Wait-Until {
        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero) { return $null }
        [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    } 'Catalog did not expose a native window.'
    [OrchardContextMenuDesktopInput]::MoveWindow($process.MainWindowHandle, 30, 30, 1450, 950, $true) | Out-Null
    [OrchardContextMenuDesktopInput]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
    (ByName $root 'Find a component' ([System.Windows.Automation.ControlType]::Edit)).GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Context menus')
    $nav = Wait-Until { ByName $root 'Show Context menus' ([System.Windows.Automation.ControlType]::Button) } 'Context Menu navigation unavailable.'
    $nav.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    if ($Owned) {
        $open = Wait-Until { ByName $root 'Open owned context example' ([System.Windows.Automation.ControlType]::Button) } 'Owned Context Menu trigger unavailable.'
        $open.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    }
    $targetName = if ($Owned) { 'Fictional owned context target' } else { 'Fictional context target' }
    $itemName = if ($Owned) { 'Inspect owned fictional item' } else { 'Inspect fictional item' }
    $statusName = if ($Owned) { 'No owned context action yet' } else { 'No fictional context action yet' }
    $outsideName = if ($Owned) { 'Fictional owned outside action' } else { 'Fictional outside action' }
    $outsideStatus = if ($Owned) { 'Owned outside action activated' } else { 'Outside action activated' }
    $target = Wait-Until { ByName $root $targetName ([System.Windows.Automation.ControlType]::Button) } 'Context Menu target unavailable.'
    $status = Wait-Until { ByName $root $statusName ([System.Windows.Automation.ControlType]::Text) } 'Context Menu status unavailable.'
    if ($Nonmodal) {
        $toggleName = if ($Owned) { 'Owned modal context menu' } else { 'Modal context menu' }
        $toggle = Wait-Until { ByName $root "$toggleName Outside actions are blocked" ([System.Windows.Automation.ControlType]::Button) } 'Context Menu mode switch unavailable.'
        $toggle.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle()
        Wait-Until { (ByName $root "$toggleName Outside actions remain available" ([System.Windows.Automation.ControlType]::Button)) } 'Catalog did not enter nonmodal mode.' | Out-Null
    }
    Ensure-Foreground $process.MainWindowHandle
    $outside = $null
    if ($CheckOutside) {
        $outside = Wait-Until { ByName $root $outsideName ([System.Windows.Automation.ControlType]::Button) } 'Catalog outside action unavailable.'
        $report.steps.outsideBaseline = Pointer $outside $process.MainWindowHandle $false
        Wait-Until { $status.Current.Name -eq $outsideStatus } 'Catalog outside button failed without an open menu.' | Out-Null
        $report.steps.outsideBaselineAction = $status.Current.Name
    }
    $target.SetFocus()
    Wait-Until { $target.Current.HasKeyboardFocus } 'Context target did not take focus.' | Out-Null
    [OrchardContextMenuDesktopInput]::keybd_event(0x10, 0, 0, [UIntPtr]::Zero)
    Key 0x79
    [OrchardContextMenuDesktopInput]::keybd_event(0x10, 0, 2, [UIntPtr]::Zero)
    $report.steps.keyboardOpen = [bool](Wait-Until { ByName $root $itemName ([System.Windows.Automation.ControlType]::MenuItem) } 'Shift+F10 did not expose the Context Menu.')
    Key 0x1B
    Wait-Until { -not (ByName $root $itemName ([System.Windows.Automation.ControlType]::MenuItem)) } 'Escape did not dismiss the Context Menu.' | Out-Null
    $report.steps.escapeDismissed = $true
    $report.steps.pointerOpen = Pointer $target $process.MainWindowHandle $true
    $item = Wait-Until { ByName $root $itemName ([System.Windows.Automation.ControlType]::MenuItem) } 'Physical right-click did not expose the native menu item.'
    $report.steps.menuItemVisible = -not $item.Current.IsOffscreen
    $report.steps.pointerSelect = Pointer $item $process.MainWindowHandle $false
    Wait-Until { $status.Current.Name -eq "Selected: $itemName" } 'Physical selection did not reach the original catalog Click handler.' | Out-Null
    Wait-Until { -not (ByName $root $itemName ([System.Windows.Automation.ControlType]::MenuItem)) } 'Selection did not dismiss the Context Menu.' | Out-Null
    $report.steps.originalAction = $status.Current.Name

    if ($CheckOutside) {
        $report.steps.pointerReopen = Pointer $target $process.MainWindowHandle $true
        Wait-Until { ByName $root $itemName ([System.Windows.Automation.ControlType]::MenuItem) } 'Nonmodal menu did not reopen.' | Out-Null
        $report.steps.outsideClick = Pointer $outside $process.MainWindowHandle $false
        Wait-Until { $status.Current.Name -eq $outsideStatus } 'First outside click did not reach the original catalog button.' | Out-Null
        Wait-Until { -not (ByName $root $itemName ([System.Windows.Automation.ControlType]::MenuItem)) } 'Outside click did not dismiss the nonmodal menu.' | Out-Null
        $report.steps.outsideAction = $status.Current.Name
    }

    if ($status.Current.Name -ne $(if ($CheckOutside) { $outsideStatus } else { "Selected: $itemName" })) { throw 'Caller action status changed unexpectedly.' }
    $report.passed = $true
} catch {
    $report.error = $_.Exception.Message
    if ($process -and -not $process.HasExited) {
        $bitmap = [System.Drawing.Bitmap]::new(1450, 950)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.CopyFromScreen(30, 30, 0, 0, $bitmap.Size)
            $bitmap.Save((Join-Path $folder 'failure.png'))
        } finally { $graphics.Dispose(); $bitmap.Dispose() }
        $tree = [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        @($tree | Where-Object { $_.Current.ProcessId -eq $process.Id -and $_.Current.Name -match 'fictional|Inspect|Context' } | ForEach-Object { [ordered]@{ name = $_.Current.Name; type = $_.Current.ControlType.ProgrammaticName; offscreen = $_.Current.IsOffscreen } }) | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $folder 'failure-tree.json') -Encoding utf8
    }
    throw
} finally {
    if ($process) {
        if (-not $process.HasExited) {
            $process.CloseMainWindow() | Out-Null
            if (-not $process.WaitForExit(5000)) { Stop-Process -Id $process.Id -Force }
        }
        $report.processExited = (Get-Process -Id $process.Id -ErrorAction SilentlyContinue) -eq $null
    }
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $folder 'context-menu-desktop.json') -Encoding utf8
}
