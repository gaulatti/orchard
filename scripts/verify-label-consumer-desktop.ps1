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
public static class OrchardLabelConsumerDesktop
{
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}
'@

$app = (Resolve-Path -LiteralPath $AppDirectory).Path
$exe = Join-Path $app 'PackageConsumer.exe'
$library = Join-Path $app 'Orchard.dll'
$reportPath = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $reportPath) { throw 'Use a fresh consumer desktop report.' }
foreach ($path in @($exe, $library)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing consumer desktop input: $path" } }
$libraryHash = (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()
if ($libraryHash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'Consumer Orchard DLL differs from the requested package.' }
if (@(Get-Process PackageConsumer -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count) { throw 'Close this consumer before its isolated desktop check.' }
New-Item -ItemType Directory -Path (Split-Path -Parent $reportPath) -Force | Out-Null
$report = [ordered]@{
    format = 'orchard-label-consumer-desktop-v1'
    passed = $false
    consumerExeSha256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    orchardDllSha256 = $libraryHash
    mode = 'System'
    route = $null
    physicalPointerAndKeyboardInputSent = $false
    osSettingsChanged = $false
    nativeArm64RuntimeVerified = $false
    componentAccepted = $false
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
function FocusedId() { [System.Windows.Automation.AutomationElement]::FocusedElement.Current.AutomationId }
function Key([byte]$code) {
    [OrchardLabelConsumerDesktop]::keybd_event($code, 0, 0, [UIntPtr]::Zero)
    [OrchardLabelConsumerDesktop]::keybd_event($code, 0, 2, [UIntPtr]::Zero)
    $report.physicalPointerAndKeyboardInputSent = $true
}
function ClickLabel($element) {
    $bounds = $element.Current.BoundingRectangle
    if ($element.Current.IsOffscreen -or $bounds.IsEmpty -or $bounds.Width -le 0 -or $bounds.Height -le 0) { throw 'Consumer Label is not visible for pointer input.' }
    $x = [int][math]::Floor($bounds.X + 24)
    $y = [int][math]::Floor($bounds.Y + $bounds.Height / 2)
    if (-not [OrchardLabelConsumerDesktop]::SetCursorPos($x, $y)) { throw 'Windows rejected consumer Label pointer position.' }
    [OrchardLabelConsumerDesktop]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    [OrchardLabelConsumerDesktop]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    $report.physicalPointerAndKeyboardInputSent = $true
    return [ordered]@{ x = $x; y = $y }
}

$process = $null
try {
    $process = Start-Process -FilePath $exe -WorkingDirectory $app -PassThru
    $report.processId = $process.Id
    $root = Wait-Until {
        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero) { return $null }
        [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    } 'Independent consumer did not expose a native window.'
    $label = Wait-Until {
        $element = ById $root 'PackageFieldLabel'
        if ($element -and -not $element.Current.IsOffscreen) { $element }
    } 'Independent consumer Label did not become visible.'
    $input = ById $root 'Input'
    $toggle = ById $root 'PackageThemeToggle'
    if (-not $input -or -not $toggle -or $input.Current.IsOffscreen) { throw 'Independent consumer input or theme toggle is unavailable.' }
    if ($input.Current.LabeledBy.Current.AutomationId -ne 'PackageFieldLabel') { throw 'Package input lost its Label automation relationship.' }
    $value = $input.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    if ($value.Current.Value -ne 'Harbor update') { throw 'Consumer input did not retain its initial value.' }
    if ([OrchardLabelConsumerDesktop]::GetForegroundWindow() -ne $process.MainWindowHandle) { throw 'Consumer is not foreground; no physical input was sent.' }

    if ($toggle.Current.Name -ne 'Switch to light theme') { throw 'Consumer did not begin in System theme-toggle mode.' }
    $toggle.SetFocus()
    Key 0x09
    Wait-Until { FocusedId -eq 'Input' } 'Tab did not skip the packaged Label.' | Out-Null
    $toggle.SetFocus()
    $position = ClickLabel $label
    Wait-Until { FocusedId -eq 'Input' } 'Physical Label click did not focus the packaged Input.' | Out-Null
    $before = $value.Current.Value
    Key 0x58
    Wait-Until { $value.Current.Value.Length -eq $before.Length + 1 } 'Physical typing did not reach the packaged Input.' | Out-Null
    $after = $value.Current.Value
    if (-not $after.ToLowerInvariant().Contains('x')) { throw 'Physical key did not appear in the packaged Input.' }
    $report.route = [ordered]@{ toggleName = $toggle.Current.Name; tabToInput = $true; labelPointer = $position; pointerFocus = FocusedId; valueBefore = $before; valueAfter = $after; inputLabeledBy = $input.Current.LabeledBy.Current.AutomationId }
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
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
}
