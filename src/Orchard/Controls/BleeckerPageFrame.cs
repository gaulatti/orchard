using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

/// <summary>A centered page boundary with viewport-responsive spacing and consumer-owned content.</summary>
public sealed class BleeckerPageFrame : ContentControl
{
    private XamlRoot? observedRoot;
    private readonly RecipeDefault maximum, padding;
    public BleeckerPageFrame()
    {
        DefaultStyleKey = typeof(BleeckerPageFrame); IsTabStop = false; UseLayoutRounding = false;
        maximum = new(this, MaxWidthProperty); padding = new(this, PaddingProperty);
        RegisterPropertyChangedCallback(StyleProperty, (_, _) => UpdateRecipes());
        Loaded += (_, _) =>
        {
            ObserveRoot(); UpdateRecipes(); InvalidateMeasure();
        };
        Unloaded += (_, _) => { DisconnectRoot(); DispatcherQueue.TryEnqueue(() => { if (IsLoaded) { ObserveRoot(); UpdateRecipes(); } }); };
        UpdateRecipes();
    }
    public static readonly DependencyProperty ElementProperty = Property(nameof(Element), typeof(BleeckerPageFrameElement), BleeckerPageFrameElement.Div);
    public static readonly DependencyProperty FrameWidthProperty = Property(nameof(FrameWidth), typeof(BleeckerPageFrameWidth), BleeckerPageFrameWidth.Content);
    public static readonly DependencyProperty GutterProperty = Property(nameof(Gutter), typeof(BleeckerPageFrameGutter), BleeckerPageFrameGutter.Comfortable);
    public static readonly DependencyProperty VerticalSpacingProperty = Property(nameof(VerticalSpacing), typeof(BleeckerPageFrameVerticalSpacing), BleeckerPageFrameVerticalSpacing.Comfortable);
    private static DependencyProperty Property(string name, Type type, object value) => DependencyProperty.Register(name, type, typeof(BleeckerPageFrame),
        new PropertyMetadata(value, (owner, _) => ((BleeckerPageFrame)owner).UpdateRecipes()));
    private static void Validate<T>(T value) where T : struct, Enum { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    public BleeckerPageFrameElement Element { get => (BleeckerPageFrameElement)GetValue(ElementProperty); set { Validate(value); SetValue(ElementProperty, value); } }
    public BleeckerPageFrameWidth FrameWidth { get => (BleeckerPageFrameWidth)GetValue(FrameWidthProperty); set { Validate(value); SetValue(FrameWidthProperty, value); } }
    public BleeckerPageFrameGutter Gutter { get => (BleeckerPageFrameGutter)GetValue(GutterProperty); set { Validate(value); SetValue(GutterProperty, value); } }
    public BleeckerPageFrameVerticalSpacing VerticalSpacing { get => (BleeckerPageFrameVerticalSpacing)GetValue(VerticalSpacingProperty); set { Validate(value); SetValue(VerticalSpacingProperty, value); } }
    private void ObserveRoot()
    {
        if (ReferenceEquals(observedRoot, XamlRoot)) return;
        DisconnectRoot(); observedRoot = XamlRoot; if (observedRoot is not null) observedRoot.Changed += RootChanged;
    }
    private void DisconnectRoot() { if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null; }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) { UpdateRecipes(); InvalidateMeasure(); }
    private void UpdateRecipes()
    {
        if (!Enum.IsDefined(Element)) throw new ArgumentOutOfRangeException(nameof(Element));
        var limit = BleeckerPageFrameModel.Maximum(FrameWidth);
        var insets = BleeckerPageFrameModel.Padding(XamlRoot?.Size.Width ?? 0, Gutter, VerticalSpacing);
        maximum?.Apply(limit); padding?.Apply(new Thickness(insets.Horizontal, insets.Vertical, insets.Horizontal, insets.Vertical));
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (IsLoaded) ObserveRoot(); UpdateRecipes();
        var measured = base.MeasureOverride(availableSize);
        return new(double.IsFinite(availableSize.Width) ? availableSize.Width : measured.Width, measured.Height);
    }
    private bool HasStyleSetter(DependencyProperty property)
    {
        for (var style = Style; style is not null; style = style.BasedOn)
            if (style.Setters.OfType<Setter>().Any(setter => setter.Property == property)) return true;
        return false;
    }
    private bool HasSupplied(DependencyProperty property) => ReadLocalValue(property) != DependencyProperty.UnsetValue || HasStyleSetter(property);
    // Recipes are defaults. A caller's binding, local value or Style setter
    // takes precedence without replacing its producer. ClearValue opts back in.
    private sealed class RecipeDefault
    {
        private readonly BleeckerPageFrame owner; private readonly DependencyProperty property;
        private object? owned; private bool writing;
        internal RecipeDefault(BleeckerPageFrame owner, DependencyProperty property)
        {
            this.owner = owner; this.property = property;
            owner.RegisterPropertyChangedCallback(property, (_, _) => { if (!writing) { owned = null; owner.UpdateRecipes(); } });
        }
        internal void Apply(object value)
        {
            if (writing) return;
            if (owned is not null && (owner.GetBindingExpression(property) is not null || !Equals(owner.ReadLocalValue(property), owned))) owned = null;
            writing = true;
            try
            {
                if (owned is not null && owner.HasStyleSetter(property)) { owned = null; owner.ClearValue(property); }
                if (owned is null && owner.HasSupplied(property)) return;
                if (owned is not null && Equals(owned, value)) return;
                owned = value; owner.SetValue(property, value);
            }
            finally { writing = false; }
        }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
    private sealed class Peer(BleeckerPageFrame owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetClassNameCore() => nameof(BleeckerPageFrame);
        protected override AutomationLandmarkType GetLandmarkTypeCore() => owner.HasSupplied(AutomationProperties.LandmarkTypeProperty)
            ? base.GetLandmarkTypeCore() : owner.Element == BleeckerPageFrameElement.Main ? AutomationLandmarkType.Main
                : owner.Element == BleeckerPageFrameElement.Section && GetName().Length > 0 ? AutomationLandmarkType.Custom : AutomationLandmarkType.None;
        protected override string GetLocalizedLandmarkTypeCore() => owner.HasSupplied(AutomationProperties.LocalizedLandmarkTypeProperty)
            ? base.GetLocalizedLandmarkTypeCore() : owner.Element == BleeckerPageFrameElement.Section && GetLandmarkTypeCore() == AutomationLandmarkType.Custom ? "region" : base.GetLocalizedLandmarkTypeCore();
        protected override string GetItemTypeCore() => owner.HasSupplied(AutomationProperties.ItemTypeProperty) ? base.GetItemTypeCore() : owner.Element == BleeckerPageFrameElement.Div ? "" : owner.Element.ToString().ToLowerInvariant();
    }
}
