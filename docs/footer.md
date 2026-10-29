# Footer source contract

For the 2026-10-29 x64 milestone, Footer passes all five checks in
[the acceptance contract](acceptance-contract.md). The current packaged DLL
renders 64 Light/Dark catalog states; its independent x64 NuGet consumer
compiles and runs. Physical pointer and Enter activation preserve the original
caller item, keyboard focus and caller input in both themes. Four native 150%
Light/Dark narrow/wide layouts keep visible Footer parts within the component
and viewport. Under the machine's disabled animation setting, pointer hover
changes ink immediately and remains stable without an OS setting change.
The 0.1.0-preview.20261029.22 package DLL and catalog DLL share SHA-256
`3eec9012cc91e1ac15394db3381fedda6b47da0c783fe3cb78692c085fb5daf2`.
ARM64 runtime remains unverified. Raster/pixel parity is outside this milestone;
older diagnostics below are historical rather than acceptance gates.

Footer's native implementation and catalog fixture are in progress. The reference freezes the original
component at Bleecker commit `a86059013cd59684ce452849278ea81e734f2066`,
`src/layout/footer.tsx`, SHA-256
`77756e4546713842cf511b7521fe1a5b61f39d378797d37a4b77511486f7d082`.
The reference contains 64 full client images and four source interaction
histories with 16 samples. Dependencies, styles, story, lockfile, fonts and
capture code are hashed. No Windows Footer implementation or runtime acceptance
is claimed by these browser facts. The native driver initially records geometry,
paint, original items and full client images; interaction/lifetime/package checks
remain separate required gates. Source images now use a 900-pixel client height
so the Windows driver can capture the same complete physical client. Independent
recaptures match exactly and all geometry is unchanged from the earlier height.

`BleeckerFooter` accepts an original `BleeckerFooterBrand`, a sequence of
`BleeckerFooterSection` objects, `BottomLeft`/`BottomRight` with templates and
selectors, `ShowBottomAccent`, an optional command/explicit parameter, and a
global link template/style. `ItemInvoked` reports the original section item or
the brand's constructed navigation item. Hosts own destination handling.
Brand and section models expose observable dependency properties; collection
notifications update presentation. Every link template receives the existing
`BleeckerNavLinkContext`, including prepared brand content and the source link's
special display copy while retaining its original item label and metadata.
The catalog covers normal, custom and no-section footers, original caller input,
section reordering, item changes, an explicit empty right slot and accent changes.

The guarded context now accepts an internal activation owner, preserving the
existing Nav Menu public API. Current working binaries therefore need a fresh
Nav Menu replay; the earlier verified package remains unchanged and historical.

The earlier 48-state prototype is **unaccepted**. Its x64 and ARM64 builds pass
with zero warnings or errors, and its 287 portable tests pass. The hidden source audit
passes 8,040 geometry checks with zero error and 1,434 actual brush checks over
three repetitions of all 48 source states. Seven lifetime/threading checks and
1,349 isolation guards pass, with zero activation/focus events and no input.
All 40 deliberately damaged reports are rejected. Exact tested DLLs are retained
with the report. See `artifacts/current/footer-hidden-diagnostic-v5/audit.json`.

Earlier visible sequences failed after one, 41 and 39 completed states; their
raw images and partial reports remain preserved. The underlying thread-affinity
failure is fixed and repeated hidden runs now complete, but fresh visible input
and raster verification still required renewed desktop availability. That phase stayed
in background processes. Broader property and customization matrices, native
motion/scaling and ARM64 runtime remain open.

The local `0.1.0-footer-v1-local` package builds fresh isolated x64 and ARM64
consumers with separate feeds/caches and no source project references. The x64
consumer passes all 144 layouts, 8,040 exact geometry checks, 1,434 brush checks,
seven lifetime/threading checks and 1,349 isolation guards without activation,
focus or input. All 44 damaged package reports are rejected. Temporary source
and restore caches are removed. DLL, four loose XAML files, compiled PRI and
README match the tested inputs. Package SHA-256:
`81b7e538f2515ec682928e7047e6770d3670761b9c488e62e64acace39c1aefd`.
This package remains an unaccepted local draft; no publication is claimed.

