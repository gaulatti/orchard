# Keyboard shortcut display

`BleeckerKbd` displays a keycap or a consumer-owned sequence of keys. It is an
in-progress implementation of the [pinned Kbd contract](kbd-reference.md).

```xml
<orchard:BleeckerKbd Content="Esc" />
<orchard:BleeckerKbd x:Name="SaveShortcut" />
```

```csharp
SaveShortcut.Keys = new ObservableCollection<string> { "Ctrl", "S" };
```

A nonempty `Keys` sequence takes precedence over `Content`; an empty or null
sequence restores content. Observable collections update the display as they
change. Replace or mutate collections on the UI thread. Null entries are rejected;
empty strings and duplicate labels are preserved. Unloading detaches collection
observation, and reloading refreshes it from the current collection.

The control does not register keyboard shortcuts or accept tab focus. Single
content exposes a Text role; a nonempty key sequence exposes a Group named by
joining the labels with `+`. `AutomationProperties.Name` overrides that name.
Rich single content supports `ContentTemplate` and `ContentTemplateSelector`.
Theme resources provide the light/dark palettes and system colors in contrast
mode. No disabled dimming or animation is added.

The catalog's **Keyboard shortcuts** fixture covers single, multiple, empty,
Unicode, long, disabled, RTL and rich content. Runtime checks exercise collection
mutation/replacement, precedence, accessibility roles/names, theme foregrounds
and baseline geometry. A counted collection observer verifies detach on unload
and replacement, one subscription on reload, and changes made while unloaded.
Cleanup is checked after the actual `Unloaded` notification, rather than
immediately after removing the control from its parent.
Per-theme JSON records native keycap dimensions, child positions, font and
palette values for comparison with the browser. The independent package
consumer includes a keycap.

Acceptance is still open: Consolas matches the browser reference's measured
widths for the audited Latin keys and command symbol; native glyph comparison
and other fallback content remain unverified. Rendered
shadow corners, baseline and inline alignment, rich/template edge cases,
contrast, Narrator, isolated scaling, constrained width
and ARM64 runtime still need verification. The component is not marked complete.
