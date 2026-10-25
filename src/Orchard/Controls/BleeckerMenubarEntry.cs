namespace Orchard;

/// <summary>A caller-owned entry in a Menubar menu.</summary>
public abstract record BleeckerMenubarEntry;

/// <summary>The original label, disabled flag and display-only shortcut.</summary>
public abstract record BleeckerMenubarItem : BleeckerMenubarEntry
{
    protected BleeckerMenubarItem(string label, bool isDisabled, string? shortcut)
    { ArgumentNullException.ThrowIfNull(label); Label = label; IsDisabled = isDisabled; Shortcut = shortcut; }
    public string Label { get; }
    public bool IsDisabled { get; }
    public string? Shortcut { get; }
}

/// <summary>An original caller action, with optional caller presentation content.</summary>
public sealed record BleeckerMenubarAction : BleeckerMenubarItem
{
    public BleeckerMenubarAction(string label, Action onSelect, object? icon = null, bool isDisabled = false, string? shortcut = null) : base(label, isDisabled, shortcut)
    { ArgumentNullException.ThrowIfNull(onSelect); OnSelect = onSelect; Icon = icon; }
    public Action OnSelect { get; }
    public object? Icon { get; }
}

/// <summary>A checked value whose change request is accepted or refused by its caller.</summary>
public sealed record BleeckerMenubarCheckbox : BleeckerMenubarItem
{
    public BleeckerMenubarCheckbox(string label, bool isChecked, Action<bool> onCheckedChange, bool isDisabled = false, string? shortcut = null) : base(label, isDisabled, shortcut)
    { ArgumentNullException.ThrowIfNull(onCheckedChange); IsChecked = isChecked; OnCheckedChange = onCheckedChange; }
    public bool IsChecked { get; }
    public Action<bool> OnCheckedChange { get; }
}

/// <summary>An original value-keyed radio option; place it inside a radio group.</summary>
public sealed record BleeckerMenubarRadioItem : BleeckerMenubarItem
{
    public BleeckerMenubarRadioItem(string label, string value, bool isDisabled = false, string? shortcut = null) : base(label, isDisabled, shortcut)
    { ArgumentNullException.ThrowIfNull(value); Value = value; }
    public string Value { get; }
}

/// <summary>A caller-controlled radio value and its original option collection.</summary>
public sealed record BleeckerMenubarRadioGroup : BleeckerMenubarEntry
{
    public BleeckerMenubarRadioGroup(string value, Action<string> onValueChange, IReadOnlyList<BleeckerMenubarRadioItem> items)
    { ArgumentNullException.ThrowIfNull(value); ArgumentNullException.ThrowIfNull(onValueChange); ArgumentNullException.ThrowIfNull(items); Value = value; OnValueChange = onValueChange; Items = items; }
    public string Value { get; }
    public Action<string> OnValueChange { get; }
    public IReadOnlyList<BleeckerMenubarRadioItem> Items { get; }
}

public sealed record BleeckerMenubarSeparator : BleeckerMenubarEntry;

/// <summary>A recursive menu with original caller entries.</summary>
public sealed record BleeckerMenubarSubmenu : BleeckerMenubarItem
{
    public BleeckerMenubarSubmenu(string label, IReadOnlyList<BleeckerMenubarEntry> items, bool isDisabled = false, string? shortcut = null) : base(label, isDisabled, shortcut)
    { ArgumentNullException.ThrowIfNull(items); Items = items; }
    public IReadOnlyList<BleeckerMenubarEntry> Items { get; }
}

/// <summary>A top-level trigger and its original caller-owned entry collection.</summary>
public sealed record BleeckerMenubarMenu
{
    public BleeckerMenubarMenu(string trigger, IReadOnlyList<BleeckerMenubarEntry> items)
    { ArgumentNullException.ThrowIfNull(trigger); ArgumentNullException.ThrowIfNull(items); Trigger = trigger; Items = items; }
    public string Trigger { get; }
    public IReadOnlyList<BleeckerMenubarEntry> Items { get; }
}
