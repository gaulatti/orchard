using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

public enum BleeckerEyebrowTone { Accent, Muted, Inverse }

/// <summary>Small uppercase section content with an optional decorative rule.</summary>
public sealed class BleeckerEyebrow : ContentControl
{
    private ResourceDictionary? palette;
    private TextBlock? text;
    private ContentPresenter? rich;
    private Border? rule;

    public BleeckerEyebrow()
    {
        DefaultStyleKey = typeof(BleeckerEyebrow);
        ApplyTone();
        RegisterPropertyChangedCallback(ContentTemplateProperty, (_, _) => UpdateContent());
        RegisterPropertyChangedCallback(ContentTemplateSelectorProperty, (_, _) => UpdateContent());
        RegisterPropertyChangedCallback(LanguageProperty, (_, _) => UpdateContent());
    }

    public static readonly DependencyProperty ToneProperty = DependencyProperty.Register(nameof(Tone), typeof(BleeckerEyebrowTone), typeof(BleeckerEyebrow),
        new PropertyMetadata(BleeckerEyebrowTone.Accent, (owner, _) => ((BleeckerEyebrow)owner).ApplyTone()));
    public BleeckerEyebrowTone Tone { get => (BleeckerEyebrowTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    public static readonly DependencyProperty RuleProperty = DependencyProperty.Register(nameof(Rule), typeof(bool), typeof(BleeckerEyebrow),
        new PropertyMetadata(false, (owner, _) => ((BleeckerEyebrow)owner).UpdateRule()));
    public bool Rule { get => (bool)GetValue(RuleProperty); set => SetValue(RuleProperty, value); }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        text = GetTemplateChild("PART_Text") as TextBlock;
        rich = GetTemplateChild("PART_Content") as ContentPresenter;
        rule = GetTemplateChild("PART_Rule") as Border;
        UpdateContent();
        UpdateRule();
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        UpdateContent();
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }

    private void UpdateContent()
    {
        if (text is null || rich is null) return;
        var plain = (Content is null || Content is string) && ContentTemplate is null && ContentTemplateSelector is null;
        text.Text = plain ? BleeckerTextCasing.Uppercase(Content as string ?? string.Empty, Language) : string.Empty;
        text.Visibility = plain ? Visibility.Visible : Visibility.Collapsed;
        rich.Visibility = plain ? Visibility.Collapsed : Visibility.Visible;
        rich.ContentTemplate = ContentTemplate;
        rich.ContentTemplateSelector = ContentTemplateSelector;
        rich.Content = plain ? null : Content;
    }

    private void UpdateRule()
    {
        if (rule is not null) rule.Visibility = Rule ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyTone()
    {
        if (!Enum.IsDefined(Tone)) throw new ArgumentOutOfRangeException(nameof(Tone));
        if (palette is null)
        {
            palette = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Eyebrow.xaml") };
            Resources.MergedDictionaries.Add(palette);
        }
        // Keep resolved brushes stable across live tone changes, as for Cards.
        foreach (var theme in new[] { "Light", "Default" })
        {
            var resources = (ResourceDictionary)palette.ThemeDictionaries[theme];
            var source = (SolidColorBrush)resources[$"Eyebrow{Tone}"];
            var destination = (SolidColorBrush)resources["EyebrowForeground"];
            destination.Color = source.Color;
            destination.Opacity = source.Opacity;
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new EyebrowPeer(this);
    private sealed class EyebrowPeer(BleeckerEyebrow owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerEyebrow);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override string GetNameCore()
        {
            if (BleeckerAutomationMetadata.TryGetName(owner, this, out var callerName)) return callerName;
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) && owner.Content is string value ? value : name;
        }
    }
}
