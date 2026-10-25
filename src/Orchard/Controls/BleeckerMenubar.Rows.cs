using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

public sealed partial class BleeckerMenubar
{
    private static TextBlock Text(string value, double? size = null, bool secondary = false)
    {
        var text = (TextBlock)XamlReader.Load("<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TextWrapping='Wrap' TextAlignment='Center' LineHeight='20' LineStackingStrategy='BlockLineHeight' VerticalAlignment='Center'" + (secondary ? " Foreground='{ThemeResource BleeckerTextSecondaryBrush}'" : "") + "/>");
        text.Text = value; if (size.HasValue) text.FontSize = size.Value; return text;
    }
    private static FrameworkElement Glyph(string kind)
    {
        var markup = kind switch
        {
            "check" => "<Viewbox Width='12' Height='12'><Canvas Width='24' Height='24'><Path Data='M20,6 L9,17 L4,12' Stroke='{ThemeResource BleeckerAccentBlueBrush}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/></Canvas></Viewbox>",
            "radio" => "<Ellipse Width='8' Height='8' Fill='{ThemeResource BleeckerAccentBlueBrush}'/>",
            _ => "<Viewbox Width='14' Height='14'><Canvas Width='24' Height='24'><Path Data='M9,18 L15,12 L9,6' Stroke='{ThemeResource BleeckerTextSecondaryBrush}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/></Canvas></Viewbox>"
        };
        return (FrameworkElement)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='" + (kind == "chevron" ? "14" : "16") + "' Height='16' VerticalAlignment='Center' AutomationProperties.AccessibilityView='Raw'>" + markup + "</Grid>");
    }

