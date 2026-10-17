# Calendar and date-control source contract

`BleeckerCalendar`, `BleeckerDatePicker` and `BleeckerDateRangePicker` now have
native controls and a registered Dates catalog fixture. Their browser producer
contract remains the acceptance reference. Implementation and targeted runtime
evidence do not establish complete visual or Windows platform acceptance.

## Authoritative feature path

The reference is Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066`:

- `src/components/date-range-picker.tsx`, SHA-256
  `3757594c4ecd17bba365b4ce582e4c60d40d8972df2e2149931ac2b965dd48f9`.
- `src/components/date-picker.tsx`, SHA-256
  `479daea966f3e6432b2527be53ab87f2ec16cda55da7ed9945f6387338e8a6df`.
- Date Range Picker composes two Date Pickers and Button actions. Each Date
  Picker composes Popover and Calendar; Calendar uses IconButton navigation.
  Popover uses the actual Radix implementation and modal-branch hook.

The collector executes all seven imported Bleecker source files unchanged and
checks their bytes against the pinned Git objects. It also checks the original
package lock, global CSS and token CSS. Production dependencies come from that
lock: React 19.2.4 and Radix Popover 1.1.23. Separate locked tooling uses
esbuild 0.25.12 and Tailwind 4.2.2. Bundled Encode Sans regular, medium and
semibold replace the external font request.

## Measured state and interaction contract

`tests/Reference/DateControls.json` contains 188 endpoint frames: 132 Calendar,
28 Date Picker and 28 Date Range Picker frames, at 320/640px in Light/Dark.
Calendar frames include normal, hover, actual held press, keyboard focus,
empty selection, leap February, six-week August, bounds, a noon minimum,
disabled weekends, inverted bounds and RTL. Fully disabled months have only
the normal frame. Repeated computed paint objects are stored once under
`paints`; each measured element refers to its exact object with `paintRef`.
PNG hashes, layout values and all source/collector/compiler hashes remain in
the committed data. Raw images are in `artifacts/current/date-controls-browser`.

The harness uses HTML standards mode (`CSS1Compat`), a fresh component mount
per endpoint, explicit viewport scroll reset and device scale 1. It records
viewport offsets and range-card geometry. These details matter when the
source's 360px minimum range card exceeds a constrained viewport. Enabled
motion timelines, Windows scaling and forced-color/Narrator behavior remain
separate acceptance work. Endpoint frames finish animations under reduced
motion; their durations cannot establish normal-motion timing.

Forty-eight real browser input sequences verify twelve workflows at both
widths/themes. Every recorded pointer, click and key event is trusted:

- Selecting a day requests the date from the consumer. Without consumer
  acceptance the selected value remains unchanged. Calendar's viewed month
  is initialized on mount and survives later external value changes.
- December/January navigation crosses the year. Calendar is a sequence of
  buttons: Tab changes focus and Enter requests a day; ArrowRight alone does
  not move its focus in this source.
- Picker selection and Clear date close the popup and return focus to the
  trigger. Selecting without consumer acceptance also closes it while keeping
  the old value. Reopening remounts Calendar at the current consumer value.
- Opening focuses Previous month. Escape returns focus to the trigger. An
  outside click closes the surface, keeps focus on that outside button and
  makes no selection request. The reference records both Radix's outer dialog
  and the named inner dialog so the consumer's accessible label is retained.
- A disabled picker accepts no click and opens no popup. Range start/end
  selectors receive the opposite endpoint as their maximum/minimum; editing
  one retains the other, and Clear date range requests an empty object.
- A seven-day preset subtracts seven local calendar dates, producing October
  10 through October 17. Zero days requests the same day twice. Negative days
  preserve the resulting inverted range; the component adds no correction.
- Presets retain local calendar dates across both DST transitions. At
  `2026-03-09T04:30:00Z` in New York, source subtraction requests March 2
  through March 9. Subtracting seven 24-hour UTC durations would instead
  select March 1. The reference asserts this observable difference.

Calendar uses leading empty cells and variable row count, with no trailing
padding cells. At 280px width, October 2026 has 35 cells and measures 252px
high. February 2024 has 33 cells/29 dates and the same height. August 2020 has
37 cells and measures 286px. Days are 32px high with a 2px vertical gap; a held
press scales the day to 0.94. The header is 32px high followed by 16px space;
the weekday row is 32px high followed by 4px space. Selected days use the sea
or accent-blue palette and semibold 13px text; disabled days retain selected
state while applying opacity 0.3.

Range checks compare timestamps, not only date labels. A local October 17
noon minimum disables the October 17 midnight cell. A midnight October 20
maximum includes that cell. Contradictory bounds disable every day while
retaining the controlled selection and month navigation.

The default picker trigger is 200px minimum width and 40px high. Its surface
is 288px wide with a 254px inner Calendar after border and padding. A selected
date adds the separate Clear date action. Range uses local `yyyy-MM-dd`
strings; selection requests and preset results must preserve those dates
through public state, bindings and any consumer serialization.

## Reproduce and inspect

Use Node.js 24 or newer, npm, Git and Edge on Windows. Prepare one reusable
source/dependency cache, then run the collector from the repository root:

```powershell
./scripts/reference/prepare-date-controls.ps1
node scripts/reference/capture-date-controls.cjs `
  --source-root "$env:USERPROFILE/.cache/orchard/reference/source" `
  --tooling-root "$env:USERPROFILE/.cache/orchard/reference/tooling" `
  --playwright-module '<installed Playwright module directory>' `
  --output artifacts/current/date-controls-browser