The later `0.1.0-text-media-v4-local` package and extracted x64 catalog pass the
complete pinned hidden Footer layout/paint audit on the same current Orchard
DLL. Each host repeats all 64 source states three times, including the existing
long and unbroken text cases, with 15,216 geometry checks at zero error, 1,890
paint checks at zero error, seven ownership checks and 1,877 isolation guards.
The independent package auditor also rejects 50 damaged reports. The
v4 checkpoint at `artifacts/current/text-media-v4-footer-layout-checkpoint-v1.json`
pins the package, report and audit hashes. This closes the current-package gap
for those pinned layout cases; broader custom rendering and fractional-width
cases, visible interaction, full client raster review, scaling and native ARM64
runtime remain open.

The existing BrandLockup now exposes internal preferred-width metrics for its
Footer host. Those metrics apply after its template exists and support the
source's shrink-to-fit brand width and inline baseline. Existing public brand
APIs remain unchanged. Further background diagnosis and regression coverage are
required before these prototype changes can be shipped.

Background diagnosis identified the earlier hard-crash path: a motion callback
read `IsLoaded` on its originating worker thread; the catalog error reporter then
also queried UI resources from that thread. Footer now enqueues motion work
before touching UI state. Footer/Nav Menu producer callbacks use their captured
UI queues, and error reporting avoids live resource enumeration. The hidden
sequence completes 144 layouts over three full reference cycles with zero
activation/focus events, plus real worker-thread command and collection checks.
The actual motion callback is also exercised from a worker through the test
harness without changing the OS preference. Template retirement and detached-command checks pass. This is separate
from still-required visible input, full raster and platform acceptance.

Footer's prepared brand text uses the existing shared text bridge so native
wrapping follows source line breaks. Its private host preserves intrinsic image
size while applying the source's inline preferred width, rather than fixing a
not-yet-decoded logo to zero width. Section links preserve unbreakable minimum
widths in constrained columns. The complete source-layout/paint audit remains
the gate for these changes; stable runtime alone does not prove parity.

The native component must satisfy these observable requirements:

- Preserve caller-owned brand information, section/title/item identities,
  arbitrary bottom content and custom link rendering. Section links receive
  the original item; host routing handles raw destinations. Reordering sections
  preserves caller input, selection and focus.
- Use viewport breakpoints: one column below 768 DIPs, two from 768 and four
  from 1024. A narrow host inside a wide viewport still uses the wide recipe.
  Brand spans two large-grid columns when there are at most two sections;
  additional sections create normal grid rows. Preserve the centered container,
  16-DIP horizontal and 64-DIP vertical padding, and 48-DIP grid gaps.
- Compose the large BrandLockup and optional description. Description uses the
  source's 448-DIP maximum width and 32-DIP top margin. Preserve the inline
  brand baseline's contribution to block height when integrating the existing
  native brand component.
- Section headings use 14/20 medium text, uppercase and .05em tracking, with
  32 DIPs below. Casing follows the footer's live `Language`. The original section title
  remains the model value and the heading's accessible name; `istanbul`
  displays as `İSTANBUL` under `tr-TR` and `ISTANBUL` under `en-US`.
  Links use 16/24 regular text, secondary ink and 16-DIP row
  gaps. These differ from the standalone vertical Nav Menu's 18/28 medium
  recipe; interaction behavior can be shared while keeping Footer typography.
- Bottom content follows a 64-DIP top gap, a one-DIP rule and 28-DIP top
  padding. Its two slots stack below 768 and form a spaced row from 768.
  Preserve arbitrary content/template ownership and normal native input.
- Only an omitted or null bottom-right value uses the source's default link.
  Empty text and `false` suppress it; numeric zero renders `0`. The default
  uses the first original item whose raw href contains the case-sensitive
  substring `github.com`, with displayed text `View source on GitHub` and
  the original destination/metadata/custom renderer. This is the source's
  explicit behavior; it does not authorize a service integration or URI launch.
