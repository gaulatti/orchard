# App Tab Bar preview

`BleeckerAppTabBar` implements the compact standalone App Tab Bar recipe from
Sabella commit `f5ac479bfceaa7eab23c1716ac47420ac53dae25`, `Mobile.swift`.
The Windows inventory includes this cross-platform component. Mobile app shells,
tvOS policies, navigation services and notification services are outside this API.

```csharp
var tabs = new BleeckerAppTabBar
{
    Tabs = [new("overview", "Overview", new FontIconSource
    {
        Glyph = char.ConvertFromUtf32((int)Symbol.Home)
    }), new("queue", "Queue", new FontIconSource
    {
        Glyph = char.ConvertFromUtf32((int)Symbol.Mail)
    }, badge: 127)],
    SelectedValue = "overview"
};
```

`Tabs` is an `ObservableCollection<BleeckerAppTab>`. Each immutable destination
retains its original `Value`, label and caller-owned `FontIconSource`. Value
equality must be unique and stable while present. Values retain their types,
including enums, numbers, records and null. Selection assigns the original value;
bind `SelectedValue` two ways with `UpdateSourceTrigger.PropertyChanged` when the
caller needs immediate updates. `SelectionChanged` reports caller or control
changes. Reselecting keeps the same value. Empty collections and unmatched values
do not choose a default. A selected disabled destination retains selection.

Windows uses the supplied font glyph/family/style/mirroring, without an SF Symbols
name translator. Mutable icon-source properties update the displayed icon. The
bar owns icon size 18, selection weight 600/400 and sea/secondary foreground; the
source icon is never mutated. Labels use bundled Encode Sans at 10, weight
600/500, a two-line limit and native fitting down to 80% before ellipsis. Set
`FontSize` to demonstrate enlarged label text. System text scaling remains enabled
by default. Equal-width buttons have minimum height 54, with top inset 5, card
background and a one-pixel top separator. Glyph and line metrics are Windows
metrics; Apple raster and implicit SwiftUI layout equivalence remain unverified.

Positive badge counts show 1–99 or `99+`; zero and negative counts have no visible
badge. Accessibility ItemStatus preserves any supplied signed 64-bit count as
`<count> notifications`, including zero and negative values, matching the source
metadata. A null count has empty status. Only a null `accessibilityLabel` falls
back to the visible label; an explicit empty label is preserved. Badge text uses
the pinned Encode Sans Bold face, bundled as the sixth font asset.

Native Button Invoke and TabItem SelectionItem activate destinations. The bar
provides single-selection Tab semantics, supports Left/Right (including RTL),
Home/End and skips disabled tabs; Ctrl+Arrow moves focus without selecting.
Removed or unloaded actions reject invocation. A [focused x64 catalog foreground
check](evidence/app-tab-bar-foreground-v1/checkpoint.json) verifies real pointer
selection, disabled Archive rejection, Left/Right/End, Ctrl+Left focus without
selection, Space activation, and Tab/Shift+Tab transfer. The full physical
input matrix, RTL, and independent-consumer foreground input remain open.

`AppTabBarsFixture.xaml` demonstrates fictional local destinations, typed values,
badges, constrained width, disabled state and enlarged text. Monitoring and
Editorial remain required demonstrations. The source host passes 58 behavior,
badge, icon-source and lifecycle cases plus eight layouts/captures. Actual PNG
pixels verify selected-icon placement and badge paint, including physical RTL
bounds. The actual catalog passes both themes and checks equal tracks and centered
icons for all five examples. The foreground check covers the longer sequence
in System dark and focused pointer/keyboard actions in explicit Light and Dark;
it uses the same Orchard DLL SHA-256 as the v9 local package. Actual system text
scaling, full visual/customization and ARM64 runtime acceptance remain open.
Fresh NuGet delivery is recorded separately in the current validation checkpoint.
