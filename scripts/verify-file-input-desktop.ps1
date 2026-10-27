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
public static class OrchardFileInputDesktop
{
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr extra, string text);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr window, uint command);
}
'@

$taskApp = (Resolve-Path -LiteralPath $AppDirectory).Path
$taskExe = Join-Path $taskApp 'Orchard.Catalog.exe'
$taskLibrary = Join-Path $taskApp 'Orchard.dll'
$taskSelection = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../tests/Fixtures/fictional-playlist.m3u')).Path
$taskOutput = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $taskOutput) { throw 'Use a fresh File Input desktop output directory.' }
foreach ($path in @($taskExe, $taskLibrary)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing desktop-test input: $path" } }
$taskLibraryHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $taskLibrary).Hash.ToLowerInvariant()
if ($taskLibraryHash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'The catalog does not contain the requested Orchard library bytes.' }
if (@(Get-Process Orchard.Catalog -ErrorAction SilentlyContinue | Where-Object Path -eq $taskExe).Count) { throw 'Close the same catalog app before this isolated desktop check.' }
New-Item -ItemType Directory -Path $taskOutput | Out-Null

$taskReport = [ordered]@{
    format = 'orchard-file-input-desktop-v1'
    passed = $false
    catalogExeSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $taskExe).Hash.ToLowerInvariant()
    orchardDllSha256 = $taskLibraryHash
    fictionalFileSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $taskSelection).Hash.ToLowerInvariant()
    fictionalFileName = 'fictional-playlist.m3u'
    steps = [ordered]@{}
    osSettingsChanged = $false
    nativeArm64RuntimeVerified = $false
    componentAccepted = $false
}

