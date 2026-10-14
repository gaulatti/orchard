using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

/// <summary>A section heading with optional consumer-owned eyebrow and summary text.</summary>
public sealed class BleeckerSectionHeader : Control
{
    private TextBlock? title, description;
    private BleeckerEyebrow? eyebrow;
    private StackPanel? group;
    private Grid? summaryHost;
    private SummaryPanel? summaryPanel;

    public BleeckerSectionHeader() => DefaultStyleKey = typeof(BleeckerSectionHeader);

    private static DependencyProperty Property(string name) => DependencyProperty.Register(
        name, typeof(string), typeof(BleeckerSectionHeader), new PropertyMetadata(null,
            (owner, _) => ((BleeckerSectionHeader)owner).UpdatePresentation()));
    public static readonly DependencyProperty TitleProperty = Property(nameof(Title));
    public static readonly DependencyProperty DescriptionProperty = Property(nameof(Description));
    public static readonly DependencyProperty EyebrowProperty = Property(nameof(Eyebrow));

    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public string? Eyebrow { get => (string?)GetValue(EyebrowProperty); set => SetValue(EyebrowProperty, value); }

    protected override void OnApplyTemplate()
    {
        summaryPanel?.Children.Clear();
        summaryPanel = null;
        base.OnApplyTemplate();
        title = GetTemplateChild("PART_Title") as TextBlock;
        description = GetTemplateChild("PART_Description") as TextBlock;
        eyebrow = GetTemplateChild("PART_Eyebrow") as BleeckerEyebrow;
        group = GetTemplateChild("PART_Group") as StackPanel;
        summaryHost = GetTemplateChild("PART_DescriptionHost") as Grid;
        if (summaryHost is not null && description is not null)
        {
            summaryHost.Children.Remove(description);
            summaryPanel = new SummaryPanel(); summaryPanel.Children.Add(description); summaryHost.Children.Add(summaryPanel);
        }
        UpdatePresentation();
    }

    private void UpdatePresentation()
    {
        var hasTitle = !string.IsNullOrEmpty(Title);
        var hasDescription = !string.IsNullOrEmpty(Description);
        var hasEyebrow = !string.IsNullOrEmpty(Eyebrow);
        if (title is not null) title.Visibility = hasTitle ? Visibility.Visible : Visibility.Collapsed;
        if (eyebrow is not null) eyebrow.Visibility = hasEyebrow ? Visibility.Visible : Visibility.Collapsed;
        if (description is not null)
        {
            description.Visibility = hasDescription ? Visibility.Visible : Visibility.Collapsed;
        }
        if (summaryHost is not null)
        {
            summaryHost.Visibility = hasDescription ? Visibility.Visible : Visibility.Collapsed;
            summaryHost.Margin = new Thickness(0, hasTitle ? 12 : 0, 0, 0);
        }
        // In the source, the summary's 12px spacing collapses through an empty
        // h2 into the group margin. It remains inside the header only when an
        // eyebrow precedes the group; a standalone summary has no internal gap.
        if (group is not null) group.Margin = new Thickness(0, hasEyebrow && !hasTitle && hasDescription ? 12 : 0, 0, 0);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new SectionHeaderPeer(this);
    private sealed class SectionHeaderPeer(BleeckerSectionHeader owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerSectionHeader);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }

    // A CSS block fills its available inline size up to max-width. TextBlock's
    // Left alignment instead measures the glyph width; a layout panel preserves
    // the source allocation without binding layout output back into measurement.
    private sealed class SummaryPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            var width = Math.Min(672, availableSize.Width);
            var child = Children[0]; child.Measure(new Size(width, availableSize.Height));
            return new Size(width, child.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            Children[0].Arrange(new Rect(0, 0, Math.Min(672, finalSize.Width), finalSize.Height));
            return finalSize;
        }
    }
}
