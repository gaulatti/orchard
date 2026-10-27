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
public static class OrchardPackagePointer
{
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X; public int Y; }
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
}
'@

$app = (Resolve-Path -LiteralPath $AppDirectory).Path
$exe = Join-Path $app 'PackageConsumer.exe'
$library = Join-Path $app 'Orchard.dll'
$outputPath = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $outputPath) { throw 'Use a fresh package Button pointer report path.' }
foreach ($path in @($exe, $library)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing package consumer input: $path" } }
$libraryHash = (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()
if ($libraryHash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'Consumer Orchard.dll does not match the expected package.' }
if (@(Get-Process PackageConsumer -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count) { throw 'Close this consumer before its isolated pointer check.' }

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
    if ([OrchardPackagePointer]::GetForegroundWindow() -ne $window) { throw 'Consumer lost foreground; no pointer input was sent.' }
    $bounds = $element.Current.BoundingRectangle
    if ($element.Current.IsOffscreen -or $bounds.IsEmpty -or $bounds.Width -le 0 -or $bounds.Height -le 0) { throw "Target $($element.Current.Name) is not visible." }
    $x = [int][Math]::Floor($bounds.X + $bounds.Width / 2)
    $y = [int][Math]::Floor($bounds.Y + $bounds.Height / 2)
    if (-not [OrchardPackagePointer]::SetCursorPos($x, $y)) { throw 'Windows rejected the pointer position.' }
    Start-Sleep -Milliseconds 80
    [OrchardPackagePointer]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardPackagePointer]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    [ordered]@{ name = $element.Current.Name; x = $x; y = $y }
}

$report = [ordered]@{
    format = 'orchard-package-button-pointer-v1'
    passed = $false
    packageConsumerExeSha256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    orchardDllSha256 = $libraryHash
    physicalPointerInputSent = $false
    osSettingsChanged = $false
    nativeArm64RuntimeVerified = $false
    componentAccepted = $false
}
$cursor = [OrchardPackagePointer+Point]::new()
$cursorSaved = [OrchardPackagePointer]::GetCursorPos([ref]$cursor)
$process = $null
try {
    $process = Start-Process -FilePath $exe -WorkingDirectory $app -PassThru
    $root = Wait-Until {
        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero) { return $null }
        [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    } 'Package consumer did not expose a native window.'
    $action = Wait-Until { ByButtonName $root 'Package component' } 'Package Button was not visible.'
    $result = Wait-Until { ById $root 'Result' } 'Package result label was unavailable.'
    if ($result.Current.Name -ne 'Ready') { throw 'Package consumer did not start ready.' }
    $report.initialResult = $result.Current.Name
    $report.actionClick = Click $action $process.MainWindowHandle
    $report.physicalPointerInputSent = $true
    Wait-Until { $result.Current.Name -eq 'Saved: Harbor update' } 'Pointer click did not reach the consumer handler.' | Out-Null
    $report.finalResult = $result.Current.Name
    $report.passed = $true
} catch {
    $report.error = $_.Exception.Message
    throw
} finally {
    if ($process -and -not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(5000)) { Stop-Process -Id $process.Id -Force }
    }
    $report.processExited = if ($process) { -not [bool](Get-Process -Id $process.Id -ErrorAction SilentlyContinue) } else { $true }
    if ($cursorSaved) { [OrchardPackagePointer]::SetCursorPos($cursor.X, $cursor.Y) | Out-Null }
    New-Item -ItemType Directory -Path (Split-Path -Parent $outputPath) -Force | Out-Null
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $outputPath -Encoding utf8
}
