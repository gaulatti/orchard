# Sidebar

`BleeckerSidebar` renders consumer-owned destinations, one level of expandable
groups, optional header/footer content and independently scrolling navigation.
The frozen [source contract](sidebar-reference.md) is the acceptance baseline.
This component is in progress; full desktop and visual acceptance is open.

```xml
<orchard:BleeckerSidebar Height="480" Header="Workspace" Footer="Local only">
    <orchard:BleeckerSidebarItem Id="overview" Label="Overview"
                                Href="/overview" Active="True" />
    <orchard:BleeckerSidebarItem Id="coverage" Label="Coverage">
        <orchard:BleeckerSidebarItem.Items>
            <orchard:BleeckerSidebarItem Id="drafts" Label="Drafts" Href="/drafts" />
            <orchard:BleeckerSidebarItem Id="published" Label="Published"
                                        Href="/published" Active="True" />
        </orchard:BleeckerSidebarItem.Items>
    </orchard:BleeckerSidebarItem>
</orchard:BleeckerSidebar>
```

`Items` and each item's `Items` retain their original
`ObservableCollection<BleeckerSidebarItem>`. Item property and collection
notifications update the mounted control. Expansion is initialized from
immediate active children once per mounted top-level id. Later active changes,
same-id replacement, reorder and collapsed mode retain that state. Removing
and readding an id initializes it again. An immediate child with a destination
stays a link even if its record has children. Entries without destinations
disappear in collapsed mode. Item ids follow the source's keyed contract;
provide stable ids and do not depend on duplicate-key reconciliation.

`Href` is opaque. Subscribe to `ItemInvoked` or provide an item's `Command` and
`CommandParameter` to perform application navigation. The library passes the
original item and parameter, and never constructs or launches a URI. Group
triggers only toggle expansion; group commands are not navigation actions.
Command availability and the whole control's `IsEnabled` remain native guards.

`Disabled` deliberately matches the source CSS state: links have half opacity
and block pointer hit testing while remaining keyboard accessible. A group's
`Disabled` field does not disable its expanded-mode trigger. Use command
availability or the native control boundary when an action must be unavailable
from every input path. Do not replace these distinct contracts with one flag.

The expanded default is 256 units wide with `Padding="12,20"`; collapsed mode
uses 64 units and `CollapsedPadding="8,20,8,20"`. An explicit native `Width`
overrides both preferred widths. To override padding in both modes, set both
`Padding` and `CollapsedPadding`. Header/footer borders, 40-unit rows, four-unit
top-level gaps, the child indent and active rail follow the measured source.
Navigation uses a native `ScrollViewer`; its scrollbar raster remains a parity
gap against the source's six-pixel scrollbar.

Header, footer, icon and badge content accepts native UI elements, text,
numbers and booleans. Numeric zero and boolean truthiness follow the recorded
source edge cases. Set `HeaderTemplate`, `FooterTemplate`, `IconTemplate` or
`BadgeTemplate` before supplying a model object; untemplated model objects are
rejected instead of being rendered with `ToString`. A supplied template
represents a rendered node, so a false/zero/null payload is passed intact to
that template and does not suppress its slot. UI elements have one native
parent; use data and templates for repeated content. Original elements and
template data are retained rather than cloned.

`LinkTemplate` receives `BleeckerSidebarLinkContext`: the original `Item`, exact
`Href`, `Label`, prepared `Content`, activation `Command` and native `Style`.
The style targets `HyperlinkButton` and installs the source's state rendering.
The context also exposes live `Foreground`, `FontFamily`, `FontSize`,
`FontWeight` and `CharacterSpacing` for consumer content. The context and item
types carry WinUI bindable metadata, including properties used by runtime
bindings. Default icon/badge controls receive owned formatting bindings only
where local bindings, local values and explicit styles have not specified
them. Those owned bindings are removed when content is detached; consumer
overrides are preserved.

```xml
<DataTemplate x:Key="Destination" x:DataType="orchard:BleeckerSidebarLinkContext">
    <HyperlinkButton Content="{x:Bind Content, Mode=OneWay}"
                     Style="{x:Bind Style}"
                     Command="{x:Bind Command}"
                     AutomationProperties.Name="{x:Bind Label, Mode=OneWay}" />
</DataTemplate>
```

The native adaptation gives collapsed links their consumer labels as
accessible names, exposes group expansion through UI Automation's
ExpandCollapse pattern, and uses Bleecker focus brushes with native Tab and
button/link activation. Group expansion raises the corresponding automation
property change. These are Windows adaptations; the source's collapsed anchor
can have an empty name. In-app contrast resources use system colors without
changing Windows settings.

Width/padding motion uses the measured 200 ms default easing. Palette and rail
motion uses 190 ms premium easing; the chevron uses 220 ms. The held one-unit
translation follows the actual individual CSS property, whose source
transition list does not animate it. Windows reduced-motion and contrast
preferences stop these clocks and publish their final state. No OS setting is
changed by the control or its checks.

The registered **Sidebar** catalog fixture follows Dashboard Grids. Focused
`scripts/smoke-catalog.ps1 -Component sidebar` exercises the frozen geometry and
actual label/icon colors, original callbacks, pointer/keyboard semantics,
expansion retention, independent scrolling, command availability, live data,
key changes, reparenting and template replacement. Tab verifies one press at
its source and one release at its new focus destination; same-target key
checks and pointer/presentation gates remain unchanged. The independent NuGet
consumer declares both the control and the typed link template.

Exact raster, the full state/motion/override and RTL matrices, arbitrary content
and template combinations, component theme/automation behavior, display/text
scaling, measured large-collection performance and native ARM64 runtime remain
required. The 35-row source fixture is not large-data performance acceptance.
The existing catalog left navigation still uses button/input previews. This
native presentation component has no backend or Prometheus concern. The wiki
is unavailable; documentation remains in the code repository. Push Actions
remain paused by user instruction.
