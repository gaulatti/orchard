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

    public ButtonsFixture()
    {
        InitializeComponent();
        LoadingAction.Command = new PreviewCommand(parameter =>
        {
            invocations++;
            ButtonResult.Text = $"Command {parameter}: {invocations}";
        });
    }

    internal async Task VerifyAsync(BleeckerColorScheme scheme)
    {
        LoadingAction.ApplyTemplate();
        var native = Find<Button>(LoadingAction) ?? throw new InvalidOperationException("BleeckerButton has no native action.");
        var peer = new ButtonAutomationPeer(native);
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
        try { invoke.Invoke(); throw new InvalidOperationException("Loading action accepted invocation."); }
        catch (ElementNotEnabledException) { }
        catch (System.Runtime.InteropServices.COMException error) when (error.HResult == unchecked((int)0x80040200)) { } // UIA_E_ELEMENTNOTENABLED
        ConsumerEnabled.IsOn = false;
        LoadingSwitch.IsOn = false;
        await Task.Delay(80);
        Require(!native.IsEnabled && !LoadingAction.IsEnabled, "Loading completion discarded the consumer's disabled state.");
        Require(LoadingAction.GetBindingExpression(Control.IsEnabledProperty) is not null, "Consumer IsEnabled binding was replaced.");
        ConsumerEnabled.IsOn = true;
        await Task.Delay(80);
        Require(native.IsEnabled, "Consumer re-enabling did not reach the native action.");
        foreach (var variant in Enum.GetValues<BleeckerButtonVariant>())
        {
            LoadingAction.Variant = variant;
            LoadingAction.UpdateLayout();
            await Task.Delay(120);
            var presenter = Find<ContentPresenter>(native) ?? throw new InvalidOperationException("Native presenter is missing.");
            var token = variant switch
            {
                BleeckerButtonVariant.Primary => "primaryForeground",
                BleeckerButtonVariant.Destructive => "destructiveForeground",
                BleeckerButtonVariant.Outline or BleeckerButtonVariant.Link => "primary",
                BleeckerButtonVariant.Subtle when scheme == BleeckerColorScheme.Light => "deepSea",
                _ => "textPrimary"
            };
            Require(presenter.Foreground is SolidColorBrush brush &&
                $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}" == BleeckerTokens.Colors(scheme)[token],
                $"{scheme} {variant} did not resolve its foreground at the native action scope.");
            foreach (var state in new[] { "PointerOver", "Pressed", "Disabled", "Normal" })
            {
                Require(VisualStateManager.GoToState(native, state, false), $"Native button is missing {state}.");
                await Task.Delay(250);
                var suffix = state == "Normal" ? "" : state;
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

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string Describe(Brush brush) => brush is SolidColorBrush color ? $"{color.Color}/{color.Opacity}" : brush.ToString()!;

    private sealed class PreviewCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
    }
}