    private sealed class Row : IDisposable
    {
        private readonly MenuLevel parent;
        private readonly BleeckerMenubar owner;
        private BleeckerMenubarEntry? item;
        private BleeckerMenubarRadioGroup? group;
        private MenuLevel? child;
        internal Row(MenuLevel parent, BleeckerMenubar owner)
        {
            this.parent = parent; this.owner = owner;
            Button = new MenubarButton(owner) { Name = "MenubarItem", Level = parent };
            Button.Request = Invoke;
            Button.Expand = () => { if (item is BleeckerMenubarSubmenu { IsDisabled: false }) parent.OpenSubmenu(Index); };
            Button.Collapse = () => parent.OpenSubmenu(null);
            Button.PointerEntered += (_, _) => { if (item is BleeckerMenubarSubmenu) Button.Expand?.Invoke(); };
            Button.PointerExited += (_, _) => { if (item is BleeckerMenubarSubmenu) Button.Collapse?.Invoke(); };
            Element = Button;
        }
        internal FrameworkElement Element { get; private set; }
        internal MenubarButton Button { get; }
        internal MenuLevel? Child => child;
        internal int Index { get; private set; }
        internal void Update(BleeckerMenubarEntry value, BleeckerMenubarRadioGroup? radioGroup, int index)
        {
            Index = index;
            var changed = !ReferenceEquals(item, value) || !ReferenceEquals(group, radioGroup);
            item = value; group = radioGroup; Button.RadioGroup = radioGroup;
            if (value is BleeckerMenubarSeparator)
            {
                child?.Dispose(); child = null;
                if (Element is not Border)
                {
                    if (Element.Parent is Panel old) old.Children.Remove(Element);
                    Element = (Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Height='1' Margin='0,4' Background='{ThemeResource BleeckerSeparatorBrush}' AutomationProperties.AccessibilityView='Raw'/>");
                }
                return;
            }
            if (value is not BleeckerMenubarItem labeled) throw new ArgumentException("Unsupported Menubar entry.");
            if (!ReferenceEquals(Element, Button)) { if (Element.Parent is Panel old) old.Children.Remove(Element); Element = Button; }
            Button.Style = owner.ItemStyle; Button.DataContext = value; Button.Label = labeled.Label; Button.IsEnabled = !labeled.IsDisabled;
            Button.IsCheck = value is BleeckerMenubarCheckbox; Button.IsRadio = value is BleeckerMenubarRadioItem; Button.HasSubmenu = value is BleeckerMenubarSubmenu;
            Button.IsChecked = value is BleeckerMenubarCheckbox check ? check.IsChecked : value is BleeckerMenubarRadioItem radio && radioGroup?.Value == radio.Value;
            if (changed)
            {
                // Releasing the previous presenter first keeps an original
                // consumer UIElement icon free to occupy its next presentation.
                Button.Content = null;
                var content = new Grid { ColumnSpacing = Button.HasSubmenu ? 0 : 8, UseLayoutRounding = false };
                var hasSlot = !Button.HasSubmenu;
                if (hasSlot) content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var hasTail = Button.HasSubmenu || !string.IsNullOrEmpty(labeled.Shortcut);
                if (hasTail) content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                if (hasSlot)
                {
                    FrameworkElement icon = value is BleeckerMenubarAction { Icon: { } original } ? new ContentPresenter { Content = original, VerticalAlignment = VerticalAlignment.Center } : new Border { Width = 16 };
                    if (Button.IsChecked) icon = Glyph(Button.IsRadio ? "radio" : "check");
                    Grid.SetColumn(icon, 0); content.Children.Add(icon);
                }
                var label = Text(labeled.Label); Grid.SetColumn(label, hasSlot ? 1 : 0); content.Children.Add(label);
                if (hasTail) { var tail = Button.HasSubmenu ? Glyph("chevron") : Text(labeled.Shortcut!, 12, true); Grid.SetColumn(tail, content.ColumnDefinitions.Count - 1); content.Children.Add(tail); }
                Button.Content = content;
            }
            if (value is BleeckerMenubarSubmenu sub) child?.Update(sub.Items);
            else { child?.Dispose(); child = null; Button.IsExpanded = false; }
            Button.InvalidateMetadata();
        }
        private void Invoke()
        {
            if (!Button.IsEnabled || !owner.InputAllowed) return;
            switch (item)
            {
                case BleeckerMenubarAction action: owner.Activate(action.OnSelect); break;
                case BleeckerMenubarCheckbox check: owner.Activate(() => check.OnCheckedChange(!check.IsChecked)); break;
                case BleeckerMenubarRadioItem radio: owner.Activate(() => (group ?? throw new InvalidOperationException("Radio option has no original group.")).OnValueChange(radio.Value)); break;
                // A source submenu has pointer handlers, not a Click handler.
            }
        }
        internal void SetOpen(bool value, bool immediate = false)
        {
            value &= item is BleeckerMenubarSubmenu { IsDisabled: false };
            Button.IsExpanded = value;
            if (value && child is null) { child = new(owner, Button, true); child.Update(((BleeckerMenubarSubmenu)item!).Items); }
            child?.SetOpen(value, immediate);
        }
        internal void Position(UIElement target) => child?.Position(target);
        public void Dispose() { child?.Dispose(); child = null; if (Element.Parent is Panel parent) parent.Children.Remove(Element); Button.Content = null; Button.RadioGroup = null; Button.SelectionGroup = null; Button.Request = Button.Expand = Button.Collapse = null; }
    }

