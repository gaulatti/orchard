# Nav Menu

The `.56` x64 preview accepts Nav Menu under the five-check milestone. The
extracted catalog passed 42 Light/Dark states and eight physical pointer,
keyboard and focus histories. The independent package consumer compiled and
ran 16 native 150% layouts across themes, directions, widths and RTL/LTR
without clipped links. Theme repaint settled immediately with animations
disabled. ARM64 built and its consumer compiled; ARM64 runtime is unverified.
Older raster and normal-motion diagnostics below are not milestone gates.

This in-progress inventory entry follows Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066`,
`src/components/nav-menu.tsx` (SHA-256
`1a9b3d9c7c7d43c52d23080c37af477211d37ed457f1c6a8eb2a18269b2f8cc5`).
It presents caller-owned navigation items and delegates destination handling to
the host, following Orchard's existing link conventions. It adds no router,
network request, application service or product workflow.

Acceptance requires these observable behaviors:

- Horizontal links use the source's typography, 32-DIP inter-item spacing,
  flex shrink and vertical centering. Vertical links use the larger source
  typography, 16-DIP spacing and full-width link hit areas. Empty menus retain
  their zero-height layout. Long labels, overflow, RTL and constrained widths
  follow the pinned source and normal Windows text-scaling behavior.
- The original item, label, destination and external flag reach the default
  link, custom link context and activation request. The caller retains its item
  objects, collection, binding and application navigation state. Pointer and
  keyboard activation emit the intended callback once; disabled, detached,
  removed and obsolete template actions cannot emit it.
- A custom link template receives the original item, prepared source content,
  default style and guarded activation command. Reordering stable source keys
  retains its native child state and focus. Mutable metadata and collection
  notifications update the visible links, with subscriptions detached on unload.
- Light/Dark normal, hover, pressed and focused presentations use the existing
  tokens and source transition semantics. Reduced motion follows the current
  preference. Native link roles, names and keyboard focus remain available.
- A registered catalog fixture, source-bound regressions, both architecture
  builds, real native input and an independent NuGet consumer exercise the
  public control. Full raw client images, geometry and actual paint evidence
  remain separate from complete component acceptance.

`BleeckerNavMenu` accepts caller-owned `BleeckerNavItem` sequences, `Direction`,
`LinkStyle`, `LinkTemplate`, an optional command and its explicit parameter.
`ItemInvoked` carries the original item; the host handles its unchanged `Href`
and `External` values. `BleeckerNavLinkContext` supplies that item, prepared
content, source style, live foreground and a guarded activation command to
custom templates. The catalog includes horizontal, vertical, constrained, RTL,
markup-created items and custom links with retained text input.

The source reference has 42 full PNGs, eight mouse/keyboard histories with 24
samples, min/max-content measurements and two real paused color transitions.
All images remain byte-identical across the expanded measurement capture.
The source's global link rules make its effective transition 220 ms with the
standard timing curve, overriding the declared 400 ms utility. The production
flex and color models match the source widths and 110 ms transition samples;
the portable suite passes 285 tests. The x64 native replay passes all 42 layouts
with 888 exact geometry checks, 138 actual paint checks, eight native input
histories (24 samples) and 26 ownership/lifetime checks. These include retained
custom text, selection and focus after reordering; keyed original-item updates;
live brush alpha; disabled/hidden/retired commands; template replacement; and
unload. Forty deliberately damaged native reports are rejected. Four of the
42 raw client images are exact; 88,562 pixels differ across the full matrix.
The images are unmodified and use no masks or pixel tolerance.

A fresh foreground replay on the current Footer-shared Orchard DLL repeats the
42-state x64 result: all 888 geometry, 138 paint, eight physical input histories
and 26 lifecycle checks pass. The strict image diagnostic still has four exact
PNGs and 88,562 differing pixels, so complete visual acceptance remains open.
See `docs/evidence/nav-menu-current-after-footer-v1/checkpoint.json`.

Shared intrinsic measurement preserves connected cursive scripts when applying
CSS letter spacing. Latin spacing still applies; Arabic connections and spacing
on whitespace match CSS Text and the native XAML renderer. All nine Windows
text tests pass, including the seven Nav Menu Latin, Japanese and Arabic labels.
This follows [CSS Text's cursive-spacing rule](https://www.w3.org/TR/css-text-3/#letter-spacing-property)
and the pinned Win2D 1.4.0 script analysis API.

The final `0.1.0-nav-menu-v2-local` package builds fresh isolated x64 and ARM64
consumers. Its x64 hidden runtime passes 42 layouts, 888 exact geometry checks,
78 normal-state brush checks, 16 ownership checks and 336 isolation guards, with
zero activation/focus events. Pointer/focus states are exercised by the separate
foreground run; the hidden run claims their geometry only. All 42 damaged
hidden reports are rejected. Both consumer caches are removed. Package DLL,
four loose XAML files, compiled PRI and README match the tested source/build.
Both catalog architectures build without warnings or errors.

The `.56` release and inventory record the current five x64 checks. The public
wiki is unavailable; documentation remains in this repository.

Collection and command notifications may be raised from a background thread;
the control queues them before accessing UI state. Producers must synchronize
concurrent collection access, and native dependency objects retain their normal
UI-thread construction/mutation requirements. A hidden regression raises real
collection and `CanExecuteChanged` events on a worker and verifies their effect
on the public menu without activating or focusing its window.
