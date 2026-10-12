# Eyebrow section labels

`BleeckerEyebrow` is an in-progress implementation of Bleecker's small section
label. The pinned reference is commit
`a86059013cd59684ce452849278ea81e734f2066`, `src/components/eyebrow.tsx` and
`stories/components/Eyebrow.stories.tsx`.

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
can expand into multiple characters. The template uses bundled Encode Sans semibold,
11px font size, 600 weight, a 16px line box and 0.12em tracking. `Rule` adds a
28×1px decorative line, a 12px content gap and 60% rule opacity. The line follows
the current foreground and is excluded from accessibility. The control is not
a tab stop and exposes text semantics. It adds no disabled dimming or motion.

The **Eyebrows** catalog fixture includes the three pinned story examples,
empty/long/non-Latin/disabled content, a constrained wrapped label and rich
content. Its runtime check exercises all six tone/rule combinations in each
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

The rendered pinned Bleecker audit confirms the same five regression outputs:
`STRASSE`, `FFI · FFL`, Greek iota plus diaeresis/acute, Armenian `ԵՒ`, and
`CAFÉ · 東京 · 👩🏽‍💻`. Every browser row has a 16px line box. Measured code points
and the reference capture are retained at
`artifacts/eyebrow-uppercase-reference.json` and
`artifacts/eyebrow-uppercase-reference.png`. This verifies these text outputs;
native glyph and baseline parity still need the runtime comparison.

Acceptance remains open. Context-sensitive and language-specific CSS casing
still require reconciliation. Consumer rich content/templates retain their
text and must currently supply uppercase content themselves. This does not yet
match the reference's inherited CSS text transform. Exact native/browser glyph
and tracking comparison, inline/baseline behavior, template/RTL edge cases,
contrast, Narrator, isolated scaling and ARM64 runtime remain unverified. HTML
`div`/`p`/`span` selection has no direct native equivalent; composed layout and
semantics still require acceptance evidence. No desktop-wide settings should
be changed for scaling verification.
