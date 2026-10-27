param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [Parameter(Mandatory)][string]$Output,
    [Parameter(Mandatory)][string]$ExpectedLibrarySha256
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class OrchardThemePackageInput
{
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X; public int Y; }
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}
'@

$app = (Resolve-Path -LiteralPath $AppDirectory).Path
$exe = Join-Path $app 'PackageConsumer.exe'
$library = Join-Path $app 'Orchard.dll'
$outputPath = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $outputPath) { throw 'Use a fresh Theme Toggle package input report.' }
foreach ($path in @($exe, $library)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing package consumer input: $path" } }
$libraryHash = (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()
if ($libraryHash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'Consumer library bytes do not match the expected package.' }
if (@(Get-Process PackageConsumer -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count) { throw 'Close this package consumer before isolated input.' }

function Wait-Until([scriptblock]$condition, [string]$failure) {
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
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
function ByButtonName($root, [string]$name) {
    $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    @($all | Where-Object { $_.Current.Name -eq $name -and $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Button -and -not $_.Current.IsOffscreen }) | Select-Object -First 1
}
function Click($element, [IntPtr]$window) {
    if ([OrchardThemePackageInput]::GetForegroundWindow() -ne $window) { throw 'Consumer lost foreground; no pointer input was sent.' }
    $bounds = $element.Current.BoundingRectangle
    if ($element.Current.IsOffscreen -or $bounds.IsEmpty -or $bounds.Width -le 0 -or $bounds.Height -le 0) { throw 'Pointer target is not visible.' }
    $x = [int][Math]::Floor($bounds.X + $bounds.Width / 2)
    $y = [int][Math]::Floor($bounds.Y + $bounds.Height / 2)
    $virtualX = [OrchardThemePackageInput]::GetSystemMetrics(76)
    $virtualY = [OrchardThemePackageInput]::GetSystemMetrics(77)
    $virtualWidth = [OrchardThemePackageInput]::GetSystemMetrics(78)
    $virtualHeight = [OrchardThemePackageInput]::GetSystemMetrics(79)
    if ($virtualWidth -le 1 -or $virtualHeight -le 1) { throw 'Windows virtual desktop bounds are invalid.' }
    $absoluteX = [uint32][Math]::Round(($x - $virtualX) * 65535 / ($virtualWidth - 1))
    $absoluteY = [uint32][Math]::Round(($y - $virtualY) * 65535 / ($virtualHeight - 1))
    [OrchardThemePackageInput]::mouse_event(0xC001, $absoluteX, $absoluteY, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 80
    if ([OrchardThemePackageInput]::GetForegroundWindow() -ne $window) { throw 'Consumer lost foreground after pointer movement.' }
    [OrchardThemePackageInput]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardThemePackageInput]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    [ordered]@{ name = $element.Current.Name; x = $x; y = $y }
}
function PressKey($element, [IntPtr]$window, [byte]$key) {
    if ([OrchardThemePackageInput]::GetForegroundWindow() -ne $window) { throw 'Consumer lost foreground; no keyboard input was sent.' }
    $element.SetFocus()
    Wait-Until { $element.Current.HasKeyboardFocus } 'Theme Toggle did not receive keyboard focus.' | Out-Null
    [OrchardThemePackageInput]::keybd_event($key, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [OrchardThemePackageInput]::keybd_event($key, 0, 2, [UIntPtr]::Zero)
    [ordered]@{ key = $key; focused = $element.Current.HasKeyboardFocus }
}

$report = [ordered]@{
    format = 'orchard-theme-toggle-package-input-v1'
    passed = $false
    packageConsumerExeSha256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    orchardDllSha256 = $libraryHash
    physicalPointerInputSent = $false
    physicalKeyboardInputSent = $false
    osSettingsChanged = $false
    nativeArm64RuntimeVerified = $false
    componentAccepted = $false
}
$cursor = [OrchardThemePackageInput+Point]::new()
$cursorSaved = [OrchardThemePackageInput]::GetCursorPos([ref]$cursor)
$process = $null
try {
    $process = Start-Process -FilePath $exe -WorkingDirectory $app -PassThru
    $root = Wait-Until {
        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero) { return $null }
        [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    } 'Package consumer did not expose a native window.'
    $toggle = Wait-Until { ById $root 'PackageThemeToggle' } 'Package Theme Toggle was unavailable.'
    $result = Wait-Until { ById $root 'Result' } 'Package result label was unavailable.'
    $button = Wait-Until { ByButtonName $root 'Package component' } 'Package Button was unavailable.'
    if ($toggle.Current.Name -ne 'Switch to light theme' -or $result.Current.Name -ne 'Ready') { throw 'Package consumer did not begin with a System-mode Theme Toggle and ready result.' }
    $report.initialName = $toggle.Current.Name
    $report.pointer = Click $toggle $process.MainWindowHandle
    $report.physicalPointerInputSent = $true
    Wait-Until { $result.Current.Name -eq 'Theme request 1: Light (not applied)' } 'Pointer click did not reach the consumer request handler.' | Out-Null
    $report.pointerResult = $result.Current.Name

    $report.space = PressKey $toggle $process.MainWindowHandle 0x20
    $report.physicalKeyboardInputSent = $true
    Wait-Until { $result.Current.Name -eq 'Theme request 2: Light (not applied)' } 'Space did not reach the consumer request handler.' | Out-Null
    $report.spaceResult = $result.Current.Name
    $report.enter = PressKey $toggle $process.MainWindowHandle 0x0D
    Wait-Until { $result.Current.Name -eq 'Theme request 3: Light (not applied)' } 'Enter did not reach the consumer request handler.' | Out-Null
    $report.enterResult = $result.Current.Name
    if ($toggle.Current.Name -ne 'Switch to light theme') { throw 'Rejected requests changed the consumer-owned Theme Toggle mode.' }
    $report.rejectedName = $toggle.Current.Name

    $report.button = Click $button $process.MainWindowHandle
    Wait-Until { $result.Current.Name -eq 'Saved: Harbor update' } 'Theme request handling changed the existing package Button action.' | Out-Null
    $report.buttonResult = $result.Current.Name
    $report.passed = $true
} catch {
    $report.error = $_.Exception.Message
    throw
} finally {
    if ($process -and -not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(5000)) { Stop-Process -Id $process.Id -Force }
    }
    if ($process) { $process.Refresh() }
    $report.processExited = if ($process) { -not [bool](Get-Process -Id $process.Id -ErrorAction SilentlyContinue) } else { $true }
    $report.processExitCode = if ($process -and $process.HasExited) { $process.ExitCode } else { $null }
    $report.cleanShutdown = $report.processExited -and $report.processExitCode -eq 0
    $report.passed = $report.passed -and $report.cleanShutdown
    if ($cursorSaved) { [OrchardThemePackageInput]::SetCursorPos($cursor.X, $cursor.Y) | Out-Null }
    New-Item -ItemType Directory -Path (Split-Path -Parent $outputPath) -Force | Out-Null
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $outputPath -Encoding utf8
}
if (-not $report.passed) { throw 'Package consumer input or clean shutdown failed; inspect the report.' }
