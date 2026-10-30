param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [Parameter(Mandatory)][string]$ExpectedLibrarySha256,
    [Parameter(Mandatory)][ValidateSet('Light', 'Dark')][string]$Theme,
    [Parameter(Mandatory)][string]$Output,
    [ValidatePattern('^[A-Za-z0-9-]+$')][string]$RunId = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class OrchardTooltipDesktopInput {
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
$suffix = if ($RunId) { "-$RunId" } else { '' }
$reportPath = Join-Path $folder "tooltip-desktop$suffix-$Theme.json"
if (Test-Path -LiteralPath $reportPath) { throw 'Preserve the previous Tooltip report.' }
foreach ($path in @($exe, $dll)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing catalog input: $path" } }
$libraryHash = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
if ($libraryHash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'Catalog Orchard.dll does not match the expected package.' }
if (@(Get-Process Orchard.Catalog -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count) { throw 'Close this catalog before its isolated input check.' }
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$report = [ordered]@{ format='orchard-tooltip-desktop-v1'; theme=$Theme; catalogExeSha256=(Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant(); librarySha256=$libraryHash; passed=$false; physicalPointerSent=$false; physicalKeyboardSent=$false; physicalEscapeSent=$false; osSettingsChanged=$false; steps=[ordered]@{} }

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
    if ([OrchardTooltipDesktopInput]::GetForegroundWindow() -ne $window) { throw 'Catalog lost the foreground; no physical input was sent.' }
}
function Move-Pointer([int]$x, [int]$y, [IntPtr]$window) {
    Ensure-Foreground $window
    if (-not [OrchardTooltipDesktopInput]::SetCursorPos($x, $y)) { throw 'Windows rejected the pointer position.' }
    [OrchardTooltipDesktopInput]::mouse_event(1, 1, 0, 0, [UIntPtr]::Zero)
    $report.physicalPointerSent = $true
}

$process = $null
try {
    $process = Start-Process -FilePath $exe -WorkingDirectory $app -PassThru
    Wait-For { $process.Refresh(); -not $process.HasExited -and $process.MainWindowHandle -ne [IntPtr]::Zero } 'Catalog did not expose a native window.' | Out-Null
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    [OrchardTooltipDesktopInput]::MoveWindow($process.MainWindowHandle, 30, 30, 1600, 1000, $true) | Out-Null
    [OrchardTooltipDesktopInput]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
    $selector = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'ThemeSelector'))
    $selector.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
    Wait-For { $script:option = Find-VisibleName $root $Theme ([System.Windows.Automation.ControlType]::ListItem); $script:option } 'Theme option was not visible.' | Out-Null
    $option.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $report.steps.selectedTheme = @($selector.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern).Current.GetSelection())[0].Current.Name
    if ($report.steps.selectedTheme -ne $Theme) { throw 'Catalog did not switch theme.' }
    (Find-VisibleName $root 'Find a component' ([System.Windows.Automation.ControlType]::Edit)).GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Tooltips')
    Wait-For { $script:nav = Find-VisibleName $root 'Show Tooltips' ([System.Windows.Automation.ControlType]::Button); $script:nav } 'Tooltips navigation unavailable.' | Out-Null
    $nav.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { $script:trigger = Find-VisibleName $root 'Top' ([System.Windows.Automation.ControlType]::Button); $script:trigger } 'Top tooltip trigger unavailable.' | Out-Null
    $bounds = $trigger.Current.BoundingRectangle
    if ($bounds.IsEmpty) { throw 'Top tooltip trigger has no bounds.' }
    $report.steps.triggerBounds = @{ x=$bounds.X; y=$bounds.Y; width=$bounds.Width; height=$bounds.Height }
    $centerX = [int]($bounds.X + $bounds.Width / 2)
    $centerY = [int]($bounds.Y + $bounds.Height / 2)
    Move-Pointer ($centerX - 120) $centerY $process.MainWindowHandle
    Start-Sleep -Milliseconds 250
    for ($x = $centerX - 100; $x -lt $centerX; $x += 20) {
        Move-Pointer $x $centerY $process.MainWindowHandle
        Start-Sleep -Milliseconds 30
    }
    Move-Pointer $centerX $centerY $process.MainWindowHandle
    Wait-For { Find-VisibleName ([System.Windows.Automation.AutomationElement]::RootElement) 'Top tooltip' ([System.Windows.Automation.ControlType]::Text) } 'Physical hover did not expose the Top tooltip.' | Out-Null
    $report.steps.hoverOpened = $true
    foreach ($offset in @(10, 30, 70)) {
        Move-Pointer ([int]($bounds.X + $bounds.Width / 2)) ([int]($bounds.Bottom + $offset)) $process.MainWindowHandle
        Start-Sleep -Milliseconds 30
    }
    Wait-For { -not (Find-VisibleName ([System.Windows.Automation.AutomationElement]::RootElement) 'Top tooltip' ([System.Windows.Automation.ControlType]::Text)) } 'Leaving the trigger did not close the Top tooltip.' | Out-Null
    $report.steps.pointerLeaveClosed = $true
    $trigger.SetFocus()
    Ensure-Foreground $process.MainWindowHandle
    [OrchardTooltipDesktopInput]::keybd_event(0x09, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardTooltipDesktopInput]::keybd_event(0x09, 0, 2, [UIntPtr]::Zero)
    Wait-For { $script:bottom = Find-VisibleName $root 'Bottom' ([System.Windows.Automation.ControlType]::Button); $script:bottom -and $script:bottom.Current.HasKeyboardFocus } 'Tab did not focus the Bottom trigger.' | Out-Null
    [OrchardTooltipDesktopInput]::keybd_event(0x10, 0, 0, [UIntPtr]::Zero)
    try {
        [OrchardTooltipDesktopInput]::keybd_event(0x09, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 40
        [OrchardTooltipDesktopInput]::keybd_event(0x09, 0, 2, [UIntPtr]::Zero)
    } finally { [OrchardTooltipDesktopInput]::keybd_event(0x10, 0, 2, [UIntPtr]::Zero) }
    $report.physicalKeyboardSent = $true
    Wait-For { $trigger.Current.HasKeyboardFocus } 'Shift+Tab did not return focus to the Top trigger.' | Out-Null
    Wait-For { Find-VisibleName ([System.Windows.Automation.AutomationElement]::RootElement) 'Top tooltip' ([System.Windows.Automation.ControlType]::Text) } 'Keyboard focus did not expose the Top tooltip.' | Out-Null
    $report.steps.focusOpened = $true
    Ensure-Foreground $process.MainWindowHandle
    [OrchardTooltipDesktopInput]::keybd_event(0x1B, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardTooltipDesktopInput]::keybd_event(0x1B, 0, 2, [UIntPtr]::Zero)
    $report.physicalEscapeSent = $true
    Wait-For { -not (Find-VisibleName ([System.Windows.Automation.AutomationElement]::RootElement) 'Top tooltip' ([System.Windows.Automation.ControlType]::Text)) } 'Escape did not close the Top tooltip.' | Out-Null
    $report.steps.escapeClosed = $true
    $report.passed = $true
} catch {
    $report.error = $_.Exception.Message
    if ($process -and -not $process.HasExited) {
        Add-Type -AssemblyName System.Drawing
        $image = [System.Drawing.Bitmap]::new(1600, 1000)
        $graphics = [System.Drawing.Graphics]::FromImage($image)
        try { $graphics.CopyFromScreen(30, 30, 0, 0, $image.Size); $image.Save((Join-Path $folder "tooltip-failure$suffix-$Theme.png")) }
        finally { $graphics.Dispose(); $image.Dispose() }
    }
    throw
}
finally {
    if ($process -and -not $process.HasExited) { $process.CloseMainWindow() | Out-Null; if (-not $process.WaitForExit(5000)) { Stop-Process -Id $process.Id -Force } }
    $report.processExited = if ($process) { -not [bool](Get-Process -Id $process.Id -ErrorAction SilentlyContinue) } else { $true }
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $reportPath -Encoding utf8
}
