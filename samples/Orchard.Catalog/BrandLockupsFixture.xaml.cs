using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Catalog;

public sealed partial class BrandLockupsFixture : UserControl
{
    private readonly ConsumerCommand command = new();
    private readonly object parameter = new();
    private int invocations;
    private string? invokedHref;
    private bool logoFailed;
    public BrandLockupsFixture()
    {
        InitializeComponent();
        var bitmap = new WriteableBitmap(4, 1);
        using (var pixels = bitmap.PixelBuffer.AsStream()) pixels.Write(new byte[] { 0,0,0,255, 110,70,30,255, 55,35,15,128, 0,0,0,0 });
        bitmap.Invalidate(); CustomImage.LogoSource = bitmap;
        SmallNamed.Command = command; SmallNamed.CommandParameter = parameter;
        foreach (var control in Rows.Children.OfType<BleeckerBrandLockup>().Concat(new[] { SmallLogo, LargeLogo, SmallNamed, LargeNamed, EmptyName, Disabled, CustomLink }))
            control.Invoked += (_, href) => { invocations++; invokedHref = href; Outcome.Text = $"Consumer action: {href}"; };
        FailedImage.LogoFailed += (_, args) => { logoFailed = true; Outcome.Text = "Consumer logo unavailable."; };
        FailedImage.LogoSource = new BitmapImage(new Uri("ms-appx:///missing-consumer-brand.png"));
    }

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        var samples = new List<object>();
        using var input = new NativeInputSession(XamlRoot, folder, $"brand-{scheme}");
        await input.AcquireAsync(Heading); await input.MoveAsync(Heading);
        var controls = new[] { SmallLogo, LargeLogo, SmallNamed, LargeNamed, EmptyName, Disabled, CustomLink, LongName, CustomImage };
        foreach (var control in controls) control.ApplyTemplate();
        await AwaitAsync(() => controls.All(control => Find<Image>(control).ActualWidth > 0), "Brand images did not produce native layout.");
        await MainWindow.AwaitPresentationAsync(this);
        foreach (var control in new[] { SmallLogo, LargeLogo, SmallNamed, LargeNamed, EmptyName })
        {
            var image = Find<Image>(control); var large = control.Size == BleeckerBrandLockupSize.Large;
            var edge = large ? 48 : 32;
            Require(Math.Abs(image.ActualHeight-edge)<.01 && Math.Abs(image.ActualWidth-edge*1000/1289.11)<.05,
                $"Brand logo aspect ratio differs: {control.Name} {image.ActualWidth:R}x{image.ActualHeight:R}.");
            var text = Descendants<TextBlock>(control).Single(item => item.Name == "PART_Name");
            var separator = Descendants<Border>(control).Single(item => item.Name == "PART_Separator");
            Require(text.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf") && text.FontSize==(large?30:20) &&
                text.LineHeight==(large?36:28) && text.CharacterSpacing==-20, "Brand typography differs from the pinned source.");
            var named = !string.IsNullOrEmpty(control.BrandName);
            Require(text.Visibility==(named?Visibility.Visible:Visibility.Collapsed) && separator.Visibility==text.Visibility,
                "Null/empty brand name retained the name/separator group.");
            if (named)
            {
                Require(separator.ActualWidth==1 && separator.ActualHeight==edge && separator.Background is SolidColorBrush brush &&
                    Hex(brush)==(scheme==BleeckerColorScheme.Light?"#FFE6D5B8":"#FFFFFFFF") &&
                    Math.Abs(brush.Opacity-(double)(float)(scheme==BleeckerColorScheme.Light?.55:.18))<1e-8,
                    "Brand separator did not use the pinned theme color/alpha.");
            }
            samples.Add(new { control.Name, image.ActualWidth, image.ActualHeight, text.FontSize, text.LineHeight, text.CharacterSpacing, named });
        }
        Require(Find<HyperlinkButton>(SmallNamed).NavigateUri is null && AutomationProperties.GetHelpText(Find<HyperlinkButton>(SmallNamed))==SmallNamed.Href,
            "Brand lockup reconstructed or launched a consumer-owned route.");
        var native = Find<HyperlinkButton>(SmallNamed);
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(native);
        Require(peer.GetAutomationControlType()==AutomationControlType.Hyperlink && peer.GetName()=="gaulatti Fictional studio" && peer.IsKeyboardFocusable(),
            "Brand link lost its native role, accessible name or keyboard access.");
        var startInvocations = invocations; var startCommands = command.Count;
        ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
        Require(invocations==startInvocations+1 && command.Count==startCommands+1 && ReferenceEquals(command.LastParameter, parameter) && invokedHref==SmallNamed.Href,
            "Brand native invocation did not preserve the exact route/command parameter exactly once.");
        command.Enabled = false;
        await AwaitAsync(() => !native.IsEnabled, "Command rejection did not disable the native brand link.");
        command.Enabled = true;
        await AwaitAsync(() => native.IsEnabled, "Command availability did not restore the native brand link.");
        Require(!Find<HyperlinkButton>(Disabled).IsEnabled && !Find<HyperlinkButton>(Disabled).Focus(FocusState.Keyboard), "Disabled brand link remained actionable.");
        var customNative = Find<HyperlinkButton>(CustomLink);
        Require(customNative.DataContext is BleeckerBrandLinkContext context && context.Label=="Custom link" && context.Href=="/custom-renderer" &&
            ReferenceEquals(customNative.Content, context.Content), "Custom renderer did not receive the source item and prepared brand content.");
        CustomLink.BrandName = ""; CustomLink.Href = "/custom-updated?keep=%2F";
        await AwaitAsync(() => AutomationProperties.GetName(customNative)=="" && AutomationProperties.GetHelpText(customNative)==CustomLink.Href,
            "Custom renderer did not retain the empty label and live exact href.");
        CustomLink.BrandName = "Custom link"; CustomLink.Href = "/custom-renderer";
        Require(LongName.ActualHeight>32 && Find<TextBlock>(LongName, "PART_Name").ActualHeight>28, "Constrained non-Latin brand name did not wrap.");
        await VerifyPixelsAsync(input, scheme, folder);
        await VerifyMotionAsync(input,scheme,folder);
        await input.MoveAsync(native);
        await AwaitAsync(() => Descendants<Grid>(SmallNamed).Single(item => item.Name=="PART_Logo").Opacity==1, "Brand pointer hover did not reach full logo opacity.");
        await input.PressPointerAsync();
        try { await AwaitAsync(() => native.IsPressed, "Native brand link did not receive its held press."); await input.MoveAsync(Heading); }
        finally { input.PointerButton(false); }
        Require(invocations==startInvocations+1, "Cancelled brand press invoked the consumer.");
        await input.ClickAsync(native);
        Require(invocations==startInvocations+2 && command.Count==startCommands+2, "Brand physical click did not invoke exactly once.");
        await input.MoveAsync(Heading);
        Require(KeyboardStart.Focus(FocusState.Programmatic), "Brand keyboard entry anchor refused focus.");
        input.Key(0x09);
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot),Find<HyperlinkButton>(SmallLogo)) && Find<HyperlinkButton>(SmallLogo).FocusState==FocusState.Keyboard,
            "Physical Tab did not enter the first native brand link.");
        var beforeKeyboard = invocations; input.Key(0x0D);
        await AwaitAsync(() => invocations==beforeKeyboard+1 && invokedHref=="/", "Physical Enter did not invoke the consumer-owned brand route.");
        await input.MoveAsync(Heading);
        // CatalogRoot owns the fixture theme. The outer modal host binds to it,
        // so changing the host cannot override CatalogRoot's explicit theme.
        var themeOwner = (FrameworkElement)((BleeckerModalHost)XamlRoot.Content).Content;
        var ownerTheme = themeOwner.RequestedTheme;
        try
        {
            themeOwner.RequestedTheme = scheme==BleeckerColorScheme.Light?ElementTheme.Dark:ElementTheme.Light;
            await AwaitAsync(() => SmallNamed.ActualTheme==(scheme==BleeckerColorScheme.Light?ElementTheme.Dark:ElementTheme.Light), "Brand did not follow a live owner theme change.");
            await VerifyPixelsAsync(input,scheme==BleeckerColorScheme.Light?BleeckerColorScheme.Dark:BleeckerColorScheme.Light,folder);
        }
        finally { themeOwner.RequestedTheme=ownerTheme; }
        await AwaitAsync(() => SmallNamed.ActualTheme==(scheme==BleeckerColorScheme.Light?ElementTheme.Light:ElementTheme.Dark), "Brand did not restore its owner theme.");
        SmallNamed.FlowDirection=FlowDirection.RightToLeft;CustomImage.FlowDirection=FlowDirection.RightToLeft;UpdateLayout();
        await MainWindow.AwaitPresentationAsync(SmallNamed);
        var rtlNameElement=Find<TextBlock>(SmallNamed,"PART_Name");var rtlLogoElement=Find<Grid>(SmallNamed,"PART_Logo");
        var rtlText=rtlNameElement.TransformToVisual(XamlRoot.Content).TransformBounds(new Rect(0,0,rtlNameElement.ActualWidth,rtlNameElement.ActualHeight));
        var rtlLogo=rtlLogoElement.TransformToVisual(XamlRoot.Content).TransformBounds(new Rect(0,0,rtlLogoElement.ActualWidth,rtlLogoElement.ActualHeight));
        await File.WriteAllTextAsync(Path.Combine(folder,$"brand-rtl-{scheme}.json"),JsonSerializer.Serialize(new{rtlText,rtlLogo}));
        Require(rtlText.Right<rtlLogo.Left,"RTL brand did not reverse its layout while preserving source artwork.");
        await VerifyPixelsAsync(input,scheme,folder);
        SmallNamed.FlowDirection=FlowDirection.LeftToRight;CustomImage.FlowDirection=FlowDirection.LeftToRight;UpdateLayout();
        await MainWindow.AwaitPresentationAsync(SmallNamed);
        var originalSource=CustomImage.LogoSource;
        CustomImage.LogoSource=null;UpdateLayout();
        Require(Find<Image>(CustomImage).Source is null,"Cleared consumer logo silently selected the packaged logo.");
        CustomImage.LogoSource=originalSource;
        var parent=(Panel)VisualTreeHelper.GetParent(SmallNamed);var position=parent.Children.IndexOf(SmallNamed);var template=SmallNamed.Template;
        parent.Children.Remove(SmallNamed);
        await AwaitAsync(() => !SmallNamed.IsLoaded,"Brand did not unload from its consumer host.");
        SmallNamed.Template=null;
        Require(VisualTreeHelper.GetChildrenCount(SmallNamed)==0,"Detached brand template retained its link content.");
        SmallNamed.Template=template;SmallNamed.ApplyTemplate();parent.Children.Insert(position,SmallNamed);
        await AwaitAsync(() => SmallNamed.IsLoaded&&Find<Image>(SmallNamed).ActualWidth>0,"Retemplated brand did not remount its logo.");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(Find<HyperlinkButton>(SmallNamed)).GetName()=="gaulatti Fictional studio"&&SmallNamed.Command==command,
            "Brand remount replaced consumer state or native semantics.");
        await MainWindow.CaptureElementAsync(this, Path.Combine(folder,$"brand-fixture-{scheme}.png"));
        await File.WriteAllTextAsync(Path.Combine(folder,$"brand-measurements-{scheme}.json"),JsonSerializer.Serialize(new { samples, invocations, command.Count, logoFailed,
            physicalPointer=true,physicalTabEnter=true,customRenderer=true,exactRoutes=true,liveTheme=true,sourceClear=true,unloadRetemplateReload=true,rtl=true }));
    }

    private async Task VerifyPixelsAsync(NativeInputSession input, BleeckerColorScheme scheme, string folder)
    {
        CustomImage.StartBringIntoView(new BringIntoViewOptions { AnimationDesired=false }); await Task.Delay(100); UpdateLayout();
        var path=Path.Combine(folder,$"brand-window-{scheme}.png");
        var image=Find<Image>(CustomImage);
        var visibleHost=Find<Grid>(CustomImage,"PART_Logo");
        var origin=visibleHost.TransformToVisual(XamlRoot.Content).TransformPoint(new Point());
        await File.WriteAllTextAsync(Path.Combine(folder,$"brand-pixel-geometry-{scheme}.json"),JsonSerializer.Serialize(new { origin,
            sourceOrigin=image.TransformToVisual(XamlRoot.Content).TransformPoint(new Point()), image.ActualWidth,image.ActualHeight,
            XamlRoot.RasterizationScale,rootSize=XamlRoot.Size }));
        var observations=new List<object>();var elapsed=Stopwatch.StartNew();var matching=false;
        do
        {
        await NativeScreenCapture.CaptureClientAsync(input.Window,path);
        using var stream=new InMemoryRandomAccessStream(); await stream.WriteAsync((await File.ReadAllBytesAsync(path)).AsBuffer()); stream.Seek(0);
        var decoder=await BitmapDecoder.CreateAsync(stream); var pixels=(await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Straight,
            new BitmapTransform(),ExifOrientationMode.IgnoreExifOrientation,ColorManagementMode.DoNotColorManage)).DetachPixelData();
        var scale=XamlRoot.RasterizationScale;
        var background=scheme==BleeckerColorScheme.Light?new byte[]{255,255,255}:new byte[]{13,24,33};
        matching=true;
        foreach(var (index,color,alpha) in new[]{(0,new[]{0d,0,0},1d),(1,new[]{30d,70,110},1d),(2,new[]{30d,70,110},128/255d),(3,new[]{0d,0,0},0d)})
        {
            var x=(int)Math.Round((origin.X+image.ActualWidth*(index+.5)/4)*scale); var y=(int)Math.Round((origin.Y+image.ActualHeight/2)*scale);
            Require(x>=0&&y>=0&&x<decoder.PixelWidth&&y<decoder.PixelHeight,"Brand pixel sample exceeds the owned client capture.");
            var offset=(y*(int)decoder.PixelWidth+x)*4; var actual=new[]{(int)pixels[offset+2],pixels[offset+1],pixels[offset]};
            var expected=color.Select((channel,c)=>(int)Math.Round((scheme==BleeckerColorScheme.Dark?255-channel:channel)*alpha*.9+background[c]*(1-alpha*.9))).ToArray();
            var matched=actual.Zip(expected).All(pair=>Math.Abs(pair.First-pair.Second)<=3);
            observations.Add(new{milliseconds=elapsed.Elapsed.TotalMilliseconds,index,x,y,actual,expected,matched});
            matching&=matched;
        }
        if(matching)break;await Task.Delay(32);
        }while(elapsed.Elapsed<TimeSpan.FromSeconds(3));
        await File.WriteAllTextAsync(Path.Combine(folder,$"brand-pixels-{scheme}.json"),JsonSerializer.Serialize(observations));
        var reflection=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var helperType=visibleHost.GetType();var sprite=helperType.GetField("sprite",reflection)?.GetValue(visibleHost) as Microsoft.UI.Composition.SpriteVisual;
        await File.WriteAllTextAsync(Path.Combine(folder,$"brand-render-state-{scheme}.json"),JsonSerializer.Serialize(new {
            CustomImage.ActualTheme,CustomImage.RequestedTheme,helperTheme=visibleHost.ActualTheme,visibleHost.IsLoaded,visibleHost.Opacity,
            inverted=helperType.GetField("invert",reflection)?.GetValue(visibleHost),contrast=helperType.GetField("contrast",reflection)?.GetValue(visibleHost),
            spriteVisible=sprite?.IsVisible,spriteOpacity=sprite?.Opacity,imageTheme=image.ActualTheme,sourceType=image.Source?.GetType().FullName,image.Visibility }));
        Require(matching,$"Brand composed image inversion/alpha did not settle: {JsonSerializer.Serialize(observations.TakeLast(4))}.");
    }
    private async Task VerifyMotionAsync(NativeInputSession input,BleeckerColorScheme scheme,string folder)
    {
        var logo=Find<Grid>(SmallNamed,"PART_Logo");var native=Find<HyperlinkButton>(SmallNamed);
        await input.MoveAsync(Heading);
        await AwaitAsync(()=>Math.Abs(logo.Opacity-.9)<1e-6,"Brand hover exit did not settle before motion sampling.");
        var frames=new List<(string Phase,double Milliseconds,double Opacity)>();
        var clock=Stopwatch.StartNew();var phase="rest";var start=clock.Elapsed.TotalMilliseconds;
        void Enter(object sender,Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args){if(phase=="enter")return;phase="enter";start=clock.Elapsed.TotalMilliseconds;}
        void Exit(object sender,Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args)
        {
            var point=args.GetCurrentPoint(SmallNamed).Position;
            if(new Rect(0,0,SmallNamed.ActualWidth,SmallNamed.ActualHeight).Contains(point))return;
            phase="exit";start=clock.Elapsed.TotalMilliseconds;
        }
        void Frame(object? sender,object args){if(phase!="rest")frames.Add((phase,clock.Elapsed.TotalMilliseconds-start,logo.Opacity));}
        SmallNamed.PointerEntered+=Enter;SmallNamed.PointerExited+=Exit;CompositionTarget.Rendering+=Frame;
        var enabled=new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;var passed=false;
        try
        {
            await input.MoveAsync(native);
            await AwaitAsync(()=>logo.Opacity==1,"Brand entry motion did not reach full opacity.");
            await input.MoveAsync(Heading);
            await AwaitAsync(()=>Math.Abs(logo.Opacity-.9)<1e-6,"Brand exit motion did not return to resting opacity.");
            Require(frames.All(frame=>frame.Opacity>=.9-1e-6&&frame.Opacity<=1),"Brand opacity motion exceeded the source range.");
            if(enabled)
            {
                Require(frames.Any(frame=>frame.Phase=="enter"&&frame.Opacity>.9+1e-5&&frame.Opacity<1-1e-5)&&
                    frames.Any(frame=>frame.Phase=="exit"&&frame.Opacity>.9+1e-5&&frame.Opacity<1-1e-5),"Enabled brand motion did not expose intermediate enter and exit opacity.");
            }
            else Require(frames.All(frame=>Math.Abs(frame.Opacity-(frame.Phase=="enter"?1:.9))<1e-6),"Reduced-motion brand hover retained intermediate opacity.");
            passed=true;
        }
        finally
        {
            SmallNamed.PointerEntered-=Enter;SmallNamed.PointerExited-=Exit;CompositionTarget.Rendering-=Frame;
            await File.WriteAllTextAsync(Path.Combine(folder,$"brand-motion-{scheme}.json"),JsonSerializer.Serialize(new{passed,enabled,
                frames=frames.Select(frame=>new{frame.Phase,frame.Milliseconds,frame.Opacity})}));
        }
    }
    private async Task AwaitAsync(Func<bool> predicate,string error){var timer=Stopwatch.StartNew();var readiness = new NativeReadiness(DispatcherQueue); while(!predicate()&&timer.Elapsed<TimeSpan.FromSeconds(3)){UpdateLayout();if (predicate()) break; await readiness.WaitAsync();}Require(predicate(),error);}
    private static T Find<T>(DependencyObject root,string? name=null) where T:FrameworkElement=>Descendants<T>(root).First(item=>name is null||item.Name==name);
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject{for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T match)yield return match;foreach(var nested in Descendants<T>(child))yield return nested;}}
    private static string Hex(SolidColorBrush brush)=>$"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
    private static void Require(bool condition,string error){if(!condition)throw new InvalidOperationException(error);}
    private sealed class ConsumerCommand:ICommand
    {
        private bool enabled=true; public bool Enabled{get=>enabled;set{enabled=value;CanExecuteChanged?.Invoke(this,EventArgs.Empty);}}
        public int Count{get;private set;}public object? LastParameter{get;private set;}public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter)=>Enabled;public void Execute(object? parameter){Count++;LastParameter=parameter;}
    }
}
