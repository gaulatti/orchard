# BrandLockup reference and acceptance

The frozen source is
[`brand-lockup.tsx`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/brand-lockup.tsx),
SHA-256 `8b2319f72c31ffb0c5e775484ed3b59d2ceeb59a4e73a779c805053585d601e6`.
Its downloaded source matches the inventory hash. The native implementation is
in progress; complete visual and accessibility acceptance remains open.

## Required native contract

- Preserve the consumer's exact href and custom link-rendering path. The
  renderer's item label is the supplied name when non-null, otherwise the
  logo's alternative text. The default link's accessible name derives from its
  image alternative text and visible name content. An empty name differs from
  a null name: it supplies the renderer's item label but does not
  render the name/separator group.
- Default alternative text is `gaulatti`. Use the pinned SVG from
  `src/assets/logo.ts`, with its 1000×1289.11 view box and six exact paths.
  A consumer-provided logo source remains authoritative.
- Small/large logo heights are 32/48; width preserves the source aspect ratio.
  Named lockups have a 16px gap around a 1px separator of the same height.
  Text uses Encode Sans Semibold at 20/30 with 28/36 line height and -0.02em
  tracking. Separator colors are sand at .55 opacity and dark white at .18.
- Logo opacity is .9 normally and 1 on link hover. Dark appearance inverts logo
  RGB, including custom sources, while preserving alpha. The frozen generated
  CSS resolves `duration-400` to **220ms**, rather than the class's numeric name.
- Preserve native link/command activation, keyboard focus and accessible names;
  supply Windows contrast adaptation without substituting brand colors.

## Native API and resources

`BleeckerBrandLockup` provides `Href` (required before mounting), `LogoAlt`
(default `gaulatti`), `LogoSource` (a native `ImageSource`), nullable `BrandName`,
`Size` (`Small`/`Large`), `Command`, `CommandParameter` and `Invoked`. `Href` stays
an opaque consumer-owned string; the control does not create a `NavigateUri` or
launch it. Command parameters retain object identity and invocation reports the
exact route. Native command availability and `IsEnabled` control activation.

```xml
<orchard:BleeckerBrandLockup Href="/studio?view=a%20b#anchor"
    BrandName="Fictional studio" Command="{Binding OpenStudio}" />
```

`LinkTemplate` receives a framework-created `BleeckerBrandLinkContext`, with
live `Href`, `Label`, prepared `Content`, `Command` and `CommandParameter`.
Bind these to a consumer-owned native action. Its accessible name and styling
belong to that template. The default HyperlinkButton combines logo alternative
text and the visible name, retains native focus/keyboard/UIA behavior, and uses
branded content without native underline or padding.

The packaged SVG matches frozen `src/assets/logo.svg`, SHA-256
`83163649337020d84c6819d4fcc554ed83e222dc5e06d9a071b951e8d4f1c713`.
Build and publish copy it to `Orchard/Assets/Images/gaulatti-logo.svg`, including
independent NuGet consumers. The Bleecker license covers this resource.
Setting `LogoSource` to null clears the logo; an unavailable custom source
raises `LogoFailed` and never substitutes the default asset. The catalog exposes
that error through its visible outcome text. Exact broken-image rendering
against the browser remains an acceptance gap.

The live native Image remains the decoder and source of pixels. Win2D 1.4.0 is a
pinned transitive dependency for the composition invert/alpha-mask effects,
verified to activate in an unpackaged x64 probe with Windows App SDK 2.5.1.
Effects retain custom RGB and alpha. In ordinary Dark, RGB is inverted; in
Windows contrast, opaque logo pixels use semantic WindowText while transparent
pixels remain transparent. Contrast forces full logo opacity. The private source
host is translated beyond the window while filtered output is displayed, keeping
the original image from painting beneath half-alpha filtered pixels. Source,
surface, brushes and factories follow the mounted control lifecycle.

