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
public static class OrchardSegmentedDesktopInput {
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr window, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}
'@

$app = (Resolve-Path -LiteralPath $AppDirectory).Path
$exe = Join-Path $app 'PackageConsumer.exe'
$library = Join-Path $app 'Orchard.dll'
$folder = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $folder) { throw 'Use a fresh Segmented Control output directory.' }
foreach ($path in @($exe, $library)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing package-consumer input: $path" } }
$hash = (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'Consumer Orchard.dll differs from the expected published package.' }
if (@(Get-Process PackageConsumer -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count) { throw 'Close this consumer before its isolated input check.' }
New-Item -ItemType Directory -Path $folder | Out-Null
$report = [ordered]@{
    format = 'orchard-segmented-control-consumer-desktop-v1'
    theme = $Theme
    packageConsumerExeSha256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    orchardDllSha256 = $hash
    passed = $false
    physicalPointerAndKeyboardSent = $false
    osSettingsChanged = $false
    componentAccepted = $false
    steps = [ordered]@{}
}

function Wait-Until([scriptblock]$condition, [string]$failure) {
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    do {
        try { $value = & $condition; if ($value) { return $value } } catch { }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)
    throw $failure
}
function ById($root, [string]$id) {
    $condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function Option($root, [string]$name) {
    $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    @($all | Where-Object { $_.Current.Name -eq $name -and $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::RadioButton }) | Select-Object -First 1
}
function Ensure-Foreground([IntPtr]$window) {
    if ([OrchardSegmentedDesktopInput]::GetForegroundWindow() -ne $window) { throw 'Consumer lost foreground; no input was sent.' }
}
function Key([byte]$code, [IntPtr]$window) {
    Ensure-Foreground $window
    [OrchardSegmentedDesktopInput]::keybd_event($code, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [OrchardSegmentedDesktopInput]::keybd_event($code, 0, 2, [UIntPtr]::Zero)
    $report.physicalPointerAndKeyboardSent = $true
}
function Click($element, [IntPtr]$window) {
    Ensure-Foreground $window
    $bounds = $element.Current.BoundingRectangle
    if ($bounds.IsEmpty -or $element.Current.IsOffscreen) { throw "Segment option is not visible: $($element.Current.Name)" }
    $x = [int][math]::Floor($bounds.X + $bounds.Width / 2)
    $y = [int][math]::Floor($bounds.Y + $bounds.Height / 2)
    if (-not [OrchardSegmentedDesktopInput]::SetCursorPos($x, $y)) { throw 'Windows rejected the pointer position.' }
    Start-Sleep -Milliseconds 70
    [OrchardSegmentedDesktopInput]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardSegmentedDesktopInput]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    $report.physicalPointerAndKeyboardSent = $true
    [ordered]@{ x = $x; y = $y; option = $element.Current.Name }
}

$process = $null
try {
    $process = Start-Process -FilePath $exe -WorkingDirectory $app -ArgumentList @('--probe-segmented-foreground', '--segmented-theme', $Theme) -PassThru
    $root = Wait-Until {
        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero) { return $null }
        [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    } 'Independent consumer did not expose a native window.'
    [OrchardSegmentedDesktopInput]::MoveWindow($process.MainWindowHandle, 30, 30, 800, 420, $true) | Out-Null
    [OrchardSegmentedDesktopInput]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
    $group = Wait-Until { ById $root 'RtlSegmentedGroup' } 'RTL group unavailable.'
    $status = Wait-Until { ById $root 'SegmentedSelectionStatus' } 'Caller status unavailable.'
    $east = Wait-Until { Option $group 'East' } 'East option unavailable.'
    $west = Wait-Until { Option $group 'West' } 'West option unavailable.'
    $closed = Wait-Until { Option $group 'Closed' } 'Disabled option unavailable.'
    if ($closed.Current.IsEnabled -or $status.Current.Name -ne 'Selection: east') { throw 'RTL fixture did not begin with East selected and Closed disabled.' }
    $report.steps.visualOrder = @($west.Current.BoundingRectangle.X, $east.Current.BoundingRectangle.X, $closed.Current.BoundingRectangle.X)
    if (-not ($closed.Current.BoundingRectangle.X -lt $west.Current.BoundingRectangle.X -and $west.Current.BoundingRectangle.X -lt $east.Current.BoundingRectangle.X)) { throw 'RTL option geometry is not visually reversed.' }
    Ensure-Foreground $process.MainWindowHandle
    $east.SetFocus()
    Wait-Until { $east.Current.HasKeyboardFocus } 'East did not acquire focus.' | Out-Null
    Key 0x25 $process.MainWindowHandle
    Wait-Until { $west.Current.HasKeyboardFocus -and $status.Current.Name -eq 'Selection: west' } 'RTL Left did not move to and select West.' | Out-Null
    $report.steps.leftSelected = $status.Current.Name
    Key 0x27 $process.MainWindowHandle
    Wait-Until { $east.Current.HasKeyboardFocus -and $status.Current.Name -eq 'Selection: east' } 'RTL Right did not move to and select East.' | Out-Null
    $report.steps.rightSelected = $status.Current.Name
    Key 0x23 $process.MainWindowHandle
    Wait-Until { $west.Current.HasKeyboardFocus -and $status.Current.Name -eq 'Selection: west' } 'End did not select last enabled option.' | Out-Null
    Key 0x24 $process.MainWindowHandle
    Wait-Until { $east.Current.HasKeyboardFocus -and $status.Current.Name -eq 'Selection: east' } 'Home did not select first enabled option.' | Out-Null
    [OrchardSegmentedDesktopInput]::keybd_event(0x11, 0, 0, [UIntPtr]::Zero)
    try { Key 0x25 $process.MainWindowHandle }
    finally { [OrchardSegmentedDesktopInput]::keybd_event(0x11, 0, 2, [UIntPtr]::Zero) }
    Wait-Until { $west.Current.HasKeyboardFocus -and $status.Current.Name -eq 'Selection: east' } 'Control+Left changed selection or missed RTL focus.' | Out-Null
    $report.steps.controlLeftKeptSelection = $status.Current.Name
    Key 0x20 $process.MainWindowHandle
    Wait-Until { $west.Current.HasKeyboardFocus -and $status.Current.Name -eq 'Selection: west' } 'Space did not activate the focused West option.' | Out-Null
    $report.steps.spaceSelected = $status.Current.Name
    $report.steps.pointerSelectedEast = Click $east $process.MainWindowHandle
    Wait-Until { $status.Current.Name -eq 'Selection: east' } 'Pointer did not select East.' | Out-Null
    $bounds = $root.Current.BoundingRectangle
    $bitmap = [System.Drawing.Bitmap]::new([int]$bounds.Width, [int]$bounds.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try { $graphics.CopyFromScreen([int]$bounds.X, [int]$bounds.Y, 0, 0, $bitmap.Size); $bitmap.Save((Join-Path $folder 'segmented-consumer.png')) }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
    $report.passed = $true
} catch {
    $report.error = $_.Exception.Message
    throw
} finally {
    if ($process) {
        if (-not $process.HasExited) {
            $process.CloseMainWindow() | Out-Null
            if (-not $process.WaitForExit(5000)) { Stop-Process -Id $process.Id -Force }
        }
        $report.processExited = (Get-Process -Id $process.Id -ErrorAction SilentlyContinue) -eq $null
        $report.processExitCode = $process.ExitCode
        if ($report.processExitCode -ne 0) { $report.passed = $false }
    }
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $folder 'segmented-consumer-desktop.json') -Encoding utf8
}
if (-not $report.passed) { throw 'Segmented Control physical consumer check did not pass.' }
