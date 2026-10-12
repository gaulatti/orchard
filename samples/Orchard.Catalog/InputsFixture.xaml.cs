using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class InputsFixture : UserControl
{
    public InputsFixture() => InitializeComponent();

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
        await Task.Delay(150);
        UpdateLayout();
        Require(SmallInput.ActualHeight == 36 && MediumInput.ActualHeight == 40 && LargeInput.ActualHeight == 44, "Input size heights differ from the reference.");
        Require(MediumInput.FontFamily.Source.Contains("EncodeSans") && MediumInput.FontSize == 14, "Input lost branded typography.");
        Require(IconsInput.Padding.Left == 40 && IconsInput.Padding.Right == 40, "Adornments overlap the text inset.");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(MediumInput);
        Require(peer.GetAutomationControlType() == AutomationControlType.Edit && peer.GetName() == "Search channels", "Input lost native edit semantics.");
        // Native TextBox patterns are exposed to external UIA clients, not managed IValueProvider.
        MediumInput.Text = "Consumer update";
        MediumInput.Select(0, 8);
        Require(MediumInput.SelectedText == "Consumer", "Native selection did not reflect the consumer value.");
        MediumInput.SelectedText = "Editorial";
        Require(MediumInput.Text == "Editorial update", "Native selection replacement did not reach the editor.");
        Require(!DisabledInput.Focus(FocusState.Keyboard), "Disabled input accepted focus.");
        Require(ErrorInput.Focus(FocusState.Keyboard), "Error input rejected keyboard focus.");
        UpdateLayout();
        var border = Find<Border>(ErrorInput, "BorderElement");
        var expected = BleeckerTokens.Colors(scheme)[scheme == BleeckerColorScheme.Dark ? "accentBlue" : "terracotta"];
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (ColorHex() != expected && elapsed.Elapsed < TimeSpan.FromSeconds(1)) await Task.Delay(16);
        var actual = ((SolidColorBrush)border.BorderBrush).Color;
        Require($"#{actual.A:X2}{actual.R:X2}{actual.G:X2}{actual.B:X2}" == expected, $"Error focus border differs from the reference cascade: {actual}, expected {expected}.");
        string ColorHex() { var color = ((SolidColorBrush)border.BorderBrush).Color; return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}"; }
        MediumInput.Text = "";
        ReadOnlyInput.Focus(FocusState.Keyboard);
        VerifyDynamicGeometry();
        VerifyDisabledError(scheme);
        await MainWindow.CaptureElementAsync(this, System.IO.Path.Combine(folder, $"inputs-{scheme}.png"));
    }

    private void VerifyDisabledError(BleeckerColorScheme scheme)
    {
        var editor = Find<Grid>(DisabledErrorInput, "Editor");
        var border = Find<Border>(DisabledErrorInput, "BorderElement");
        Require(Math.Abs(editor.Opacity - .6) < .000001, "Disabled error input lost its opacity.");
        Require(Hex(border.BorderBrush) == BleeckerTokens.Colors(scheme)["terracotta"], "Disabled error input lost its error border.");
        DisabledErrorInput.IsError = false;
        Require(Hex(border.BorderBrush) != BleeckerTokens.Colors(scheme)["terracotta"] && Math.Abs(editor.Opacity - .6) < .000001,
            "Clearing disabled validation retained the error border or removed disabled opacity.");
        DisabledErrorInput.IsError = true;
        DisabledErrorInput.IsEnabled = true;
        Require(editor.Opacity == 1 && Hex(border.BorderBrush) == BleeckerTokens.Colors(scheme)["terracotta"], "Enabling an invalid editor lost its validation state.");
        DisabledErrorInput.IsEnabled = false;
    }

    private void VerifyDynamicGeometry()
    {
        var start = IconsInput.StartIcon;
        var end = IconsInput.EndIcon;
        var startPresenter = Find<ContentPresenter>(IconsInput, "StartAdornment");
        var endPresenter = Find<ContentPresenter>(IconsInput, "EndAdornment");
        IconsInput.Text = "Retained selection";
        IconsInput.Select(0, 8);
        foreach (var (size, height, inset, font) in new[] {
            (BleeckerInputSize.Small, 36d, 12d, 13d),
            (BleeckerInputSize.Medium, 40d, 14d, 14d),
            (BleeckerInputSize.Large, 44d, 16d, 15d) })
        {
            IconsInput.InputSize = size;
            IconsInput.StartIcon = null;
            IconsInput.EndIcon = null;
            UpdateLayout();
            Require(IconsInput.ActualHeight == height && IconsInput.FontSize == font && IconsInput.Padding == new Thickness(inset, 0, inset, 0),
                "Changing size or removing adornments retained old geometry.");
            Require(startPresenter.Content is null && endPresenter.Content is null, "Removed adornment remained presented.");
            IconsInput.StartIcon = start;
            UpdateLayout();
            Require(IconsInput.Padding == new Thickness(40, 0, inset, 0) && ReferenceEquals(startPresenter.Content, start), "Start adornment failed to restore.");
            IconsInput.EndIcon = end;
            UpdateLayout();
            Require(IconsInput.Padding == new Thickness(40, 0, 40, 0) && ReferenceEquals(endPresenter.Content, end), "End adornment failed to restore.");
            Require(IconsInput.SelectedText == "Retained", "Presentation changes destroyed native selection.");
        }
        IconsInput.IsEnabled = false;
        Require(startPresenter.Opacity == 1 && endPresenter.Opacity == 1 && !startPresenter.IsHitTestVisible && !endPresenter.IsHitTestVisible,
            "Decorative adornments acquired disabled opacity or pointer behavior.");
        IconsInput.IsEnabled = true;
        IconsInput.InputSize = BleeckerInputSize.Medium;
        IconsInput.Text = "";
        UpdateLayout();
    }

    private static string Hex(Brush brush)
    {
        var color = ((SolidColorBrush)brush).Color;
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static T Find<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T element && element.Name == name) return element;
            try { return Find<T>(child, name); } catch (InvalidOperationException) { }
        }
        throw new InvalidOperationException($"Missing template part {name}.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