- Preserve Light/Dark palette, fractional rule/accent alpha, optional two-DIP
  accent, native names/roles/focus and current motion preference. Further
  hover/focus/pressed and normal-motion references, real Windows interaction,
  native rendering, scaling, customization, both architecture builds and a
  fresh NuGet consumer are required before acceptance.

The native implementation has a registered catalog fixture using fictional
local data. Monitoring and Editorial demos remain required. This component
adds no product service, routing application or OS test campaign.
The public wiki is unavailable; documentation remains in this repository.

## Public slot and link contracts

The shared `tests/PackageConsumer/FooterContractFixture.cs` fixture checks twelve
public-API scenarios in an unactivated hidden window:

- Bound null/empty/false/zero right slots and their source-link transitions.
- A caller's observable model presented by a left DataTemplate, including
  TwoWay editing and bound model replacement.
- A right DataTemplateSelector receiving the original observable model and
  retaining its live bindings.
- Original native editor identity, text, selection and slot binding through
  section reordering, title changes and brand-description updates.
- Both original editors and their selection through complete template removal
  and reapplication.
- Both original editors through a constrained 640.5-DIP host and a wider
  768.5-DIP host, retaining row spacing and selection at fractional widths.
- Custom link rendering with the original item, style/font, explicit command
  parameter and original event payload; disabled commands remain inactive.
- A caller's padded link renderer retaining its own container width.
- Equal-key item replacement retaining its view while a changed key retires
  the previous action.
- Raw, case-sensitive, first-match source selection and explicit suppression.
- Bound brand metadata, explicit-null logo and original brand replacement.
- Custom link template retirement, unload and remount behavior.

Slot templates use the caller's ordinary observable data models. Pass native
UI elements directly as slot content when their own identity and editor state
must persist. A control used as template data has WinUI's usual DataContext
semantics; the fixture deliberately uses an observable model for template data.
The initial two model-template failures were fixture errors caused by using
TextBlock controls as data models; they do not establish library defects.

The original isolated x64 consumer passed its ten-scenario checkpoint with 179 isolation
guards, zero activation/focus events and no native input. All 28 damaged
receipts were rejected. The catalog passed the same ten scenarios and 179 guards;
its audit rejects 24 damaged receipts. Both catalog architecture builds and fresh
x64/ARM64 consumer compilations pass, and their temporary caches are removed.
Evidence is indexed in `artifacts/current/footer-contract-checkpoint.json`.
The library and current local package remain unchanged
at `0.1.0-producer-threading-v1-local`, SHA-256
`d9f8ea8283d8011a9196358cec1c33174dadad7f6d4f76ba8b07833bf539f38c`.
The same fixture is linked into the catalog and registered for both native CI
architectures; no workflow has been dispatched. Run it with:

```powershell
./scripts/verify-hidden-footer-contracts.ps1 -AppDirectory <catalog-output> -Output <fresh-output>
```

For a fresh `verify-package.ps1 -BuildOnly` consumer, supply its output directory
and `-HostKind PackageConsumer -Package <exact-nupkg>`. Use native ARM64 only on
an ARM64 host. These checks do not prove keyboard/pointer input, visible raster,
enabled-motion timing or full component acceptance. Long/empty/mutable layout
matrices, broader custom style/renderer cases, real scaling and integration
remain required alongside native ARM64 runtime.

## Expanded layout correction

The expanded source capture independently reproduces all 64 full client images.
Every original 48-state image and measurement is unchanged. New cases cover
wrapped and unbroken links, empty brand/section/item values, long descriptions
and bottom slots, six section columns across grid rows, narrow RTL/CJK text and
four live changes applied to the same source/native footer. The original
pre-expansion capture is retained under
`artifacts/current/footer-reference-before-expanded-v1`.

