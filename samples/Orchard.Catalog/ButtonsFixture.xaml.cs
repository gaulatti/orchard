using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class ButtonsFixture : UserControl
{
    private int invocations;
    private readonly PreviewCommand previewCommand;
    internal bool SpinnerAnimationsEnabled { get; private set; }

    public ButtonsFixture()
    {
        InitializeComponent();
        previewCommand = new PreviewCommand(parameter =>
        {
            invocations++;
            ButtonResult.Text = $"Command {parameter}: {invocations}";
        });
        LoadingAction.Command = previewCommand;
    }

    internal async Task VerifyAsync(BleeckerColorScheme scheme)
    {
        NamedAction.ApplyTemplate();
        var namedNative = Find<Button>(NamedAction)!;
        var namedPeer = new ButtonAutomationPeer(namedNative);
        Require(namedPeer.GetName() == "Secondary", "Text content did not supply the native accessible name.");
        var resultBeforeNameCheck = ButtonResult.Text;
        NamedAction.SetBinding(ContentControl.ContentProperty, new Microsoft.UI.Xaml.Data.Binding
        {
            Source = ButtonResult, Path = new PropertyPath("Text")
        });
        ButtonResult.Text = "Review translated story";
        Require(namedPeer.GetName() == "Review translated story", "Bound content updates did not reach the accessible name.");
        AutomationProperties.SetName(NamedAction, "Review story details");
        ButtonResult.Text = "Updated visible label";
        Require(namedPeer.GetName() == "Review story details", "Visible text replaced an explicit accessible name.");
        Require(NamedAction.GetBindingExpression(ContentControl.ContentProperty) is not null, "Naming replaced the consumer content binding.");
        AutomationProperties.SetName(NamedAction, "");
        Require(namedPeer.GetName() == "Updated visible label", "Clearing an explicit name did not restore the visible label.");
        NamedAction.ClearValue(ContentControl.ContentProperty);
        NamedAction.Content = "Secondary";
        AutomationProperties.SetLabeledBy(NamedAction, ButtonResult);
        Require(namedPeer.GetName() == "Updated visible label", "External labeling did not reach the native action.");
        AutomationProperties.SetLabeledBy(NamedAction, null);
        Require(namedPeer.GetName() == "Secondary", "Removing an external label did not restore content naming.");
        ButtonResult.Text = resultBeforeNameCheck;
        LoadingAction.ApplyTemplate();
        var native = Find<Button>(LoadingAction) ?? throw new InvalidOperationException("BleeckerButton has no native action.");
        var peer = new ButtonAutomationPeer(native);
        Require(native.FontWeight.Weight == 500 && native.FontFamily.Source.Contains("EncodeSans-Medium.ttf#Encode Sans", StringComparison.Ordinal),
            "Button did not select the bundled medium-weight face.");
        var invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
        Require(peer.GetName() == "Run preview", "Button lost its accessible name.");
        var before = invocations;
        invoke.Invoke();
        await Task.Delay(80);
        Require(invocations == before + 1 && ButtonResult.Text == $"Command preview: {invocations}", "Consumer command/parameter did not execute exactly once.");
        LoadingSwitch.IsOn = true;
        await Task.Delay(80);
        Require(!native.IsEnabled && Find<ProgressRing>(LoadingAction)?.IsActive == true && AutomationProperties.GetItemStatus(native) == "Loading",
            "Loading must expose busy status, show progress and disable activation.");
        var spinner = FindNamed<Microsoft.UI.Xaml.Shapes.Path>(LoadingAction, "PART_SpinnerPath")
            ?? throw new InvalidOperationException("Loading button did not render the reference spinner path.");
        Require(spinner.StrokeThickness == 2 && spinner.StrokeStartLineCap == PenLineCap.Round && spinner.StrokeEndLineCap == PenLineCap.Round,
            "Loading spinner lost the reference stroke treatment.");
        var rotation = (RotateTransform)spinner.RenderTransform;
        var firstAngle = rotation.Angle;
        await Task.Delay(180);
        SpinnerAnimationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
        if (SpinnerAnimationsEnabled)
            Require(Math.Abs(rotation.Angle - firstAngle) > 1, "Loading spinner did not rotate while system animations are enabled.");
        else Require(rotation.Angle == 0, "Loading spinner ignored reduced motion.");
        try { invoke.Invoke(); throw new InvalidOperationException("Loading action accepted invocation."); }
        catch (ElementNotEnabledException) { }
        catch (System.Runtime.InteropServices.COMException error) when (error.HResult == unchecked((int)0x80040200)) { } // UIA_E_ELEMENTNOTENABLED
        ConsumerEnabled.IsOn = false;
        LoadingSwitch.IsOn = false;
        await Task.Delay(80);
        Require(rotation.Angle == 0, "Loading completion did not stop and reset the spinner animation.");
        Require(!native.IsEnabled && !LoadingAction.IsEnabled, "Loading completion discarded the consumer's disabled state.");
        Require(LoadingAction.GetBindingExpression(Control.IsEnabledProperty) is not null, "Consumer IsEnabled binding was replaced.");
        ConsumerEnabled.IsOn = true;
        await Task.Delay(80);
        Require(native.IsEnabled, "Consumer re-enabling did not reach the native action.");
        previewCommand.SetCanExecute(false);
        await Task.Delay(80);
        Require(!native.IsEnabled, "CanExecuteChanged(false) did not disable the action.");
        LoadingSwitch.IsOn = true;
        LoadingSwitch.IsOn = false;
        LoadingAction.Size = BleeckerButtonSize.Large;
        await Task.Delay(80);
        Require(!native.IsEnabled, "A presentation change bypassed CanExecute(false).");
        previewCommand.SetCanExecute(true);
        LoadingAction.Size = BleeckerButtonSize.Medium;
        await Task.Delay(80);
        Require(native.IsEnabled, "CanExecuteChanged(true) did not re-enable the action.");
        foreach (var variant in Enum.GetValues<BleeckerButtonVariant>())
        {
            LoadingAction.Variant = variant;
            LoadingAction.UpdateLayout();
            await MainWindow.AwaitPresentationAsync(LoadingAction);
            Require(native.FocusVisualPrimaryBrush is SolidColorBrush focusRing &&
                Math.Abs(focusRing.Opacity - (scheme == BleeckerColorScheme.Dark ? 0.45 : 0.35)) < 0.001 &&
                native.FocusVisualPrimaryThickness == new Thickness(2) && native.FocusVisualSecondaryThickness == new Thickness(2) &&
                native.FocusVisualMargin == new Thickness(-4), "Button focus visuals escaped the branded ring recipe.");
            var presenter = Find<ContentPresenter>(native) ?? throw new InvalidOperationException("Native presenter is missing.");
            if (variant == BleeckerButtonVariant.Link)
            {
                foreach (var size in Enum.GetValues<BleeckerButtonSize>())
                {
                    LoadingAction.Size = size;
                    Require(native.FontSize == 16 && native.MinHeight == 0 && native.Padding == new Thickness(0),
                        "Link inherited a button size recipe instead of the source's un-sized text treatment.");
                }
                LoadingAction.Size = BleeckerButtonSize.Medium;
                Require(FindNamed<Border>(LoadingAction, "PART_LinkUnderline")?.Visibility == Visibility.Collapsed,
                    "Link underline must be absent before pointer hover.");
            }
            var token = variant switch
            {
                BleeckerButtonVariant.Primary => scheme == BleeckerColorScheme.Dark ? "deepSea" : "#FFFFFFFF",
                BleeckerButtonVariant.Destructive => "#FFFFFFFF",
                BleeckerButtonVariant.Outline or BleeckerButtonVariant.Link => "primary",
                BleeckerButtonVariant.Subtle when scheme == BleeckerColorScheme.Light => "deepSea",
                _ => "textPrimary"
            };
            Require(presenter.Foreground is SolidColorBrush brush &&
                $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}" == (token.StartsWith('#') ? token : BleeckerTokens.Colors(scheme)[token]),
                $"{scheme} {variant} did not resolve its foreground at the native action scope.");
            foreach (var state in new[] { "PointerOver", "Pressed", "Disabled", "Normal" })
            {
                Require(VisualStateManager.GoToState(native, state, false), $"Native button is missing {state}.");
                await MainWindow.AwaitPresentationAsync(native);
                if (variant == BleeckerButtonVariant.Link)
                {
                    var underline = FindNamed<Border>(LoadingAction, "PART_LinkUnderline")!;
                    Require(underline.Background is SolidColorBrush line && presenter.Foreground is SolidColorBrush text && line.Color == text.Color,
                        "Link underline did not follow the rendered text state color.");
                }
                var suffix = state == "Normal" ? "" : state;
                if (variant == BleeckerButtonVariant.Secondary && state == "PointerOver")
                {
                    // Independent expectations from the pinned rendered Bleecker recipe.
                    var fill = (SolidColorBrush)presenter.Background;
                    var edge = (SolidColorBrush)presenter.BorderBrush;
                    Require(Math.Abs(fill.Opacity - (scheme == BleeckerColorScheme.Dark ? 0.06 : 0.35)) < 0.001 &&
                        Math.Abs(edge.Opacity - (scheme == BleeckerColorScheme.Dark ? 0.18 : 0.75)) < 0.001,
                        "Secondary hover opacity does not match Bleecker.");
                    if (scheme == BleeckerColorScheme.Dark)
                        Require(fill.Color == Microsoft.UI.Colors.White && edge.Color == Microsoft.UI.Colors.White,
                            "Bleecker white overlays were replaced with a semantic text color.");
                }
                foreach (var (property, rendered) in new[] { ("Background", presenter.Background), ("Foreground", presenter.Foreground), ("BorderBrush", presenter.BorderBrush) })
                {
                    // Direct dictionary indexing otherwise uses the process theme,
                    // not the requested theme inherited by the rendered element.
                    var themeResources = (ResourceDictionary)native.Resources.MergedDictionaries.Single().ThemeDictionaries[
                        scheme == BleeckerColorScheme.Light ? "Light" : "Default"];
                    var expected = (SolidColorBrush)themeResources[$"Button{property}{suffix}"];
                    Require(rendered is SolidColorBrush actual && actual.Color == expected.Color && Math.Abs(actual.Opacity - expected.Opacity) < 0.001,
                        $"{scheme} {variant} {state}: native {property} escaped its component palette. Rendered={Describe(rendered)}, expected={Describe(expected)}.");
                }
            }
        }
        LoadingAction.Variant = BleeckerButtonVariant.Primary;
        var originalTemplate = LoadingAction.Template;
        LoadingAction.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                             xmlns:orchard="using:Orchard" TargetType="orchard:BleeckerButton">
                <Button x:Name="PART_Action" Command="{TemplateBinding Command}" CommandParameter="{TemplateBinding CommandParameter}">
                    <StackPanel Orientation="Horizontal"><ProgressRing x:Name="PART_Progress" Width="16" Height="16" /><TextBlock Text="Alternate template" /></StackPanel>
                </Button>
            </ControlTemplate>
            """);
        LoadingAction.ApplyTemplate();
        LoadingAction.UpdateLayout();
        var replacement = Find<Button>(LoadingAction)!;
        Require(!ReferenceEquals(native, replacement), "Template replacement did not create a new native action.");
        var clicks = 0;
        RoutedEventHandler onClick = (_, _) => clicks++;
        LoadingAction.Click += onClick;
        var countBeforeReplacement = invocations;
        ((IInvokeProvider)new ButtonAutomationPeer(replacement).GetPattern(PatternInterface.Invoke)).Invoke();
        await Task.Delay(80);
        Require(clicks == 1 && invocations == countBeforeReplacement + 1, "Template replacement lost or duplicated activation.");
        LoadingAction.Click -= onClick;
        LoadingAction.Template = originalTemplate;
        LoadingAction.ApplyTemplate();
        LoadingAction.UpdateLayout();
        GroupedActions.UpdateLayout();
        var groupedFirst = Find<Button>(GroupedActions.Children[0])!;
        var groupedLast = Find<Button>(GroupedActions.Children[1])!;
        Require(groupedFirst.CornerRadius.TopRight == 0 && groupedFirst.BorderThickness.Right == 0 &&
            groupedLast.CornerRadius.TopRight == BleeckerTokens.Radii["button"], "BleeckerButton did not honor connected group edges.");
    }

    private static T? Find<T>(DependencyObject owner) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++)
        {
            var child = VisualTreeHelper.GetChild(owner, i);
            if (child is T match) return match;
            if (Find<T>(child) is { } nested) return nested;
        }
        return null;
    }

    private static T? FindNamed<T>(DependencyObject owner, string name) where T : FrameworkElement
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++)
        {
            var child = VisualTreeHelper.GetChild(owner, i);
            if (child is T match && match.Name == name) return match;
            if (FindNamed<T>(child, name) is { } nested) return nested;
        }
        return null;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string Describe(Brush brush) => brush is SolidColorBrush color ? $"{color.Color}/{color.Opacity}" : brush.ToString()!;

    private sealed class PreviewCommand(Action<object?> execute) : ICommand
    {
        private bool allowed = true;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => allowed;
        public void Execute(object? parameter) => execute(parameter);
        public void SetCanExecute(bool value)
        {
            allowed = value;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
