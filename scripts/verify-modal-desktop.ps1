param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [Parameter(Mandatory)][string]$ExpectedLibrarySha256,
    [Parameter(Mandatory)][ValidateSet('Light', 'Dark')][string]$Theme,
    [Parameter(Mandatory)][string]$Output
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class OrchardModalDesktopInput {
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
$dll = Join-Path $app 'Orchard.dll'
$folder = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $folder) { throw 'Use a fresh Modal output directory.' }
foreach ($path in @($exe, $dll)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing catalog input: $path" } }
$libraryHash = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
if ($libraryHash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'Catalog Orchard.dll does not match the expected package.' }
if (@(Get-Process Orchard.Catalog -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count) { throw 'Close this catalog before its isolated input check.' }
New-Item -ItemType Directory -Path $folder | Out-Null
$report = [ordered]@{ format='orchard-modal-desktop-v1'; theme=$Theme; catalogExeSha256=(Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant(); librarySha256=$libraryHash; passed=$false; physicalPointerSent=$false; physicalEscapeSent=$false; osSettingsChanged=$false; steps=[ordered]@{} }

function Wait-For([scriptblock]$condition, [string]$failure) {
    $until = [DateTime]::UtcNow.AddSeconds(12)
    do { try { if (& $condition) { return $true } } catch { }; Start-Sleep -Milliseconds 100 } while ([DateTime]::UtcNow -lt $until)
    throw $failure
}
function Find-VisibleName($root, [string]$name, $type) {
    $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    @($all | Where-Object { $_.Current.ProcessId -eq $process.Id -and $_.Current.Name -eq $name -and $_.Current.ControlType -eq $type -and -not $_.Current.IsOffscreen }) | Select-Object -First 1
}
function Ensure-Foreground([IntPtr]$window) {
    if ([OrchardModalDesktopInput]::GetForegroundWindow() -ne $window) { throw 'Catalog lost the foreground; no physical input was sent.' }
}
function Click($element, [IntPtr]$window) {
    Ensure-Foreground $window
    $r = $element.Current.BoundingRectangle
    if ($r.IsEmpty -or $element.Current.IsOffscreen) { throw "Target is not visible: $($element.Current.Name)" }
    $x = [int]($r.X + $r.Width / 2); $y = [int]($r.Y + $r.Height / 2)
    if (-not [OrchardModalDesktopInput]::SetCursorPos($x - 10, $y)) { throw 'Windows rejected the pointer position.' }
    Start-Sleep -Milliseconds 50
    if (-not [OrchardModalDesktopInput]::SetCursorPos($x, $y)) { throw 'Windows rejected the pointer position.' }
    [OrchardModalDesktopInput]::mouse_event(1, 1, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 80
    [OrchardModalDesktopInput]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardModalDesktopInput]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    $report.physicalPointerSent = $true
}
function Escape([IntPtr]$window) {
    Ensure-Foreground $window
    [OrchardModalDesktopInput]::keybd_event(0x1B, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardModalDesktopInput]::keybd_event(0x1B, 0, 2, [UIntPtr]::Zero)
    $report.physicalEscapeSent = $true
}

$process = $null
try {
    $process = Start-Process -FilePath $exe -WorkingDirectory $app -PassThru
    Wait-For { $process.Refresh(); -not $process.HasExited -and $process.MainWindowHandle -ne [IntPtr]::Zero } 'Catalog did not expose a native window.' | Out-Null
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    [OrchardModalDesktopInput]::MoveWindow($process.MainWindowHandle, 30, 30, 1600, 1000, $true) | Out-Null
    [OrchardModalDesktopInput]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
    $selector = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'ThemeSelector'))
    $selector.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
    Wait-For { $script:option = Find-VisibleName $root $Theme ([System.Windows.Automation.ControlType]::ListItem); $script:option } 'Theme option was not visible.' | Out-Null
    $option.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $report.steps.selectedTheme = @($selector.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern).Current.GetSelection())[0].Current.Name
    if ($report.steps.selectedTheme -ne $Theme) { throw 'Catalog did not switch theme.' }
    (Find-VisibleName $root 'Find a component' ([System.Windows.Automation.ControlType]::Edit)).GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Modal')
    Wait-For { $script:nav = Find-VisibleName $root 'Show Modal' ([System.Windows.Automation.ControlType]::Button); $script:nav } 'Modal navigation unavailable.' | Out-Null
    $nav.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { $script:open = Find-VisibleName $root 'Open component example' ([System.Windows.Automation.ControlType]::Button); $script:open } 'Modal opener unavailable.' | Out-Null
    Click $open $process.MainWindowHandle
    Wait-For { Find-VisibleName $root 'Fictional component demonstration' ([System.Windows.Automation.ControlType]::Text) } 'Pointer click did not open the Modal.' | Out-Null
    $report.steps.parentOpened = $true
    Wait-For { $script:nestedOpen = Find-VisibleName $root 'Open nested confirmation' ([System.Windows.Automation.ControlType]::Button); $script:nestedOpen } 'Nested opener unavailable.' | Out-Null
    Click $nestedOpen $process.MainWindowHandle
    Wait-For { Find-VisibleName $root 'Fictional nested confirmation' ([System.Windows.Automation.ControlType]::Text) } 'Pointer click did not open the nested dialog.' | Out-Null
    $report.steps.nestedOpened = $true
    Escape $process.MainWindowHandle
    Wait-For { -not (Find-VisibleName $root 'Fictional nested confirmation' ([System.Windows.Automation.ControlType]::Text)) } 'Escape did not close the nested dialog.' | Out-Null
    if (-not (Find-VisibleName $root 'Fictional component demonstration' ([System.Windows.Automation.ControlType]::Text))) { throw 'Closing the nested dialog also closed its parent Modal.' }
    $report.steps.firstEscapeClosedOnlyNested = $true
    Escape $process.MainWindowHandle
    Wait-For { -not (Find-VisibleName $root 'Fictional component demonstration' ([System.Windows.Automation.ControlType]::Text)) } 'Second Escape did not close the parent Modal.' | Out-Null
    Wait-For { Find-VisibleName $root 'Close requests: 1' ([System.Windows.Automation.ControlType]::Text) } 'Parent close request did not reach the catalog caller exactly once.' | Out-Null
    Wait-For { $open.Current.HasKeyboardFocus } 'Closing the parent Modal did not restore focus to its opener.' | Out-Null
    $report.steps.secondEscapeClosedParent = $true
    $report.steps.callerCloseRequests = 1
    $report.steps.focusRestoredToOpener = $true
    $report.passed = $true
} catch {
    $report.error = $_.Exception.Message
    if ($process -and -not $process.HasExited) {
        Add-Type -AssemblyName System.Drawing
        $image = [System.Drawing.Bitmap]::new(1600, 1000)
        $graphics = [System.Drawing.Graphics]::FromImage($image)
        try { $graphics.CopyFromScreen(30, 30, 0, 0, $image.Size); $image.Save((Join-Path $folder 'modal-failure.png')) }
        finally { $graphics.Dispose(); $image.Dispose() }
    }
    throw
}
finally {
    if ($process -and -not $process.HasExited) { $process.CloseMainWindow() | Out-Null; if (-not $process.WaitForExit(5000)) { Stop-Process -Id $process.Id -Force } }
    $report.processExited = if ($process) { -not [bool](Get-Process -Id $process.Id -ErrorAction SilentlyContinue) } else { $true }
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'modal-desktop.json') -Encoding utf8
}
