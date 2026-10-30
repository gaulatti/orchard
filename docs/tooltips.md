# Tooltip preview

For the 2026-10-29 x64 milestone, Tooltip passes the five required checks:
Light/Dark states across its four placements and content examples, physical
pointer hover/leave and keyboard focus/Escape, 16 native 150% placement and
viewport cases without clipping, immediate dismissal under reduced motion,
and independent package-consumer compilation. ARM64 remains build-only and its
runtime is unverified. Older diagnostics below are historical and do not add
milestone gates.

Current scope is the [component library and its fictional-data demos](library-scope.md).
Narrator and OS accessibility/contrast campaigns are excluded.

Inside `BleeckerModalHost`, Tooltip and Field Help Tooltip now use the shared
portal plane. Their original control, consumer content, template/selector,
description relationship and requested theme stay intact. Outside that host,
the application-constrained native Popup path remains available. A private
wrapper gates pointer input without changing caller enabled values or bindings.
Unowned tooltips keep source z-index 180; modal-owned help uses overlay+1.
Newer modals or Select scopes suspend older help. Help declared inside a Select
option belongs to that active logical scope; owner unloading closes it once.
The host's peer tree follows the same active popup scope and logical branches.

Escape dismisses active help before its modal, Popover or Select owner. The
source browser histories verify tooltip-only dismissal and retained trigger
focus. Hidden native checks verify the dismissal decision and ownership; the
current extracted x64 catalog also verifies physical keyboard delivery and
native focus in both themes.

The default style disables layout rounding, and the shared text renderer uses
the pinned CSS layout-unit metrics. This prevents the popup container from
changing body and arrow geometry. Placement snaps body coordinates to the
actual raster scale; arrow alignment uses Floating UI's integer client extent.
The default content slot supports original model data with `ContentTemplate`
or `ContentTemplateSelector`, assigning the model before selecting its template.
Live content, typography and foreground changes update that presentation.
Replacement control templates can supply the built-in `PART_ContentHost` panel;
templates that render their own content keep ownership of that rendering.

`TooltipsFixture` includes a fictional Popover containing an original tooltip
trigger. The frozen `tests/Reference/Tooltip.json` contains 40 light/dark states
and four Escape histories, with two independent JSON/PNG captures matching byte
for byte. The shared placement model matches all 40 source states. Current
background native validation compares both popup paths, modal ownership,
Select scope changes, field-help removal and loaded-window teardown.
Those historical checks alone did not establish physical input/focus or scaling;
the current x64 milestone evidence is stated above. Native ARM64 runtime is
unverified.

The [extracted-v7 x64 foreground probe](evidence/tooltip-foreground-v1/checkpoint.json)
now verifies the Top specimen in both Light and Dark. A swept physical pointer
entry opens it; moving below the trigger closes it. Physical Tab and Shift+Tab
return keyboard focus and open it, and physical Escape dismisses it. The probe
checks the catalog process identity and foreground window. Earlier one-step
cursor injection was intermittent, so its failed reports are retained without
claiming a component defect. UI Automation programmatic focus alone does not
model keyboard focus. The control and XAML specimen are unchanged since this
v7 archive; fresh v8 extraction and the remaining placements/transit, modal,
timing, visual, scaling and ARM64 checks remain open.
The [archive-identical v8 catalog file set](evidence/components-v8-linked-catalog-v1/checkpoint.json)
also passes this focused Top hover/leave, Tab/Shift+Tab and Escape probe in
both themes from a separate runnable directory. It does not close the broader
Tooltip matrix or establish a fresh independent ZIP extraction.

The programmatic appearance checks establish a genuine neutral pointer receipt
at the fixture heading before opening tooltip specimens. At source `ba1434b`,
`artifacts/smoke-stepper-motion-full` records LongTip closing after a delayed
BottomTip hover timer opens BottomTip. That is a competing input path during a
programmatic check, not proof that LongTip failed to render. Both old and new
focused builds pass. The fixture now isolates that programmatic input condition;
the physical hover/transit checks, original two-second deadline, two stable
Rendering transitions and single-open assertion remain unchanged. This evidence
establishes the cause of this run only; earlier historical failures below retain
their recorded scope.

`BleeckerTooltip` owns a WinUI `Popup` constrained to the application's XAML root. Attach it with Orchard's service, which supplies the input/placement owner and its accessible description relationship:

```xml
<Button Content="Settings" Style="{StaticResource BleeckerSecondaryButtonStyle}">
    <orchard:BleeckerTooltipService.ToolTip>
        <orchard:BleeckerTooltip Content="Workspace settings" Placement="Top" />
    </orchard:BleeckerTooltipService.ToolTip>
</Button>
```

This replaces the earlier preview's native `ToolTipService.ToolTip` API. Migrate that attached property to `orchard:BleeckerTooltipService.ToolTip`. The component now derives from ContentControl, not ToolTip. Do not wrap it in the native service: that creates another independently positioned Windows tooltip. A programmatic consumer can set PlacementTarget directly before opening IsOpen; the target must be loaded. One tooltip has one owner.