```

The preparation script accepts `-NodeExecutable` and `-NpmCli` when using
bundled runtimes. The collector records the browser and Playwright versions;
this checkpoint uses Edge 155.0.4283.45 and Playwright 1.62.1. Its test clock
uses [Playwright's fixed-time API](https://playwright.dev/docs/clock), leaving
timers and animation frames running. It does not change the Windows clock.
Review successful output before replacing the committed reference JSON.
`DateControlsReferenceTests` checks provenance, geometry/paint, timestamp
bounds, dialog labels, trusted requests, focus and DST/preset behavior.

## Native consumer API

Calendar and Date Picker expose consumer-owned `DateTimeOffset? Value`,
`MinDate`, `MaxDate` and a `Func<DateTimeOffset, bool>? DisabledDates` predicate.
They raise `DateRequested`; the consumer decides whether to update `Value`.
The control never replaces an existing value binding to accept its own request.
For example:

```csharp
var picker = new BleeckerDatePicker { Placeholder = "Publication date" };
picker.DateRequested += (_, request) => picker.Value = request.Value;
```

`BleeckerCalendar.ViewMonth` reports the currently viewed month. Changing
`Value` retains that month; a genuinely unmounted calendar initializes again
when loaded. Date Picker exposes `IsDropDownOpen` and mounts a fresh calendar
for each opening. Its native trigger supplies Button and ExpandCollapse
automation, and each day supplies Toggle automation with its full date name.
Selection, clear and Escape return focus to the trigger; an outside click
keeps the outside target's focus.

Date Range Picker exposes a non-null `BleeckerDateRange Value`, containing
nullable `From`/`To` strings, and raises `RangeRequested`. Assign the requested
record in the consumer to accept it. Each endpoint uses the opposite endpoint
as its timestamp bound. `Presets` accepts `IEnumerable<BleeckerDatePreset>`;
observable collection changes are tracked. Presets with the same label retain
their button on live replacement and read the current record when invoked.

```csharp
var range = new BleeckerDateRangePicker {
    Value = new("2026-10-10", "2026-10-20"),
    Presets = new[] { new BleeckerDatePreset("Seven days", 7) }
};
range.RangeRequested += (_, request) => range.Value = request.Value;
```

Date labels and range serialization use local calendar dates, with English
display labels and `yyyy-MM-dd` request strings. Bounds compare timestamps,
including the time of day. Existing range strings remain consumer-owned:
their display follows the source numeric date constructor, including overflow
(`2026-02-30` displays March 2) and years 0–99 (year 99 displays 1999).
Empty strings display empty endpoints. Fractional preset days follow the
source's local `setDate` truncation, rather than subtracting UTC durations.
Malformed/nonfinite values and dates outside .NET's representable range fail
explicitly. No Windows clock or production configuration is changed for tests;
the catalog uses an internal, per-instance frozen clock.

The model/reference/resource suite currently contains 143 tests. The
`date-controls` native runner compares Calendar day geometry, selected/disabled
state and names against the frozen reference at both viewport widths, and
exercises consumer requests, focus, dismissal, remounting and live presets.
Its output is `artifacts/current/dates-native-{320|640}-current`. Run it with:

```powershell
./scripts/smoke-catalog.ps1 -Executable '<x64 publish>/Orchard.Catalog.exe' `
  -Output artifacts/current/dates-native-320-current `
  -Component date-controls -ExpectedArchitecture X64 -ClientSize 320,640
