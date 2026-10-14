using System.Diagnostics;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class ErrorStatesFixture : UserControl
{
    private readonly RetryAction command = new();
    private readonly object parameter = new();
    private int notifications;
    public ErrorStatesFixture()
    {
        InitializeComponent();
        Retrying.RetryCommand = command; Retrying.RetryCommandParameter = parameter;
        Retrying.Retry += (_, _) => notifications++;
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"error-state-{scheme}");
        await input.AcquireAsync(Heading); await input.MoveAsync(Heading);
        command.Allowed = true; command.RaiseChanged();
        foreach (var control in new[] { Default, Retrying, Custom, Long, Rtl, EmptyTitle, EmptyText }) control.ApplyTemplate();
        UpdateLayout();
        using var reference = JsonDocument.Parse(typeof(ErrorStatesFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.ErrorStateLayout.json")
            ?? throw new InvalidOperationException("Missing frozen Error State browser reference."));
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1,
            "The frozen Error State geometry comparison requires display/text scale 1; broader scaling acceptance remains open.");
        var cases = reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()).ToArray();
        foreach (var (id,control) in new[] { ("default",Default), ("retry",Retrying), ("custom",Custom), ("long",Long), ("rtl",Rtl), ("empty-title",EmptyTitle), ("empty-text",EmptyText) })
        {
            var expected = cases.Single(item => item.GetProperty("id").GetString() == id);
            Require(Math.Abs(control.ActualWidth-expected.GetProperty("width").GetDouble()) < .001 &&
                Math.Abs(control.ActualHeight-expected.GetProperty("layout").GetProperty("surface").GetProperty("height").GetDouble()) < .001,
                $"Error State {id} differs from frozen browser block geometry: {control.ActualWidth:R}x{control.ActualHeight:R}.");
        }
        var defaultRetry = Part<BleeckerButton>(Default, "PART_Retry");
        Require(Default.Title == "Something went wrong" && Default.Description == "We couldn't load this data. Please try again." &&
            Default.RetryLabel == "Try again" && defaultRetry.Visibility == Visibility.Collapsed, "Error State lost its pinned defaults or invented retry.");
        Require(Default.ActualWidth == 640 && Default.ActualHeight == 230 && Retrying.ActualHeight == 286,
            $"Error State default/retry block geometry differs: {Default.ActualHeight:R}/{Retrying.ActualHeight:R}.");
        var background = Part<Border>(Default, "PART_Background"); var iconBackground = Part<Border>(Default, "PART_IconBackground");
        Require(Math.Abs(background.Opacity-(scheme == BleeckerColorScheme.Dark ? .07 : .035)) < 1e-7 &&
            Math.Abs(iconBackground.Opacity-(scheme == BleeckerColorScheme.Dark ? .15 : .07)) < 1e-7 &&
            Part<Border>(Default,"PART_Border").Opacity == .25 && Math.Abs(Part<Border>(Default,"PART_IconBorder").Opacity-.15) < 1e-7,
            "Error State lost its source theme opacity.");
        Require(Default.Padding == new Thickness(32,56,32,56) && Default.CornerRadius == new CornerRadius(12) &&
            Part<Grid>(Default,"PART_IconBox").ActualWidth == 48 && Part<Viewbox>(Default,"PART_DefaultIcon").ActualWidth == 28 &&
            Part<TextBlock>(Default,"PART_Title").FontSize == 16 && Part<TextBlock>(Default,"PART_Title").LineHeight == 24 &&
            Part<TextBlock>(Default,"PART_Title").CharacterSpacing == -20 &&
            Part<TextBlock>(Default,"PART_Description").FontSize == 14 && Part<TextBlock>(Default,"PART_Description").LineHeight == 20,
            "Error State spacing, icon or text recipe differs.");
        Require(ReferenceEquals(Part<ContentPresenter>(Custom,"PART_Icon").Content, ConsumerIcon) &&
            Part<FrameworkElement>(Custom,"PART_DefaultIcon").Visibility == Visibility.Collapsed &&
            Part<TextBlock>(Custom,"PART_Description").Visibility == Visibility.Collapsed, "Error State changed its consumer icon or retained empty description.");
        Require(Part<TextBlock>(Long,"PART_Title").ActualWidth <= Long.ActualWidth-66 &&
            Part<TextBlock>(Long,"PART_Description").ActualWidth <= Long.ActualWidth-66 &&
            Part<TextBlock>(Long,"PART_Description").ActualHeight > 20, "Constrained Error State text escaped its consumer width.");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(Retrying);
        Require(peer.GetAutomationControlType() == AutomationControlType.Group && !peer.IsKeyboardFocusable() &&
            peer.GetName() == Retrying.Title && peer.GetHelpText() == Retrying.Description, "Error State lost its accessible summary contract.");
        var retry = Part<BleeckerButton>(Retrying,"PART_Retry"); retry.ApplyTemplate();
        var native = Part<Button>(retry,"PART_Action");
        native.ApplyTemplate();
        var nativeContent=(ContentPresenter)((FrameworkElement)VisualTreeHelper.GetChild(native,0)).FindName("ContentPresenter");
        Require(retry.Size == BleeckerButtonSize.Small && retry.Variant == BleeckerButtonVariant.Secondary &&
            ReferenceEquals(native.Command, command) && ReferenceEquals(native.CommandParameter, parameter) &&
            new ButtonAutomationPeer(native).GetName() == "Reload archive", "Error State retry lost native command, parameter or accessible label.");
        await AwaitAsync(() => Part<Microsoft.UI.Xaml.Shapes.Path>(retry,"PART_RetryIcon").Stroke is SolidColorBrush glyph &&
            Part<TextBlock>(retry,"PART_RetryLabel").Foreground is SolidColorBrush label && glyph.Color == label.Color && label.Color == ((SolidColorBrush)nativeContent.Foreground).Color,
            "Retry icon/label did not adopt the native action foreground.");
        var before = command.Executions; var notified = notifications;
        var pointerEvents=new List<object>();
        var rawEvents=new List<object>();
        var island=Microsoft.UI.Content.ContentIsland.FindAllForCurrentThread().Single(item=>item.Environment.Equals(XamlRoot.ContentIslandEnvironment));
        var rawSource=Microsoft.UI.Input.InputPointerSource.GetForIsland(island);
        void TraceRaw(string kind,Microsoft.UI.Input.PointerEventArgs args) => rawEvents.Add(new {kind,args.Handled,args.CurrentPoint.PointerId,args.CurrentPoint.PointerDeviceType,args.CurrentPoint.Position,args.CurrentPoint.Properties.IsLeftButtonPressed});
        void RawPressed(Microsoft.UI.Input.InputPointerSource sender,Microsoft.UI.Input.PointerEventArgs args) => TraceRaw("pressed",args);
        void RawReleased(Microsoft.UI.Input.InputPointerSource sender,Microsoft.UI.Input.PointerEventArgs args) => TraceRaw("released",args);
        void RawMoved(Microsoft.UI.Input.InputPointerSource sender,Microsoft.UI.Input.PointerEventArgs args) => TraceRaw("moved",args);
        rawSource.PointerPressed+=RawPressed;rawSource.PointerReleased+=RawReleased;rawSource.PointerMoved+=RawMoved;
        void TracePointer(string kind,Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args)
        {
            var point=args.GetCurrentPoint(XamlRoot.Content);
            pointerEvents.Add(new{kind,args.Handled,point.Position,point.Properties.IsLeftButtonPressed,
                sourceType=args.OriginalSource?.GetType().Name,sourceName=(args.OriginalSource as FrameworkElement)?.Name});
        }
        var pressed=new Microsoft.UI.Xaml.Input.PointerEventHandler((_,args)=>TracePointer("pressed",args));
        var released=new Microsoft.UI.Xaml.Input.PointerEventHandler((_,args)=>TracePointer("released",args));
        var moved=new Microsoft.UI.Xaml.Input.PointerEventHandler((_,args)=>TracePointer("moved",args));
        XamlRoot.Content.AddHandler(PointerPressedEvent,pressed,true);XamlRoot.Content.AddHandler(PointerReleasedEvent,released,true);XamlRoot.Content.AddHandler(PointerMovedEvent,moved,true);
        try
        {
            await input.MoveAsync(native);
            await input.PressPointerAsync();
            await AwaitAsync(() => native.IsPressed, "Native retry did not receive the held pointer press.");
            input.PointerButton(false);
            await AwaitAsync(() => command.Executions == before+1 && notifications == notified+1, "Pointer retry did not deliver exactly one consumer command/notification.");
        }
        finally
        {
            XamlRoot.Content.RemoveHandler(PointerPressedEvent,pressed);XamlRoot.Content.RemoveHandler(PointerReleasedEvent,released);XamlRoot.Content.RemoveHandler(PointerMovedEvent,moved);
            rawSource.PointerPressed-=RawPressed;rawSource.PointerReleased-=RawReleased;rawSource.PointerMoved-=RawMoved;
            var bounds=input.ScreenBounds(native);var rootPoint=native.TransformToVisual(XamlRoot.Content).TransformPoint(new Windows.Foundation.Point(native.ActualWidth/2,native.ActualHeight/2));
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"error-state-retry-pointer-{scheme}.json"),JsonSerializer.Serialize(new
            {
                before,notified,command.Executions,notifications,command.Allowed,native.IsEnabled,native.IsLoaded,native.IsPressed,native.FocusState,pointerEvents,rawEvents,rawSource.DeviceKinds,
                commandRetained=ReferenceEquals(native.Command,command),parameterRetained=ReferenceEquals(native.CommandParameter,parameter),bounds,rootPoint,
                hits=VisualTreeHelper.FindElementsInHostCoordinates(rootPoint,XamlRoot.Content).Take(10).Select(element=>new{type=element.GetType().Name,name=(element as FrameworkElement)?.Name}),
                native=input.DescribeCurrentPoint(),routing=input.DescribeInputRouting()
            }));
        }
        Require(ReferenceEquals(command.LastParameter, parameter), "Retry replaced the consumer parameter.");
        Require(native.Focus(FocusState.Programmatic), "Retry refused keyboard focus.");
        input.Key(0x20);
        await AwaitAsync(() => command.Executions == before+2 && notifications == notified+2, "Space retry did not deliver exactly one consumer command/notification.");
        command.Allowed = false; command.RaiseChanged();
        await AwaitAsync(() => !native.IsEnabled, "Consumer CanExecute rejection did not disable retry.");
        Require(!native.Focus(FocusState.Keyboard), "Rejected retry accepted focus.");
        command.Allowed = true; command.RaiseChanged();
        await AwaitAsync(() => native.IsEnabled, "Consumer CanExecute restoration did not enable retry.");
        Retrying.IsEnabled = false;
        await AwaitAsync(() => !native.IsEnabled, "Disabled summary retained an enabled retry.");
        Retrying.IsEnabled = true;
        await AwaitAsync(() => native.IsEnabled, "Enabled summary did not restore retry.");
        var savedDescription = Custom.Description; var savedIcon = Custom.Icon; var savedTitle = Custom.Title;
        try
        {
            Custom.Description = "Live consumer description"; Custom.Icon = null;
            await AwaitAsync(() => Part<TextBlock>(Custom,"PART_Description").Visibility == Visibility.Visible &&
                Part<Viewbox>(Custom,"PART_DefaultIcon").Visibility == Visibility.Visible, "Live description/icon changes did not reach the mounted control.");
            Custom.Title = null;
            try
            {
                await AwaitAsync(() => Part<TextBlock>(Custom,"PART_Title").Height == 0 && Custom.ActualHeight == 206 && FrameworkElementAutomationPeer.CreatePeerForElement(Custom).GetName() == "",
                    "Null consumer title retained a phantom line or invented an accessible name.");
            }
            finally
            {
                var titlePart=Part<TextBlock>(Custom,"PART_Title");
                await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"error-state-null-title-{scheme}.json"),JsonSerializer.Serialize(new
                { Custom.Title,titlePart.Text,titlePart.Height,titlePart.ActualHeight,surfaceHeight=Custom.ActualHeight,titlePart.DesiredSize,accessibleName=FrameworkElementAutomationPeer.CreatePeerForElement(Custom).GetName() }));
            }
            Custom.Title = savedTitle;
            await AwaitAsync(() => double.IsNaN(Part<TextBlock>(Custom,"PART_Title").Height) && Custom.ActualHeight == 230, "Restoring title did not restore its source line.");
            Retrying.RetryCommand = null;
            await AwaitAsync(() => retry.Visibility == Visibility.Collapsed, "Removing the command retained retry.");
            Retrying.RetryCommand = command;
            await AwaitAsync(() => retry.Visibility == Visibility.Visible, "Restoring the command did not restore retry.");
            var template = Custom.Template; Custom.Template = null; Custom.Template = template; Custom.ApplyTemplate();
            UpdateLayout();
            Require(Custom.Icon is null && Custom.Description == "Live consumer description", "Retemplating changed consumer state.");
        }
        finally { Custom.Title = savedTitle; Custom.Description = savedDescription; Custom.Icon = savedIcon; Retrying.RetryCommand = command; Retrying.IsEnabled = true; command.Allowed = true; command.RaiseChanged(); }
        UpdateLayout();
        foreach (var control in new[] { Default, Retrying, Custom, Long, Rtl, EmptyTitle, EmptyText })
            await MainWindow.CaptureElementAsync(control, System.IO.Path.Combine(folder,$"error-state-{control.Name}-{scheme}.png"));
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"error-state-{scheme}.json"),JsonSerializer.Serialize(new
        {
            passed=true, sourceCommit="a86059013cd59684ce452849278ea81e734f2066", sourceSha256="b1c0e51f0d36ef3e29443393b983f8bf05caaf47d1d666fcf6202092902e033d",
            commandDelivery=2, commandParameterRetained=true, customIconRetained=ReferenceEquals(Custom.Icon,ConsumerIcon),
            defaultHeight=Default.ActualHeight,retryHeight=Retrying.ActualHeight,constrainedHeight=Long.ActualHeight,
            backgroundOpacity=background.Opacity,iconBackgroundOpacity=iconBackground.Opacity
        }));
    }
    private async Task AwaitAsync(Func<bool> condition,string message)
    { var timer=Stopwatch.StartNew(); while(!condition()&&timer.ElapsedMilliseconds<2000){UpdateLayout();await Task.Delay(16);}Require(condition(),message); }
    internal async Task VerifyContrastAsync(string folder)
    {
        using var contrast = new NativeContrastSession(folder);
        using var input = new NativeInputSession(XamlRoot,folder,"error-state-contrast");
        await input.AcquireAsync(Heading); Default.ApplyTemplate(); await input.MoveAsync(Default);
        var title=Part<TextBlock>(Default,"PART_Title");var description=Part<TextBlock>(Default,"PART_Description");
        var background=Part<Border>(Default,"PART_Background");var border=Part<Border>(Default,"PART_Border");
        var iconBackground=Part<Border>(Default,"PART_IconBackground");var iconBorder=Part<Border>(Default,"PART_IconBorder");
        var originalBackgroundOpacity=background.Opacity;var originalIconOpacity=iconBackground.Opacity;
        var originalTitle=((SolidColorBrush)title.Foreground).Color;var originalDescription=((SolidColorBrush)description.Foreground).Color;
        var titleInk=await ContrastFixture.ReadInkAsync(input,title,folder,"error-title");
        var descriptionInk=await ContrastFixture.ReadInkAsync(input,description,folder,"error-description");
        await MainWindow.CaptureElementAsync(Default,System.IO.Path.Combine(folder,"contrast-before.png"));
        try
        {
            contrast.Enable();
            await AwaitAsync(()=>NativeContrastSession.Read().Enabled&&new Windows.UI.ViewManagement.AccessibilitySettings().HighContrast,
                "Windows did not enter actual Error State contrast mode.");
            await AwaitAsync(()=>ColorMatches(background.Background,5)&&background.Opacity==1&&ColorMatches(iconBackground.Background,5)&&iconBackground.Opacity==1&&
                ColorMatches(border.BorderBrush,8)&&border.Opacity==1&&ColorMatches(iconBorder.BorderBrush,8)&&iconBorder.Opacity==1&&
                ColorMatches(title.Foreground,8)&&ColorMatches(description.Foreground,8),"Error State did not adopt Windows semantic colors.");
            await ContrastFixture.AwaitRecoveryTargetAsync(input,folder,this);
            await input.RequireForegroundAsync();
            await ContrastFixture.VerifyInkAsync(input,title,8,titleInk,folder,"error-title");
            await ContrastFixture.VerifyInkAsync(input,description,8,descriptionInk,folder,"error-description");
            var bitmap=new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();await bitmap.RenderAsync(Default);
            var buffer=await bitmap.GetPixelsAsync();var bytes=new byte[checked((int)buffer.Length)];
            using(var reader=Windows.Storage.Streams.DataReader.FromBuffer(buffer))reader.ReadBytes(bytes);
            var offset=(10*bitmap.PixelWidth+bitmap.PixelWidth/2)*4;var color=NativeContrastSession.Color(5);
            Require(bytes[offset]==color.B&&bytes[offset+1]==color.G&&bytes[offset+2]==color.R&&bytes[offset+3]==255,
                "Error State's actual painted background differs from Windows Window.");
            await MainWindow.CaptureElementAsync(Default,System.IO.Path.Combine(folder,"contrast-active.png"));
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder,"error-state-contrast.json"),JsonSerializer.Serialize(new{passed=true,backgroundOpacity=background.Opacity,iconBackgroundOpacity=iconBackground.Opacity,titleInk=titleInk.Pixels.Length,descriptionInk=descriptionInk.Pixels.Length}));
        }
        finally
        {
            contrast.Restore();
            await AwaitAsync(()=>!NativeContrastSession.Read().Enabled&&background.Opacity==originalBackgroundOpacity&&iconBackground.Opacity==originalIconOpacity&&
                ((SolidColorBrush)title.Foreground).Color==originalTitle&&((SolidColorBrush)description.Foreground).Color==originalDescription,
                "Error State did not restore its original palette after contrast mode.");
            await MainWindow.CaptureElementAsync(Default,System.IO.Path.Combine(folder,"contrast-restored.png"));
        }
        static bool ColorMatches(Brush brush,int systemColor)=>brush is SolidColorBrush solid&&solid.Color==NativeContrastSession.Color(systemColor)&&solid.Opacity==1;
    }
    private static T Part<T>(DependencyObject owner,string name) where T:FrameworkElement
    {
        if(owner is T item&&item.Name==name)return item;
        for(var index=0;index<VisualTreeHelper.GetChildrenCount(owner);index++)
        { try{return Part<T>(VisualTreeHelper.GetChild(owner,index),name);}catch(InvalidOperationException){} }
        throw new InvalidOperationException($"Missing Error State part {name}.");
    }
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private sealed class RetryAction:ICommand
    {
        public bool Allowed {get;set;}=true; public int Executions {get;private set;} public object? LastParameter {get;private set;}
        public bool CanExecute(object? parameter)=>Allowed;
        public void Execute(object? parameter){if(!Allowed)throw new InvalidOperationException("Rejected retry executed.");Executions++;LastParameter=parameter;}
        public event EventHandler? CanExecuteChanged;
        public void RaiseChanged()=>CanExecuteChanged?.Invoke(this,EventArgs.Empty);
    }
}
