param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [Parameter(Mandatory)][string]$ExpectedLibrarySha256,
    [Parameter(Mandatory)][ValidateSet('Light', 'Dark')][string]$Theme,
    [Parameter(Mandatory)][string]$Output,
    [switch]$CheckTooltip
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class OrchardIconDesktopInput {
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
if (Test-Path -LiteralPath $folder) { throw 'Use a fresh Icon Button output directory.' }
foreach ($path in @($exe, $dll)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing catalog input: $path" } }
$libraryHash = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
if ($libraryHash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'Catalog Orchard.dll does not match the expected package.' }
if (@(Get-Process Orchard.Catalog -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count) { throw 'Close this catalog before its isolated input check.' }
New-Item -ItemType Directory -Path $folder | Out-Null
$report = [ordered]@{ format='orchard-icon-button-desktop-v1'; theme=$Theme; catalogExeSha256=(Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant(); librarySha256=$libraryHash; passed=$false; pointerSent=$false; keyboardSent=$false; tooltipChecked=[bool]$CheckTooltip; osSettingsChanged=$false; steps=[ordered]@{} }

function Wait-For([scriptblock]$condition, [string]$failure) {
    $until = [DateTime]::UtcNow.AddSeconds(12)
    do { try { $value = & $condition; if ($value) { return $value } } catch { }; Start-Sleep -Milliseconds 100 } while ([DateTime]::UtcNow -lt $until)
    throw $failure
}
function Find-Name($root, [string]$name, $type) {
    $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    @($all | Where-Object { $_.Current.Name -eq $name -and $_.Current.ControlType -eq $type -and -not $_.Current.IsOffscreen }) | Select-Object -First 1
}
function Ensure-Foreground([IntPtr]$window) {
    if ([OrchardIconDesktopInput]::GetForegroundWindow() -ne $window) { throw 'Catalog lost the foreground; no physical input was sent.' }
}
function Click($element, [IntPtr]$window) {
    Ensure-Foreground $window
    $r = $element.Current.BoundingRectangle
    if ($r.IsEmpty -or $element.Current.IsOffscreen) { throw "Target is not visible: $($element.Current.Name)" }
    $x = [int]($r.X + $r.Width / 2); $y = [int]($r.Y + $r.Height / 2)
    if (-not [OrchardIconDesktopInput]::SetCursorPos($x, $y)) { throw 'Windows rejected the pointer position.' }
    Start-Sleep -Milliseconds 80
    [OrchardIconDesktopInput]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardIconDesktopInput]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    $report.pointerSent = $true
}
function Key([byte]$code, [IntPtr]$window) {
    Ensure-Foreground $window
    [OrchardIconDesktopInput]::keybd_event($code, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardIconDesktopInput]::keybd_event($code, 0, 2, [UIntPtr]::Zero)
    $report.keyboardSent = $true
}

$process = $null
try {
    $process = Start-Process -FilePath $exe -WorkingDirectory $app -PassThru
    $root = Wait-For { $process.Refresh(); if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero) { return $null }; [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle) } 'Catalog did not expose a native window.'
    [OrchardIconDesktopInput]::MoveWindow($process.MainWindowHandle, 30, 30, 1600, 1000, $true) | Out-Null
    [OrchardIconDesktopInput]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
    $themeSelector = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'ThemeSelector'))
    $themeSelector.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
    $themeOption = Wait-For { Find-Name $root $Theme ([System.Windows.Automation.ControlType]::ListItem) } 'Theme option was not visible.'
    $themeOption.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $report.steps.selectedTheme = @($themeSelector.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern).Current.GetSelection())[0].Current.Name
    if ($report.steps.selectedTheme -ne $Theme) { throw 'Catalog did not switch theme.' }
    (Find-Name $root 'Find a component' ([System.Windows.Automation.ControlType]::Edit)).GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Icon Buttons')
    (Wait-For { Find-Name $root 'Show Icon Buttons' ([System.Windows.Automation.ControlType]::Button) } 'Icon Buttons navigation unavailable.').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $edit = Wait-For { Find-Name $root 'Edit story' ([System.Windows.Automation.ControlType]::Button) } 'Edit story icon button unavailable.'
    $result = Wait-For { Find-Name $root 'No icon command yet' ([System.Windows.Automation.ControlType]::Text) } 'Icon command result unavailable.'
    Ensure-Foreground $process.MainWindowHandle
    if ($CheckTooltip) {
        $bounds = $edit.Current.BoundingRectangle
        [OrchardIconDesktopInput]::SetCursorPos([int]($bounds.X - 20), [int]($bounds.Y - 20)) | Out-Null
        Start-Sleep -Milliseconds 250
        [OrchardIconDesktopInput]::SetCursorPos([int]($bounds.X + $bounds.Width / 2), [int]($bounds.Y + $bounds.Height / 2)) | Out-Null
        [OrchardIconDesktopInput]::mouse_event(1, 1, 0, 0, [UIntPtr]::Zero)
        try {
            $tip = Wait-For { Find-Name ([System.Windows.Automation.AutomationElement]::RootElement) 'Edit story' ([System.Windows.Automation.ControlType]::ToolTip) } 'Hover did not expose the consumer tooltip.'
            $report.steps.tooltipName = $tip.Current.Name
        } catch {
            Add-Type -AssemblyName System.Drawing
            $image = New-Object System.Drawing.Bitmap 1600, 1000
            $graphics = [System.Drawing.Graphics]::FromImage($image)
            try { $graphics.CopyFromScreen(30, 30, 0, 0, $image.Size); $image.Save((Join-Path $folder 'icon-button-hover-timeout.png')) } finally { $graphics.Dispose(); $image.Dispose() }
            throw
        }
    }
    Click $edit $process.MainWindowHandle
    Wait-For { if ($result.Current.Name -eq 'Edit story: 1') { $true } } 'Pointer click did not invoke the consumer command.' | Out-Null
    $report.steps.pointerResult = $result.Current.Name
    $edit.SetFocus()
    Key 0x20 $process.MainWindowHandle
    Wait-For { if ($result.Current.Name -eq 'Edit story: 2') { $true } } 'Space did not invoke the consumer command.' | Out-Null
    $report.steps.spaceResult = $result.Current.Name
    Key 0x0D $process.MainWindowHandle
    Wait-For { if ($result.Current.Name -eq 'Edit story: 3') { $true } } 'Enter did not invoke the consumer command.' | Out-Null
    $report.steps.enterResult = $result.Current.Name
    $disabled = Wait-For { Find-Name $root 'Delete unavailable' ([System.Windows.Automation.ControlType]::Button) } 'Disabled icon button unavailable.'
    if ($disabled.Current.IsEnabled) { throw 'Disabled icon button became enabled.' }
    $report.steps.disabledButtonRejected = $true
    $report.passed = $true
} catch { $report.error = $_.Exception.Message; throw }
finally {
    if ($process -and -not $process.HasExited) { $process.CloseMainWindow() | Out-Null; if (-not $process.WaitForExit(5000)) { Stop-Process -Id $process.Id -Force } }
    $report.processExited = if ($process) { -not [bool](Get-Process -Id $process.Id -ErrorAction SilentlyContinue) } else { $true }
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'icon-button-desktop.json') -Encoding utf8
}