The stronger hidden audit also measures both bottom slots and every section and
heading. Its baseline completed 192 layouts but failed 318 geometry checks,
including heading/RTL discrepancies that the older audit did not measure. Link
layout now separates the complete line box from its inline interactive bounds.
The full line height is saved before the parent measure constrains DesiredSize;
empty links retain zero height. Constrained headings keep their CSS block width
without breaking an otherwise unbreakable word. Footer-only text metrics provide
directional max-content widths and wrapped inline extents. Bottom flex widths
use source layout-unit precision. Other shared-text callers retain the previous
default behavior.

The first corrected layout run matches 15,216 geometry checks with zero error,
1,890 brush checks, seven lifetime/threading checks and 1,877 isolation guards
across 192 layouts. There is no activation, focus or input. Its audit rejects 46
damaged receipts. Final package/renderer validation is recorded separately;
these facts do not establish visible rendering or component acceptance.

The catalog's item-change action now toggles a long fictional link label. A new
public contract regression compares a caller's padded renderer with an ordinary
link, ensuring Footer does not force the private text width onto arbitrary
caller containers. All prior ten contracts and this eleventh contract passed on
that x64 catalog build. Caller LinkStyle objects and explicit dimensions
remain caller-owned. Broader renderer/style, wrapping and motion/scaling cases,
visible pointer/keyboard/raster, final integration and native ARM64 remain open.

The final local expanded package is `0.1.0-footer-expanded-v1-local`, SHA-256
`d272cd8230e15c132550ab85ed7ff16c7da5d08553b0bd74c2ada46bd52748cc`.
Its library DLL is
`5a2a59b9d85ea61ae049ff5fc7070471b9784082af81caa06b5fb31bd2940835`.
Both final catalog and fresh isolated x64 consumer repeat all 64 states three
times: all 15,216 geometry checks match exactly, with 1,890 brush checks, seven
lifetime/threading checks and 1,877 isolation guards. Their auditors reject
46/50 damaged receipts. All eleven public contracts pass in both hosts, with
192 guards each and 24/28 damaged contract receipts rejected. Both architectures
build cleanly and fresh x64/ARM64 consumers compile with their temporary caches
removed. All 288 portable tests and nine windowless font tests pass. The fresh
x64 consumer also retains five threading scenarios/23 worker publications and
the Nav Menu's 42-state zero-error geometry/paint/metadata regression.

DLL, four loose XAML files, compiled PRI and README match the tested package
inputs. Versioned x64 and ARM64 catalog archives retain the build output, with
every archive entry compared to the source file. These are local drafts: normal
visible startup, full source raster/input and native ARM64 runtime remain
unverified. See `artifacts/current/footer-expanded-checkpoint.json`.

Caller automation-name overrides, including empty values and styles, follow
[the shared metadata contract](automation-metadata.md).

The current twelfth contract checks fractional host widths and caller-owned
bottom editors. In the hidden x64 source catalog and an independently restored
consumer of `0.1.0-text-media-v4-local`, all twelve checks pass with 205 isolation
guards per host, zero activation/focus events and no native input. Their auditors
reject 24 and 28 damaged receipts respectively. The narrow host uses the
wide-viewport row recipe; this does not test a viewport breakpoint or physical
input. The source catalog reuses its normal build directory, and the consumer's
isolated restore directory was removed after the build. The new test changes no
public library code. Evidence and hashes are in
`artifacts/current/footer-fractional-contract-checkpoint-v1.json`, SHA-256
`90535c0a86ff040aa7d9aabca16b1fb0a4b3c9a70c76da78a7c7acb212fc3d58`.
The [small receipt archive](evidence/footer-fractional-v1/README.md) preserves
byte-identical copies when generated build output is cleared.
The shared fixture also compiles in the current ARM64 catalog build, with zero
warnings or errors; native ARM64 execution remains unverified.
Broader custom content, full raster parity, motion/scaling and native ARM64
runtime remain open.