```

Use `-ClientSize 640,640` and the corresponding output directory for the other
width. A targeted native pass covers the reported checks only. The independent
package consumer also mounts these public controls and drives them through
native keyboard input; its reports are `consumer-dates-{Light|Dark}.json`.

Source `48cca09c146e7bf4159d650f142700edc7957a77` passes the focused native
audit: 132 Calendar frames, 4,060 day geometry/state/name comparisons, 28
interaction/lifecycle phases, 16 native pointer press/release pairs, 308 stable
pointer gates and 36 owned keyboard transfers. This covers Calendar geometry;
it does not claim the other 56 Picker/Range reference frames have matched.

The `0.1.0-48cca09` package passes the independent x64 consumer's complete
Light/Dark runtime. Its dates contribute eight owned native key pairs and six
consumer updates. The ARM64 consumer builds from that same package; both
consumers' library bytes match it, and both isolated temporary workspaces are
removed. Actual ARM64 execution remains unverified. Recheck the packaged dates
after running `scripts/verify-package.ps1` for both architectures:

```powershell
./scripts/verify-date-package.ps1 `
  -Package artifacts/current/packages/Gaulatti.Orchard.0.1.0-0758f03.nupkg `
  -X64 artifacts/current/consumer-dates-x64 `
  -Arm64 artifacts/current/consumer-dates-ARM64 `
  -Output artifacts/current/dates-package-audit.json
```

The same library source passes the complete x64 catalog: 152 phases and 1,150
captures in 469.1742111 seconds, preserving all prior capture names and 174
protected chart glyph hashes. Dates contributes two new captures and three
consumer updates per theme. The independent integration audit also checks the
actual native pointer press/release receipts for both popup selections.

## Picker and range presentation follow-up

The `date-presentation` native runner now covers the other 56 reference frames,
at 320/640px in Light/Dark. It mounts fresh native consumers, uses real mouse
input to open each popup and real Escape input to close it, and retains source
selection/bounds state. Its horizontal ScrollViewer models the browser host's
overflow and device-pixel scroll offsets; scroll extent comes from actual native
content. Comparisons use viewport coordinates, including the source's nonzero
scroll positions, with the existing 0.001px geometry tolerance unchanged.

The independent `scripts/verify-date-presentation.mjs` audit passes 964 box
comparisons, 620 popup day geometry/state/name checks, 132 basic paint checks,
80 owned pointer presentation gates and 20 owned Escape transfers. It compares
trigger caption text/font/line height, colors and alpha, border width/corners,
disabled opacity, range-card and popup paint. All 56 native captures are saved.
This supplements the original 132 Calendar frames; it is not a raster, shadow
or normal-motion acceptance claim.

Source `c687e27` and package `0.1.0-c687e27` carry these fixes. Both catalog
architectures publish. The independent x64 consumer passes its complete
Light/Dark runtime, including explicit checks for the corrected caption, clear
action and intrinsic range fields; ARM64 builds from the same package, with
both consumer libraries matching its bytes and both temporary workspaces removed.
The complete x64 catalog passes 152 phases and 1,150 captures in 467.1893479
seconds, preserving all previous capture names and 174 protected chart glyph
hashes. Native ARM64 execution remains unverified.

The fixes apply across normal and range triggers: intrinsic widths round up to
the source's 1/64px layout units, action/preset slots retain those widths,
two-column range tracks use cumulative layout-unit boundaries, and popup offsets
snap to device pixels. Captions use the source's 20px line height. The popup's
Clear date action now spans its complete 254px inner width. An added source
measurement proves its 31px border box, 296px parent and one-pixel trailing line
descent in all twelve selected popup frames. Every previous measurement and all
188 PNG hashes remain unchanged after adding this observation; the seven source
component files are unchanged. A source regression test preserves that boundary.

Run `-Component date-presentation -ClientSize 320,640` and `640,640` with the
same executable/output pattern shown above, then audit both outputs:

```powershell
node scripts/verify-date-presentation.mjs `
  --native-320 artifacts/current/dates-presentation-320 `
  --native-640 artifacts/current/dates-presentation-640 `
  --output artifacts/current/dates-presentation-audit.json
```