Content and ContentTemplate remain consumer-owned. The template uses Encode Sans 12px/19.5px, 12/8px padding, a 224px maximum body width, a 6px corner and no border. Top/bottom arrows are 10×5px; left/right arrows occupy 5×10px. The arrow matches the surface. Light and dark colors follow the [measured reference](tooltip-reference.md); Windows contrast uses paired window/window-text colors. The shared overlay supplies the shadow recipe without the earlier separate tooltip window clipping its blur.

The popup takes its inherited theme from PlacementTarget.ActualTheme when opened and follows the owner's ActualThemeChanged event. Its existing open-layout observation also synchronizes that value: on the pinned runtime, a detached and reattached owner can change ActualTheme without delivering the event. This keeps an already-open surface, arrow and text synchronized with application appearance changes. The Tooltip's own RequestedTheme is left available for an explicit consumer override. The catalog includes a consumer ContentTemplate fixture; bound text retains the inherited font and foreground, wraps, and updates when Content changes. Supply AutomationProperties.Name for structured content that is not represented by a plain string.

Placement accepts Top, Bottom, Left or Right. ActualPlacement reports the resolved side. The calculation uses a 7px arrow-tip clearance and 10px viewport padding, flips to the opposite side when it has more space and the preferred side cannot fit, and shifts the body within the viewport. The arrow tracks its owner's center and hides if the shifted body cannot point at it. Geometry changes trigger remeasurement; unchanged geometry does not repeatedly invalidate layout.

Keyboard focus opens immediately. Escape, focus loss, pointer press and activation of a native ButtonBase or BleeckerButton dismiss without moving focus. Activation handlers leave the normal click/command path intact and detach when the tooltip owner changes. Hover uses DelayDuration (400ms by default) and a 150ms same-tooltip skip window. Only one tooltip is open per XAML root. The control exposes the tooltip automation role and open/close events, and the service links it through the owner's DescribedBy relationship. Removing the service detaches its owner handlers and description relationship.

Native window deactivation also delivers focus loss and dismisses help, even
when WinUI retains its logical focused element for later activation. The hosted
Windows contrast fixture distinguishes that native lifecycle from a popup
closing while its owner retains foreground; see [contrast verification](contrast-verification.md).

The catalog checks all four directions in the viewport center and all four opposite-side collision flips in both themes. Checks measure 7px clearance, assert the popup is constrained, and verify body geometry, fonts, colors, arrows, role, wrapping, live content remeasurement, single-open behavior and the description link. Physical dark-theme Tab entry and Escape dismissal preserve trigger focus; the desktop capture is `artifacts/tooltip-owned-keyboard.jpg`.

The final full catalog run is `artifacts/smoke-tooltip-owned-verified`; the isolated package consumer is `artifacts/package-consumer-tooltip-owned`. Both pass. An earlier full run stopped at the notification fixture's short-text remeasurement check; the final run passes that same check without changing the notification implementation. ARM64 compilation also passes.

Pointer transit uses the pinned Radix 1.2.16 convex grace polygon, including its 5px exit padding. Moving between the trigger and content keeps the tooltip open; subsequent movement outside that polygon dismisses it. The geometry is tested in both directions on all four sides. Actual Windows pointer input verifies top placement in dark appearance through the gap, into content, back to the trigger, and out of the polygon. A discrete jump creates the polygon at the exit sample; the next movement outside dismisses it. The content capture is `artifacts/tooltip-transit-content.jpg`. The adapted geometry's MIT notice is included in the NuGet package as `licenses/LICENSE-Radix.txt`.

The transit checkpoint passes 13 unit tests, x64 published catalog smoke (`artifacts/smoke-tooltip-transit`), ARM64 compilation, and the isolated x64 package consumer (`artifacts/package-consumer-tooltip-transit`).

Motion now uses the surface and motionExit duration tokens: delayed hover entry fades from 0 to 1 and scales .98 to 1 over 220ms with cubic-bezier(.22,1,.36,1); exit fades to 0 and scales to .98 over 150ms with CSS ease-in. Keyboard/programmatic/skip-window entry remains immediate. The template owns the transform, leaving the consumer's control transform untouched. IsOpen and Closed describe logical dismissal; the popup is removed after exit. Reopening cancels the old exit. Detaching or unloading the owner removes it immediately. Windows disabled animations bypass both animations; disabling animations during motion settles an open tooltip or removes a closing one.

This is still in progress. The complete physical transit matrix, rendered normal-motion timing/curve comparison, measured hover/skip timing, modal ownership, collision corners and very small viewports, explicit nested theme overrides, and scaled layouts require further work and verification. Shadow and font appearance remain subject to meaningful rendered comparison. ARM64 compilation does not establish ARM64 runtime on this Intel host.

