using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class SidebarsFixture:UserControl
{
    private BleeckerSidebar current;
    public SidebarsFixture(){InitializeComponent();current=Preview;Preview.ItemInvoked+=(_,item)=>Status.Text="Consumer invoked "+item.Href;}
    internal async Task VerifyAsync(BleeckerColorScheme scheme,string folder)
    {
        using var document=JsonDocument.Parse(CatalogReferenceResources.Open(typeof(SidebarsFixture).Assembly, "Orchard.Catalog.Reference.SidebarLayout.json")??throw new InvalidOperationException("Missing Sidebar reference."));
        using var sourceInput=new NativeInputSession(XamlRoot,folder,$"sidebar-source-{scheme}");await sourceInput.AcquireAsync(Outside);await sourceInput.MoveAsync(Outside);
        var measurements=new List<object>();BleeckerSidebar? sourceSidebar=null;
        try
        {
        foreach(var sample in document.RootElement.GetProperty("cases").EnumerateArray().Where(c=>c.GetProperty("scheme").GetString()==scheme.ToString()))
        {
            var id=sample.GetProperty("id").GetString()!;var sidebar=Create(sample,sourceSidebar);sourceSidebar=sidebar;
            await MountAsync(sidebar);var normal=sample.GetProperty("normal");var expectedRows=normal.GetProperty("rows").EnumerateArray().ToArray();
            var actualRows=Actions(sidebar);var actual=actualRows.Select(row=>new{label=((BleeckerSidebarItem)row.Tag).Label,kind=row is HyperlinkButton?"A":"BUTTON",bounds=Bounds(row,sidebar),row.IsEnabled,row.IsHitTestVisible,row.Opacity,paint=PaintDetails(row)}).ToArray();
            var surface=Bounds(sidebar,sidebar);var nav=Find<ScrollViewer>(sidebar,"PART_Navigation");var navBounds=Bounds(nav,sidebar);
            measurements.Add(new{id,scheme,surface,navBounds,nav.ExtentHeight,rows=actual});
            Compare(surface,normal.GetProperty("surface").GetProperty("box"),id+" surface");Compare(navBounds,normal.GetProperty("nav").GetProperty("box"),id+" navigation");
            Require(actualRows.Length==expectedRows.Length,$"{id}: native rows {actualRows.Length} differ from source {expectedRows.Length}.");
            for(var index=0;index<actualRows.Length;index++)
            {
                var row=actualRows[index];var expected=expectedRows[index];Compare(Bounds(row,sidebar),expected.GetProperty("box"),id+" row "+index);
                var item=(BleeckerSidebarItem)row.Tag;var peer=FrameworkElementAutomationPeer.CreatePeerForElement(row);
                var rgb=Regex.Matches(document.RootElement.GetProperty("styles")[expected.GetProperty("styleIndex").GetInt32()].GetProperty("color").GetString()!,@"\d+").Select(match=>byte.Parse(match.Value)).ToArray();
                var label=Descendants<TextBlock>(row).SingleOrDefault(text=>text.Name=="PART_Label");
                if(label is not null)Require(Matches(label.Foreground,rgb),id+" prepared label lost its actual source foreground: "+Hex(label.Foreground));
                if(item.Icon is SourceIcon icon)Require(Matches(icon.Foreground,rgb),id+" original icon lost currentColor: "+Hex(icon.Foreground));
                Require(peer.GetName()==item.Label && row.NavigateUriIfAny() is null,id+" lost accessible name or launched a consumer route.");
                if(expected.GetProperty("tag").GetString()=="BUTTON")
                {Require(peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider provider&&provider.ExpandCollapseState.ToString().ToLowerInvariant()==(expected.GetProperty("attributes").GetProperty("aria-expanded").GetString()=="true"?"expanded":"collapsed"),id+" lost group expansion state.");}
                else{Require(row is HyperlinkButton && item.Href==expected.GetProperty("attributes").GetProperty("href").GetString(),id+" rewrote an original href.");}
            }
            if(new[]{"default","collapsed","long-labels","multilingual","dense","custom-link"}.Contains(id))await MainWindow.CaptureElementAsync(sidebar,Path.Combine(folder,$"sidebar-{scheme}-{id}.png"));
            if(id=="state-retention"||id=="state-closed")await VerifyStateAsync(sidebar,id=="state-retention");
            if(id=="state-reorder")await VerifyReorderAsync(sidebar);
            if(id=="dense")
            {
                var header=Bounds(Find<Border>(sidebar,"PART_Header"),sidebar);var footer=Bounds(Find<Border>(sidebar,"PART_Footer"),sidebar);
                await FocusAsync((Control)actualRows[^1]);await AwaitAsync(()=>nav.VerticalOffset>0,"Focusing the final destination did not scroll navigation.");await MainWindow.AwaitPresentationAsync(sidebar);
                Require(header==Bounds(Find<Border>(sidebar,"PART_Header"),sidebar)&&footer==Bounds(Find<Border>(sidebar,"PART_Footer"),sidebar),"Navigation scrolling moved its header or footer.");
                await File.WriteAllTextAsync(Path.Combine(folder,$"sidebar-{scheme}-scroll.json"),JsonSerializer.Serialize(new{nav.VerticalOffset,nav.ScrollableHeight,nav.ViewportHeight,header,footer,focused=actualRows[^1].FocusState.ToString()}));
                await FocusAsync(Outside);
            }
        }
        }
        finally{await File.WriteAllTextAsync(Path.Combine(folder,$"sidebar-{scheme}-layout.json"),JsonSerializer.Serialize(measurements,new JsonSerializerOptions{WriteIndented=true}));}
        await VerifyInputAsync(scheme,folder);
        await VerifyLifecycleAsync();
        await MountAsync(Preview);
    }
    private BleeckerSidebar Create(JsonElement sample,BleeckerSidebar? consumer=null)
    {
        var props=sample.GetProperty("props");var isCollapsed=props.TryGetProperty("collapsed",out var collapsedValue)&&collapsedValue.GetBoolean();
        // A different initial collapse mode gets a fresh owner so enabled
        // animation cannot turn independent source cases into a transition.
        var control=consumer is not null&&consumer.Collapsed==isCollapsed?consumer:new BleeckerSidebar{Collapsed=isCollapsed};
        // Independent reference cases start with no keyed state. Replace the
        // collection through the consumer API; never clear consumer-owned data.
        control.Items=new();control.Header=null;control.Footer=null;control.LinkTemplate=null;
        control.ClearValue(WidthProperty);control.ClearValue(PaddingProperty);control.ClearValue(BleeckerSidebar.CollapsedPaddingProperty);
        control.Height=sample.GetProperty("height").GetDouble();control.Items=Items(props.GetProperty("items"));
        if(props.TryGetProperty("collapsed",out var collapsed))control.Collapsed=collapsed.GetBoolean();
        if(props.TryGetProperty("header",out var header))control.Header=SourceContent(header);
        if(props.TryGetProperty("footer",out var footer))control.Footer=SourceContent(footer);
        if(props.TryGetProperty("customLink",out var custom)&&custom.GetBoolean())control.LinkTemplate=(DataTemplate)Resources["ConsumerLink"];
        if(sample.GetProperty("id").GetString()=="custom-width"){control.Width=320;control.Padding=new Thickness(16,20,16,20);control.CollapsedPadding=control.Padding;}
        Stage.Width=sample.GetProperty("hostWidth").GetDouble();return control;
    }
    private static ObservableCollection<BleeckerSidebarItem> Items(JsonElement array)
    {
        var items=new ObservableCollection<BleeckerSidebarItem>();foreach(var data in array.EnumerateArray())
        {
            var item=new BleeckerSidebarItem{Id=data.GetProperty("id").GetString()!,Label=data.GetProperty("label").GetString()!};
            if(data.TryGetProperty("href",out var href))item.Href=href.GetString();
            if(data.TryGetProperty("active",out var active))item.Active=active.GetBoolean();
            if(data.TryGetProperty("disabled",out var disabled))item.Disabled=disabled.GetBoolean();
            if(data.TryGetProperty("icon",out var icon))item.Icon=SourceContent(icon);
            if(data.TryGetProperty("badge",out var badge))item.Badge=SourceContent(badge);
            if(data.TryGetProperty("items",out var children))item.Items=Items(children);
            items.Add(item);
        }return items;
    }
    private static object? SourceContent(JsonElement data)
    {
        if(data.ValueKind==JsonValueKind.Null)return null;if(data.ValueKind==JsonValueKind.True)return true;if(data.ValueKind==JsonValueKind.False)return false;
        if(data.ValueKind==JsonValueKind.String)return data.GetString();if(data.ValueKind==JsonValueKind.Number)return data.GetDouble();
        var kind=data.GetProperty("kind").GetString();var text=data.TryGetProperty("text",out var value)?value.GetString()!:"";
        if(kind=="icon")return new SourceIcon(data.TryGetProperty("size",out var size)?size.GetDouble():20);
        if(kind=="badge")
        {
            var border=(Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerSandBrush}' CornerRadius='4' Padding='6,2'/>");
            var textBlock=new TextBlock{Text=text,FontSize=12,LineHeight=16,LineStackingStrategy=LineStackingStrategy.BlockLineHeight,UseLayoutRounding=false};
            var content=new ContentControl{Content=border,IsTabStop=false,Padding=new Thickness(0),UseLayoutRounding=false};border.Child=textBlock;
            textBlock.SetBinding(TextBlock.ForegroundProperty,new Binding{Source=content,Path=new PropertyPath(nameof(Control.Foreground))});textBlock.SetBinding(TextBlock.FontFamilyProperty,new Binding{Source=content,Path=new PropertyPath(nameof(Control.FontFamily))});textBlock.SetBinding(TextBlock.FontWeightProperty,new Binding{Source=content,Path=new PropertyPath(nameof(Control.FontWeight))});return content;
        }
        if(kind=="button")return new Button{Content=new TextBlock{Text=text,FontSize=16,FontFamily=(FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"],LineHeight=24,LineStackingStrategy=LineStackingStrategy.BlockLineHeight},Height=24,MinHeight=0,MinWidth=0,Padding=new Thickness(0),BorderThickness=new Thickness(0)};
        return new TextBlock{Text=text,FontSize=16,LineHeight=24,LineStackingStrategy=LineStackingStrategy.BlockLineHeight,FontWeight=Microsoft.UI.Text.FontWeights.Medium,FontFamily=(FontFamily)Application.Current.Resources["BleeckerPrimaryMediumFontFamily"],UseLayoutRounding=false};
    }
    private async Task MountAsync(BleeckerSidebar sidebar)
    {
        if(!ReferenceEquals(current,sidebar)){Stage.Children.Clear();Stage.Children.Add(sidebar);current=sidebar;}
        await AwaitAsync(()=>sidebar.IsLoaded,"Sidebar did not mount.");sidebar.ApplyTemplate();sidebar.UpdateLayout();
        foreach(var action in Actions(sidebar))action.ApplyTemplate();
        await AwaitAsync(()=>Actions(sidebar).All(action=>action.IsLoaded),"Sidebar actions did not load their native templates.");
        sidebar.UpdateLayout();await MainWindow.AwaitPresentationAsync(sidebar);
    }
    private async Task VerifyStateAsync(BleeckerSidebar sidebar,bool open)
    {
        var group=sidebar.Items[0];var original=Actions(sidebar)[0];group.Items[0].Active=!open;await Present(sidebar);CheckOpen(sidebar,open);
        sidebar.Collapsed=true;await Present(sidebar);Require(Actions(sidebar).Length==0,"Collapsed group without a route remained visible.");
        sidebar.Collapsed=false;await Present(sidebar);CheckOpen(sidebar,open);
        var replacement=new BleeckerSidebarItem{Id=group.Id,Label="Replacement group",Items=new(){new(){Id="replacement",Label="Replacement child",Href="#replacement"}}};
        sidebar.Items[0]=replacement;await Present(sidebar);CheckOpen(sidebar,open);
        sidebar.Items.RemoveAt(0);await Present(sidebar);replacement.Items[0].Active=true;sidebar.Items.Add(replacement);await Present(sidebar);CheckOpen(sidebar,true);
        Require(!ReferenceEquals(original,Actions(sidebar)[0]),"Removed group did not establish a new mounted trigger.");
    }
    private async Task VerifyReorderAsync(BleeckerSidebar sidebar)
    {
        var original=Actions(sidebar).Where(a=>a is not HyperlinkButton).ToArray();sidebar.Items.Move(0,2);await Present(sidebar);
        var moved=Actions(sidebar).Where(a=>a is not HyperlinkButton).ToArray();Require(ReferenceEquals(original[0],moved[1])&&ReferenceEquals(original[1],moved[0]),"Keyed reorder remounted triggers.");
    }
    private async Task VerifyInputAsync(BleeckerColorScheme scheme,string folder)
    {
        Stage.Width=600;var disabled=new BleeckerSidebarItem{Id="disabled",Label="Disabled destination",Href="opaque:consumer/path?x=1#part",Disabled=true};
        var group=new BleeckerSidebarItem{Id="group",Label="Disabled group",Disabled=true,Items=new(){new(){Id="child",Label="Child",Href="#child"}}};
        var sidebar=new BleeckerSidebar{Height=360,Items=new(){disabled,group}};var invoked=new List<BleeckerSidebarItem>();var commands=0;object? parameter=null;
        var command=new FixtureCommand(value=>{commands++;parameter=value;});disabled.CommandParameter=disabled;disabled.Command=command;sidebar.ItemInvoked+=(_,item)=>invoked.Add(item);
        await MountAsync(sidebar);using var input=new NativeInputSession(XamlRoot,folder,$"sidebar-{scheme}");await input.AcquireAsync(Outside);
        var link=(HyperlinkButton)Actions(sidebar)[0];Require(link.IsEnabled&&!link.IsHitTestVisible&&link.Opacity==.5,"CSS-disabled link acquired native disabled semantics.");
        await FocusAsync(link);await input.KeyAsync(0x0D,link);await AwaitAsync(()=>invoked.Count==1,"Keyboard did not invoke disabled source link.");
        Require(ReferenceEquals(invoked[0],disabled)&&commands==1&&ReferenceEquals(parameter,disabled)&&link.NavigateUri is null,"Native activation cloned data, rewrote routing or lost the command parameter.");
        await input.ClickAsync(link);await Present(sidebar);Require(invoked.Count==1&&commands==1,"Pointer activated a CSS-disabled link.");
        var trigger=Actions(sidebar)[1];await input.ClickAsync(trigger);await Present(sidebar);CheckOpen(sidebar,true);Require(invoked.Count==1,"Group click invoked an item callback.");
        var childLink=(HyperlinkButton)Actions(sidebar)[2];await FocusAsync(childLink);await input.KeyAsync(0x0D,childLink);await AwaitAsync(()=>invoked.Count==2,"Child navigation did not invoke the consumer.");
        Require(ReferenceEquals(invoked[^1],group.Items[0]),"Child activation replaced the original child record.");
        await FocusAsync(trigger);await input.KeyAsync(0x20,trigger);await Present(sidebar);CheckOpen(sidebar,false);
        disabled.Disabled=false;await Present(sidebar);await input.ClickAsync(link);await AwaitAsync(()=>invoked.Count==3,"Enabled source link did not activate from the pointer.");
        var primary=scheme==BleeckerColorScheme.Light?new byte[]{45,45,45}:new byte[]{240,244,248};
        await AwaitAsync(()=>Matches(Find<TextBlock>(link,"PART_Label").Foreground,primary),"Hovered label did not receive the source primary foreground.");
        await input.PressPointerAsync();
        try
        {
            await AwaitAsync(()=>link.IsPressed&&((FrameworkElement)VisualTreeHelper.GetChild(link,0)).RenderTransform is TranslateTransform move&&move.Y==1,"Held source press did not translate its actual chrome one unit.");
            Require(invoked.Count==3,"Pointer down invoked a navigation callback.");
            await MainWindow.CaptureElementAsync(sidebar,Path.Combine(folder,$"sidebar-{scheme}-pressed.png"));await input.MoveAsync(Outside);
        }
        finally{await ReleaseAsync(input,folder,scheme);}
        await AwaitAsync(()=>!link.IsPressed,"Cancelled press retained its native pressed state.");Require(invoked.Count==3,"Release outside the destination invoked it.");
        command.Enabled=false;await AwaitAsync(()=>!link.IsEnabled,"CanExecuteChanged did not disable the native destination.");
        var activation=link.Style.Setters.OfType<Setter>().Select(setter=>setter.Value).OfType<BleeckerSidebarLinkContext>().Single().Command;
        Require(!activation.CanExecute(null),"Source activation ignored command availability.");activation.Execute(null);Require(commands==2&&invoked.Count==3,"Rejected command executed a consumer callback.");
        command.Enabled=true;await AwaitAsync(()=>link.IsEnabled,"Command availability did not restore the native destination.");
        await FocusAsync(link);await TabAsync(input,link,trigger,folder,scheme);
        sidebar.LinkTemplate=(DataTemplate)Resources["ConsumerLink"];await Present(sidebar);link=(HyperlinkButton)Actions(sidebar)[0];
        await FocusAsync(link);await input.KeyAsync(0x0D,link);await AwaitAsync(()=>invoked.Count==4,"Custom link did not preserve activation.");
        Require(ReferenceEquals(invoked[^1],disabled)&&ReferenceEquals(link.Tag,disabled),"Custom link did not retain the original record.");
        await MainWindow.CaptureElementAsync(sidebar,Path.Combine(folder,$"sidebar-{scheme}-interaction.png"));
    }
    private async Task VerifyLifecycleAsync()
    {
        var icon=new Border{Width=20,Height=20};var item=new BleeckerSidebarItem{Id="original",Label="Original",Href="#original",Icon=icon};
        var original=new ObservableCollection<BleeckerSidebarItem>{item};var sidebar=new BleeckerSidebar{Height=300,Items=original};await MountAsync(sidebar);
        var link=Actions(sidebar).Single();await FocusAsync((Control)link);item.Label="Updated";await Present(sidebar);Require(ReferenceEquals(link,Actions(sidebar).Single())&&ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),link)&&FrameworkElementAutomationPeer.CreatePeerForElement(link).GetName()=="Updated","Live item update remounted the destination, dropped focus or kept its old name.");
        item.Active=true;await Present(sidebar);Require(Hex(Find<TextBlock>(link,"PART_Label").Foreground)==Hex(Find<Border>(link,"PART_ActivePalette").BorderBrush),"Live active state did not reach the prepared label's real foreground.");
        item.Id="replacement-key";await Present(sidebar);Require(!ReferenceEquals(link,Actions(sidebar).Single())&&Descendants<Border>(sidebar).Any(b=>ReferenceEquals(b,icon)),"Changed key did not remount while retaining original icon content.");
        Stage.Children.Clear();sidebar.Items=new(){new(){Id="next",Label="Next",Href="#next"}};original.Add(new(){Id="stale",Label="Stale",Href="#stale"});Stage.Children.Add(sidebar);await MountAsync(sidebar);
        Require(Actions(sidebar).Length==1&&((BleeckerSidebarItem)Actions(sidebar)[0].Tag).Id=="next","Detached collection replacement retained stale producers.");
        var template=sidebar.Template;sidebar.Template=null;sidebar.Template=template;sidebar.ApplyTemplate();await Present(sidebar);Require(Actions(sidebar).Length==1,"Template replacement lost native destinations.");
        var model=new object();var slot=(DataTemplate)Resources["ScalarSlot"];
        sidebar.HeaderTemplate=slot;sidebar.Header=false;sidebar.Items[0].IconTemplate=slot;sidebar.Items[0].Icon=model;await Present(sidebar);
        Require(Find<Border>(sidebar,"PART_Header").ActualHeight==45&&Descendants<ContentControl>(sidebar).Any(content=>ReferenceEquals(content.Tag,model))&&Descendants<ContentControl>(sidebar).Any(content=>content.Tag is false),"Native template content lost original model identity or treated a rendered false payload as an absent slot.");
        var paint=new SolidColorBrush(Windows.UI.Color.FromArgb(255,23,113,147));var consumerIcon=new ContentControl{Width=20,Height=20,Foreground=paint};
        var owned=new BleeckerSidebarItem{Id="format",Label="Formatting",Href="#format",Icon=consumerIcon};sidebar.Items=new(){owned};await Present(sidebar);
        Require(ReferenceEquals(consumerIcon.Foreground,paint),"Sidebar replaced a consumer foreground value.");
        var binding=new Binding{Source=sidebar,Path=new PropertyPath(nameof(BleeckerSidebar.Foreground))};consumerIcon.SetBinding(Control.ForegroundProperty,binding);consumerIcon.FontSize=21;owned.Active=true;await Present(sidebar);
        Require(consumerIcon.GetBindingExpression(Control.ForegroundProperty)?.ParentBinding==binding&&consumerIcon.FontSize==21,"Sidebar replaced an explicit consumer formatting binding or size.");
        var style=new Style{TargetType=typeof(ContentControl)};style.Setters.Add(new Setter(Control.FontWeightProperty,Microsoft.UI.Text.FontWeights.SemiBold));consumerIcon.Style=style;await Present(sidebar);
        Require(consumerIcon.FontWeight.Weight==600&&consumerIcon.GetBindingExpression(Control.ForegroundProperty)?.ParentBinding==binding,"Sidebar replaced an explicit style or binding during a style change.");
        sidebar.Items.Clear();await Present(sidebar);
        Require(consumerIcon.ReadLocalValue(Control.FontFamilyProperty)==DependencyProperty.UnsetValue&&consumerIcon.GetBindingExpression(Control.ForegroundProperty)?.ParentBinding==binding&&consumerIcon.FontSize==21,"Detaching content left owned formatting behind or cleared consumer values.");
    }
    private static Task Present(FrameworkElement element){element.UpdateLayout();return MainWindow.AwaitPresentationAsync(element);}
    private static void CheckOpen(BleeckerSidebar sidebar,bool open)
    {var group=Actions(sidebar).First(a=>a is not HyperlinkButton);var peer=FrameworkElementAutomationPeer.CreatePeerForElement(group);Require(peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider provider&&provider.ExpandCollapseState==(open?ExpandCollapseState.Expanded:ExpandCollapseState.Collapsed),"Group expansion changed across a source update.");}
    private static ButtonBase[] Actions(BleeckerSidebar sidebar)=>Descendants<ButtonBase>(Find<StackPanel>(sidebar,"PART_Rows")).Where(a=>a.Tag is BleeckerSidebarItem).ToArray();
    private static Rect Bounds(FrameworkElement element,UIElement relative){var origin=element.TransformToVisual(relative).TransformPoint(new Point());return new Rect(origin.X,origin.Y,element.ActualWidth,element.ActualHeight);}
    private static object PaintDetails(ButtonBase action)
    {
        var context=action.Style.Setters.OfType<Setter>().Select(setter=>setter.Value).OfType<BleeckerSidebarLinkContext>().Single();
        var item=(BleeckerSidebarItem)action.Tag;
        return new{normal=Hex(Find<Border>(action,"PART_NormalPalette").BorderBrush),active=Hex(Find<Border>(action,"PART_ActivePalette").BorderBrush),painted=Hex(Find<ContentPresenter>(action,"PART_Content").Foreground),context=Hex(context.Foreground),labels=Descendants<TextBlock>(action).Where(text=>text.Text==item.Label).Select(text=>new{text.Text,foreground=Hex(text.Foreground)}).ToArray()};
    }
    private static string Hex(Brush? brush)=>brush is SolidColorBrush solid?$"#{solid.Color.A:X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2};opacity={solid.Opacity:R}":brush is null?"null":brush.GetType().Name;
    private static bool Matches(Brush? brush,byte[] rgb)=>brush is SolidColorBrush solid&&solid.Color.R==rgb[0]&&solid.Color.G==rgb[1]&&solid.Color.B==rgb[2]&&solid.Opacity==1;
    private static void Compare(Rect actual,JsonElement expected,string context)
    {foreach(var(key,value)in new[]{("x",actual.X),("y",actual.Y),("width",actual.Width),("height",actual.Height)})Require(Math.Abs(value-expected.GetProperty(key).GetDouble())<=1d/32,$"{context} {key}: {value:R} differs from {expected.GetProperty(key).GetDouble():R}.");}
    private static async Task FocusAsync(Control control)
    {
        if(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(control.XamlRoot),control))return;
        var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);void Focused(object sender,RoutedEventArgs args){if(ReferenceEquals(args.OriginalSource,control))ready.TrySetResult();}
        control.GotFocus+=Focused;try{Require(control.Focus(FocusState.Keyboard),"Native destination refused keyboard focus.");await ready.Task.WaitAsync(TimeSpan.FromSeconds(2));}finally{control.GotFocus-=Focused;}
    }
    private async Task TabAsync(NativeInputSession input,Control from,Control to,string folder,BleeckerColorScheme scheme)
    {
        var downCount=0;var upCount=0;var downOwned=true;var upOwned=true;
        var released=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var down=new Microsoft.UI.Xaml.Input.KeyEventHandler((_,args)=>{if(args.Key!=Windows.System.VirtualKey.Tab)return;downCount++;downOwned&=ReferenceEquals(args.OriginalSource,from);});
        var up=new Microsoft.UI.Xaml.Input.KeyEventHandler((_,args)=>{if(args.Key!=Windows.System.VirtualKey.Tab)return;upCount++;upOwned&=ReferenceEquals(args.OriginalSource,to);released.TrySetResult();});
        XamlRoot.Content.AddHandler(UIElement.KeyDownEvent,down,true);XamlRoot.Content.AddHandler(UIElement.KeyUpEvent,up,true);
        try
        {
            input.Key(0x09);await released.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Require(downCount==1&&upCount==1&&downOwned&&upOwned&&ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),to),"Tab did not deliver one owned press and release across its real focus transfer.");
        }
        finally
        {
            XamlRoot.Content.RemoveHandler(UIElement.KeyDownEvent,down);XamlRoot.Content.RemoveHandler(UIElement.KeyUpEvent,up);
            await File.WriteAllTextAsync(Path.Combine(folder,$"sidebar-{scheme}-tab.json"),JsonSerializer.Serialize(new{downCount,upCount,downOwned,upOwned,from=((BleeckerSidebarItem)from.Tag).Id,to=((BleeckerSidebarItem)to.Tag).Id}));
        }
    }
    private async Task ReleaseAsync(NativeInputSession input,string folder,BleeckerColorScheme scheme)
    {
        var expected=input.ReadPointerPosition();var delivered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var receipts=0;var exact=false;
        var released=new Microsoft.UI.Xaml.Input.PointerEventHandler((_,args)=>
        {
            var point=args.GetCurrentPoint(XamlRoot.Content);var client=NativeScreenCapture.ClientBounds(input.Window);receipts++;
            exact=!args.IsGenerated&&args.Pointer.PointerDeviceType==Microsoft.UI.Input.PointerDeviceType.Mouse&&!point.Properties.IsLeftButtonPressed&&Math.Abs(client.X+point.Position.X*XamlRoot.RasterizationScale-expected.X)<.001&&Math.Abs(client.Y+point.Position.Y*XamlRoot.RasterizationScale-expected.Y)<.001;
            delivered.TrySetResult();
        });
        XamlRoot.Content.AddHandler(UIElement.PointerReleasedEvent,released,true);
        try{input.PointerButton(false);await delivered.Task.WaitAsync(TimeSpan.FromSeconds(2));Require(receipts==1&&exact&&input.OwnsCurrentPoint,"Cancelled press did not release once at its exact owned mouse position.");}
        finally{XamlRoot.Content.RemoveHandler(UIElement.PointerReleasedEvent,released);await File.WriteAllTextAsync(Path.Combine(folder,$"sidebar-{scheme}-cancelled-release.json"),JsonSerializer.Serialize(new{receipts,exact,expected}));}
    }
    private static async Task AwaitAsync(Func<bool> ready,string message){var watch=Stopwatch.StartNew();while(!ready()&&watch.ElapsedMilliseconds<2000)await Task.Delay(16);Require(ready(),message);}
    private static IEnumerable<T> Descendants<T>(DependencyObject owner)where T:DependencyObject{for(var i=0;i<VisualTreeHelper.GetChildrenCount(owner);i++){var child=VisualTreeHelper.GetChild(owner,i);if(child is T found)yield return found;foreach(var nested in Descendants<T>(child))yield return nested;}}
    private static T Find<T>(DependencyObject owner,string name)where T:FrameworkElement=>Descendants<T>(owner).Single(element=>element.Name==name);
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private sealed class FixtureCommand(Action<object?> execute):ICommand
    {
        private bool enabled=true;
        internal bool Enabled{get=>enabled;set{enabled=value;CanExecuteChanged?.Invoke(this,EventArgs.Empty);}}
        public bool CanExecute(object? parameter)=>enabled;public void Execute(object? parameter)=>execute(parameter);public event EventHandler? CanExecuteChanged;
    }
    private sealed class SourceIcon:ContentControl
    {
        internal SourceIcon(double size)
        {
            Width=Height=size;IsTabStop=false;UseLayoutRounding=false;
            var view=(Viewbox)XamlReader.Load("<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Canvas Width='24' Height='24'><Path Data='M4,4 H20 V20 H4 Z M8,8 H16 V16 H8 Z' StrokeThickness='2' StrokeLineJoin='Miter'/></Canvas></Viewbox>");
            var path=(Microsoft.UI.Xaml.Shapes.Path)((Canvas)view.Child).Children[0];path.SetBinding(Microsoft.UI.Xaml.Shapes.Shape.StrokeProperty,new Binding{Source=this,Path=new PropertyPath(nameof(Foreground))});Content=view;AutomationProperties.SetAccessibilityView(this,AccessibilityView.Raw);
        }
    }
}

internal static class SidebarNativeInspection
{
    internal static Uri? NavigateUriIfAny(this ButtonBase action)=>action is HyperlinkButton link?link.NavigateUri:null;
}