## Native acceptance before completion

### Shadow rendering and rectangular reference captures

Calendar selected days now use the source's `0 1px 2px rgba(21,48,66,.12)`
shadow. Standalone triggers use `0 1px 2px rgba(26,55,77,.025)` at rest and
`0 3px 10px -8px rgba(26,55,77,.25)` on hover. Range fields suppress only the
resting shadow; their inherited hover shadow remains. Open/focus rings are
separate visual layers. Popup surfaces retain the shared two-layer overlay
renderer. These endpoint changes do not establish enabled-motion timing.

The date renderer uses a Gaussian standard deviation of half the CSS blur,
pads the complete three-sigma kernel, and masks the complete rounded border
box after blurring and offsetting. This prevents outer shadows from tinting
translucent field interiors. Unload/template replacement releases the graph;
Windows contrast mode suppresses it. The catalog tests inspect the actual
compositor properties, source surfaces and masks, independently of recipe fields.
The frozen `.025` resting opacity is serialized by Edge as `.024`; the audit
checks that both represent the same 8-bit alpha while retaining the authored
native scalar value. The white caster is filtered and clipped before the
opaque colour and scalar opacity are applied, avoiding premultiplication of
the faint caster colour before filtering. Full rendered pixel comparisons
remain separate evidence. Popup shadows preserve CSS's front-to-back order,
which the live graph and independent consumer inspect before sorting reports.

Fixed-client runs now set `DWMWCP_DONOTROUND` on their own newly launched
fixture window on Windows 11, using the documented
[per-window corner preference](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/ui/apply-rounded-corners).
This leaves the entire rectangular browser viewport comparable: earlier
rounded native corners included changing desktop pixels. `capture-window.json`
records the observed native preference. Window discovery observes the same
process under the existing 360-second focused / 480-second full watchdog;
there is no separate short startup deadline. The interactive catalog's window
policy is unchanged, and Windows 10 predates the rounding API.

The source's long picker at 320px scrolls the document by 20px when opened.
The native reference scene now scrolls its viewport-wide body paint over a
white canvas, retaining the exposed source canvas strip. This corrects the
fixture background without changing the component or frozen source images.
The pixel audit compares every client pixel, checks complete square native
corners against their own adjacent background, and checks the current top
background row against the source. It applies no bitmap masks, resampling or
pixel tolerances. Source geometry, basic paint and typography keep their
existing independent checks and 0.001px numeric bound.

```powershell
node scripts/verify-date-shadows.mjs `
  --source artifacts/current/date-controls-browser `
  --before artifacts/current/dates-shadow-before-square `
  --after artifacts/current `
  --output artifacts/current/dates-shadow-audit.json
```

The before captures are recaptured from the verified `0.1.0-0758f03` catalog
archive with the same square-window preparation. The original rounded captures
and failed comparison remain available as diagnostic evidence. Real Windows
contrast transitions remain gated to isolated hosted runners by the existing
`NativeContrastSession`; no local guard or hosted-runner identity is bypassed.

### Text metrics and remaining acceptance

