# BleeckerThemeToggle

`BleeckerThemeToggle` is a native icon button that requests a consumer-owned theme
mode. `Mode` defaults to `System`; activating it requests `Light`, then `Dark`,
then `System`. The consumer accepts a request by updating its model and choosing
the appropriate `RequestedTheme` on its own root. The component does not change
Windows preferences, an application root, storage or another window.

```xml
<orchard:BleeckerThemeToggle Mode="{Binding Appearance}"
    Size="Small" Variant="Ghost" ThemeRequested="RequestAppearance"/>
```

The handler may accept `args.Mode`, or leave the bound model unchanged to reject
the request. Inherited `Click`, `Command` and `CommandParameter` keep normal
native button semantics. Three sizes and three variants reuse `BleeckerIconButton`.
The icon represents the selected mode, while the accessible name and default
tooltip describe the next mode: “Switch to dark theme”, “Switch to system theme”
or “Switch to light theme”. Caller naming metadata, an explicit empty name, and
caller tooltips take precedence. A mode update preserves bindings and caller
content/templates. The original 18 DIP Sun, Moon and Monitor vectors use Lucide
0.563.0 with a 1.5-unit stroke; their paint comes from shared theme resources.

Caller overrides must also work through native `Style`/`BasedOn` setters. An
explicit empty name or tooltip keeps its meaning; WinUI represents an empty
styled tooltip as absent. Local null tooltips suppress the default. Local
bindings win over style setters. Clearing local metadata reveals the style, and removing a style
override restores the mode's default metadata without replacing the mode or
style binding. The default tooltip is a native style value resolved through
per-control resources, so sharing a style does not share another instance's
mode. These boundaries are covered by a hidden native diagnostic that
never shows/activates a window, focuses a control or sends input. That diagnostic
does not replace visible rendering and physical input checks.

To exercise the packaged contract in the background, use a fresh output folder:

```powershell
./scripts/verify-hidden-component-contracts.ps1 -Package <package.nupkg> -Platform x64 -Output <fresh-folder>
node scripts/verify-hidden-component-contracts.mjs --native <fresh-folder> --output <fresh-folder>/audit.json
node scripts/test-hidden-component-contracts-audit.mjs --native <fresh-folder> --output <fresh-regression-folder>
```

The diagnostic builds an independent consumer and uses a real, loaded XAML tree
in a window that stays hidden. It never constructs the visible consumer window,
activates/shows a window, calls Focus, invokes an enabled button, sends input or
changes OS preferences. The audit checks source normal-state geometry, icon
properties and metadata as well as caller overrides and lifecycle boundaries.
It explicitly leaves physical input, visible raster, motion and scaling open.

Acceptance requires all three mode transitions through physical pointer,
Space/Enter and native Invoke; disabled/detached rejection; accepted and rejected
requests; live bindings and metadata/tooltip updates; template/remount behavior;
all size/variant/theme states; original full-frame raster comparison; inherited
button motion, display scaling, package consumers and native ARM64 evidence.
No Narrator or OS preference changes are part of this work.

The Theme Toggles catalog fixture demonstrates local preview changes, a fixed
consumer mode, disabled state, variants, caller text in Japanese and style-owned
metadata. All 234 portable tests pass. The guarded x64 hidden diagnostic matches
116 source normal-state layouts with zero geometry error and passes 42 ownership
and lifecycle checks; 34 damaged receipts are rejected. It verifies source icon
sizes/paint properties, initial disabled opacity, style/local/empty/null
precedence, default restoration, shared-style isolation, retained bindings and
content, template replacement, remounts and disabled/detached rejection. A
WinUI `Setter` rejects a null constructor value; local null tooltips remain
supported. No enabled button is invoked by the hidden diagnostic.

The [source-built x64 checkpoint](evidence/theme-toggle-press-v1/checkpoint.json)
now passes the focused native catalog run in both themes. Physical pointer,
Space and Enter cycle the consumer-owned mode, and an Invoke request rejected
by the consumer leaves the binding and mode intact. Disabled/detached rejection,
metadata overrides, template replacement and remount checks pass. Across 134
reference states, the native layout and icon geometry match within 0.000004 DIP,
including pressed scale/translation. The full raster still differs in every
frame; native normal animation, actual scaling and ARM64 runtime remain
unverified. The v8 artifacts predate this change. The [v9 local preview](releases/2026-10-27-components-v9-unqualified.md)
contains it: its extracted x64 catalog repeats the physical and 134-state
geometry checks, and an independent v9 NuGet consumer runs on x64. The
component remains unaccepted.

The later [published v5 preview](evidence/theme-toggle-v5-published-foreground-v1/checkpoint.json)
has the same Theme Toggle library bytes in its NuGet package and extracted x64
catalog. The catalog passed 134 source-state geometry checks and pointer,
Space and Enter mode requests in both themes. Its exact raster diagnostic still
differs, and normal animation, actual scaling, independent-consumer physical
input and native ARM64 runtime remain open.

The [published v5 independent consumer](evidence/theme-toggle-package-consumer-input-v1/checkpoint.json)
now shows the request-only rejection path without changing the application
theme. Physical pointer, Space and Enter each deliver a Light request; the
consumer displays it while retaining System mode and the same next-mode name.
The existing package Button still works afterward, and the full x64 consumer
suite passes in both themes. Accepted theme transitions, the remaining
consumer focus/tooltip matrix and native ARM64 runtime are still open.
