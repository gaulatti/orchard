using System.Reflection;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunFilterChipsAsync(string folder)
    {
        using var sourceStream=CatalogReferenceResources.Open(Assembly.GetExecutingAssembly(), "Orchard.Catalog.Reference.FilterChip.json")??throw new InvalidOperationException("Missing FilterChip reference.");
        using var source=await JsonDocument.ParseAsync(sourceStream);
        var original=Content;var page=new Page();var canvas=new Canvas();page.Content=canvas;
        var reports=new List<object>();var inputs=new List<object>();var phase="mount";var passed=false;NativeInputSession? input=null;
        try
        {
            Content=page;await AwaitPresentationAsync(page);input=new NativeInputSession(page.XamlRoot,folder,"filter-chip");await input.AcquireAsync(canvas);
            var width=(int)page.XamlRoot.Size.Width;Require(width is 320 or 640,"FilterChip reference requires a 320 or 640 client.");
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                page.RequestedTheme=scheme;canvas.Background=new SolidColorBrush(scheme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));
                await input.MoveAsync(canvas,true,new Point(width-10,400));await AwaitPresentationAsync(page);
                foreach(var reference in source.RootElement.GetProperty("cases").EnumerateArray().Where(item=>item.GetProperty("width").GetInt32()==width&&item.GetProperty("scheme").GetString()==scheme.ToString()))
                {
                    var spec=reference.GetProperty("props");phase=reference.GetProperty("id").GetString()!;
                    canvas.Children.Clear();var slot=new Grid{Width=width-40,HorizontalAlignment=HorizontalAlignment.Left};Canvas.SetLeft(slot,20);Canvas.SetTop(slot,20);canvas.Children.Add(slot);
                    FrameworkElement control;
                    if(spec.TryGetProperty("group",out var grouped)&&grouped.GetBoolean())
                    {
                        var group=new BleeckerFilterGroup();foreach(var filter in spec.GetProperty("filters").EnumerateArray()){var model=new BleeckerFilter(filter.GetProperty("label").GetString()!,filter.TryGetProperty("value",out var value)?value.GetString():null);if(filter.TryGetProperty("removable",out var remove)&&remove.GetBoolean())model.RemoveRequested+=(_,_)=>{};group.Filters.Add(model);}
                        if(spec.TryGetProperty("clear",out var clear)&&clear.GetBoolean())group.ClearAllRequested+=(_,_)=>{};control=group;
                    }
                    else
                    {
                        var chip=new BleeckerFilterChip{Label=spec.GetProperty("label").GetString()!,Value=spec.TryGetProperty("value",out var value)?value.GetString():null,HorizontalAlignment=HorizontalAlignment.Left};
                        if(spec.TryGetProperty("removable",out var remove)&&remove.GetBoolean())chip.RemoveRequested+=(_,_)=>{};control=chip;
                    }
                    slot.Children.Add(control);await AwaitPresentationAsync(page);
                    var chips=control is BleeckerFilterChip single?new[]{single}:Descendants<BleeckerFilterChip>(control).ToArray();
                    reports.Add(new{id=phase,width,scheme=scheme.ToString(),root=Box(slot),group=control is BleeckerFilterGroup?Box(control):null,
                        chips=chips.Select(chip=>new{box=Box(chip),label=chip.Label,value=chip.Value,background=Paint(chip.Background),border=Paint(chip.BorderBrush),chip.Padding,chip.CornerRadius,
                            texts=Descendants<TextBlock>(chip).Where(text=>VisualTreeHelper.GetParent(text)is FrameworkElement parent&&parent.Name is "FilterLabel" or "FilterValue"&&parent.Visibility==Visibility.Visible).Select(text=>new{text=text.Text,box=Box((FrameworkElement)VisualTreeHelper.GetParent(text)),baseline=text.TransformToVisual(page).TransformPoint(new Point()).Y+text.BaselineOffset,foreground=Paint(text.Foreground)}).ToArray(),
                            remove=Descendants<Button>(chip).Where(button=>button.Visibility==Visibility.Visible).Select(button=>new{box=Box(button),name=FrameworkElementAutomationPeer.CreatePeerForElement(button).GetName(),button.IsEnabled}).SingleOrDefault()}).ToArray(),
                        clear=Descendants<Button>(control).Where(button=>button.Name=="ClearAllFilters").Select(button=>new{box=Box(button),name=FrameworkElementAutomationPeer.CreatePeerForElement(button).GetName(),button.IsEnabled}).SingleOrDefault()});
                    await SaveAsync();
                    var capturePath=Path.Combine(folder,reference.GetProperty("file").GetString()!);
                    if(chips.Length==0)
                        await NativeScreenCapture.CaptureClientAsync(input.Window,capturePath,
                            scheme==ElementTheme.Light?((byte)255,(byte)255,(byte)255):((byte)13,(byte)24,(byte)33));
                    else await NativeScreenCapture.CaptureClientAsync(input.Window,capturePath);
                    if(phase=="removable")await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,$"catalog-{scheme.ToString().ToLowerInvariant()}.png"));
                }
                phase="input-"+scheme;canvas.Children.Clear();var groupInput=new BleeckerFilterGroup{Width=width-40};Canvas.SetLeft(groupInput,20);Canvas.SetTop(groupInput,20);canvas.Children.Add(groupInput);
                var removed=0;var cleared=0;var modelInput=new BleeckerFilter("Status","Draft");modelInput.RemoveRequested+=(_,_)=>removed++;groupInput.Filters.Add(modelInput);groupInput.Filters.Add(new BleeckerFilter("Read-only","Culture"));groupInput.ClearAllRequested+=(_,_)=>cleared++;
                await AwaitPresentationAsync(page);var removeButton=Descendants<Button>(groupInput).Single(button=>AutomationProperties.GetName(button)=="Remove Status filter");
                var reducedMotionVerified=false;
                if(!new Windows.UI.ViewManagement.UISettings().AnimationsEnabled)
                {
                    var chrome=Descendants<Border>(removeButton).Single(border=>border.Name=="InlineActionChrome");
                    var actual=(SolidColorBrush)chrome.Background;
                    var expected=(SolidColorBrush)Descendants<TextBlock>(Descendants<BleeckerFilterChip>(groupInput).First()).Single(text=>text.Name=="PART_RemoveHoverBackground").Foreground;
                    await input.MoveAsync(removeButton);
                    Require(Math.Abs(actual.Color.R-expected.Color.R)<=1&&Math.Abs(actual.Color.G-expected.Color.G)<=1&&
                        Math.Abs(actual.Color.B-expected.Color.B)<=1&&Math.Abs(actual.Opacity-expected.Opacity)<.005,
                        "Reduced-motion Filter Chip hover did not reach its final paint immediately.");
                    await input.MoveAsync(canvas,true,new Point(width-10,400));
                    Require(actual.Opacity<.005,"Reduced-motion Filter Chip exit retained transitional paint.");
                    reducedMotionVerified=true;
                }
                Require(removeButton.Focus(FocusState.Keyboard),"Filter remove action refused keyboard focus.");await input.KeyAsync(0x20,removeButton);await input.KeyAsync(0x0D,removeButton);await input.ClickAsync(removeButton);
                Require(removed==3&&groupInput.Filters.Count==2,"Remove callbacks did not reach the original consumer or mutated its collection.");
                var clearButton=Descendants<Button>(groupInput).Single(button=>button.Name=="ClearAllFilters");await input.ClickAsync(clearButton);Require(cleared==1&&groupInput.Filters.Count==2,"Clear all mutated caller-owned filters.");
                modelInput.Label="Updated label";modelInput.Value="Revisão 日本語";await AwaitPresentationAsync(page);Require(AutomationProperties.GetName(removeButton)=="Remove Updated label filter","Live filter label did not reach the existing action name.");
                var parameter=new object();var executions=0;var command=new FilterFixtureCommand(value=>{Require(ReferenceEquals(value,parameter),"Filter command replaced its original parameter.");executions++;});
                modelInput.RemoveCommand=command;modelInput.RemoveCommandParameter=parameter;await AwaitPresentationAsync(page);command.Allowed=false;command.Notify();await AwaitPresentationAsync(page);Require(!removeButton.IsEnabled,"Filter command rejection did not disable the native button.");
                command.Allowed=true;command.Notify();await AwaitPresentationAsync(page);await input.ClickAsync(removeButton);Require(executions==1&&removed==4,"Filter command/event did not execute exactly once.");
                groupInput.IsEnabled=false;await AwaitPresentationAsync(page);Require(!removeButton.IsEnabled&&!clearButton.IsEnabled,"Group disablement did not reach its native actions.");groupInput.IsEnabled=true;await AwaitPresentationAsync(page);
                Require(removeButton.Focus(FocusState.Keyboard),"Filter action refused focus before reorder.");
                groupInput.Filters.Move(0,1);await AwaitPresentationAsync(page);Require(ReferenceEquals(removeButton,Descendants<Button>(groupInput).Single(button=>AutomationProperties.GetName(button)=="Remove Updated label filter")),"Filter reorder replaced its native action.");
                Require(Descendants<BleeckerFilterChip>(groupInput).Last().Label==modelInput.Label&&ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot),removeButton),"Filter reorder lost visual/automation order or focus.");
                await input.MoveAsync(canvas,true,new Point(width-10,400));await AwaitPresentationAsync(page);
                var updatedChip=Descendants<BleeckerFilterChip>(groupInput).Last();var paletteText=Descendants<TextBlock>(updatedChip).Single(text=>text.Name=="PART_Secondary");var brush=(SolidColorBrush)paletteText.Foreground;var originalColor=brush.Color;var originalOpacity=brush.Opacity;
                try{brush.Color=Microsoft.UI.Colors.Magenta;brush.Opacity=.6;await AwaitPresentationAsync(page);var line=Descendants<Microsoft.UI.Xaml.Shapes.Line>(removeButton).First();var painted=(SolidColorBrush)line.Stroke;Require(painted.Color.R==255&&painted.Color.B==255&&Math.Abs(painted.Opacity-.6)<.001,"Live source palette mutation did not reach the copied remove glyph paint.");}
                finally{brush.Color=originalColor;brush.Opacity=originalOpacity;}
                var template=groupInput.Template;groupInput.Template=null;await AwaitPresentationAsync(page);Require(!removeButton.IsLoaded&&!clearButton.IsLoaded,"Discarded FilterGroup template retained native actions.");
                groupInput.Template=template;groupInput.ApplyTemplate();await AwaitPresentationAsync(page);var remounted=Descendants<Button>(groupInput).Single(button=>AutomationProperties.GetName(button)=="Remove Updated label filter");
                Require(!ReferenceEquals(remounted,removeButton),"Replacement filter template reused a detached action.");removeButton=remounted;
                canvas.Children.Remove(groupInput);await AwaitPresentationAsync(page);modelInput.Label="Remounted label";canvas.Children.Add(groupInput);await AwaitPresentationAsync(page);
                Require(AutomationProperties.GetName(removeButton)=="Remove Remounted label filter"&&removeButton.IsEnabled,"Remounted group did not reconnect metadata or commands.");
                groupInput.Filters.Remove(modelInput);await AwaitPresentationAsync(page);Require(!removeButton.IsLoaded,"Removed filter retained a loaded action.");
                groupInput.Filters.Clear();await AwaitPresentationAsync(page);Require(groupInput.ActualHeight==0&&!Descendants<Button>(groupInput).Any(),"Empty filter group retained visual or interactive output.");
                var synchronous=0;var selfRemoving=new BleeckerFilter("Synchronous","Consumer removal");selfRemoving.RemoveRequested+=(_,_)=>{synchronous++;groupInput.Filters.Remove(selfRemoving);};groupInput.Filters.Add(selfRemoving);await AwaitPresentationAsync(page);
                var selfAction=Descendants<Button>(groupInput).Single(button=>AutomationProperties.GetName(button)=="Remove Synchronous filter");((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(selfAction).GetPattern(PatternInterface.Invoke)).Invoke();await AwaitPresentationAsync(page);
                Require(synchronous==1&&groupInput.Filters.Count==0&&!selfAction.IsLoaded,"Synchronous consumer removal retained or repeated its native action.");
                inputs.Add(new{scheme=scheme.ToString(),removed,cleared,executions,reducedMotionVerified,consumerOwnership=true,liveUpdates=true,canExecute=true,inheritedDisabled=true,reorderIdentity=true,reorderFocus=true,livePalette=true,templateReplacement=true,unloadRemount=true,emptyGroup=true,synchronousRemoval=true});await SaveAsync();
            }
            passed=true;
        }
        finally{input?.Dispose();Content=original;await AwaitPresentationAsync(CatalogRoot);await SaveAsync();}
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="filter-chip",environment=RuntimeEnvironment()}));
        object Box(FrameworkElement element){var at=element.TransformToVisual(page).TransformPoint(new Point());return new{x=at.X,y=at.Y,width=element.ActualWidth,height=element.ActualHeight};}
        static object Paint(Brush brush){var solid=(SolidColorBrush)brush;return new{color=ColorHex(solid.Color),opacity=solid.Opacity};}
        Task SaveAsync()=>File.WriteAllTextAsync(Path.Combine(folder,"filter-chip.json"),JsonSerializer.Serialize(new{passed,phase,reports,inputs,originalContentRestored=ReferenceEquals(Content,original)},new JsonSerializerOptions{WriteIndented=true}));
        static IEnumerable<T> Descendants<T>(DependencyObject parent)where T:DependencyObject
        {for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T match)yield return match;foreach(var nested in Descendants<T>(child))yield return nested;}}
    }
    private sealed class FilterFixtureCommand(Action<object?> action):ICommand
    {internal bool Allowed=true;public bool CanExecute(object? parameter)=>Allowed;public void Execute(object? parameter)=>action(parameter);public event EventHandler? CanExecuteChanged;internal void Notify()=>CanExecuteChanged?.Invoke(this,EventArgs.Empty);}
}
