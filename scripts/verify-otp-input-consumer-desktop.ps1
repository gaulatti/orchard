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
public static class OrchardOtpConsumerInput {
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
if (Test-Path -LiteralPath $folder) { throw 'Use a fresh OTP consumer desktop output directory.' }
foreach ($path in @($exe, $library)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing consumer input: $path" } }
$hash = (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'Consumer Orchard.dll differs from the expected NuGet package.' }
if (@(Get-Process PackageConsumer -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count) { throw 'Close this consumer before its isolated physical check.' }
New-Item -ItemType Directory -Path $folder | Out-Null
$report = [ordered]@{
    format = 'orchard-otp-input-consumer-desktop-v1'
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
    $deadline = [DateTime]::UtcNow.AddSeconds(12)
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
function Ensure-Foreground([IntPtr]$window) {
    if ([OrchardOtpConsumerInput]::GetForegroundWindow() -ne $window) { throw 'Consumer lost foreground; no physical input was sent.' }
}
function Click($element, [IntPtr]$window) {
    Ensure-Foreground $window
    $bounds = $element.Current.BoundingRectangle
    if ($bounds.IsEmpty -or $element.Current.IsOffscreen) { throw "OTP editor is not visible: $($element.Current.AutomationId)" }
    $x = [int][math]::Floor($bounds.X + $bounds.Width / 2)
    $y = [int][math]::Floor($bounds.Y + $bounds.Height / 2)
    if (-not [OrchardOtpConsumerInput]::SetCursorPos($x, $y)) { throw 'Windows rejected OTP pointer position.' }
    Start-Sleep -Milliseconds 70
    [OrchardOtpConsumerInput]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardOtpConsumerInput]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    $report.physicalPointerAndKeyboardSent = $true
    [ordered]@{ x = $x; y = $y; id = $element.Current.AutomationId }
}
function Key([byte]$code, [IntPtr]$window) {
    Ensure-Foreground $window
    [OrchardOtpConsumerInput]::keybd_event($code, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardOtpConsumerInput]::keybd_event($code, 0, 2, [UIntPtr]::Zero)
    $report.physicalPointerAndKeyboardSent = $true
}

$process = $null
try {
    $process = Start-Process -FilePath $exe -WorkingDirectory $app -ArgumentList @('--probe-otp-foreground', '--otp-theme', $Theme) -PassThru
    $root = Wait-Until {
        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero) { return $null }
        [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    } 'Independent consumer did not expose a native OTP window.'
    [OrchardOtpConsumerInput]::MoveWindow($process.MainWindowHandle, 30, 30, 800, 580, $true) | Out-Null
    [OrchardOtpConsumerInput]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
    $accepted = Wait-Until { ById $root 'AcceptedOtp' } 'Accepted OTP group unavailable.'
    $rejected = Wait-Until { ById $root 'RejectedOtp' } 'Rejected OTP group unavailable.'
    $secure = Wait-Until { ById $root 'SecureOtp' } 'Secure OTP group unavailable.'
    $acceptedStatus = ById $root 'AcceptedStatus'
    $rejectedStatus = ById $root 'RejectedStatus'
    $secureStatus = ById $root 'SecureStatus'
    if (-not $acceptedStatus -or -not $rejectedStatus -or -not $secureStatus) { throw 'OTP caller statuses are unavailable.' }

    $fourth = ById $accepted 'OtpEditor3'
    $fifth = ById $accepted 'OtpEditor4'
    $report.steps.acceptedClick = Click $fourth $process.MainWindowHandle
    Wait-Until { $fourth.Current.HasKeyboardFocus } 'Pointer did not focus the fourth accepted slot.' | Out-Null
    Key 0x37 $process.MainWindowHandle
    Wait-Until { $acceptedStatus.Current.Name -eq 'Accepted: 4827' -and $fifth.Current.HasKeyboardFocus } 'Physical 7 did not request and accept 4827 or advance focus.' | Out-Null
    $report.steps.acceptedValue = $acceptedStatus.Current.Name
    Key 0x08 $process.MainWindowHandle
    Wait-Until { $acceptedStatus.Current.Name -eq 'Accepted: 482' -and $fourth.Current.HasKeyboardFocus } 'Backspace from the empty fifth slot did not compact the caller value and focus.' | Out-Null
    $report.steps.backspaceValue = $acceptedStatus.Current.Name

    $rejectedFourth = ById $rejected 'OtpEditor3'
    $report.steps.rejectedClick = Click $rejectedFourth $process.MainWindowHandle
    Wait-Until { $rejectedFourth.Current.HasKeyboardFocus } 'Pointer did not focus the fourth rejected slot.' | Out-Null
    Key 0x39 $process.MainWindowHandle
    Wait-Until { $rejectedStatus.Current.Name -eq 'Rejected: 4829; retained: 482' -and $rejectedFourth.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value -eq '' } 'Caller rejection did not restore the original package editor value.' | Out-Null
    $report.steps.rejectedRequest = $rejectedStatus.Current.Name

    $secureFourth = ById $secure 'OtpEditor3'
    if (-not $secureFourth.Current.IsPassword) { throw 'Secure package editor did not expose native protected metadata.' }
    $report.steps.secureClick = Click $secureFourth $process.MainWindowHandle
    Wait-Until { $secureFourth.Current.HasKeyboardFocus } 'Pointer did not focus the fourth secure slot.' | Out-Null
    Key 0x35 $process.MainWindowHandle
    Wait-Until { $secureStatus.Current.Name -eq 'Secure accepted: 4 digits' } 'Physical 5 did not reach the secure caller value.' | Out-Null
    $report.steps.secureAccepted = $secureStatus.Current.Name
    $report.steps.secureEditorIsPassword = $secureFourth.Current.IsPassword

    $bounds = $root.Current.BoundingRectangle
    $bitmap = [System.Drawing.Bitmap]::new([int]$bounds.Width, [int]$bounds.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try { $graphics.CopyFromScreen([int]$bounds.X, [int]$bounds.Y, 0, 0, $bitmap.Size); $bitmap.Save((Join-Path $folder 'otp-consumer.png')) }
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
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $folder 'otp-consumer-desktop.json') -Encoding utf8
}
