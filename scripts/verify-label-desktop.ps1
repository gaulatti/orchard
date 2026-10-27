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
public static class OrchardLabelDesktop
{
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}
'@

$app = (Resolve-Path -LiteralPath $AppDirectory).Path
$exe = Join-Path $app 'Orchard.Catalog.exe'
$library = Join-Path $app 'Orchard.dll'
$folder = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $folder) { throw 'Use a fresh Label desktop output directory.' }
foreach ($path in @($exe, $library)) { if (-not (Test-Path -LiteralPath $path)) { throw "Missing Label desktop input: $path" } }
$libraryHash = (Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()
if ($libraryHash -ne $ExpectedLibrarySha256.ToLowerInvariant()) { throw 'The catalog contains different Orchard library bytes.' }
if (@(Get-Process Orchard.Catalog -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count) { throw 'Close the same catalog before this isolated Label check.' }
New-Item -ItemType Directory -Path $folder | Out-Null

$report = [ordered]@{
    format = 'orchard-label-desktop-v1'
    passed = $false
    catalogExeSha256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    orchardDllSha256 = $libraryHash
    requestedTheme = $Theme
    steps = [ordered]@{}
    physicalPointerAndKeyboardInputSent = $false
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
function FocusedId() { [System.Windows.Automation.AutomationElement]::FocusedElement.Current.AutomationId }
function Key([byte]$code) {
    [OrchardLabelDesktop]::keybd_event($code, 0, 0, [UIntPtr]::Zero)
    [OrchardLabelDesktop]::keybd_event($code, 0, 2, [UIntPtr]::Zero)
    $report.physicalPointerAndKeyboardInputSent = $true
}
function Click($element, [int]$offsetX = -1) {
    $bounds = $element.Current.BoundingRectangle
    if ($element.Current.IsOffscreen -or $bounds.IsEmpty -or $bounds.Width -le 0 -or $bounds.Height -le 0) { throw "Label target $($element.Current.AutomationId) is not visible." }
    $x = [int][math]::Floor($bounds.X + $(if ($offsetX -ge 0) { $offsetX } else { $bounds.Width / 2 }))
    $y = [int][math]::Floor($bounds.Y + $bounds.Height / 2)
    if (-not [OrchardLabelDesktop]::SetCursorPos($x, $y)) { throw 'Windows rejected the Label pointer position.' }
    [OrchardLabelDesktop]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    [OrchardLabelDesktop]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
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
    } 'The catalog did not expose a native window.'
    $themeSelector = ById $root 'ThemeSelector'
    $themeSelector.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
    $themeOption = Wait-Until {
        $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        @($all | Where-Object { $_.Current.Name -eq $Theme -and $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::ListItem -and -not $_.Current.IsOffscreen }) | Select-Object -First 1
    } "The $Theme theme option was unavailable."
    $themeOption.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $selectedTheme = @($themeSelector.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern).Current.GetSelection())
    if ($selectedTheme.Count -ne 1 -or $selectedTheme[0].Current.Name -ne $Theme) { throw "Catalog did not select $Theme theme." }
    $report.selectedTheme = $selectedTheme[0].Current.Name
    (ByName $root 'Find a component').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Labels')
    $navigation = Wait-Until { ByName $root 'Show Labels' } 'Labels catalog navigation was unavailable.'
    $navigation.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $emailLabel = Wait-Until { $element = ById $root 'EmailLabel'; if ($element -and -not $element.Current.IsOffscreen) { $element } } 'Email Label was not visible.'
    $emailInput = ById $root 'EmailInput'
    $richLabel = ById $root 'RichLabel'
    $richInput = ById $root 'RichInput'
    $nested = ById $root 'NestedAction'
    $pointerAction = ById $root 'PointerAction'
    $result = ById $root 'NestedResult'
    $associationLabel = ById $root 'AssociationLabel'
    if (@($emailInput, $richLabel, $richInput, $nested, $pointerAction, $result, $associationLabel | Where-Object { -not $_ }).Count) { throw 'Labels fixture controls were incomplete.' }
    if ([OrchardLabelDesktop]::GetForegroundWindow() -ne $process.MainWindowHandle) { throw 'Catalog is not the foreground window; no physical input was sent.' }

    $report.steps.emailPointer = Click $emailLabel
    Wait-Until { FocusedId -eq 'EmailInput' } 'Physical label click did not focus EmailInput.' | Out-Null
    $report.steps.emailFocus = FocusedId
    Key 0x58
    $emailValue = $emailInput.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    Wait-Until { $emailValue.Current.Value -match '[xX]' } 'Physical key input did not reach EmailInput.' | Out-Null
    $report.steps.emailTyped = $emailValue.Current.Value

    $emailInput.SetFocus()
    Key 0x09
    Wait-Until { FocusedId -eq 'NestedAction' } 'Tab did not skip labels and reach the nested Help action.' | Out-Null
    $report.steps.tabFromEmailToHelp = $true

    $emailInput.SetFocus()
    $report.steps.richLabelPointer = Click $richLabel 24
    Wait-Until { FocusedId -eq 'RichInput' } 'Physical rich-label text click did not focus RichInput.' | Out-Null
    $report.steps.richLabelFocus = FocusedId

    $emailInput.SetFocus()
    $beforeNested = $result.Current.Name
    $beforeCount = if ($beforeNested -match '^Nested actions: (\d+);') { [int]$Matches[1] } elseif ($beforeNested -eq 'No nested action yet') { 0 } else { throw "Unexpected nested action result: $beforeNested" }
    $report.steps.nestedResultBefore = $beforeNested
    $report.steps.nestedButtonPointer = Click $nested
    try { Wait-Until { $result.Current.Name -match "^Nested actions: $($beforeCount + 1);" } 'Nested Help pointer click did not invoke its consumer action.' | Out-Null }
    catch { $report.steps.nestedResultAtFailure = $result.Current.Name; throw }
    $report.steps.nestedResultAfter = $result.Current.Name
    $report.steps.nestedFocus = FocusedId
    if ($report.steps.nestedFocus -eq 'RichInput') { throw 'Nested Help pointer click incorrectly redirected focus to RichInput.' }

    $emailInput.SetFocus()
    $report.steps.nonTabbableButtonPointer = Click $pointerAction
    try { Wait-Until { $result.Current.Name -match "^Nested actions: $($beforeCount + 2);" } 'Non-tabbable nested pointer action did not invoke its consumer action.' | Out-Null }
    catch { $report.steps.nonTabbableResultAtFailure = $result.Current.Name; throw }
    $report.steps.nonTabbableResultAfter = $result.Current.Name
    $report.steps.nonTabbableFocus = FocusedId
    if ($report.steps.nonTabbableFocus -eq 'RichInput') { throw 'Non-tabbable nested button incorrectly redirected focus to RichInput.' }

    $emailInput.SetFocus()
    $report.steps.associationPointer = Click $associationLabel
    Wait-Until { FocusedId -eq 'AssociationInput' } 'Physical linked-label click did not focus AssociationInput.' | Out-Null
    $report.steps.associationFocus = FocusedId
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
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $folder 'label-desktop.json') -Encoding utf8
}
