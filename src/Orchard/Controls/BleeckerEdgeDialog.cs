using System.Diagnostics;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>Shared caller-controlled lifetime and content contract for Drawer and Sheet.</summary>
public abstract partial class BleeckerEdgeDialog : ContentControl
{
    private readonly bool drawer;
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly UISettings settings=new();
    private BleeckerEdgeDialogPresence? presence;
    private BleeckerEdgeDialogMotion? motion;
    private BleeckerModalHost.Registration? registration;
    private DispatcherQueueTimer? timer;
    private ICommand? observedCommand;
    private XamlRoot? observedRoot;
    private Grid? declaration;
    private bool mountReady,rendering,renderAgain,queued,subscribed,lastVisible;
    private int generation;
    private double visibleDeadline,removeDeadline,motionDeadline;
    private protected BleeckerEdgeDialog(bool isDrawer)
    {
        drawer=isDrawer;DefaultStyleKey=isDrawer?typeof(BleeckerDrawer):typeof(BleeckerSheet);IsTabStop=false;UseLayoutRounding=false;
        Loaded+=LoadedOwner;Unloaded+=UnloadedOwner;ActualThemeChanged+=(_,_)=>Refresh();
        RegisterPropertyChangedCallback(TemplateProperty,(_,_)=>{if(Template is null)DetachTemplate();});
        foreach(var p in new[]{ContentProperty,ContentTemplateProperty,ContentTemplateSelectorProperty})RegisterPropertyChangedCallback(p,(_,_)=>QueueRefresh());
        foreach(var p in new[]{IsEnabledProperty,FlowDirectionProperty,LanguageProperty,IsTextScaleFactorEnabledProperty})RegisterPropertyChangedCallback(p,(_,_)=>Refresh());
    }
    private static DependencyProperty Property(string name,Type type,object? value)=>DependencyProperty.Register(name,type,typeof(BleeckerEdgeDialog),new PropertyMetadata(value,(owner,_)=>((BleeckerEdgeDialog)owner).Refresh()));
    public static readonly DependencyProperty IsOpenProperty=Property(nameof(IsOpen),typeof(bool),false);
    public static readonly DependencyProperty TitleProperty=Property(nameof(Title),typeof(string),null);
    public static readonly DependencyProperty DescriptionProperty=Property(nameof(Description),typeof(string),null);
    public static readonly DependencyProperty SurfaceStyleProperty=Property(nameof(SurfaceStyle),typeof(Style),null);
    public static readonly DependencyProperty CloseCommandProperty=Property(nameof(CloseCommand),typeof(ICommand),null);
    public static readonly DependencyProperty CloseCommandParameterProperty=Property(nameof(CloseCommandParameter),typeof(object),null);
    public bool IsOpen{get=>(bool)GetValue(IsOpenProperty);set=>SetValue(IsOpenProperty,value);}
    public string? Title{get=>(string?)GetValue(TitleProperty);set=>SetValue(TitleProperty,value);}
    public string? Description{get=>(string?)GetValue(DescriptionProperty);set=>SetValue(DescriptionProperty,value);}
    public Style? SurfaceStyle{get=>(Style?)GetValue(SurfaceStyleProperty);set=>SetValue(SurfaceStyleProperty,value);}
    public ICommand? CloseCommand{get=>(ICommand?)GetValue(CloseCommandProperty);set=>SetValue(CloseCommandProperty,value);}
    public object? CloseCommandParameter{get=>GetValue(CloseCommandParameterProperty);set=>SetValue(CloseCommandParameterProperty,value);}
    public event RoutedEventHandler? CloseRequested;
    public void RequestClose()
    {
        if(!IsLoaded||!IsOpen||!IsEnabled||registration?.Layer.Active!=true||!ReferenceEquals(registration.Host.ActiveSurface,focusSurface)||CloseCommand?.CanExecute(CloseCommandParameter)==false)return;
        var command=CloseCommand;var parameter=CloseCommandParameter;CloseRequested?.Invoke(this,new RoutedEventArgs());command?.Execute(parameter);
    }
    private BleeckerEdgeSide Side=>this is BleeckerSheet sheet?(BleeckerEdgeSide)sheet.Side:BleeckerEdgeSide.Bottom;
    private bool ScrollContent=>this is not BleeckerSheet sheet||sheet.ScrollContent;
    protected override void OnApplyTemplate(){DetachTemplate();base.OnApplyTemplate();if(Template is null)return;declaration=GetTemplateChild("PART_Layout") as Grid??throw new InvalidOperationException("Drawer/Sheet requires a Grid named PART_Layout.");QueueRefresh();}
    private void DetachTemplate(){generation++;mountReady=false;StopPresentation();declaration=null;}
    private void LoadedOwner(object sender,RoutedEventArgs args)
    {
        if(!subscribed&&OperatingSystem.IsWindowsVersionAtLeast(10,0,19041)){settings.AnimationsEnabledChanged+=AnimationsChanged;subscribed=true;}
        LayoutUpdated+=LayoutOwner;QueueRefresh();
    }
    private void UnloadedOwner(object sender,RoutedEventArgs args)
    {
        generation++;mountReady=false;LayoutUpdated-=LayoutOwner;StopPresentation();DisconnectRoot();UnhookCommand();
        if(subscribed&&OperatingSystem.IsWindowsVersionAtLeast(10,0,19041))settings.AnimationsEnabledChanged-=AnimationsChanged;subscribed=false;
    }
    private void LayoutOwner(object? sender,object args)
    {
        if(!IsLoaded)return;if(!ReferenceEquals(observedRoot,XamlRoot))QueueRefresh();ObserveContentRoot();
        if(overlay is not null&&ScrollContent&&contentSlot is{IsLoaded:true} slot&&slot.DesiredSize.Height<layout.Content.Height)QueueRemeasure();
    }
    private void QueueRefresh()
    {
        if(queued)return;queued=true;var current=generation;
        if(!DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,()=>{queued=false;if(current!=generation){if(IsLoaded)QueueRefresh();return;}mountReady=true;Refresh();}))throw new InvalidOperationException("Drawer/Sheet could not queue presentation.");
    }
    private void DisconnectRoot(){if(observedRoot is not null)observedRoot.Changed-=RootChanged;observedRoot=null;}
    private void RootChanged(XamlRoot sender,XamlRootChangedEventArgs args)=>overlay?.InvalidateMeasure();
    private void AnimationsChanged(UISettings sender,UISettingsAnimationsEnabledChangedEventArgs args)=>DispatcherQueue.TryEnqueue(()=>{if(motion is null)return;var now=clock.Elapsed.TotalMilliseconds;motion.SetVisible(lastVisible,now,reduced:!settings.AnimationsEnabled);motionDeadline=now+Math.Max(motion.Durations.Surface,motion.Durations.Backdrop);UpdatePresentation();});
    private void UnhookCommand(){if(observedCommand is not null)observedCommand.CanExecuteChanged-=CanExecuteChanged;observedCommand=null;}
    private void CanExecuteChanged(object? sender,EventArgs args){if(!DispatcherQueue.TryEnqueue(Refresh))throw new InvalidOperationException("Drawer/Sheet could not update command state.");}
    internal void Refresh()
    {
        if(!Enum.IsDefined(Side))throw new ArgumentOutOfRangeException(nameof(BleeckerSheet.Side));
        var command=IsLoaded?CloseCommand:null;if(!ReferenceEquals(command,observedCommand)){UnhookCommand();observedCommand=command;if(command is not null)command.CanExecuteChanged+=CanExecuteChanged;}
        if(!mountReady||!IsLoaded||declaration is null)return;if(rendering){renderAgain=true;return;}rendering=true;
        try
        {
            if(!ReferenceEquals(observedRoot,XamlRoot)){StopPresentation();DisconnectRoot();observedRoot=XamlRoot;if(observedRoot is not null)observedRoot.Changed+=RootChanged;}
            if(IsOpen&&presence is null){presence=new();motion=new(drawer);lastVisible=false;}
            if(presence is not null)
            {
                var now=clock.Elapsed.TotalMilliseconds;if(presence.DesiredOpen!=IsOpen){presence.SetOpen(IsOpen,now);if(IsOpen)visibleDeadline=now+20;else removeDeadline=now+180;if(IsOpen)MountPresentation();else if(registration is{IsAttached:true,Layer.Active:true})registration.Host.Deactivate(registration);}
                RenderContent();UpdatePresentation();
            }
        }finally{rendering=false;}
        if(renderAgain){renderAgain=false;Refresh();}
    }
    private void UpdatePresentation()
    {
        if(presence is null||motion is null)return;var now=clock.Elapsed.TotalMilliseconds;presence.Advance(now);
        if(presence.Visible!=lastVisible){lastVisible=presence.Visible;motion.SetVisible(lastVisible,now,reduced:!settings.AnimationsEnabled);motionDeadline=now+Math.Max(motion.Durations.Surface,motion.Durations.Backdrop);}
        if(!presence.Present){StopPresentation();return;}ApplyMotion(motion.Sample(now));if(presence.DesiredOpen&&presence.Visible&&now>=motionDeadline){timer?.Stop();return;}
        var remaining=presence.DesiredOpen&&!presence.Visible?visibleDeadline-now:!presence.DesiredOpen?removeDeadline-now:motionDeadline-now;
        timer??=DispatcherQueue.CreateTimer();timer.IsRepeating=false;timer.Tick-=Tick;timer.Tick+=Tick;timer.Interval=TimeSpan.FromMilliseconds(Math.Max(1,Math.Min(16,remaining)));timer.Start();
    }
    private void Tick(DispatcherQueueTimer sender,object args)=>UpdatePresentation();
    private void StopPresentation(){timer?.Stop();if(timer is not null)timer.Tick-=Tick;timer=null;presence?.Dispose();presence=null;motion=null;registration?.Dispose();registration=null;ClearPresentation();}
    protected override AutomationPeer OnCreateAutomationPeer()=>new DeclarationPeer(this);
    private sealed class DeclarationPeer(BleeckerEdgeDialog owner):FrameworkElementAutomationPeer(owner)
    {
        protected override bool IsControlElementCore()=>false;protected override bool IsContentElementCore()=>false;protected override IList<AutomationPeer> GetChildrenCore()=>[];
    }
}

