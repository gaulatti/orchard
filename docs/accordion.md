# Accordion

The implementation target is the unmodified Bleecker `accordion.tsx` at commit
`a86059013cd59684ce452849278ea81e734f2066`. Its required behavior is one internally
expanded item, an initial expanded ID, independent header actions and rich
consumer-owned title, subtitle and content. Updating the initial ID after mount
does not change the active state. Removing an item does not invent a new active
ID; reinserting that ID restores its expansion. Empty and unknown IDs are explicit
states. Header buttons support pointer, Enter, Space and ordinary Tab navigation.

Before acceptance, source and native evidence must cover Light/Dark at 320, 639
and 640 pixels, the exact 640-pixel header-padding boundary, open/closed and rich
content, hover/focus, independent actions, live collection and metadata changes,
keyed identity, template/unload cleanup and stale automation peers. Native UIA
must expose expansion and useful names. Reduced motion, normal/reversed height
and chevron motion, text/display scaling, performance, x64 and native ARM64
runtime remain required. Existing accessibility metadata and contrast resources
remain intact; Narrator and OS contrast campaigns are excluded by the
[current library scope](library-scope.md). Any documented Windows adaptation
must preserve consumer content and commands.

`scripts/reference/capture-accordion.cjs` compiles the original component and CSS
with the pinned toolchain and records full-client PNGs, geometry, text and paint.
It does not patch the component or replace its state logic. The reference matrix
passes all 60 native cases and 108 rows with zero geometry error, exact text,
numeric typography, colors and the responsive padding boundary. Full-client
comparison retains zero masks, zero pixel tolerance and no resampling; exact
raster parity is not established. The source PNGs are committed under
`tests/Reference/Accordion` with individual hashes and browser font receipts.
The rich title uses the actual source-selected Encode Sans Semibold face rather
than synthesizing it from Medium. Styled text runs use the existing shaped-run
layout quantization. Other consumer UI elements retain their own content/layout.

`BleeckerAccordion.Items` is an observable collection of notifying
`BleeckerAccordionItem` objects. IDs are ordinal and unique; null/duplicate IDs
fail explicitly. Empty-string IDs remain valid. `Title`, `Subtitle`, `Content`
and `Actions` accept strings, scalar content and UI elements; application data
objects require the corresponding `TitleTemplate`, `SubtitleTemplate`,
`ContentTemplate` or `ActionsTemplate`. Templates retain ordinary WinUI binding,
commands and the consumer's object identities. `ExpandedId` reports current
internal state; `DefaultExpandedId` initializes it once when the control loads.

```csharp
var accordion = new BleeckerAccordion {
    DefaultExpandedId = "delivery",
    Items = new() {
        new("delivery", "Delivery schedule", new TextBox { Text = "Harbor" }) {
            Subtitle = "Consumer-owned input state",
            Actions = new Button { Content = "Edit", Command = editCommand }
        }
    }
};
```

Header actions remain outside the expansion button. Headers expose native
Invoke and ExpandCollapse patterns with names derived from rendered title and
subtitle content. Removed/template-replaced peers reject stale actions and
disabled groups reject expansion. Native Tab, Enter and Space follow ordinary
button navigation. Closing a section keeps consumer controls and values, hides
the content from keyboard/UIA navigation, and returns focus to the header when
needed. This is the Windows adaptation of the source's clipped content region.
The zero-height reveal container remains in layout. Padding follows the window
viewport, including when the control itself has a constrained width.

`scripts/smoke-accordion.ps1` exercises both real Windows animation preferences,
then captures the source matrix at 320/639/640 and restores the original setting.
`scripts/verify-accordion-runtime.mjs` checks the lifecycle/input/command/binding
receipts. `scripts/verify-accordion.mjs` compares actual layout, paint and full
client pixels to the frozen source. Both architecture jobs register these checks;
push-triggered Actions remain paused. The independent x64 package consumer passes
both themes, including native hold/release behavior, live metadata/templates,
content retention, stale/disabled guards and independent actions. The ARM64
consumer build passes. The current `0.1.0-48d8fd3` package additionally verifies
the scalar and overflow correction below; both catalog architecture builds pass
and temporary consumers are removed. Its full x64 catalog passes 156
phases and 1,154 captures in 471.9537397 seconds, retaining all prior captures and
174 protected chart glyph hashes. See the current checkpoint for hashes.
Native ARM64 execution and the remaining platform/motion/raster matrix must be
verified before acceptance.

The motion fixture captures 48 source timelines: six expansion changes, three
hover changes and three theme changes in each theme and motion preference.
Native rendering uses the source's 200ms `cubic-bezier(.16,1,.3,1)` expansion
and chevron clock, and its 220ms premium hover-color clock. Reveal heights use
Chromium's 1/64-pixel layout units. WinUI stores the chevron angle as a float;
the audit checks that exact float projection separately from the eased fraction.
Three successive reversals per theme retain the CSS shortened durations and
continuous values. The fixture records real presentation frames and actual
layout, clip, transform and brush properties without substituting a test clock.
Baseline readiness requires the delivered native hover state and completed
animation, then rechecks both after presentation. The first fresh replay's
readiness failure is retained at `artifacts/current/accordion-motion-first-replay`.