Motion checkpoint evidence: reduced-motion smoke at artifacts/smoke-tooltip-motion; final enabled-motion smoke at artifacts/smoke-tooltip-motion-duration; final isolated package consumer at artifacts/package-consumer-tooltip-motion. The catalog checks immediate entry, different reduced/normal dismissal lifetimes, interrupted exit/reopening and eventual popup removal. All pass; 13 unit tests and the final ARM64 build also pass. These checks do not yet measure the physical delayed-hover animation frame by frame.

Live-appearance checkpoint: artifacts/smoke-tooltip-live-fixed passes both directions for all six fixtures, including restoration of the original palette. The independent consumer at artifacts/package-consumer-tooltip-live also passes live theme changes and restoration. All 13 unit tests and x64/ARM64 builds pass.

Reload regression: `artifacts/smoke-navigation-tooltip-layout` passes light/dark
theme switching and restoration after navigating away from and back to the
tooltip fixture. Run the focused check with `scripts/smoke-catalog.ps1 -Component
tooltips` against a published catalog. Palette assertions await the observable
result with a one-second bound; the check does not assume a fixed frame delay.
The full catalog passes with animations disabled and enabled at
`artifacts/smoke-navigation-fixed` and `artifacts/smoke-navigation-motion`.
The independent x64 consumer passes at `artifacts/package-consumer-navigation`
using package SHA-256
`3b2ab31d6c0549034d3c1455bad2cc6f58b452f0a17a17b9386350a97a05bcef`.

Activation verification passes in artifacts/smoke-tooltip-activation-ready for a native button, BleeckerButton's inner native action, and field help, including retained focus and delivery of the consumer click handler. The isolated package consumer passes at artifacts/package-consumer-tooltip-activation. Physical dark-theme Enter and Space dismiss field help without losing its focus ring (artifacts/field-help-enter-dismissed.jpg). x64/ARM64 builds and 13 unit tests pass. Other input/theme/accessibility matrix gaps remain open.

A later full run at source `23784436f58fa80d68b44bcad145bc560c1bf6be`
stopped after 141.9706138 seconds at Light `BottomTip`:
`artifacts/smoke-ghost-byte-alpha-published-full`. Its two-second presentation
report has one settled frame followed by unsettled observations, so this run
is a failure. It does not establish why the tooltip stopped settling. The
presentation checker now records open/load/popup/motion state for unsettled
frames and writes failure-only tooltip/focus/popup snapshots and actual
Opened/Closed call stacks. The original two-second deadline and two stable
render transitions remain required. Later passes must not erase this failure
or be described as a root-cause repair without evidence.
The focused diagnostic run passes at `artifacts/smoke-ghost-byte-alpha-tooltip-diagnostic` without changing tooltip behavior. The full-run timeout's cause remains unestablished.

The subsequent diagnostic full run passes all 28 tooltip presentation gates
and all 1,028 catalog captures in 351.1146312 seconds at
`artifacts/smoke-ghost-byte-alpha-diagnostic-published-full`, binary source
`b28fac0c3bf72cbe8960c9550e7081795a2bd656`. This confirms that run only; the
prior BottomTip timeout and earlier capture abort still have no established
root cause. No tooltip behavior, retry, presentation deadline or stability
requirement was changed.

At source `ab2af53`, the full run
`artifacts/smoke-chart-readiness-validated-full` fails the original live-theme
visibility/geometry assertion on Light LongTip after 323 captures and
153.2707435 seconds. The catalog now records the before/after open state,
surface/text dimensions, font, inherited themes and focus for each live
theme check. The assertion and presentation gates are unchanged, and theme
restoration runs even if writing the diagnostic fails.

The focused diagnostic run `artifacts/smoke-tooltip-theme-diagnostic` passes
all twelve theme checks. The diagnostic full sequence
`artifacts/smoke-tooltip-theme-diagnostic-full` also passes all twelve,
including LongTip's exact 219 by 75 surface, but times out at the unchanged
360-second limit after 934 captures. These passes do not establish the cause
of the preceding LongTip failure or full-suite acceptance. Its diagnostic
summary is `artifacts/tooltip-theme-diagnostic-full-summary.json`.

On 2026-10-27, the source-built x64 catalog's Tooltip fixture was updated for
its current `BleeckerModalHost` layout. Live appearance now changes
`CatalogRoot`, which owns the trigger's inherited theme. The collision and
wrapped-button specimens mount in that full-size Grid. The content-update
check observes the visible consumer template instead of the hidden plain-text
slot, and collision/motion checks inspect the mounted presentation and its
viewport bounds rather than requiring a native `Popup`. The focused Tooltip
smoke passes both appearances, including live theme restoration, templated
content, collision placement, motion lifecycle and wrapped-button activation.
The combined presentation smoke advances to Labels and fails its live-theme
palette assertion. See
`docs/evidence/tooltip-catalog-modal-host-v1/checkpoint.json`. This is a catalog
regression correction; the library control and current package are unchanged.