/// <summary>A bottom drawer with a fixed header/handle and independently scrolling content.</summary>
public sealed class BleeckerDrawer:BleeckerEdgeDialog{public BleeckerDrawer():base(true){}}

public enum BleeckerSheetSide{Left,Right,Top,Bottom}

/// <summary>A caller-controlled sheet on one of the four physical viewport edges.</summary>
public sealed class BleeckerSheet:BleeckerEdgeDialog
{
    public BleeckerSheet():base(false){}
    public static readonly DependencyProperty SideProperty=DependencyProperty.Register(nameof(Side),typeof(BleeckerSheetSide),typeof(BleeckerSheet),new PropertyMetadata(BleeckerSheetSide.Right,(owner,_)=>((BleeckerSheet)owner).Refresh()));
    public static readonly DependencyProperty ScrollContentProperty=DependencyProperty.Register(nameof(ScrollContent),typeof(bool),typeof(BleeckerSheet),new PropertyMetadata(true,(owner,_)=>((BleeckerSheet)owner).Refresh()));
    public BleeckerSheetSide Side{get=>(BleeckerSheetSide)GetValue(SideProperty);set=>SetValue(SideProperty,value);}
    public bool ScrollContent{get=>(bool)GetValue(ScrollContentProperty);set=>SetValue(ScrollContentProperty,value);}
}
