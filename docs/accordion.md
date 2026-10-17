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
and chevron motion, contrast, text/display scaling, Narrator, performance, x64
and native ARM64 runtime remain required. Any documented Windows adaptation must
preserve consumer content and commands.

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
consumer build passes. Version `0.1.0-5a115e5` and both catalog architecture builds
are verified; temporary consumers are removed. The full x64 catalog passes 156
phases and 1,154 captures in 477.986485 seconds, retaining all prior captures and
174 protected chart glyph hashes. See the current checkpoint for hashes.
Native ARM64 execution and the remaining platform/motion/raster matrix must be
verified before acceptance.

Run the reference collector from the repository root:

```powershell
node scripts/reference/capture-accordion.cjs --source-root <frozen-source-root> --tooling-root <locked-tooling-root> --playwright-module <playwright-module-directory> --output artifacts/current/accordion-source
```

The source directory is the pinned Bleecker archive with its `node_modules` and
a sibling `bleecker.git` bare repository. The collector checks its source/CSS/lock
bytes against that commit. Tooling pins are esbuild 0.25.12, Tailwind 4.2.2 and
React/React DOM 19.2.4, with Edge 155.0.4283.45. Review the resulting JSON and PNG
hashes together when updating the frozen reference.
