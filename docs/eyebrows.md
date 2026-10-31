# Eyebrow section labels

`BleeckerEyebrow` is Orchard's small section
label. The pinned reference is commit
`a86059013cd59684ce452849278ea81e734f2066`, `src/components/eyebrow.tsx` and
`stories/components/Eyebrow.stories.tsx`.

For the 2026-10-29 x64 milestone, Eyebrow passes all five checks: applicable
Light/Dark states, passive keyboard/pointer/focus behavior, native 150% no-clipping
for wrapped plain and rich content, static reduced-motion behavior, and independent
package-consumer compilation. The focused catalog fixture runs against the exact
`.74` package DLL and extracted catalog. ARM64 is build-only and runtime-unverified.

```xml
<orchard:BleeckerEyebrow Content="Private client services" />
<orchard:BleeckerEyebrow Content="The autumn edit" Tone="Muted" Rule="True" />
<orchard:BleeckerEyebrow Content="Residence secure" Tone="Inverse" />
```

Accent is the default tone. Muted uses text-secondary; Inverse uses white at
75% opacity. Light and dark palettes use the canonical accent/text colors, and
Windows contrast mode uses the system window text color. Tone changes preserve
explicit consumer foreground overrides and do not mutate other instances.

Plain string content is displayed in uppercase while the original `Content`
value remains consumer-owned. Unicode's unconditional special casing supplements
.NET's simple invariant mappings, so sharp-s, ligatures and Greek combinations
can expand into multiple characters. Plain display casing follows the control's
`Language`, including live language changes; for example, `istanbul` displays as
`İSTANBUL` under `tr-TR` and `ISTANBUL` under `en-US`. Its accessible name remains
the caller's original content. The template uses bundled Encode Sans semibold,
11px font size, 600 weight, a 16px line box and 0.12em tracking. `Rule` adds a
28×1px decorative line, a 12px content gap and 60% rule opacity. The line follows
the current foreground and is excluded from accessibility. The control is not
a tab stop and exposes text semantics. It adds no disabled dimming or motion.

The **Eyebrows** catalog fixture includes the three pinned story examples,
empty/long/non-Latin/disabled content, a constrained wrapped label, a
right-to-left rule with Arabic and accented text, and rich content. Its runtime
check exercises all six tone/rule combinations in each
theme, consumer content retention, live updates, current-color rule geometry,
instance isolation and foreground overrides. An independent NuGet consumer
also instantiates the control. Build or source assertions alone do not establish
visual acceptance.

The embedded source is [Unicode 17.0.0 SpecialCasing](https://www.unicode.org/Public/17.0.0/ucd/SpecialCasing.txt),
SHA-256 `efc25faf19de21b92c1194c111c932e03d2a5eaf18194e33f1156e96de4c9588`.
Its Unicode license is included in the NuGet package. ASCII labels avoid loading
the table; non-ASCII mappings load once. Unit and native display regressions cover
expansions, accents, uncased scripts and emoji. The independent package consumer
exercises sharp-s expansion through the embedded resource.

The registered fixture and independent consumer, including the embedded casing
table, pass on native ARM64 as well as x64. See
[runtime evidence](runtime-evidence.md) for architecture reports and scope.
Those earlier ARM64 results predate the live `Language` repair. The current hidden
x64 source run at `artifacts/current/eyebrow-language-source-v1` verifies four
Turkish/English display changes across both themes, preserving the caller's
`Content` and accessible name. The shared host also passes its existing 352 name
and 20 help-text cases with 349 hidden-window guards, zero focus or activation,
and no hardware input. Ten focused casing tests and the current x64/ARM64 catalog
builds pass without warnings. The fresh `0.1.0-title-language-v1-local` package
also builds in isolated x64/ARM64 consumers; the x64 consumer passes the same
hidden casing and metadata checks. Native ARM64 execution of this repair remains open.

The rendered pinned Bleecker audit confirms the same five regression outputs:
`STRASSE`, `FFI · FFL`, Greek iota plus diaeresis/acute, Armenian `ԵՒ`, and
`CAFÉ · 東京 · 👩🏽‍💻`. Every browser row has a 16px line box. Measured code points
and the reference capture are retained at
`artifacts/eyebrow-uppercase-reference.json` and
`artifacts/eyebrow-uppercase-reference.png`. This verifies these text outputs;
native glyph and baseline parity still need the runtime comparison. A focused
hidden x64 catalog run at `artifacts/current/eyebrow-rtl-source-v2` measures the
rule's 28×1 line and 12px gap in both text directions and both themes. All four
cases pass with 393 hidden-window guards, zero focus/activation and no hardware
input. The report auditor checks each direction/theme case and the source binary
hash. The updated x64 and ARM64 catalogs build without warnings. This verifies
rule geometry at one layout width; it does not establish
full rendered RTL parity or native ARM64 behavior.

Consumer rich content/templates retain their text and must supply uppercase
content themselves. This differs from the reference's inherited CSS text transform
but does not prevent applicable states from rendering under the milestone's five
checks. Exact native/browser glyph
and tracking comparison, inline/baseline behavior, template and broader RTL edge cases,
broader text-scaling behavior and the remaining ARM64 state checks remain
unverified for the historical parity review; they are not this milestone's
acceptance gates. HTML
`div`/`p`/`span` selection has no direct native equivalent; composed layout and
semantics still require acceptance evidence. No desktop-wide settings should
be changed for scaling verification. Narrator and OS contrast campaigns are
excluded by the current [library scope](library-scope.md).

Caller automation-name overrides, including empty values and styles, follow
[the shared metadata contract](automation-metadata.md).