function Wait-Until([scriptblock]$condition, [string]$failure) {
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    do {
        try { $result = & $condition; if ($result) { return $result } } catch { }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)
    throw $failure
}
function Key([byte]$code) {
    [OrchardFileInputDesktop]::keybd_event($code, 0, 0, [UIntPtr]::Zero)
    [OrchardFileInputDesktop]::keybd_event($code, 0, 2, [UIntPtr]::Zero)
}
function ById($root, [string]$id) {
    $condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function ByName($root, [string]$name) {
    $condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function Picker([int]$catalogProcess) {
    $window = [OrchardFileInputDesktop]::GetForegroundWindow()
    if ($window -eq [IntPtr]::Zero) { return $null }
    $element = [System.Windows.Automation.AutomationElement]::FromHandle($window)
    if ($element.Current.Name -eq 'Open' -and $element.Current.ClassName -eq '#32770' -and $element.Current.ProcessId -ne $catalogProcess) {
        return [pscustomobject]@{ Window = $window; Element = $element; Process = $element.Current.ProcessId; Class = $element.Current.ClassName; Owner = [OrchardFileInputDesktop]::GetWindow($window, 4) }
    }
    return $null
}
function Click($element) {
    $bounds = $element.Current.BoundingRectangle
    if ($element.Current.IsOffscreen -or $bounds.IsEmpty -or $bounds.Width -le 0 -or $bounds.Height -le 0) { throw 'The File Input is not visible for native pointer input.' }
    $x = [int][math]::Floor($bounds.X + $bounds.Width / 2)
    $y = [int][math]::Floor($bounds.Y + $bounds.Height / 2)
    if (-not [OrchardFileInputDesktop]::SetCursorPos($x, $y)) { throw 'Windows rejected the File Input pointer position.' }
    [OrchardFileInputDesktop]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    [OrchardFileInputDesktop]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    return [ordered]@{ x = $x; y = $y }
}

$taskProcess = $null
try {
    $taskProcess = Start-Process -FilePath $taskExe -WorkingDirectory $taskApp -PassThru
    $taskReport.processId = $taskProcess.Id
    $taskRoot = Wait-Until {
        $taskProcess.Refresh()
        if ($taskProcess.HasExited -or $taskProcess.MainWindowHandle -eq [IntPtr]::Zero) { return $null }
        [System.Windows.Automation.AutomationElement]::FromHandle($taskProcess.MainWindowHandle)
    } 'The catalog did not expose a native window.'
    $taskReport.windowHandle = $taskProcess.MainWindowHandle.ToInt64()
    $search = Wait-Until { ByName $taskRoot 'Find a component' } 'The catalog search was not available.'
    $search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('File Input')
    $navigation = Wait-Until { ByName $taskRoot 'Show File Input' } 'File Input catalog navigation was not available.'
    $navigation.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $primary = Wait-Until { $item = ById $taskRoot 'Primary'; if ($item -and -not $item.Current.IsOffscreen) { $item } } 'The primary File Input was not visible.'
    $result = ById $taskRoot 'Result'
    $value = $primary.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    if ($result.Current.Name -ne 'No file selected' -or $value.Current.Value -ne '') { throw 'File Input did not begin empty.' }

    $pointer = Click $primary
    $firstPicker = Wait-Until { Picker $taskProcess.Id } 'Pointer activation did not open the native system picker.'
    if ($firstPicker.Owner -ne $taskProcess.MainWindowHandle) { throw 'The native picker does not belong to the catalog window.' }
    $taskReport.steps.pointer = [ordered]@{ position = $pointer; dialogProcess = $firstPicker.Process; dialogClass = $firstPicker.Class; ownerHandle = $firstPicker.Owner.ToInt64() }
    Start-Sleep -Milliseconds 500
    Key 0x1B
    Wait-Until { if ((ById $taskRoot 'Result').Current.Name -eq 'Selection canceled; previous files retained') { $true } } 'Escape did not report native picker cancellation.' | Out-Null
    if ($value.Current.Value -ne '') { throw 'Cancel unexpectedly selected a file.' }
    $taskReport.steps.cancelEmpty = $true

    Click $primary | Out-Null
    $secondPicker = Wait-Until { Picker $taskProcess.Id } 'Second pointer activation did not open the native picker.'
    if ($secondPicker.Owner -ne $taskProcess.MainWindowHandle) { throw 'The selection picker does not belong to the catalog window.' }
    $edit = Wait-Until {
        $all = $secondPicker.Element.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        $edits = @(foreach ($item in $all) { if ($item.Current.AutomationId -eq '1148' -and $item.Current.ClassName -eq 'Edit' -and $item.Current.NativeWindowHandle -ne 0) { $item } })
        if ($edits.Count -eq 1) { $edits[0] }
    } 'The system picker filename field was not unique.'
    [OrchardFileInputDesktop]::SendMessage([IntPtr]$edit.Current.NativeWindowHandle, 0x000C, [IntPtr]::Zero, $taskSelection) | Out-Null
    Key 0x0D
    Wait-Until { if ((ById $taskRoot 'Result').Current.Name -eq 'Selected 1 original native file(s); callback list retained: True' -and (ById $taskRoot 'Primary').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value -eq 'fictional-playlist.m3u') { $true } } 'Native selection did not reach the original-file callback and automation value.' | Out-Null
    $taskReport.steps.selection = [ordered]@{ dialogProcess = $secondPicker.Process; dialogClass = $secondPicker.Class; ownerHandle = $secondPicker.Owner.ToInt64(); originalFileCallbackRetained = $true; automationValue = $value.Current.Value }

    $small = ById $taskRoot 'Small'
    $small.SetFocus()
    $before = [System.Windows.Automation.AutomationElement]::FocusedElement.Current
    Key 0x09
    $after = Wait-Until { $focused = [System.Windows.Automation.AutomationElement]::FocusedElement.Current; if ($focused.AutomationId -eq 'Primary' -and $focused.ProcessId -eq $taskProcess.Id) { $focused } } 'Tab did not focus the primary File Input.'
    if ($before.AutomationId -ne 'Small' -or $before.ProcessId -ne $taskProcess.Id) { throw 'The keyboard sequence did not begin at the small File Input.' }
    Key 0x20
    $thirdPicker = Wait-Until { Picker $taskProcess.Id } 'Space did not open the native picker from the focused File Input.'
    if ($thirdPicker.Owner -ne $taskProcess.MainWindowHandle) { throw 'The keyboard-opened picker does not belong to the catalog window.' }
    Start-Sleep -Milliseconds 500
    Key 0x1B
    Wait-Until { if ((ById $taskRoot 'Result').Current.Name -eq 'Selection canceled; previous files retained') { $true } } 'Keyboard-opened picker did not cancel.' | Out-Null
    if ($value.Current.Value -ne 'fictional-playlist.m3u') { throw 'Cancellation lost the prior original file.' }
    $taskReport.steps.keyboard = [ordered]@{ tabFrom = $before.AutomationId; tabTo = $after.AutomationId; spaceDialogProcess = $thirdPicker.Process; ownerHandle = $thirdPicker.Owner.ToInt64(); previousFileRetained = $true }

    $clear = ByName $taskRoot 'Clear selection silently'
    $clear.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-Until { if ((ById $taskRoot 'Result').Current.Name -eq 'Selection cleared without a SelectionChanged callback' -and (ById $taskRoot 'Primary').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value -eq '') { $true } } 'Programmatic clear changed the callback contract or retained a file.' | Out-Null
    $taskReport.steps.clear = [ordered]@{ automationValueEmpty = $true; callerResult = (ById $taskRoot 'Result').Current.Name }
    $taskReport.passed = $true
}
catch {
    $taskReport.error = $_.Exception.Message
    if ($taskProcess -and -not $taskProcess.HasExited) {
        try { $taskReport.resultAtFailure = (ById $taskRoot 'Result').Current.Name } catch { }
        try { $taskReport.foregroundAtFailure = [System.Windows.Automation.AutomationElement]::FromHandle([OrchardFileInputDesktop]::GetForegroundWindow()).Current.Name } catch { }
    }
    throw
}
finally {
    if ($taskProcess) {
        if (-not $taskProcess.HasExited) {
            if ((Picker $taskProcess.Id)) { Key 0x1B; Start-Sleep -Milliseconds 200 }
            $taskProcess.CloseMainWindow() | Out-Null
            if (-not $taskProcess.WaitForExit(5000)) { Stop-Process -Id $taskProcess.Id -Force }
        }
        $taskReport.processExited = (Get-Process -Id $taskProcess.Id -ErrorAction SilentlyContinue) -eq $null
    }
    $taskReport | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $taskOutput 'file-input-desktop.json') -Encoding utf8
}
