using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class IconButtonsFixture : UserControl
{
    private int count;

    public IconButtonsFixture()
    {
        InitializeComponent();
        EditAction.Command = new IconCommand(parameter => Result.Text = $"Edit {parameter}: {++count}");
    }

    internal async Task VerifyAsync(BleeckerColorScheme scheme)
    {
        var buttons = Descendants(this).OfType<BleeckerIconButton>().ToArray();
        Require(buttons.Length == 10, "Icon button matrix is incomplete.");
        foreach (var button in buttons)
        {
            var dimension = button.Size switch { BleeckerIconButtonSize.Small => 32, BleeckerIconButtonSize.Medium => 40, _ => 44 };
            Require(button.ActualWidth == dimension && button.ActualHeight == dimension, "Icon button lost its pinned square dimensions.");
            Require(!Descendants(button).OfType<SymbolIcon>().Any(), "Icon fixture reverted to a Windows glyph.");
            var vector = Descendants(button).OfType<Viewbox>().Single();
            Require(vector.ActualWidth == 18 && vector.ActualHeight == 18, "Lucide icon lost its reference dimensions.");
            var paths = Descendants(vector).OfType<Microsoft.UI.Xaml.Shapes.Path>().ToArray();
            Require(paths.Length > 0 && paths.All(path => path.StrokeThickness == 2 && path.StrokeStartLineCap == PenLineCap.Round && path.StrokeEndLineCap == PenLineCap.Round), "Lucide strokes differ from the reference.");
            var peer = new ButtonAutomationPeer(button);
            Require(peer.GetAutomationControlType() == AutomationControlType.Button && !string.IsNullOrWhiteSpace(peer.GetName()), "Icon action has no button role or accessible name.");
        }
        var before = count;
        ((IInvokeProvider)new ButtonAutomationPeer(EditAction).GetPattern(PatternInterface.Invoke)).Invoke();
        await Task.Delay(80);
        Require(count == before + 1 && Result.Text == $"Edit story: {count}", "Icon command or parameter failed.");
        Require(!DisabledAction.IsEnabled, "Disabled icon action became enabled.");
        foreach (var button in new[] { EditAction, SubtleAction, GhostAction })
        {
            var presenter = VisualTreeHelper.GetChild(button, 0) as ContentPresenter
                ?? throw new InvalidOperationException("Icon button lost its native presenter.");
            var resources = (ResourceDictionary)button.Resources.MergedDictionaries.Single().ThemeDictionaries[scheme == BleeckerColorScheme.Light ? "Light" : "Default"];
            foreach (var state in new[] { "Normal", "PointerOver", "Pressed", "Disabled", "Normal" })
            {
                Require(VisualStateManager.GoToState(button, state, false), $"Missing icon button state {state}.");
                await Task.Delay(120);
                foreach (var path in Descendants(button).OfType<Microsoft.UI.Xaml.Shapes.Path>())
                {
                    Require(path.Stroke is SolidColorBrush stroke && presenter.Foreground is SolidColorBrush foreground && stroke.Color == foreground.Color && Math.Abs(stroke.Opacity - foreground.Opacity) < 0.001,
                        $"{scheme} {button.Variant} {state} icon does not follow rendered text color.");
                }
                foreach (var (property, rendered) in new[] { ("Background", presenter.Background), ("Foreground", presenter.Foreground), ("BorderBrush", presenter.BorderBrush) })
                {
                    var expected = (SolidColorBrush)resources[$"Button{property}{(state == "Normal" ? "" : state)}"];
                    Require(rendered is SolidColorBrush actual && actual.Color == expected.Color && Math.Abs(actual.Opacity - expected.Opacity) < 0.001,
                        $"{scheme} {button.Variant} {state} {property} does not use the icon palette.");
                }
            }
        }
        var original = (Control)EditAction.Content;
        var custom = new ContentControl { Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red) };
        EditAction.Content = custom;
        Require(original.ReadLocalValue(Control.ForegroundProperty) == DependencyProperty.UnsetValue, "Detached icon retained the button's foreground binding.");
        Require(((SolidColorBrush)custom.Foreground).Color == Microsoft.UI.Colors.Red, "Consumer icon color was overwritten.");
        var customStyle = new Style(typeof(ContentControl));
        customStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Microsoft.UI.Colors.Green)));
        var styled = new ContentControl { Style = customStyle };
        EditAction.Content = styled;
        Require(styled.ReadLocalValue(Control.ForegroundProperty) == DependencyProperty.UnsetValue, "Consumer style foreground was overwritten.");
        EditAction.Content = original;
        var originalStyle = original.Style;
        original.Style = customStyle;
        Require(original.ReadLocalValue(Control.ForegroundProperty) == DependencyProperty.UnsetValue, "A replacement consumer style could not take ownership of its foreground.");
        original.Style = originalStyle;
        Require(original.GetBindingExpression(Control.ForegroundProperty) is not null, "Reference foreground was not restored after replacing the custom style.");
        await Task.Delay(80);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class IconCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
    }
}
