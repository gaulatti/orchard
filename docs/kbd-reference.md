# Keyboard shortcut reference contract

Pinned Bleecker commit: `a86059013cd59684ce452849278ea81e734f2066`.
Sources: `src/components/kbd.tsx`, `stories/components/Kbd.stories.tsx`
and the pinned build's `dist/styles.css`. A Windows preview is implemented;
complete rendered comparison and acceptance remain pending.

## Content and behavior

Without a nonempty `keys` array, Kbd renders its consumer's children inside one
keycap. An absent or empty array uses this same content path. With one or more
keys, it renders a group containing a keycap for every string, with a literal
plus sign between adjacent keycaps. Keys take precedence over children. The
group's accessible name joins keys with `+`; even a one-key array uses a group.
The display does not register shortcuts, invoke actions, or accept keyboard focus.
Do not replace the supplied command symbol with Ctrl or otherwise localize key
names on the consumer's behalf.

The Windows control must support consumer content, replaceable keys and observable
collection updates. Clearing the collection must restore the content path.
Replacement and unload must detach collection subscriptions. Explicit consumer
accessibility names must remain authoritative. Native Text and Group roles should
represent the two paths; decorative separators must not become separate tab stops.

## Visual contract

| Part | Pinned contract |
| --- | --- |
| Keycap | 20px height, minimum width 20px, centered content |
| Insets and shape | 6px horizontal padding, 1px border, 4px radius |
| Typography | 11px monospace, weight 500 |
| Light appearance | light-sand at 50% background, black at 8% border and shadow, text-secondary foreground |
| Dark appearance | deep-sea at 50% background, white at 10% border and shadow, text-primary foreground |
| Shadow | x=0, y=1px, blur=0, spread=0 |
| Combination | horizontal inline layout, vertically centered, 2px gap between every child |
| Separator | literal `+`, 12px text, text-secondary foreground |

The compiled mono stack is `ui-monospace, SFMono-Regular, Menlo, Monaco,
Consolas, "Liberation Mono", "Courier New", monospace`. The actual Windows
browser font and glyph metrics must be measured before accepting a native font
choice. The separator inherits the surrounding font family; it is not assigned
the keycap's mono family. There is no component-defined animation or disabled
opacity. Windows contrast mode must use the required system colors.

The local browser audit renders identical content with the unchanged reference
stack and explicit Consolas. On this Windows Chrome host both rows have identical
measured widths: K/P 20.0625px, Esc 32.15625px, command symbol 26.0625px, Ctrl
38.203125px and Shift 44.25px; every cap is 20px high. The retained evidence is
`artifacts/kbd-font-browser.json` and `artifacts/kbd-font-browser-dark.png`.
This supports Consolas for those glyphs, but does not identify all fallback fonts
or establish exact native raster parity. The screenshot transport produced JPEG
bytes; pixel differences between translated row crops are not an exact glyph
acceptance check.

## Acceptance evidence to collect

- Both content paths, empty keys, a one-key array, two/three-key combinations,
  repeated keys, empty strings, Unicode symbols and long content.
- The pinned stories: single K; command-symbol plus K; Save, Command palette,
  Undo, Select all and Copy combinations; inline text with Esc.
- Light/dark rendered reference comparisons, live theme and content changes,
  exact keycap and separator spacing, baseline alignment and shadow corners.
- Normal consumer overrides, rich content/templates, collection replacement,
  collection mutation, unload/reload and retained consumer ownership.
- Accessible names/roles, absence of unwanted focus stops, contrast, constrained
  widths, RTL, isolated text/display scaling, and x64/ARM64 runtime.
- Registered catalog fixtures and independent NuGet consumer verification.

Scaling checks must use an isolated environment; do not change desktop-wide
settings on the developer's workstation. Successful builds or source-value
assertions alone do not establish visual parity.
