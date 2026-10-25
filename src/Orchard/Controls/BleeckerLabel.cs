using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>Branded, non-tabbable content that labels an optional input control.</summary>
public sealed class BleeckerLabel : ContentControl
{
    private TextBlock? text;
    private ContentPresenter? rich;
    private Control? linkedTarget;
    private object previousLabelValue = DependencyProperty.UnsetValue;
    private Binding? previousLabelBinding;

    public BleeckerLabel()
    {
        DefaultStyleKey = typeof(BleeckerLabel);
        RegisterPropertyChangedCallback(ContentTemplateProperty, (_, _) => UpdateContent());
        RegisterPropertyChangedCallback(ContentTemplateSelectorProperty, (_, _) => UpdateContent());
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => UpdateState());
        Loaded += (_, _) => { LinkTarget(); UpdateState(); };
        Unloaded += (_, _) => UnlinkTarget();
        Tapped += OnTapped;
    }

    public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
        nameof(Target), typeof(Control), typeof(BleeckerLabel),
        new PropertyMetadata(null, (owner, _) => ((BleeckerLabel)owner).LinkTarget()));
    public Control? Target { get => (Control?)GetValue(TargetProperty); set => SetValue(TargetProperty, value); }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        text = GetTemplateChild("PART_Text") as TextBlock;
        rich = GetTemplateChild("PART_Content") as ContentPresenter;
        UpdateContent();
        UpdateState();
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        UpdateContent();
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
        if (Target is not null) FrameworkElementAutomationPeer.FromElement(Target)?.InvalidatePeer();
    }

    private void UpdateContent()
    {
        if (text is null || rich is null) return;
        var plain = (Content is null || Content is string) && ContentTemplate is null && ContentTemplateSelector is null;
        text.Text = plain ? Content as string ?? string.Empty : string.Empty;
        text.Visibility = plain ? Visibility.Visible : Visibility.Collapsed;
        rich.Visibility = plain ? Visibility.Collapsed : Visibility.Visible;
        rich.ContentTemplate = ContentTemplate;
        rich.ContentTemplateSelector = ContentTemplateSelector;
        rich.Content = plain ? null : Content;
    }

    private void UpdateState() => VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", false);

    private void LinkTarget()
    {
        UnlinkTarget();
        if (!IsLoaded || Target is null) return;
        linkedTarget = Target;
        previousLabelValue = linkedTarget.ReadLocalValue(AutomationProperties.LabeledByProperty);
        previousLabelBinding = linkedTarget.GetBindingExpression(AutomationProperties.LabeledByProperty)?.ParentBinding;
        // Disconnect before assignment: a live two-way binding would otherwise
        // write this temporary relationship back into the caller's source.
        if (previousLabelBinding is not null) linkedTarget.ClearValue(AutomationProperties.LabeledByProperty);
        AutomationProperties.SetLabeledBy(linkedTarget, this);
    }

    private void UnlinkTarget()
    {
        if (linkedTarget is not null && linkedTarget.GetBindingExpression(AutomationProperties.LabeledByProperty) is null &&
            ReferenceEquals(linkedTarget.ReadLocalValue(AutomationProperties.LabeledByProperty), this))
        {
            if (previousLabelBinding is not null) linkedTarget.SetBinding(AutomationProperties.LabeledByProperty, previousLabelBinding);
            else if (previousLabelValue == DependencyProperty.UnsetValue) linkedTarget.ClearValue(AutomationProperties.LabeledByProperty);
            else linkedTarget.SetValue(AutomationProperties.LabeledByProperty, previousLabelValue);
        }
        linkedTarget = null;
        previousLabelValue = DependencyProperty.UnsetValue;
        previousLabelBinding = null;
    }

    private void OnTapped(object sender, TappedRoutedEventArgs args)
    {
        if (!IsEnabled || Target is not { IsEnabled: true } target) return;
        for (var element = args.OriginalSource as DependencyObject; element is not null && !ReferenceEquals(element, this); element = VisualTreeHelper.GetParent(element))
            if (element is ButtonBase or TextBox or PasswordBox or RichEditBox or Selector or ToggleSwitch or Slider or NumberBox or Control { IsTabStop: true }) return;
        target.Focus(FocusState.Pointer);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new LabelPeer(this);
    private sealed class LabelPeer(BleeckerLabel owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerLabel);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) && owner.Content is string value ? value : name;
        }
    }
}
