using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;

namespace Orchard;

/// <summary>Caller-owned command metadata, content and optional asynchronous callback.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed class BleeckerCommandSpotlightAction : DependencyObject
{
    public BleeckerCommandSpotlightAction() { }
    public BleeckerCommandSpotlightAction(string id, string title) { Id = id; Title = title; }
    internal event EventHandler? Changed;
    private static DependencyProperty Property(string name, Type type, object? value = null) => DependencyProperty.Register(name, type, typeof(BleeckerCommandSpotlightAction), new PropertyMetadata(value, (owner, _) => ((BleeckerCommandSpotlightAction)owner).Changed?.Invoke(owner, EventArgs.Empty)));
    public static readonly DependencyProperty IdProperty = Property(nameof(Id), typeof(string), "");
    public static readonly DependencyProperty TitleProperty = Property(nameof(Title), typeof(string), "");
    public static readonly DependencyProperty DescriptionProperty = Property(nameof(Description), typeof(string));
    public static readonly DependencyProperty GroupProperty = Property(nameof(Group), typeof(string));
    public static readonly DependencyProperty KeywordsProperty = Property(nameof(Keywords), typeof(object));
    public static readonly DependencyProperty ShortcutProperty = Property(nameof(Shortcut), typeof(object));
    public static readonly DependencyProperty HrefProperty = Property(nameof(Href), typeof(string));
    public static readonly DependencyProperty ExternalProperty = Property(nameof(External), typeof(bool), false);
    public static readonly DependencyProperty DisabledProperty = Property(nameof(Disabled), typeof(bool), false);
    public static readonly DependencyProperty IconProperty = Property(nameof(Icon), typeof(object));
    public static readonly DependencyProperty IconTemplateProperty = Property(nameof(IconTemplate), typeof(DataTemplate));
    public string Id { get => (string)GetValue(IdProperty); set => SetValue(IdProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public string? Group { get => (string?)GetValue(GroupProperty); set => SetValue(GroupProperty, value); }
    public IEnumerable<string>? Keywords { get => (IEnumerable<string>?)GetValue(KeywordsProperty); set => SetValue(KeywordsProperty, value); }
    public IEnumerable<string>? Shortcut { get => (IEnumerable<string>?)GetValue(ShortcutProperty); set => SetValue(ShortcutProperty, value); }
    public string? Href { get => (string?)GetValue(HrefProperty); set => SetValue(HrefProperty, value); }
    public bool External { get => (bool)GetValue(ExternalProperty); set => SetValue(ExternalProperty, value); }
    public bool Disabled { get => (bool)GetValue(DisabledProperty); set => SetValue(DisabledProperty, value); }
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public DataTemplate? IconTemplate { get => (DataTemplate?)GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    /// <summary>Runs before closing; a returned task is observed without delaying close. Takes precedence over Href.</summary>
    public Func<Task?>? OnSelectAsync { get; set; }
    internal BleeckerCommandSpotlightData Describe() => new(Id, Title, Description, Group, Keywords?.ToArray() ?? [], Href, Disabled);
}

public sealed class BleeckerCommandSpotlightActions : ObservableCollection<BleeckerCommandSpotlightAction>
{
    public BleeckerCommandSpotlightActions() { }
    public BleeckerCommandSpotlightActions(IEnumerable<BleeckerCommandSpotlightAction> actions) : base(actions) { }
}

public sealed record BleeckerCommandSpotlightNavigationRequest(BleeckerCommandSpotlightAction Action)
{
    public string Href => Action.Href!;
    public bool External => Action.External;
    /// <summary>The caller has routed the original href. Without handling, only absolute external URIs can use the Windows launcher.</summary>
    public bool Handled { get; set; }
}

public sealed record BleeckerCommandSpotlightActionFailure(BleeckerCommandSpotlightAction Action, Exception Exception);
