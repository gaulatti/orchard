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
joining the labels with `+`. `AutomationProperties.Name` overrides that name,
including explicit empty values, live bindings and explicit/BasedOn/ancestor
implicit styles. Clearing an override restores the generated name. An associated
label is authoritative when no caller Name is supplied; an empty associated label
stays empty. Content and Keys bindings remain consumer-owned.
Rich single content supports `ContentTemplate` and `ContentTemplateSelector`.
Consumer `CornerRadius` values and bindings apply to the keycap and its 1px
downward shadow, including generated keys in a combination. Square and asymmetric
bottom corners retain their shape; overlapping corner radii scale together to
fit the keycap. Live changes, resize and template replacement update the shadow.
`HorizontalContentAlignment` and `VerticalContentAlignment` default to Center,
matching the pinned keycap appearance. Caller values and bindings control content
placement within the border and padding; combination children retain live
alignment bindings to their owner. Content, templates and selectors remain
caller-owned through key combinations, template replacement and remounting.
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

Historical registered-fixture and independent-consumer runs passed on native
ARM64 and x64; see [runtime evidence](runtime-evidence.md) for their source and
process identities. Those runs do not establish native ARM64 execution of the
current caller-corner repair. Full state and visual acceptance remains open.

Acceptance is still open: Consolas matches the browser reference's measured
widths for the audited Latin keys and command symbol; native glyph comparison
and other fallback content remain unverified. Rendered
shadow corners, baseline and inline alignment, rich/template edge cases,
isolated scaling, constrained width
and the remaining ARM64 state checks still need verification. The component is
not marked complete.

The focused `--probe-hidden-kbd-corners` regression runs in the catalog and the
independent package consumer without desktop input. It checks eight radius/size
cases in both themes and text directions, captures the actual shadow pixels at
square corners, and exercises bound radius changes, combination children,
retemplating and remounting. These checks qualify that caller override; full
component visual and platform acceptance remains open. Narrator and OS contrast
campaigns are excluded by the current [library scope](library-scope.md).

```powershell
./scripts/verify-kbd-corners.ps1 -AppDirectory <frozen-catalog-app> -Output <new-output-directory>
```

The published Skeleton-preview library fails the square-corner pixel regression.
The corner-preview source catalog, exact NuGet consumer and catalog archive runtime
each pass all 32 radius/size/theme/direction states and four
combination/template/remount contexts with zero focus or activation. Source and
independent consumer builds pass for x64 and ARM64; native ARM64 execution remains
unverified for this repair.
The unaffected default Light/LTR capture is unchanged. This focused result does
not establish complete browser/native visual parity or actual display scaling.

The content-alignment preview qualifies all 16 horizontal/vertical alignment
combinations in both themes and text directions, using native Border layout as
the reference for the actual caller element. Source, independent package and
archive hosts each pass 64 alignment states and 16 checks for live combination
bindings, original model templates/selectors, and retained content through
retemplating and remounting. The source host also reruns the 32-state corner
regression; all four default Light/Dark and LTR/RTL keycap captures are unchanged.

```powershell
./scripts/verify-kbd-content.ps1 -AppDirectory <frozen-catalog-app> -Output <new-output-directory>
```

The same fixture now exercises actual isolated native XAML rasterization at
100%, 125%, 150% and 200% in both themes. It checks physical PNG dimensions,
caller content against native Border layout, default keycap/group geometry and
names, and caller bindings. This uses the existing owned XAML island mechanism;
it does not change monitor DPI or Windows text scale. Metadata cases cover single
content and key groups, local/bound/style names, label associations, template
replacement and remounting. The registered catalog includes an explicitly empty
caller name. Full rendered typography/fallback, physical scaling, native ARM64
execution and remaining lifecycle acceptance stay open.
