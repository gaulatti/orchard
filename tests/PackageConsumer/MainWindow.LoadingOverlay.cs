using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifyLoadingOverlayAsync()
    {
        await AcquireConsumerInputAsync(); Require(timelineAction.Focus(FocusState.Keyboard), "Could not focus the original action before the packaged loading portal.");
        PackageLoadingOverlay.IsVisible = true; Root.UpdateLayout();
        await AwaitAsync(() => VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Any(p => p.Child is FrameworkElement child && child.Name == "PART_Portal"), "Packaged loading portal did not open.");
        var originalTemplate = PackageLoadingOverlay.Template;
        try
        {
            var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Single(p => p.Child is FrameworkElement child && child.Name == "PART_Portal");
            var portal = (FrameworkElement)popup.Child;
            Require(Math.Abs(portal.ActualWidth - Root.XamlRoot.Size.Width) < .05 && Math.Abs(portal.ActualHeight - Root.XamlRoot.Size.Height) < .05,
                "Packaged loading portal does not cover its window.");
            Require(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(Root.XamlRoot), timelineAction), "Packaged loading portal stole opening focus.");
            var spinner = BrandDescendants<BleeckerLoadingSpinner>(portal).Single();
            Require(spinner.Size == BleeckerLoadingSize.Large && spinner.ActualWidth == 48 && spinner.ActualHeight == 48 &&
                FrameworkElementAutomationPeer.CreatePeerForElement(spinner).GetName() == "Preparing consumer data", "Packaged loading spinner lost its geometry or consumer label.");
            var count = timelineActions;
            await PaginationConsumerKeyAsync(0x20, timelineAction, timelineAction, () => Require(timelineActions == count, "Packaged underlying action fired before Space release."));
            Require(timelineActions == count + 1, "Packaged portal blocked the existing keyboard action.");
            PackageLoadingOverlay.Label = ""; Root.UpdateLayout();
            await AwaitAsync(() => BrandDescendants<BleeckerOverlaySurface>(portal).Single().ActualHeight == 98, "Packaged portal retained the omitted label gap.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(spinner);
            var nameSource = new TextBlock { Text = "Préparer le dossier 日本語" };
            PackageLoadingOverlay.SetBinding(AutomationProperties.NameProperty, new Binding { Source = nameSource, Path = new PropertyPath(nameof(TextBlock.Text)), Mode = BindingMode.OneWay });
            await AwaitAsync(() => peer.GetName() == nameSource.Text, "Packaged portal ignored its bound accessible name with an omitted label.");
            nameSource.Text = "Updated consumer operation";
            await AwaitAsync(() => peer.GetName() == nameSource.Text, "Packaged portal retained a stale accessible name.");
            PackageLoadingOverlay.ClearValue(AutomationProperties.NameProperty);
            AutomationProperties.SetLabeledBy(PackageLoadingOverlay, nameSource);
            await AwaitAsync(() => peer.GetName() == nameSource.Text, "Packaged portal ignored its linked accessible caption.");
            AutomationProperties.SetName(PackageLoadingOverlay, "Explicit consumer operation");
            Require(peer.GetName() == "Explicit consumer operation" && peer.GetAutomationControlType() == AutomationControlType.ProgressBar && peer.GetPattern(PatternInterface.RangeValue) is null && !spinner.IsTabStop,
                "Packaged portal lost accessible name priority or indeterminate progress semantics.");
            PackageLoadingOverlay.Template = ConsumerLoadingTemplate(); PackageLoadingOverlay.ApplyTemplate(); Root.UpdateLayout();
            await AwaitAsync(() => !portal.IsLoaded && VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Any(p => p.Child is FrameworkElement child && BrandDescendants<Border>(child).Any(part => part.Name == "ConsumerDecoration")), "Packaged portal did not release and replace its template.");
            var replacement = (FrameworkElement)VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Single(p => p.Child is FrameworkElement child && child.Name == "PART_Portal").Child;
            var replacementSpinner = BrandDescendants<BleeckerLoadingSpinner>(replacement).Single();
            Require(Math.Abs(replacement.ActualWidth - Root.XamlRoot.Size.Width) < .05 && Math.Abs(replacement.ActualHeight - Root.XamlRoot.Size.Height) < .05 &&
                FrameworkElementAutomationPeer.CreatePeerForElement(replacementSpinner).GetName() == "Explicit consumer operation" &&
                ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(Root.XamlRoot), timelineAction), "Packaged custom template lost its viewport, accessible name or original focus.");
            PackageLoadingOverlay.Label = "Caller template 日本語"; Root.UpdateLayout();
            await AwaitAsync(() => BrandDescendants<TextBlock>(replacement).Any(part => part.Name == "PART_Label" && part.Text == PackageLoadingOverlay.Label), "Packaged custom template ignored the live caller label.");
        }
        finally { PackageLoadingOverlay.IsVisible = false; PackageLoadingOverlay.Template = originalTemplate; PackageLoadingOverlay.ApplyTemplate(); PackageLoadingOverlay.ClearValue(AutomationProperties.NameProperty); AutomationProperties.SetLabeledBy(PackageLoadingOverlay, null); PackageLoadingOverlay.Label = "Preparing consumer data"; Root.UpdateLayout(); }
        Require(!VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Any(p => p.Child is FrameworkElement child && child.Name == "PART_Portal"), "Packaged loading portal did not close.");
    }

    private static ControlTemplate ConsumerLoadingTemplate() => (ControlTemplate)XamlReader.Load("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:local="using:Orchard" TargetType="local:BleeckerLoadingOverlay">
         <Grid Width="0" Height="0" HorizontalAlignment="Left" VerticalAlignment="Top" FlowDirection="LeftToRight">
          <Popup x:Name="PART_Popup" IsLightDismissEnabled="False" ShouldConstrainToRootBounds="True" FlowDirection="LeftToRight">
           <Canvas x:Name="PART_Portal" Background="Transparent" UseLayoutRounding="False" FlowDirection="LeftToRight">
            <Grid x:Name="PART_Backdrop" IsHitTestVisible="False" /><Border x:Name="PART_Tint" Background="{TemplateBinding OverlayBrush}" />
            <Border x:Name="ConsumerDecoration" Width="24" Height="24" Background="Gold" Canvas.Left="12" Canvas.Top="70" IsHitTestVisible="False" />
            <local:BleeckerOverlaySurface x:Name="PART_Card" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="{TemplateBinding CornerRadius}" Padding="{TemplateBinding Padding}" UseLayoutRounding="False">
             <Canvas x:Name="PART_Body" UseLayoutRounding="False">
              <local:BleeckerLoadingSpinner x:Name="PART_Spinner" Size="Large" IsHitTestVisible="False" />
              <Canvas x:Name="PART_LabelHost" UseLayoutRounding="False"><TextBlock x:Name="PART_Label" Foreground="{TemplateBinding Foreground}" TextWrapping="WrapWholeWords" TextReadingOrder="UseFlowDirection" LineStackingStrategy="BlockLineHeight" UseLayoutRounding="False" /></Canvas>
             </Canvas>
            </local:BleeckerOverlaySurface>
           </Canvas>
          </Popup>
         </Grid>
        </ControlTemplate>
        """);
}
