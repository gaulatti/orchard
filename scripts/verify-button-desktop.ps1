param(
    [Parameter(Mandatory)][string]$AppDirectory,
    [Parameter(Mandatory)][string]$Output,
    [Parameter(Mandatory)][string]$ExpectedLibrarySha256,
    [Parameter(Mandatory)][ValidateSet('Light', 'Dark')][string]$Theme
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class OrchardButtonDesktop
{
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X; public int Y; }
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
}
'@

$app = (Resolve-Path -LiteralPath $AppDirectory).Path
$exe = Join-Path $app 'Orchard.Catalog.exe'
$library = Join-Path $app 'Orchard.dll'
$folder = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $folder) { throw 'Use a fresh Button desktop output directory.' }
foreach ($path in @($exe, $library)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing Button desktop input: $path" } }
$libraryHash = (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()
if ($libraryHash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'The catalog contains different Orchard library bytes.' }
if (@(Get-Process Orchard.Catalog -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count) { throw 'Close this catalog before its isolated Button check.' }
New-Item -ItemType Directory -Path $folder | Out-Null

$report = [ordered]@{
    format = 'orchard-button-desktop-v1'
    passed = $false
    requestedTheme = $Theme
    catalogExeSha256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    orchardDllSha256 = $libraryHash
    steps = [ordered]@{}
    physicalPointerInputSent = $false
    osSettingsChanged = $false
    nativeArm64RuntimeVerified = $false
    componentAccepted = $false
}
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
function ByName($root, [string]$name) {
    $condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function Action($root, [string]$name) {
    $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    @($all | Where-Object { $_.Current.Name -eq $name -and $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Button }) | Select-Object -First 1
}
function Click($element, [IntPtr]$window) {
    if ([OrchardButtonDesktop]::GetForegroundWindow() -ne $window) { throw 'Catalog lost foreground; no Button input was sent.' }
    $bounds = $element.Current.BoundingRectangle
    if ($element.Current.IsOffscreen -or $bounds.IsEmpty -or $bounds.Width -le 0 -or $bounds.Height -le 0) { throw "Button target $($element.Current.Name) is not visible." }
    $x = [int][math]::Floor($bounds.X + $bounds.Width / 2)
    $y = [int][math]::Floor($bounds.Y + $bounds.Height / 2)
    if (-not [OrchardButtonDesktop]::SetCursorPos($x, $y)) { throw 'Windows rejected Button pointer position.' }
    Start-Sleep -Milliseconds 80
    [OrchardButtonDesktop]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [OrchardButtonDesktop]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    $report.physicalPointerInputSent = $true
    [ordered]@{ x = $x; y = $y; name = $element.Current.Name }
}

$originalCursor = [OrchardButtonDesktop+Point]::new()
$cursorSaved = [OrchardButtonDesktop]::GetCursorPos([ref]$originalCursor)
$process = $null
try {
    $process = Start-Process -FilePath $exe -WorkingDirectory $app -PassThru
    $report.processId = $process.Id
    $root = Wait-Until {
        $process.Refresh()
        if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero) { return $null }
        [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    } 'The catalog did not expose a native window.'
    $themeSelector = ById $root 'ThemeSelector'
    $themeSelector.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
    $themeOption = Wait-Until {
        $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        @($all | Where-Object { $_.Current.Name -eq $Theme -and $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::ListItem -and -not $_.Current.IsOffscreen }) | Select-Object -First 1
    } "The $Theme theme option was unavailable."
    $themeOption.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $selection = @($themeSelector.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern).Current.GetSelection())
    if ($selection.Count -ne 1 -or $selection[0].Current.Name -ne $Theme) { throw "Catalog did not select $Theme theme." }
    $report.selectedTheme = $selection[0].Current.Name
    (ByName $root 'Find a component').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Buttons')
    (Wait-Until { ByName $root 'Show Buttons' } 'Buttons catalog navigation was unavailable.').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $run = Wait-Until { Action $root 'Run preview' } 'Run preview action was unavailable.'
    $loading = ById $root 'LoadingSwitch'
    $consumerEnabled = ById $root 'ConsumerEnabled'
    $start = Action $root 'Start loading on activation'
    $finish = Action $root 'Finish loading'
    $result = ById $root 'ButtonResult'
    if (-not $loading -or -not $consumerEnabled -or -not $start -or -not $finish -or -not $result) { throw 'Buttons fixture omitted a required consumer control.' }
    $loadingToggle = $loading.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $consumerToggle = $consumerEnabled.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    if ($result.Current.Name -ne 'No command yet' -or -not $run.Current.IsEnabled) { throw 'Buttons fixture did not begin ready.' }
    if ([OrchardButtonDesktop]::GetForegroundWindow() -ne $process.MainWindowHandle) { throw 'Catalog is not foreground; no physical input was sent.' }

    $report.steps.firstCommandClick = Click $run $process.MainWindowHandle
    Wait-Until { $result.Current.Name -eq 'Command preview: 1' } 'Pointer click did not reach the consumer command.' | Out-Null
    $report.steps.commandAfterFirstClick = $result.Current.Name
    $report.steps.enableLoadingClick = Click $loading $process.MainWindowHandle
    $report.steps.loadingSwitchAfterEnable = $loadingToggle.Current.ToggleState.ToString()
    Wait-Until { -not $run.Current.IsEnabled } 'Loading did not disable the native action.' | Out-Null
    $report.steps.loadingStatus = $run.Current.ItemStatus
    $report.steps.rejectedLoadingClick = Click $run $process.MainWindowHandle
    Start-Sleep -Milliseconds 250
    if ($result.Current.Name -ne 'Command preview: 1' -or $run.Current.IsEnabled) { throw 'Physical click activated the loading action.' }
    $report.steps.commandDuringLoading = $result.Current.Name
    $report.steps.disableLoadingClick = Click $loading $process.MainWindowHandle
    $report.steps.loadingSwitchAfterDisable = $loadingToggle.Current.ToggleState.ToString()
    $report.steps.consumerEnabledSwitch = $consumerToggle.Current.ToggleState.ToString()
    try { Wait-Until { $run.Current.IsEnabled } 'Loading completion did not restore the native action.' | Out-Null }
    catch { $report.steps.loadingAtFailure = [ordered]@{ switch = $loadingToggle.Current.ToggleState.ToString(); consumerEnabled = $consumerToggle.Current.ToggleState.ToString(); runEnabled = $run.Current.IsEnabled; runStatus = $run.Current.ItemStatus }; throw }
    $report.steps.restoredCommandClick = Click $run $process.MainWindowHandle
    Wait-Until { $result.Current.Name -eq 'Command preview: 2' } 'Restored action did not reach its consumer command exactly once.' | Out-Null
    $report.steps.commandAfterRestore = $result.Current.Name

    $report.steps.selfLoadingClick = Click $start $process.MainWindowHandle
    Wait-Until { $result.Current.Name -eq 'Self-loading action busy' -and -not $start.Current.IsEnabled } 'Pointer click did not enter self-loading.' | Out-Null
    $report.steps.rejectedSelfLoadingClick = Click $start $process.MainWindowHandle
    Start-Sleep -Milliseconds 250
    if ($result.Current.Name -ne 'Self-loading action busy') { throw 'Disabled self-loading action accepted a second pointer click.' }
    $report.steps.finishClick = Click $finish $process.MainWindowHandle
    Wait-Until { $result.Current.Name -eq 'Self-loading action ready' -and $start.Current.IsEnabled } 'Finish action did not restore the consumer action.' | Out-Null
    $report.steps.selfLoadingRestored = $result.Current.Name
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
    if ($cursorSaved) { [OrchardButtonDesktop]::SetCursorPos($originalCursor.X, $originalCursor.Y) | Out-Null }
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $folder 'button-desktop.json') -Encoding utf8
}
