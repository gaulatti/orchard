# ScrollArea source contract and native preparation

ScrollArea remains pending in the fixed inventory. The reference is Bleecker
commit `a86059013cd59684ce452849278ea81e734f2066`,
`src/components/scroll-area.tsx`, SHA-256
`20c254f224ec44aa8e965674435f40e7f5fc05ce4bdd8abe3c4dc443df5cd78f`.
It is a single scrolling element with original children, forwarded element
attributes/events, caller classes/style, optional maximum height and three
orientations. It adds no data provider, virtualization, selection or storage.

## Observable acceptance contract

- Default Vertical uses `overflow-y:auto` and `overflow-x:hidden`. Horizontal
  swaps the axes; Both uses auto on both. Hidden axes suppress user scrolling
  and their scrollbar while retaining programmatic scrolling and focus-related
  scrolling. Do not conflate hidden with an unscrollable/clipped axis.
- The element keeps natural content size and constrains only its viewport.
  Maximum height is optional. Fixed height, empty/fitting content, caller
  padding/borders and live content size change viewport/extent geometry. Caller
  style spreads after the `maxHeight` convenience prop, including an explicit
  null/undefined maximum that clears it. Caller overflow can override the recipe.
  Native dimension APIs must retain their own documented WinUI precedence.
- The effective Windows source scrollbar reserves six CSS pixels vertically
  and fifteen horizontally. Both-axis overflow feeds back into viewport size.
  The vertical scrollbar moves left in RTL. Auto-width block content fills the
  remaining viewport; explicitly wider content keeps its width. Source DOM
  integer client/extent metrics remain distinct from fractional box geometry.
- Programmatic offsets clamp independently of the selected user orientation.
  Source RTL horizontal offsets are negative from the right origin. Wheel and
  keyboard actions respect the enabled user axis. Orientation/maximum/content
  changes preserve original controls, edits and selection and reclamp offsets.
  Forward original scroll notifications and caller region/name/tab metadata.
- Scrollbar tracks are transparent and fully rounded. Thumbs are rounded and
  use the effective theme's sand token at light 30%/hover 50% or dark 40%/hover
  60%. Resolve actual dark token values rather than substituting the light sand
  color. The source does not animate ordinary scrolling through a component
  motion recipe; its computed scroll behavior is auto in both motion modes.

## Source capture and remaining work

The collector verifies original Git blobs, actual bundled dependencies, styles,
fonts and capture code. It captures all orientations, both directions/themes,
fitting/empty/overflowing content, unbounded and fractional dimensions,
relative/caller maximum-height precedence, custom padding/border and actual
scrollbar hover/drag. Histories include programmatic hidden-axis offsets,
wheel/keyboard actions, original editing and live layout changes.

Playwright normally adds `--hide-scrollbars` to headless launches. This collector
explicitly removes that switch; otherwise it would record a zero-gutter layout
and omit the component's visible scrollbar paint. Earlier exploratory captures
with that switch are superseded. No Windows preferences are changed.

Final independent captures match all 107 receipt/PNG files. Five portable
regressions bind all 94 layouts and hidden-axis/RTL offset histories to
`BleeckerScrollAreaModel`; all 309 portable tests pass. The model compiles in
the x64 and ARM64 library builds. This is preparation, not a mounted control.

The hidden native capability probe compares four stock scrolling configurations
and six reserved-gutter templates (all orientations in both directions).
`ScrollView` with `ContentOrientation="Vertical"` preserves explicitly wide
original content, constrains wrapping content, and allows programmatic offsets
on hidden user axes. Legacy `ScrollViewer` loses either the hidden-axis range
with `Disabled` or the wrapping constraint with `Hidden`. Content orientation
therefore stays vertical independently of the component's user orientation.
The reserved template matches 90 geometry comparisons, including eight thumb
bounds extracted from original PNGs. Ten complete native client PNGs verify
18 diagnostic fill samples. All 338 guards record zero visibility, activation,
focus and input. The independent auditor rejects 27 damaged receipts.

The probe uses the previously verified HoverCard package only as a native host;
it neither contains nor validates a new ScrollArea control. Its deliberately
blue diagnostic thumbs do not claim source theme/hover paint. The pinned
WinUI 2.3.9 package's XML and `generic.xaml` establish the actual API/template
parts. Microsoft's [scrolling guidance](https://learn.microsoft.com/windows/apps/develop/ui/controls/scroll-controls)
describes the content-orientation/visibility distinction; the online 2.5 API
view was unavailable, so the installed pinned metadata remains authoritative.

This preparation is followed by the [native control and fixture](scroll-area.md),
including fresh package replay. ScrollArea is now in progress. Actual physical
input/focus, complete visual/motion, rounded-edge raster matching, scaling,
performance, visible startup and native ARM64 acceptance remain open. Monitoring
and Editorial remain required fictional-data component demos. Work stays in the
background; no Narrator, OS settings, pointer heartbeat, Actions or pushes.
