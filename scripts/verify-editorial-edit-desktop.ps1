param(
 [Parameter(Mandatory)][string]$AppDirectory,
 [Parameter(Mandatory)][string]$ExpectedLibrarySha256,
 [Parameter(Mandatory)][ValidateSet('Light','Dark')][string]$Theme,
 [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class OrchardEditorialEditInput {
 [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left; public int Top; public int Right; public int Bottom; }
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
 [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr window, int x, int y, int width, int height, bool repaint);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rect rect);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
 [DllImport("user32.dll")] public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}
'@
$app=(Resolve-Path -LiteralPath $AppDirectory).Path
$exe=Join-Path $app 'Orchard.Catalog.exe'
if(-not (Test-Path -LiteralPath $exe)){throw 'Catalog exe unavailable'}
$hash=(Get-FileHash (Join-Path $app 'Orchard.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
if($hash -ne $ExpectedLibrarySha256.ToLowerInvariant()){throw 'Catalog contains different Orchard library bytes'}
if(@(Get-Process Orchard.Catalog -ErrorAction SilentlyContinue | Where-Object Path -eq $exe).Count){throw 'Close this catalog before its isolated Editorial check'}
$folder=[IO.Path]::GetFullPath($Output)
if(Test-Path $folder){throw 'Fresh output required'}
New-Item -ItemType Directory -Path $folder | Out-Null
$report=[ordered]@{format='orchard-editorial-edit-desktop-v1';theme=$Theme;catalogExeSha256=(Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant();librarySha256=$hash;passed=$false;physicalPointerInputSent=$false;physicalKeyboardInputSent=$false;osSettingsChanged=$false;steps=[ordered]@{}}
function Wait-For([scriptblock]$condition,[string]$failure){$until=[DateTime]::UtcNow.AddSeconds(12);do{try{$value=& $condition;if($value){return $value}}catch{};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow -lt $until);throw $failure}
function Find($root,[string]$name,$type){$all=$root.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition);@($all|Where-Object{$_.Current.Name -eq $name -and $_.Current.ControlType -eq $type -and -not $_.Current.IsOffscreen})|Select-Object -First 1}
function Click($element,[IntPtr]$window,[double]$part=0.5){if([OrchardEditorialEditInput]::GetForegroundWindow() -ne $window){throw 'Catalog lost foreground'};$r=$element.Current.BoundingRectangle;if($r.IsEmpty -or $element.Current.IsOffscreen){throw "Target not visible: $($element.Current.Name)"};$x=[int]($r.X+$r.Width/2);$y=[int]($r.Y+$r.Height*$part);[OrchardEditorialEditInput]::SetCursorPos($x,$y)|Out-Null;Start-Sleep -Milliseconds 80;[OrchardEditorialEditInput]::mouse_event(2,0,0,0,[UIntPtr]::Zero);Start-Sleep -Milliseconds 40;[OrchardEditorialEditInput]::mouse_event(4,0,0,0,[UIntPtr]::Zero);$report.physicalPointerInputSent=$true;$report.steps.lastClick="$($element.Current.Name)@$x,$y"}
function Key([byte]$code,[IntPtr]$window,[bool]$control=$false){if([OrchardEditorialEditInput]::GetForegroundWindow() -ne $window){throw 'Catalog lost foreground'};if($control){[OrchardEditorialEditInput]::keybd_event(0x11,0,0,[UIntPtr]::Zero)};[OrchardEditorialEditInput]::keybd_event($code,0,0,[UIntPtr]::Zero);Start-Sleep -Milliseconds 35;[OrchardEditorialEditInput]::keybd_event($code,0,2,[UIntPtr]::Zero);if($control){[OrchardEditorialEditInput]::keybd_event(0x11,0,2,[UIntPtr]::Zero)};$report.physicalKeyboardInputSent=$true}
function Capture([IntPtr]$window,[string]$path){$rect=[OrchardEditorialEditInput+Rect]::new();if(-not [OrchardEditorialEditInput]::GetWindowRect($window,[ref]$rect)){throw 'Catalog window bounds unavailable'};$bitmap=[System.Drawing.Bitmap]::new($rect.Right-$rect.Left,$rect.Bottom-$rect.Top);$graphics=[System.Drawing.Graphics]::FromImage($bitmap);try{$graphics.CopyFromScreen($rect.Left,$rect.Top,0,0,$bitmap.Size);$bitmap.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)}finally{$graphics.Dispose();$bitmap.Dispose()}}
$process=$null
try{
 $process=Start-Process -FilePath $exe -WorkingDirectory $app -PassThru
 $root=Wait-For {$process.Refresh();if($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero){return $null};[System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)} 'No native catalog window'
 if(-not [OrchardEditorialEditInput]::MoveWindow($process.MainWindowHandle,20,20,1600,1000,$true)){throw 'Could not size the native catalog window'}
 $selector=$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'ThemeSelector'))
 $selector.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
 (Wait-For {Find $root $Theme ([System.Windows.Automation.ControlType]::ListItem)} 'Theme unavailable').GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
 (Wait-For {Find $root 'Find a component' ([System.Windows.Automation.ControlType]::Edit)} 'Catalog search unavailable').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Editorial')
 $nav=Wait-For {Find $root 'Show Editorial demo' ([System.Windows.Automation.ControlType]::Button)} 'Editorial navigation unavailable'
 [OrchardEditorialEditInput]::SetForegroundWindow($process.MainWindowHandle)|Out-Null
 Wait-For {[OrchardEditorialEditInput]::GetForegroundWindow() -eq $process.MainWindowHandle} 'Catalog not foreground'|Out-Null
 Click $nav $process.MainWindowHandle
 Wait-For {Find $root '4 matching pages · 0 selected · Local sample' ([System.Windows.Automation.ControlType]::Text)} 'Editorial did not open'|Out-Null
 $open=Wait-For {Find $root 'Edit fictional page: Harbor reading room' ([System.Windows.Automation.ControlType]::Button)} 'Harbor edit action unavailable'
 Click $open $process.MainWindowHandle
 $title=Wait-For {Find $root 'Local page title' ([System.Windows.Automation.ControlType]::Edit)} 'Title editor did not open'
 $original=$title.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
 $report.steps.originalTitle=$original
 if($original -ne 'Harbor reading room'){throw "Unexpected title: $original"}
 Click $title $process.MainWindowHandle 0.2
 Wait-For {[System.Windows.Automation.AutomationElement]::FocusedElement.Current.Name -eq 'Local page title'} 'Title editor did not gain focus'|Out-Null
 Key 0x23 $process.MainWindowHandle $true
 foreach($code in @(0x20,0x54,0x45,0x53,0x54)){Key $code $process.MainWindowHandle}
 $edited='Harbor reading room test'
 Wait-For {$value=$title.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value;if($value -eq $edited){$value}} 'Physical typing did not edit the title'|Out-Null
 $report.steps.editedTitle=$edited
 Wait-For {Find $root '4 matching pages · 0 selected · Unsaved local changes' ([System.Windows.Automation.ControlType]::Text)} 'Draft did not become dirty'|Out-Null
 $save=Wait-For {$item=Find $root 'Save local sample' ([System.Windows.Automation.ControlType]::Button);if($item -and $item.Current.IsEnabled){$item}} 'Save did not enable'
 Click $save $process.MainWindowHandle
 Wait-For {Find $root 'Saved the local sample; no external content changed.' ([System.Windows.Automation.ControlType]::Text)} 'Save feedback unavailable'|Out-Null
 Wait-For {Find $root 'Edit fictional page: Harbor reading room test' ([System.Windows.Automation.ControlType]::Button)} 'Saved row title not visible'|Out-Null
 if((Find $root 'Save local sample' ([System.Windows.Automation.ControlType]::Button)).Current.IsEnabled){throw 'Save stayed enabled after local save'}
 $report.steps.savedTitle=$edited
 Click $title $process.MainWindowHandle 0.2
 Key 0x23 $process.MainWindowHandle $true
 Key 0x58 $process.MainWindowHandle
 Wait-For {$value=$title.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value;if($value -eq 'Harbor reading room testx'){$value}} 'Second title edit did not apply'|Out-Null
 $garden=Wait-For {Find $root 'Edit fictional page: Community garden' ([System.Windows.Automation.ControlType]::Button)} 'Garden edit action unavailable'
 Click $garden $process.MainWindowHandle
 Wait-For {Find $root 'Discard unsaved local changes before continuing?' ([System.Windows.Automation.ControlType]::Text)} 'Dirty navigation decision did not appear'|Out-Null
 $image=Join-Path $folder 'editorial-dirty-decision.png'
 Capture $process.MainWindowHandle $image
 $report.steps.dirtyDecisionScreenshotSha256=(Get-FileHash $image -Algorithm SHA256).Hash.ToLowerInvariant()
 $keep=Wait-For {Find $root 'Keep editing' ([System.Windows.Automation.ControlType]::Button)} 'Keep editing action unavailable'
 Click $keep $process.MainWindowHandle
 Wait-For {$decision=Find $root 'Discard unsaved local changes before continuing?' ([System.Windows.Automation.ControlType]::Text);if(-not $decision){$true}} 'Keep editing did not close the decision'|Out-Null
 if($title.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value -ne 'Harbor reading room testx'){throw 'Keep editing lost the unsaved title'}
 $report.steps.keepEditingRetainedDraft=$true
 Click $garden $process.MainWindowHandle
 Wait-For {Find $root 'Discard unsaved local changes before continuing?' ([System.Windows.Automation.ControlType]::Text)} 'Second dirty navigation decision did not appear'|Out-Null
 $discard=Wait-For {Find $root 'Discard changes' ([System.Windows.Automation.ControlType]::Button)} 'Discard action unavailable'
 Click $discard $process.MainWindowHandle
 Wait-For {$editor=Find $root 'Local page title' ([System.Windows.Automation.ControlType]::Edit);if($editor -and $editor.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value -eq 'Community garden'){$editor}} 'Discard did not open the requested page'|Out-Null
 if(-not (Find $root 'Edit fictional page: Harbor reading room test' ([System.Windows.Automation.ControlType]::Button))){throw 'Discard changed the previously saved row'}
 $report.steps.discardOpenedRequestedPage=$true
 $report.passed=$true
}catch{$report.error=$_.Exception.Message;throw}finally{if($process -and -not $process.HasExited){$process.CloseMainWindow()|Out-Null;if(-not $process.WaitForExit(5000)){Stop-Process -Id $process.Id -Force}};$report.processExited=if($process){-not [bool](Get-Process -Id $process.Id -ErrorAction SilentlyContinue)}else{$true};$report|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $folder 'editorial-edit.json') -Encoding utf8}