    private sealed partial class MenubarButton : Button
    {
        private readonly BleeckerMenubar owner;
        private bool expanded, hovered;
        internal MenubarButton(BleeckerMenubar owner)
        {
            this.owner = owner; Click += (_, _) => Request?.Invoke(); Loaded += (_, _) => UpdateVisualState();
            InitializeRowPaint();
            PointerEntered += (_, _) => { hovered = true; UpdateVisualState(); };
            PointerExited += (_, _) => { hovered = false; UpdateVisualState(); };
        }
        internal Action? Request, Expand, Collapse;
        internal string Label { get; set; } = "";
        internal bool HasSubmenu { get; set; }
        internal MenuLevel? Level { get; set; }
        internal MenuLevel? ControlledMenu { get; set; }
        internal RadioItemsPanel? SelectionGroup { get; set; }
        internal bool IsRootTrigger { get; set; }
        internal bool IsCheck { get; set; }
        internal bool IsRadio { get; set; }
        internal BleeckerMenubarRadioGroup? RadioGroup { get; set; }
        internal bool IsChecked { get; set; }
        internal bool IsExpanded { get => expanded; set { if (expanded == value) return; expanded = value; UpdateVisualState(); InvalidateMetadata(); } }
        private void UpdateVisualState() => VisualStateManager.GoToState(this, IsRootTrigger && expanded ? "MenuOpen" : hovered ? "MenuHover" : "MenuClosed", false);
        internal void InvalidateMetadata() => FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
        protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this, owner);
        protected override void OnKeyDown(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs args)
        {
            var modifier = new[] { Windows.System.VirtualKey.Control, Windows.System.VirtualKey.Menu, Windows.System.VirtualKey.LeftWindows, Windows.System.VirtualKey.RightWindows }
                .Any(key => Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down));
            if (!args.Handled && (!modifier || args.Key == Windows.System.VirtualKey.Tab))
            {
                var result = owner.NavigateMenu(this, args.Key);
                if (result.Target is { } target) owner.FocusMenuTarget(target);
                args.Handled = result.Handled;
            }
            if (!args.Handled) base.OnKeyDown(args);
        }
        private sealed class Peer(MenubarButton button, BleeckerMenubar owner) : ButtonAutomationPeer(button), IInvokeProvider, IExpandCollapseProvider, IToggleProvider, ISelectionItemProvider
        {
            protected override string GetClassNameCore() => "BleeckerMenubarItem";
            protected override string GetNameCore() => string.IsNullOrEmpty(AutomationProperties.GetName(button)) ? button.Label : AutomationProperties.GetName(button);
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.MenuItem;
            protected override IList<AutomationPeer> GetChildrenCore() => [];
            protected override bool IsEnabledCore() => button.IsLoaded && button.IsEnabled && owner.InputAllowed && (button.Level is null || button.Level.IsOpen);
            protected override bool IsKeyboardFocusableCore() => IsEnabledCore() && base.IsKeyboardFocusableCore();
            protected override void SetFocusCore() { Require(); base.SetFocusCore(); }
            protected override IReadOnlyList<AutomationPeer> GetControlledPeersCore()
            {
                var peers = base.GetControlledPeersCore()?.ToList() ?? [];
                if (button.ControlledMenu?.AutomationPeer is { } menu && !peers.Contains(menu)) peers.Add(menu);
                return peers;
            }
            protected override object? GetPatternCore(PatternInterface pattern) => pattern switch { PatternInterface.Invoke => this, PatternInterface.ExpandCollapse when button.HasSubmenu => this, PatternInterface.Toggle when button.IsCheck => this, PatternInterface.SelectionItem when button.IsRadio => this, _ => base.GetPatternCore(pattern) };
            private void Require() { if (!IsEnabledCore()) throw new ElementNotEnabledException(); }
            public new void Invoke() { Require(); button.Request?.Invoke(); }
            public ExpandCollapseState ExpandCollapseState => button.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
            public void Expand() { Require(); button.Expand?.Invoke(); }
            public void Collapse() { Require(); button.Collapse?.Invoke(); }
            public ToggleState ToggleState => button.IsChecked ? ToggleState.On : ToggleState.Off;
            public void Toggle() => Invoke();
            public bool IsSelected => button.IsChecked;
            public IRawElementProviderSimple? SelectionContainer => button.SelectionGroup is { } group ? ProviderFromPeer(CreatePeerForElement(group)) : null;
            public void Select() { Require(); if (!IsSelected) button.Request?.Invoke(); }
            public void AddToSelection()
            {
                Require();
                if (IsSelected) return;
                if (button.RadioGroup is { } group && group.Items.Any(option => option.Value == group.Value))
                    throw new InvalidOperationException("A Menubar radio group cannot add a second selection.");
                button.Request?.Invoke();
            }
            public void RemoveFromSelection()
            {
                Require();
                if (IsSelected) throw new InvalidOperationException("A Menubar radio option requests its caller's selected value and cannot deselect itself.");
            }
        }
    }
}
