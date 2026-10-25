using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>Consumer-owned label/control/help/error composition. The child owns its value and validation.</summary>
public sealed partial class BleeckerField : ContentControl
{
    private readonly string identity = "orchard-field-" + Guid.NewGuid().ToString("N");
    private Grid? root;
    private FieldPanel? panel;
    private FieldHeader? header;
    private FieldLabel? label;
    private BleeckerContentText? labelText, description, error, right;
    private ContentPresenter? controlHost;
    private Control? target;
    private PropertyLease? labelLease, identityLease, invalidLease;
    private long invalidToken;
    private bool updatingMetadata;

    public BleeckerField()
    {
        DefaultStyleKey = typeof(BleeckerField); IsTabStop = false;
        Loaded += (_, _) => { if (Content is not Control) throw new InvalidOperationException("Field requires one Control as its content."); Render(); };
        Unloaded += (_, _) => { if (!IsLoaded) DetachControl(); };
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(Render);
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachParts(); });
        RegisterPropertyChangedCallback(IsTextScaleFactorEnabledProperty, (_, _) => Render());
    }
    public static readonly DependencyProperty LabelProperty = Property(nameof(Label), typeof(object), null);
    public static readonly DependencyProperty DescriptionProperty = Property(nameof(Description), typeof(object), null);
    public static readonly DependencyProperty ErrorProperty = Property(nameof(Error), typeof(object), null);
    public static readonly DependencyProperty ActionProperty = Property(nameof(Action), typeof(object), null);
    public static readonly DependencyProperty RequiredProperty = Property(nameof(Required), typeof(bool), false);
    public static readonly DependencyProperty OptionalProperty = Property(nameof(Optional), typeof(bool), false);
    public static readonly DependencyProperty LabelTemplateProperty = Property(nameof(LabelTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty DescriptionTemplateProperty = Property(nameof(DescriptionTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty ErrorTemplateProperty = Property(nameof(ErrorTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty ActionTemplateProperty = Property(nameof(ActionTemplate), typeof(DataTemplate), null);
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerField), new PropertyMetadata(value, (owner, _) => ((BleeckerField)owner).Render()));
    public object? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public object? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public object? Error { get => GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }
    public object? Action { get => GetValue(ActionProperty); set => SetValue(ActionProperty, value); }
    public bool Required { get => (bool)GetValue(RequiredProperty); set => SetValue(RequiredProperty, value); }
    public bool Optional { get => (bool)GetValue(OptionalProperty); set => SetValue(OptionalProperty, value); }
    public DataTemplate? LabelTemplate { get => (DataTemplate?)GetValue(LabelTemplateProperty); set => SetValue(LabelTemplateProperty, value); }
    public DataTemplate? DescriptionTemplate { get => (DataTemplate?)GetValue(DescriptionTemplateProperty); set => SetValue(DescriptionTemplateProperty, value); }
    public DataTemplate? ErrorTemplate { get => (DataTemplate?)GetValue(ErrorTemplateProperty); set => SetValue(ErrorTemplateProperty, value); }
    public DataTemplate? ActionTemplate { get => (DataTemplate?)GetValue(ActionTemplateProperty); set => SetValue(ActionTemplateProperty, value); }
    private bool HasDescription => DescriptionTemplate is not null || BleeckerFieldModel.Present(Description);
    private bool HasError => ErrorTemplate is not null || BleeckerFieldModel.Present(Error);

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        if (newContent is not null && newContent is not Control) throw new ArgumentException("Field content must be one Control.");
        DetachControl(); base.OnContentChanged(oldContent, newContent); Render();
    }
    protected override void OnApplyTemplate()
    {
        DetachParts(); base.OnApplyTemplate();
        root = GetTemplateChild("PART_Root") as Grid;
        if (root is null)
        {
            if (ReadLocalValue(TemplateProperty) is null) return;
            throw new InvalidOperationException("Field requires PART_Root.");
        }
        labelText = new("FieldLabelText", "BleeckerPrimaryMediumFontFamily", 13, 19.5, "BleeckerTextPrimaryBrush", true);
        AutomationProperties.SetAccessibilityView(labelText, AccessibilityView.Raw);
        label = new FieldLabel(labelText) { Name = "FieldLabel", Content = labelText, IsTabStop = false, Padding = new Thickness(0), BorderThickness = new Thickness(0), UseLayoutRounding = false };
        label.Tapped += LabelTapped;
        right = new("FieldRight", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush");
        description = new("FieldDescription", "BleeckerSecondaryFontFamily", 12, 20, "BleeckerTextSecondaryBrush");
        error = new("FieldError", "BleeckerPrimaryFontFamily", 12, 20, "BleeckerTerracottaBrush");
        AutomationProperties.SetAutomationId(description, identity + "-description"); AutomationProperties.SetAutomationId(error, identity + "-error");
        header = new(label, labelText, right) { Name = "FieldHeader", UseLayoutRounding = false };
        controlHost = new ContentPresenter { Name = "FieldControl", HorizontalContentAlignment = HorizontalAlignment.Stretch, UseLayoutRounding = false };
        panel = new(header, controlHost, description, error) { Name = "FieldFlow", UseLayoutRounding = false };
        root.Children.Add(panel); Render();
    }
    private void Render()
    {
        if (panel is null || labelText is null || right is null || description is null || error is null || controlHost is null) return;
        foreach (var slot in new[] { labelText, right, description, error })
        {
            slot.Text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled;
            slot.Marker.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled;
        }
        labelText.Set(Label, LabelTemplate, Required);
        var optional = Action is null && ActionTemplate is null && Optional;
        if (optional)
        {
            right.Text.FontFamily = new FontFamily("ms-appx:///Orchard/Assets/Fonts/LibreFranklin-Regular.ttf#Libre Franklin"); right.Text.FontSize = 11; right.Text.LineHeight = 16.5;
            right.Text.Foreground = description.Text.Foreground; right.Set("Optional", null);
        }
        else
        {
            right.Text.FontFamily = new FontFamily("ms-appx:///Orchard/Assets/Fonts/EncodeSans-Regular.ttf#Encode Sans"); right.Text.FontSize = 16; right.Text.LineHeight = 24;
            right.Text.Foreground = labelText.Text.Foreground; right.Set(Action, ActionTemplate);
        }
        right.Visibility = optional || ActionTemplate is not null || BleeckerFieldModel.Display(Action) is not null ? Visibility.Visible : Visibility.Collapsed;
        description.Set(Description, DescriptionTemplate); error.Set(Error, ErrorTemplate);
        description.Visibility = HasDescription ? Visibility.Visible : Visibility.Collapsed; error.Visibility = HasError ? Visibility.Visible : Visibility.Collapsed;
        if (!ReferenceEquals(controlHost.Content, Content)) controlHost.Content = Content;
        panel.InvalidateMeasure();
        if (IsLoaded && Content is Control control) { AttachControl(control); UpdateMetadata(); }
        if (label is not null) FrameworkElementAutomationPeer.FromElement(label)?.InvalidatePeer();
    }
    private void AttachControl(Control control)
    {
        if (ReferenceEquals(target, control)) return;
        DetachControl(); target = control;
        if (AutomationProperties.GetLabeledBy(control) is null && label is not null) labelLease = new(control, AutomationProperties.LabeledByProperty, label);
        if (control.Name.Length == 0 && !HasSuppliedValue(control, AutomationProperties.AutomationIdProperty) && AutomationProperties.GetAutomationId(control).Length == 0)
            identityLease = new(control, AutomationProperties.AutomationIdProperty, identity + "-control");
        invalidToken = control.RegisterPropertyChangedCallback(AutomationProperties.IsDataValidForFormProperty, (_, _) =>
        {
            if (updatingMetadata || !HasError) return;
            invalidLease?.Restore(); invalidLease = null; UpdateMetadata();
        });
    }
    private static bool HasSuppliedValue(Control control, DependencyProperty property)
    {
        if (control.ReadLocalValue(property) != DependencyProperty.UnsetValue) return true;
        for (var style = control.Style; style is not null; style = style.BasedOn)
            if (style.Setters.OfType<Setter>().Any(setter => setter.Property == property)) return true;
        return false;
    }
    private void UpdateMetadata()
    {
        if (target is null || description is null || error is null) return;
        var references = AutomationProperties.GetDescribedBy(target);
        references.Remove(description); references.Remove(error);
        if (HasDescription) references.Add(description); if (HasError) references.Add(error);
        updatingMetadata = true;
        try
        {
            if (HasError && invalidLease is null) invalidLease = new(target, AutomationProperties.IsDataValidForFormProperty, false);
            if (!HasError) { invalidLease?.Restore(); invalidLease = null; }
        }
        finally { updatingMetadata = false; }
        FrameworkElementAutomationPeer.FromElement(target)?.InvalidatePeer();
    }
    private void DetachControl()
    {
        if (target is not null)
        {
            target.UnregisterPropertyChangedCallback(AutomationProperties.IsDataValidForFormProperty, invalidToken);
            var references = AutomationProperties.GetDescribedBy(target); if (description is not null) references.Remove(description); if (error is not null) references.Remove(error);
        }
        updatingMetadata = true;
        try { invalidLease?.Restore(); labelLease?.Restore(); identityLease?.Restore(); }
        finally { updatingMetadata = false; }
        invalidLease = labelLease = identityLease = null; target = null;
    }
    private void DetachParts()
    {
        DetachControl();
        if (label is not null) { label.Tapped -= LabelTapped; label.Content = null; }
        if (controlHost is not null) controlHost.Content = null;
        foreach (var slot in new[] { labelText, right, description, error }) slot?.Release();
        root = null; panel = null; header = null; label = null; labelText = right = description = error = null; controlHost = null;
    }
    private void LabelTapped(object sender, TappedRoutedEventArgs args)
    {
        if (Content is not Control { IsEnabled: true } control) return;
        for (var element = args.OriginalSource as DependencyObject; element is not null && !ReferenceEquals(element, label); element = VisualTreeHelper.GetParent(element))
            if (element is ButtonBase or TextBox or PasswordBox or RichEditBox or Selector or ToggleSwitch or Slider or NumberBox or Control { IsTabStop: true }) return;
        control.Focus(FocusState.Pointer);
    }
    private sealed class PropertyLease(Control owner, DependencyProperty property, object value)
    {
        private readonly object previous = owner.ReadLocalValue(property);
        private readonly Binding? binding = owner.GetBindingExpression(property)?.ParentBinding;
        private readonly object owned = Assign(owner, property, value);
        private static object Assign(Control owner, DependencyProperty property, object value)
        { if (owner.GetBindingExpression(property) is not null) owner.ClearValue(property); owner.SetValue(property, value); return value; }
        internal void Restore()
        {
            if (owner.GetBindingExpression(property) is not null || !Equals(owner.GetValue(property), owned)) return;
            if (binding is not null) owner.SetBinding(property, binding);
            else if (previous == DependencyProperty.UnsetValue) owner.ClearValue(property);
            else owner.SetValue(property, previous);
        }
    }
    private sealed class FieldLabel(BleeckerContentText content) : ContentControl
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this, content);
        private sealed class Peer(FieldLabel owner, BleeckerContentText content) : FrameworkElementAutomationPeer(owner)
        {
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
            protected override string GetNameCore() => content.AccessibleName();
        }
    }
    private sealed partial class FieldHeader : Panel
    {
        private readonly FrameworkElement label;
        private readonly BleeckerContentText labelText, right;
        private double leftWidth, rightWidth, leftY, rightY, height;
        private double measuredLeftBaseline, measuredRightBaseline;
        internal FieldHeader(FrameworkElement label, BleeckerContentText labelText, BleeckerContentText right)
        {
            this.label = label; this.labelText = labelText; this.right = right; Children.Add(label); Children.Add(right);
            Loaded += (_, _) => LayoutUpdated += UpdateBaselines;
            Unloaded += (_, _) => LayoutUpdated -= UpdateBaselines;
        }
        private void UpdateBaselines(object? sender, object args)
        {
            if (Math.Abs(measuredLeftBaseline - labelText.FirstBaseline) >= .000001 ||
                right.Visibility == Visibility.Visible && Math.Abs(measuredRightBaseline - right.FirstBaseline) >= .000001) InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size available)
        {
            right.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); rightWidth = right.Visibility == Visibility.Visible ? right.DesiredSize.Width : 0;
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); var natural = label.DesiredSize.Width;
            leftWidth = Math.Min(natural, Math.Max(labelText.MinimumWidth, available.Width - rightWidth - (right.Visibility == Visibility.Visible ? 16 : 0)));
            label.Measure(new Size(Math.Max(0, leftWidth), double.PositiveInfinity));
            // Arrange probes establish each arbitrary consumer control's native
            // first text baseline before the final shared-baseline placement.
            label.Arrange(new Rect(0, 0, Math.Max(0, leftWidth), label.DesiredSize.Height)); right.Arrange(new Rect(0, 0, rightWidth, right.DesiredSize.Height));
            var leftBase = labelText.FirstBaseline; var rightBase = right.FirstBaseline;
            measuredLeftBaseline = leftBase; measuredRightBaseline = rightBase;
            var baseline = Math.Max(leftBase, right.Visibility == Visibility.Visible ? rightBase : 0);
            leftY = baseline - leftBase; rightY = baseline - rightBase;
            height = Math.Max(leftY + label.DesiredSize.Height, right.Visibility == Visibility.Visible ? rightY + right.DesiredSize.Height : 0);
            return new(double.IsPositiveInfinity(available.Width) ? natural + rightWidth + (rightWidth > 0 ? 16 : 0) : available.Width, height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            label.Arrange(new Rect(0, leftY, Math.Max(0, leftWidth), label.DesiredSize.Height));
            if (right.Visibility == Visibility.Visible) right.Arrange(new Rect(final.Width - rightWidth, rightY, rightWidth, right.DesiredSize.Height));
            return final;
        }
    }
    private sealed partial class FieldPanel(FrameworkElement header, FrameworkElement control, FrameworkElement description, FrameworkElement error) : Panel
    {
        private BleeckerFieldModel.Flow flow;
        private void EnsureChildren() { if (Children.Count == 0) { Children.Add(header); Children.Add(control); Children.Add(description); Children.Add(error); } }
        protected override Size MeasureOverride(Size available)
        {
            EnsureChildren(); foreach (var child in Children) child.Measure(new Size(available.Width, double.PositiveInfinity));
            flow = BleeckerFieldModel.Place(header.DesiredSize.Height, control.DesiredSize.Height, description.Visibility == Visibility.Visible, description.DesiredSize.Height, error.Visibility == Visibility.Visible, error.DesiredSize.Height);
            return new(Children.Max(child => child.DesiredSize.Width), flow.Height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            header.Arrange(new Rect(0, 0, final.Width, header.DesiredSize.Height)); control.Arrange(new Rect(0, flow.ControlY, final.Width, control.DesiredSize.Height));
            if (description.Visibility == Visibility.Visible) description.Arrange(new Rect(0, flow.DescriptionY, final.Width, description.DesiredSize.Height));
            if (error.Visibility == Visibility.Visible) error.Arrange(new Rect(0, flow.ErrorY, final.Width, error.DesiredSize.Height));
            return final;
        }
    }
}