`smoke-accordion.ps1` also runs `smoke-accordion-motion.ps1` under both real
Windows preferences. `verify-accordion-motion.mjs` verifies all frozen browser
background samples, source endpoints/durations, intermediate native paints,
height/clip/angle trajectories, reversal clocks and preference restoration.
`test-accordion-motion-audit.mjs` rejects damaged and missing receipts. The
source's reduced CSS uses a 0.01ms transition; the frozen source tests include
its paused initial sample, while Windows reduced motion suppresses animation
and verifies the rendered final state. Source and native motion raster parity
remain unverified. The source's rendered title/arrow colors change immediately
on theme changes; its inherited header color transition does not animate those
explicitly styled children.

`AccordionContinuity.json` captures eight collection/content operations per theme
and motion preference. Moving the expanding first row to the end preserves its
keyed header but cancels its transition. Moving the last row to the beginning
moves its sibling instead and retains the active clock. Same-key replacement,
collection replacement, body growth and a changed default ID retain the clock;
removal/reinsertion and a new group reset it. Body growth changes the natural
height from 53px to 113px without changing the running fraction. These are
measurements of the original React component and browser, not a native-only
placement policy.

`smoke-accordion-continuity.ps1` records real native layout/clip/angle samples,
header identity, load/unload events, rendered content, accessible names and stale
peer rejection in both themes and real motion preferences. It is included in
`smoke-accordion.ps1`; both architecture jobs register the continuity verifier
and negative audit. Local continuity evidence covers x64. Native ARM64 execution,
motion/static raster and the remaining customization/platform acceptance stay
open. The diagnostic adds no library API or behavior change.

The scalar reference adds 292 cases: eighteen null/boolean/number/string values
in each of the four slots, both themes and 320/640px viewports, plus mixed-word
overflow specimens. Its 36 full-client
source PNGs are committed under `tests/Reference/AccordionScalars`, with hashes
and actual browser accessible names. Native text and geometry pass every case
with zero geometry error. Twenty-two damaged receipts/captures are rejected; exact
pixel parity remains unproven under zero masks/tolerance/resampling.

Numbers use the source's [decimal Number text](https://tc39.es/ecma262/multipage/ecmascript-data-types-and-values.html#sec-numeric-types-number-tostring):
negative zero displays as `0`, `1e-6` as `0.000001`, `1e20` as
`100000000000000000000` and `1e21` as `1e+21`. The shared text helper is extracted
from the existing DataTable filter conversion without changing that conversion.
Floats follow the existing Stat Card adaptation through their shortest decimal
representation; integer/decimal values retain invariant formatting, with zero
normalized to `0`. Consumer model objects and values are unchanged. Subtitle and
action truthiness remains independent of rendered scalar text, including numeric
zero/NaN versus the string `0`, and empty rendered content for booleans.

Plain slot text uses [whole-word wrapping](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.textwrapping?view=windows-app-sdk-2.0)
to match CSS when long action content leaves no text width in the header.
Unbreakable words may overflow their line; consumer-owned rich controls keep
their explicit wrapping settings. A bounded DirectWrite line plan preserves
breaks before arranging overflow at its native unclipped paint width; merely
widening a wrapping text block would combine short words. UIA names retain the
logical text. The mixed-word specimen checks three rendered lines, visible
last-word ink and native pointer activation beyond the header's logical bounds.
Full-client raster comparison still uses no masks, tolerance or resampling;
the additional ink check establishes presence, not pixel parity.
The native pre-fix capture is retained at
`artifacts/current/accordion-scalars-before`: 340 discrepancies included 156
text/name mismatches and a 360px header-height error. The committed-source capture
is `artifacts/current/capture-pool-runtime/scalars`. Both CI
architecture jobs register the scalar capture and audits. Fresh package delivery
passes at `48d8fd3`, together with full-suite replay and both catalog archive
verifications.

Run the reference collector from the repository root:

```powershell
node scripts/reference/capture-accordion.cjs --source-root <frozen-source-root> --tooling-root <locked-tooling-root> --playwright-module <playwright-module-directory> --output artifacts/current/accordion-source
node scripts/reference/capture-accordion-motion.cjs --source-root <frozen-source-root> --tooling-root <locked-tooling-root> --playwright-module <playwright-module-directory> --source-css artifacts/current/accordion-source --output artifacts/current/accordion-motion-source
node scripts/reference/capture-accordion-continuity.cjs --source-root <frozen-source-root> --tooling-root <locked-tooling-root> --playwright-module <playwright-module-directory> --source-css artifacts/current/accordion-source --output artifacts/current/accordion-continuity-source
node scripts/reference/capture-accordion-scalars.cjs --source-root <frozen-source-root> --tooling-root <locked-tooling-root> --playwright-module <playwright-module-directory> --source-css artifacts/current/accordion-source --output artifacts/current/accordion-scalars-source
```

The source directory is the pinned Bleecker archive with its `node_modules` and
a sibling `bleecker.git` bare repository. The collector checks its source/CSS/lock
bytes against that commit. Tooling pins are esbuild 0.25.12, Tailwind 4.2.2 and
React/React DOM 19.2.4, with Edge 155.0.4283.45. Review the resulting JSON and PNG
hashes together when updating the frozen reference.
