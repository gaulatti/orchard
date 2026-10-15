using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>The original destination, prepared content, native source style and activation supplied to LinkTemplate.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed class BleeckerSidebarLinkContext : DependencyObject
{
    private readonly Activation activation;
    private readonly BleeckerSidebar owner;
    internal BleeckerSidebarLinkContext(BleeckerSidebar owner, bool group, Action? toggle)
    { this.owner = owner; IsGroup = group; Toggle = toggle; activation = new(this); }
    internal bool IsGroup { get; }
    internal Action? Toggle { get; }
    internal bool Mounted { get; set; } = true;
    internal bool Expanded { get; set; }
    internal RotateTransform? Chevron { get; set; }
    internal Microsoft.UI.Xaml.Shapes.Path? ChevronStroke { get; set; }
    internal event EventHandler? Changed;
    private static void Change(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerSidebarLinkContext)owner).Notify();
    public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(nameof(Item), typeof(BleeckerSidebarItem), typeof(BleeckerSidebarLinkContext), new PropertyMetadata(null, Change));
    public BleeckerSidebarItem Item { get => (BleeckerSidebarItem)GetValue(ItemProperty); internal set => SetValue(ItemProperty, value); }
    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(object), typeof(BleeckerSidebarLinkContext), new PropertyMetadata(null));
    public UIElement? Content { get => (UIElement?)GetValue(ContentProperty); internal set => SetValue(ContentProperty, value); }
    public static readonly DependencyProperty StyleProperty = DependencyProperty.Register(nameof(Style), typeof(Style), typeof(BleeckerSidebarLinkContext), new PropertyMetadata(null));
    public Style Style { get => (Style)GetValue(StyleProperty); internal set => SetValue(StyleProperty, value); }
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(BleeckerSidebarLinkContext), new PropertyMetadata(""));
    public string Label { get => (string)GetValue(LabelProperty); internal set => SetValue(LabelProperty, value); }
    public static readonly DependencyProperty HrefProperty = DependencyProperty.Register(nameof(Href), typeof(string), typeof(BleeckerSidebarLinkContext), new PropertyMetadata(""));
    public string Href { get => (string)GetValue(HrefProperty); internal set => SetValue(HrefProperty, value); }
    public static readonly DependencyProperty ActiveProperty = DependencyProperty.Register(nameof(Active), typeof(bool), typeof(BleeckerSidebarLinkContext), new PropertyMetadata(false, Change));
    public bool Active { get => (bool)GetValue(ActiveProperty); internal set => SetValue(ActiveProperty, value); }
    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(nameof(Disabled), typeof(bool), typeof(BleeckerSidebarLinkContext), new PropertyMetadata(false, Change));
    public bool Disabled { get => (bool)GetValue(DisabledProperty); internal set => SetValue(DisabledProperty, value); }
    public ICommand Command => activation;
    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(BleeckerSidebarLinkContext), new PropertyMetadata(null));
    /// <summary>The live source color for consumer content that uses currentColor in the web reference.</summary>
    public Brush? Foreground { get => (Brush?)GetValue(ForegroundProperty); internal set => SetValue(ForegroundProperty, value); }
    public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(nameof(FontFamily), typeof(FontFamily), typeof(BleeckerSidebarLinkContext), new PropertyMetadata(null));
    public FontFamily FontFamily { get => (FontFamily)GetValue(FontFamilyProperty); internal set => SetValue(FontFamilyProperty, value); }
    public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(BleeckerSidebarLinkContext), new PropertyMetadata(16d));
    public double FontSize { get => (double)GetValue(FontSizeProperty); internal set => SetValue(FontSizeProperty, value); }
    public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(nameof(FontWeight), typeof(Windows.UI.Text.FontWeight), typeof(BleeckerSidebarLinkContext), new PropertyMetadata(Microsoft.UI.Text.FontWeights.Normal));
    public Windows.UI.Text.FontWeight FontWeight { get => (Windows.UI.Text.FontWeight)GetValue(FontWeightProperty); internal set => SetValue(FontWeightProperty, value); }
    public static readonly DependencyProperty CharacterSpacingProperty = DependencyProperty.Register(nameof(CharacterSpacing), typeof(int), typeof(BleeckerSidebarLinkContext), new PropertyMetadata(0));
    public int CharacterSpacing { get => (int)GetValue(CharacterSpacingProperty); internal set => SetValue(CharacterSpacingProperty, value); }
    internal void Notify() { Changed?.Invoke(this, EventArgs.Empty); activation.Notify(); }
    private bool CanInvoke() => Mounted && owner.IsEnabled && (IsGroup || (Item.Command?.CanExecute(Item.CommandParameter) ?? true));
    private void Invoke()
    {
        if (!CanInvoke()) return;
        if (IsGroup) Toggle!(); else owner.Invoke(Item);
    }
    private sealed class Activation(BleeckerSidebarLinkContext context) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => context.CanInvoke();
        public void Execute(object? parameter) => context.Invoke();
        internal void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