On 2026-10-27, the catalog's three existing Footer demos were retained behind a
variant selector. Mounting all three at once caused a native WinUI crash; Default
and Custom together, and Minimal alone, each mounted. The selector presents one
variant at a time and keeps the normal, custom-link/caller-input, and no-section
examples available. A fresh x64 Release catalog build passed with zero warnings
and errors. The foreground catalog run visited every variant in Light and Dark,
physically clicked the original Home link in Default, pressed Enter on the
custom-rendered Home link, verified host `ItemInvoked` received the original
item, preserved keyboard focus and caller input, and captured hover/focus PNGs.
The result was `passed: true` in both themes. The component's single-host
64-state reference capture path passed on the same build. This verifies the
catalog interaction path; it does not establish full source raster parity,
native ARM64 behavior, or final Footer acceptance. The initial dark
custom-variant capture exposed low-contrast brand text. The Footer brand link
now passes its resolved primary foreground to the nested BrandLockup, so the
brand name follows the active light or dark token even in a caller link
template. The corrected dark foreground capture is legible; the x64 catalog and
ARM64 library build with zero warnings and errors, seven focused Footer unit
tests pass, and the fresh 64-state source audit reports zero errors across
5,576 geometry, 630 paint and 1,760 metadata checks. None of the 64 full PNGs
match byte-for-byte, so complete raster parity remains open. The prior v11
preview package does not contain this repair; packaging and native ARM64 runtime
remain separate gates. See
`docs/evidence/footer-brand-token-foreground-v1/checkpoint.json` and
`docs/evidence/footer-foreground-variants-v1/checkpoint.json`.

The local `0.1.0-footer-brand-v1-local` NuGet package contains the exact
source-tested Footer DLL. Its package-reference consumer publishes as x64 with
zero warnings and errors, passes the same 12 hidden public contracts and 205
isolation guards, and passes its normal resource/theme/native-action runtime
check. The new contract assertion verifies that a caller-provided link style
also colors the nested brand. This is a local package probe, not a replacement
for the published v11 preview: the consumer used the repository's separate
package-reference project rather than a fresh isolated checkout/cache, and the
existing package auditor requires a cleanup receipt that this run did not
produce. Package-specific Footer physical input and native ARM64 runtime remain
open. See `docs/evidence/footer-brand-package-v1/checkpoint.json`.

The versioned `0.1.0-preview.20261027.2` NuGet package now publishes this exact
Footer DLL. Its new-version package-reference x64 consumer builds without
warnings or errors and passes the normal Light/Dark runtime check; the hidden
Footer probe passes 12 public contracts and 205 isolation guards. The package
payload is identical to the tested local package apart from version metadata.
The same release now includes refreshed x64 and ARM64 catalog ZIPs. The current
x64 build output contains the exact packaged Footer DLL and passes Monitoring
and Editorial demo input in Light and Dark. Each ZIP entry matches its build
output; extracted-ZIP execution, the isolated-consumer receipt, full visual
review, and native ARM64 runtime remain open. See
`docs/evidence/footer-brand-preview-v2/checkpoint.json` and
`docs/evidence/catalog-preview-v2/checkpoint.json`.

The published package's exact DLL now passes a focused x64 consumer foreground
check in Light and Dark. A real pointer click invokes the original section item;
physical Enter on a caller-rendered link invokes that same item and retains
keyboard focus. The caller's editor text remains intact, and the nested brand
uses the active theme foreground. The verifier matches the restored consumer
DLL to the package and records both process exits. This closes the focused
package-input gap, while broader visual, scaling, motion, isolated-consumer and
native ARM64 checks remain open. See
`docs/evidence/footer-package-physical-input-v1/checkpoint.json`.

The Nav Menu foreground source replay also passes on the same current Orchard
DLL used by Footer: 42 states, 888 exact geometry checks, 138 paint checks,
eight physical input histories and 26 lifecycle checks. This verifies the
shared navigation change did not break those observed routes. Its strict image
diagnostic still reports 88,562 differing pixels, and full Nav Menu acceptance
remains open. See `docs/evidence/nav-menu-current-after-footer-v1/checkpoint.json`.
