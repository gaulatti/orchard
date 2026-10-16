using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerDataTableColumnToggle
{
    private sealed class ColumnRow : ContentControl
    {
        private readonly BleeckerDataTableColumnToggle owner;
        private readonly TextBlock text;
        private bool hovered;
        internal bool LabelPressed;
        internal IBleeckerDataTableVisibilityColumn Column;
        internal BleeckerCheckbox Checkbox { get; }
        internal ColumnRow(BleeckerDataTableColumnToggle owner, IBleeckerDataTableVisibilityColumn column)
        {
            this.owner = owner; Column = column; Name = "PART_ColumnRow"; MinHeight = 36; Padding = new Thickness(8, 6, 8, 6); CornerRadius = new CornerRadius(5); UseLayoutRounding = false;
            IsTabStop = false; HorizontalContentAlignment = HorizontalAlignment.Stretch; VerticalContentAlignment = VerticalAlignment.Stretch;
            AutomationProperties.SetAccessibilityView(this, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
            Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ContentControl'><Border Background='{TemplateBinding Background}' CornerRadius='{TemplateBinding CornerRadius}' Padding='{TemplateBinding Padding}' UseLayoutRounding='False'><ContentPresenter Content='{TemplateBinding Content}' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch'/></Border></ControlTemplate>");
            var grid = new Grid { ColumnSpacing = 10, UseLayoutRounding = false }; grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); grid.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            Checkbox = new BleeckerCheckbox { Width = 20, Height = 20, MinWidth = 0, MinHeight = 0, VerticalAlignment = VerticalAlignment.Center, UseLayoutRounding = false, IsThreeState = false };
            text = new TextBlock { FontSize = 13, LineHeight = 19.5, LineStackingStrategy = LineStackingStrategy.BlockLineHeight, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, UseLayoutRounding = false, IsHitTestVisible = false };
            AutomationProperties.SetAccessibilityView(text, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw); Grid.SetColumn(text, 1); grid.Children.Add(Checkbox); grid.Children.Add(text); Content = grid;
            Checkbox.Checked += Toggled; Checkbox.Unchecked += Toggled;
            PointerEntered += (_, _) => { hovered = true; Render(); }; PointerExited += (_, _) => { hovered = false; Render(); };
            PointerPressed += Pressed; PointerReleased += Released; PointerCanceled += (_, _) => LabelPressed = false;
        }
        internal void Update()
        {
            var label = Column.Header is string value ? value : Column.Id;
            Tag = Column; Checkbox.Tag = Column; AutomationProperties.SetName(Checkbox, label); Checkbox.IsEnabled = owner.IsEnabled; Checkbox.IsChecked = Column.IsVisible; text.Text = label; Render();
        }
        internal void Render()
        {
            Background = owner.Palette(hovered ? "RowHover" : "RowBackground"); text.FontFamily = owner.FontFamily;
            text.Foreground = hovered && owner.accessibility.HighContrast ? owner.Palette("RowHoverForeground") : owner.Foreground;
        }
        private void Toggled(object sender, RoutedEventArgs args) => owner.Change(this, Checkbox.IsChecked == true);
        private void Pressed(object sender, PointerRoutedEventArgs args)
        {
            if (!owner.IsEnabled || !args.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            var point = args.GetCurrentPoint(Checkbox).Position;
            LabelPressed = !new Rect(0, 0, Checkbox.ActualWidth, Checkbox.ActualHeight).Contains(point);
            if (LabelPressed) Checkbox.Focus(FocusState.Pointer);
        }
        private void Released(object sender, PointerRoutedEventArgs args)
        {
            if (!LabelPressed || args.GetCurrentPoint(this).Properties.PointerUpdateKind != Microsoft.UI.Input.PointerUpdateKind.LeftButtonReleased) return;
            LabelPressed = false;
            if (new Rect(0, 0, ActualWidth, ActualHeight).Contains(args.GetCurrentPoint(this).Position)) { owner.Change(this, !Column.IsVisible); args.Handled = true; }
        }
        internal void Detach() { LabelPressed = false; Checkbox.Checked -= Toggled; Checkbox.Unchecked -= Toggled; Checkbox.IsEnabled = false; }
    }
}