The hover uses the source's 220ms duration and Tailwind's default
[`cubic-bezier(0.4, 0, 0.2, 1)`](https://github.com/tailwindlabs/tailwindcss/blob/v4.2.2/packages/tailwindcss/theme.css)
curve. Windows reduced motion uses immediate endpoints. The opacity clock now
uses CSS reversal shortening while preserving the existing premium defaults of
other controls. An isolated browser audit of this source property (Edge
155.0.4283.45) freezes entry at 80ms, exit at 40ms after reversal, and re-entry at
25ms after another reversal in `tests/Reference/BrandOpacityTransitions.json`.
The source package declares `^4.2.1`, but its frozen lock resolves Tailwind
4.2.2. The earlier version annotation was corrected; that locked version has
the same default easing and does not change the retained timeline samples.
It observes .953329 entry opacity, a 117.322837ms shortened exit and a
158.642297ms shortened re-entry. `BrandMotionTests` compares opacity and effect
durations with these browser results and reduced-motion settlement; the existing
Switch browser-timeline tests still pass. This is a property-boundary regression,
not a full source-component raster comparison. The native catalog retains actual
rendering-frame opacity for physical enter/exit, requiring intermediate values
when Windows animations are enabled and immediate endpoints when disabled.
Exact composed motion timing and interrupted physical reversal acceptance remain
open.

## Verification and remaining acceptance

The implementation must verify logo-only/named, both sizes, empty/null labels,
custom images and link templates, runtime updates, long/non-Latin content,
constrained width, native pointer/keyboard activation, theme changes, reduced
motion, package resources and an independent consumer. Full rendered source
comparison, x64/ARM64, contrast, scaling and Narrator remain required before
acceptance. The registered Brand lockups page includes those primary variants,
custom RGBA imagery, a typed consumer link template, disabled/command states,
long Japanese/Arabic text, a constrained name and a failed source. The focused
`scripts/smoke-catalog.ps1 -Component brand` path and the full catalog exercise
the same fixture. Initial x64 evidence at `artifacts/smoke-brand-preview-2`
passes both themes' registered geometry, typography, palette, composed RGBA,
native UIA invocation, pointer cancellation/click, physical Tab/Enter and live
renderer data checks. It reports animations disabled and display/text scale 1.
The expanded fixture passes both themes at `artifacts/smoke-brand-reconciled`,
including live Dark/Light reversal, RTL with unmirrored RGBA artwork, source
clearing and detached retemplate/remount. Pixel observations compare the
composed window, not RenderTargetBitmap's omission of child visuals. Sampling
uses the visible logo host; the private source image is translated out of the
window while filtered. Each RGB channel allows three integer steps for native
image interpolation and alpha rounding; incorrect inversion or background
compositing still fails. A live-theme failure first showed the helper retaining
Dark while its owner reported Light. The mounted control now reconciles the
authoritative ActualTheme at presentation, updating the effects only on a
change and removing the observer on unload. This establishes registered
endpoints/lifecycle, not exact browser/native raster or timing acceptance.
The complete local x64 catalog also passes both appearances at
`artifacts/smoke-brand-all`, including existing overlay, Input, Checkbox and
keyboard checks. All 26 unit/coverage tests and generated-token checks pass.
The x64 and ARM64 builds have zero warnings. A fresh isolated x64 consumer at
`artifacts/package-consumer-brand-preview` passes both appearances, requiring
the packaged SVG, its native aspect ratio, Encode Sans Semibold, Hyperlink UIA
semantics and exactly-once invocation of the unmodified consumer route. It uses
the 0.1.0 package with SHA-256
`bf2ec1c5b8a6bb793570dfe597f461f7fcf19ade4956e8773def11a49080b571`.
No source project reference or sibling checkout satisfies that check.

The Windows contrast fixture now also includes a custom RGBA logo and requires
real composed opaque WindowText and transparent Window pixels after the actual
hosted mode change. The following hosted checkpoint verifies that boundary.

Run `38458612519` at `883b0c9` passes the complete native ARM64 catalog, live
contrast transition and independent consumer. Both appearances pass the
registered BrandLockup geometry, RGBA, native action, live theme, RTL and
lifecycle checks. The actual contrast capture samples exact opaque WindowText
`#FFFFFFFF` and transparent Window `#FF202020` pixels. Runtime archive
`11844828753` was downloaded and verified against SHA-256
`cd8ab5b527f214e76e532dd8e6b620b8d9999b4ddc4a4424c069e209613acf49`;
selected reports and captures are under `artifacts/ci-883b0c9-arm64-evidence`.
The ARM64 independent consumer package SHA-256 is
`5a9ca18b1d3fc2b7ef18633b5cd7eb8f90f8e26cfecf8d6cd3c2c428033eb7fc`.
The x64 job passes contrast and independent consumption but fails Input's
rounded-pointer cursor matrix. That failure prevents a green overall run and
does not establish full x64 catalog acceptance for this checkpoint. This hosted
evidence precedes the new opacity reversal clock; its fresh native acceptance
is still required.

The new clock passes the focused x64 fixture at `artifacts/smoke-brand-motion`
and complete catalog at `artifacts/smoke-brand-motion-all-diagnostics`, in both
appearances with Windows animations disabled and display/text scale 1. Its
physical enter/exit reports retain 10/11 frames in the focused Light/Dark runs,
with no intermediate opacity under reduced motion. All 27 unit tests pass,
including the frozen browser reversal regression and unchanged premium tests.
The first full run stopped at Input's reading-direction opaque-span assertion;
an isolated full Input run and the repeated complete catalog pass. The fixture
now retains the exact second raster and box/span diagnostics if it recurs.
This sequence does not establish a root cause or silently remove that gate.
Both architecture builds have zero warnings. The fresh isolated x64 package
consumer passes at `artifacts/package-consumer-brand-motion`, package SHA-256
`7326a3e5ab098fc2db3822c992923f0c83683fb1101e14c5dba349cc71eff5a0`.

The public wiki remote is unavailable; the contract is maintained here.

Run `38461347470` at `77eb85a` passes the x64 native catalog, live contrast
transition and independent package consumer after the opacity-clock change.
The ARM64 job passes its build, unit tests, live contrast and independent
consumer, but its ordinary catalog stops at Input's disabled rounded-pointer
cursor assertion (visible Arrow while the publisher retained UniversalNo).
That keeps the required CI run red. It does not negate the earlier Brand
checks, and does not establish a fix for the cursor boundary or complete
component acceptance.