Typography measurements are now recorded separately in
`tests/Reference/DateTypography.json`: 6,076 text entries across the same 188
frames. The optional `--typography` collector mode combines live DOM text ranges
with the browser's alphabetic font bounding metrics, without changing the DOM.
It checks every screenshot and layout measurement before and after probing.
The original component bytes, existing frame measurements and PNG hashes remain
unchanged. The metric definitions follow the
[HTML TextMetrics contract](https://html.spec.whatwg.org/multipage/canvas.html#textmetrics).

The opt-in `BleeckerTextLayout.UseReferenceLineBox` attached property aligns
branded TextBlocks with those reference line metrics. Set a positive explicit
`LineHeight` before enabling it on single-line text under a Panel or Border.
Other element/parent types fail explicitly. It preserves the original text element and
bindings, uses the active font's measured ascent/descent, retains existing render
transforms, and restores the original parent/alignment when disabled. Date styles
enable it automatically; the Dates fixture explicitly exercises enable/disable.
Re-enabling uses an already available Panel/Border parent even during a temporary
unload. Elements without a parent wait for Loaded; stale Loaded events honor the
current attached value. The focused runner exercises the fixture's same restoration
check with a known original transform and alignments in both themes and widths.
Calendar days request tabular numerals and the heading uses the source's normal
letter spacing. Exact glyph raster, shadows and motion remain separate checks.

The focused x64 audit matches all 6,076 source text entries, including 4,680
tabular day entries, across all 188 frames. It checks actual transformed baselines,
display text, size, weight, explicit line height, tracking and numeral alignment
with the existing 0.001px numeric tolerance. Reproduce with
`node scripts/verify-date-typography.mjs --native-root artifacts/current --output artifacts/current/date-typography-audit.json`
after both native runners at both widths. This does not compare glyph ink or
horizontal subpixel raster placement.

Source `0758f03` passes the complete x64 catalog after the typography and
re-enable corrections: 152 phases, 1,150 captures and 469.5629669 seconds under
the unchanged 480-second watchdog. All prior capture names and 174 protected
chart glyph hashes remain. The focused Calendar runner now includes the
shared restoration fixture at both widths/themes, giving 312 pointer gates;
its original 4,060 day comparisons and 28 interaction/lifecycle phases remain.

Package `0.1.0-0758f03` passes the full independent x64 consumer runtime in both
themes, including the source's local 14px day, 15px caption and 12px preset
baselines, tabular day numerals, eight native date key pairs and six requests.
The ARM64 consumer builds from that package. Both consumers and the full x64
catalog use identical library bytes; temporary consumer workspaces are removed.
Both catalog architectures publish and their source-identified ZIPs are verified
file by file. Native ARM64 execution remains unverified.

Acceptance must carry these contracts through public C# models/properties,
consumer-owned value requests, native templates, layout, min/max and disabled
predicates, popup lifetime, keyboard/pointer focus, automation and package use.
Targeted runs cover consumer requests/binding, clear, rejected values, stale
actions, disabled state, template replacement, unmount/remount and four-row
months. Callback failures, remaining customization/data/live-theme boundaries,
additional date/content matrices and enabled-motion behavior remain open.

The Dates fixture and independent NuGet tests are registered. Acceptance still
requires exact glyph/raster/shadow and enabled/reduced-motion comparisons, the
remaining customization/lifecycle matrices, real scaling, Windows contrast,
Narrator and native ARM64 execution. The public wiki remains unavailable, so
this contract is maintained here.

The shadow checkpoint passes 144 regression tests and the four focused x64
runs at 320/640px in both themes. Its independent whole-client audit checks
4,680 days, 128 selected-day shadow graphs, 84 triggers, 36 trigger shadow
graphs, 40 popup layers and four stale-graph releases. Of 152 frames with
added shadows, 144 improve and eight retain the same full-frame source error;
none regress. All 188 source PNG hashes remain unchanged. The 150 shadow
frames with unchanged viewport backgrounds reduce absolute RGBA error from
76,359,806 to 75,731,841. The two corrected scrolling backdrops are reported
separately, so their larger improvement is not attributed to shadow rendering.
Masks, resampling and pixel tolerance remain zero. The independent geometry,
basic-paint and 6,076-entry typography audits also pass. These results establish
recipe/graph verification and measured raster improvement; exact raster,
enabled motion and the other acceptance requirements above remain open.
