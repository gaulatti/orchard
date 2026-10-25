# Popover source contract and native implementation

`BleeckerPopover` and its catalog fixture are implemented locally and undergoing
native validation. The frozen source, production state and placement model,
and prior native primitive probe establish their contract. Implementation alone
does not accept the component or prove a current package. See [its API](popover.md).

The reference is Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066`, `src/components/popover.tsx`,
SHA-256 `045511fc776cb635818eb786f8c7c722de2f167bc1934493617c1ed5ae222c6d`.
`tests/Reference/Popover.json` records 122 light/dark source states, every full
client PNG and 16 input histories. Independent captures match every receipt and
PNG byte. It binds the component, story, hooks, CSS, tokens, lockfile, fonts,
capture code and all bundled dependency files. Module provenance follows the
actual bundle, including nested Radix Popper 1.3.7 and Focus Scope 1.1.16 under
Popover 1.1.23; unrelated root versions are not used as their provenance.

## Observable acceptance contract

- Original trigger actions run before the popover toggle. Disabled triggers and
  prevented trigger clicks emit no open request. Programmatic open state can
  still display content when the original trigger is disabled.
- An optional controlled open value is authoritative. A request alone never
  assigns it. Internal history starts from `defaultOpen` once; later default
  changes do not reset it. Releasing control restores that separate history.
- Side defaults to bottom, alignment to center and offset to four. Top, bottom,
  left and right combine with start/center/end, logical RTL alignment, fractional
  and negative offsets. The source shifts with 12-pixel collision padding,
  preserves anchor contact, flips sides/alignment and selects the best fit.
  Oversized content keeps its intrinsic size. Final coordinates round to device
  pixels. `BleeckerPopoverModel` matches all 120 open source rectangles and their
  actual side/alignment, including content and viewports that cannot fully fit.
- The surface uses a 224 minimum width, eight-pixel padding, one-pixel border,
  seven-pixel shared UI radius, source overlay shadows and Encode Sans 16/24.
  Light is white with sand at 35%; dark is deep sea with white at 15%.
- Opening focuses the first focusable content, or the content surface when it
  has none. Tab and Shift+Tab loop inside the open surface. Escape restores the
  trigger; outside pointer/focus dismissal preserves outside focus. Live content,
  side and alignment changes preserve the same source editor, text and selection.
- The portal belongs to the nearest modal branch. Native implementation must
  cover nested popup ownership, close ordering and parent-modal integration.
  Original native controls, commands, bindings, templates and metadata remain
  caller-owned. Replacement, unload and template retirement must disconnect old
  producers and prevent stale requests.

The source declares 220/150-ms motion tokens and premium easing. In its frozen
normal-motion stylesheet, computed `animation-name` is `none` and the popup has
zero active animations. The tokens alone do not justify adding an animation.
Native motion acceptance must follow the effective source styles and preserve
the current reduced-motion preference without changing OS settings.

## Verification and remaining work

Five portable tests bind the reference images and exact inputs, replay controlled
and internal state histories, compare the production placement model, and check
source focus/dismissal, paint tokens, oversized content and motion definitions.
The native primitive probe opens a real root-constrained WinUI `Popup` under an
unshown window. It verifies the anchor-relative rectangle `(248,276,224,78)` and
actual child pixels, enumerates its process's native windows, and records zero
visibility, activation, focus or input. This proves the primitive's positioning
path; it does not exercise a finished `BleeckerPopover`.

The implementation and fresh independent x64 package consumer both pass all 122
native source layouts with zero geometry error, 360 brush comparisons and five
ownership/lifecycle groups. The prototype, shared regressions and x64/ARM64 builds
are recorded in [the implementation checkpoint](popover.md#verified-prototype-checkpoint).
Native input, visible startup,
scaling, performance, complete raster and ARM64 runtime acceptance remain open.
Desktop work stays in the background until availability is extended. Both
fictional-data Monitoring and Editorial demos remain required and preserved.
