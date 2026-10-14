# Windows contrast verification

Run `38452957927` at `46a7ae2` passes the stable-recovery contrast check and
independent package consumption on both native architectures. Its ordinary
catalogs fail at separate cursor/input boundaries; the entire run is not green.
The x64 archive `11842080658` was verified against SHA-256
`680c037e52d7218b28a1ed749a2e31a6d7a6f93e6591284cc221d48f31af335b`.

The new BrandLockup specimen uses an actual four-pixel RGBA source. Its hosted
check requires the opaque area to paint Windows WindowText and the transparent
area to retain Window, sampled from the composed native window after the real
mode switch. Reports are `contrast-brand-pixels.json` and
`contrast-brand-window.png`. That extension is pending hosted verification;
the original text, action, input, popup, keyboard and restoration gates remain.

Run `38451360536` at `98e44ca` passes the complete x64 catalog and independent
consumer, but its contrast check observes a transient zero foreground while
Windows replaces its `sethc` cover. The retained events show the original cover
at 428ms and the catalog regaining focus at 932ms; recovery was attempted before
the native transition settled and its unchanged ownership guard refused.
There is no foreground-recovery request in this failure's archive. The x64
archive `11841860615` has verified SHA-256
`38a9e41b6960c44a87a57fdf3a63451fc9bc915613226530e2360cbfa1117ea5`.
The fixture now requires an owned stationary pointer target and a known
foreground continuously for 250ms, within its existing eight-second deadline,
before entering the existing single guarded recovery path. It records changes
in point/foreground ownership in `contrast-recovery-target.json`. A zero
foreground or obstructed point resets settlement. The input-session guard,
normal popup dismissal, real keyboard re-entry, colors, pixels and cleanup
remain required. The x64 and ARM64 builds have zero warnings, all 26 unit tests
pass, and the local mode-changing entry point still refuses at the hosted-runner
guard (`artifacts/contrast-settlement-local-refusal`, reported contrast false).
Fresh hosted transition evidence is pending. The same run's ARM64 job passes
its ordinary catalog, contrast and independent consumer; runtime archive
`11840514044` has verified SHA-256
`1976e27fcc04053757363e8df7a1243a3e06a35aed6c3946d4446713c34aff3f`.

Checkpoint `b6ca38d`, run `38434880055`, passes all three required jobs, including
both complete ordinary native catalogs, both live contrast transitions and both
independent package consumers. The x64 archive `11832577609` has verified SHA-256
`7423ec09fd1fd82b3db47106f42daae4f16c8bf96647fa66daec16197bbc0580`, with selected
contrast evidence at `artifacts/ci-contrast-b6ca38d-x64`. Its real system cover
deactivates the window, normal dismissal occurs, and guarded keyboard re-entry
passes after the cover releases the owned target. All 154 established action
ink pixels use Windows HighlightText, and the painted background equals Highlight.
ARM64 runtime archive `11833011887` has published digest
`4afe18d8d9f2a83cd40a00ba3093c96d72e9decb3ed7960e88c116ac444c18f0`.

This accepts the representative transition regression. All four contrast
schemes, all components/states, scaling and Narrator remain open; this is not
complete library accessibility acceptance.

Run `38433098660` at `868fff5` passes the full registered catalog and independent
consumers on both architectures. The ARM64 contrast phase also passes actual
mode switching, exact action/heading paint, open popup keyboard focus,
breadcrumb hover and restoration. Its archive is `11832133524`, SHA-256
`28d95f21bd5577000399be9d48e5bd3be3dd96408a7c084cde3828994d35e31d`.

The x64 contrast phase stops at guarded foreground recovery. Its retained trace
identifies Windows' `sethc` / `CoverWindowClass` transition cover owning both
foreground and the parked pointer point. Recovery correctly refuses while that
point is obstructed. The fixture now waits up to eight seconds for the catalog's
foreground or pointer target to become owned before its existing single guarded
recovery attempt. It does not move the pointer, dismiss the system cover or
relax ownership. The x64 archive `11832183204` has verified SHA-256
`296ff67ef1ac1e019225364458d543bc87114befb326c6b1ddcb2b53864c759d`.
Its contrast acceptance remains open.

Run `38431196521` at `f074df3` passes the live color assertion on x64 and reaches
popup/focus verification. Its trace records a native window deactivation while
the logical help trigger retains keyboard focus. The x64 archive `11830797900`
has verified SHA-256
`c3e7566147f73f6841750b0cd606935d12ac73d707cfa419e58e9b9ae0c950f5`;
selected evidence is at `artifacts/ci-contrast-f074df3-x64`.

The test now records the native foreground process at focus events. Only a
proven foreign-process foreground transition while contrast is enabled and the
same help owner remains focused permits keyboard re-entry. That branch first
requires normal tooltip dismissal, restores only the catalog's foreground via
the existing guarded input session, and uses actual Tab to reopen the same help
control. It then verifies the live popup palette and keyboard focus. An
unexplained closure, an Orchard-owned popup taking foreground, or focus moving
to another XAML element still fails. This branch is pending hosted verification;
it does not establish open-popup retention across an uninterrupted transition.

The `f4fd80a` failure image also exposes a rendering defect: its primary action
label has 154 solid white ink pixels and a dark backing in the sampled label
rectangle, while its mapped HighlightText brush is `#FF263B50` on Highlight
`#FF8EE3F0`. Generated button styles and the foundation button/text styles now
set `HighContrastAdjustment=None`, following the
[Windows guidance](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/high-contrast-themes)
for already-correct system-aware brushes. Windows semantic theme resources stay
authoritative. Native verification of the repaired paint remains required.

The baseline now enters contextual help through physical Tab from breadcrumb
expansion, proving both actual focus and keyboard modality. Programmatically
requesting Keyboard focus after a mouse move did not establish the user's last
input device. The ordinary Light/Dark suite verifies this same entry and actual
Escape dismissal with retained keyboard focus, without changing Windows mode.
The existing popup/focus retention assertion is unchanged.
Independent package verification runs after either native-smoke outcome, even
when contrast fails; those failures still fail the required job.

The same run's ARM64 phase retains its popup and keyboard focus, then fails the
immediate painted-background assertion. Archive `11831991299` has verified
SHA-256 `e68701887295ed6b9c6ad93798de282689e6d7c939628cc4da6170ab49d818eb`.
The failure page has the current Highlight fill, so the earlier pixel's value
and exact expected color need direct observation. Paint checks now retain each
actual/expected background sample and glyph-match count, observe live Windows
colors after each raster read, and await exact pixel settlement within eight
seconds. The original ink mask, dimensions, opaque pixels and exact colors
remain required; this settling check makes no motion-duration claim.

Run `38428299357` at `f4fd80a` enters real contrast mode and restores the original
mode/palette. Its x64 color assertion fails although the retained failure
snapshot's colors match the then-current Win32 colors. The test had captured its
expected colors immediately after the mode flag changed; stale expectations
during asynchronous scheme application are a suspected cause, not yet proven.
Expected colors now come from the live Windows
producer on each observation; reports also retain brush opacity. The x64 archive
`11828899827` has verified SHA-256
`30e626b5b809b9647047c7b8806b3622083597ef5fb9fe2832ccc6e7157cde69` and selected
contrast evidence is retained at `artifacts/ci-contrast-f4fd80a-x64`. Popup/focus,
pixel and full transition acceptance remain open.
Focus events and brush opacity are retained to distinguish an actual popup
lifecycle/focus change from a palette-settling failure.

At `4c5ed02`, x64 reaches the managed fixture after the repeated-getter regression,
then its initial snapshot fails because the unopened tooltip has unset brushes.
The baseline now opens the help popup first and diagnostics preserve null brush
values rather than substituting a palette. Transition acceptance remains pending.

The initial x64 phase at `4901392`, run `38422767730`, terminates with native
heap corruption (`0xC0000374`) before a managed report or page capture. Its
archive `11828535874` has verified SHA-256
`b88d3e10f2d39a1b8884412cf5f1eb23b6107b0ac0ba658f17e0d3d48a270399`.
Read-only local probes show two `SPI_GETHIGHCONTRASTW` calls returning the same
scheme pointer. The helper now copies that reused system buffer without freeing
it; setter input remains a separately allocated/freed caller buffer. The native
entry point repeats the read 32 times before enabling the mode and requires the
same flags/scheme, covering the original repeated-getter corruption boundary. Native
checkpoints before/after the initial read, enable and restore are retained in
`contrast-native-stages.jsonl`. The corrected transition still needs native
verification; no contrast acceptance is claimed from the failed process.

The registered **Windows contrast** catalog page combines a primary action,
editable input, collapsed breadcrumb and contextual-help popup. Its automated
entry point is `scripts/smoke-catalog.ps1 -Component contrast`; both native CI
jobs run it even if the preceding ordinary catalog runtime check fails, provided
that check ran. All failures remain required-check failures.

The mode-changing helper requires `CI=true`, `GITHUB_ACTIONS=true` and
`RUNNER_ENVIRONMENT=github-hosted` before reading or changing settings. Local and
self-hosted invocations fail explicitly. No environment values are introduced;
these are [GitHub's runner metadata](https://docs.github.com/en/actions/reference/workflows-and-actions/variables).
Ordinary catalog navigation only displays the page and never changes Windows.

The test requires Windows contrast initially disabled and available. It retains
the flags and configured default scheme returned by
[HIGHCONTRAST](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-highcontrastw),
uses [SystemParametersInfo](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow)
to toggle the real mode and broadcast the change without requesting a profile write,
and restores the original state in cleanup. Both Win32 and Windows Runtime must
report the transition and restoration. The existing application process and
controls stay mounted throughout.

Acceptance requires the root/text, primary action, input and already-open help
popup to adopt their mapped Window/WindowText or Highlight/HighlightText colors.
The action's rendered background and actual label/heading ink must also match
Windows' colors. Glyph sampling reuses solid ink positions from the pre-change
transparent text render, so a matching border or background cannot masquerade
as correctly painted foreground. Raster dimensions must remain unchanged.
Physical breadcrumb hover must pair Highlight with HighlightText. The
popup must stay open with keyboard focus while the catalog owns foreground;
native deactivation follows the existing focus-loss dismissal contract and
requires verified keyboard re-entry as described above. The original root/text
palette must return afterward.

Reports retain before, active/failure and restored state in
`contrast-measurements.json`; PNGs retain the three page phases, active tooltip
and composed window. Mode restoration failures fail verification. Local refusal
has been verified with `highContrast=false`; native mode-transition verification
is pending. The ordinary complete x64 catalog still passes both themes/system
appearance under `artifacts/smoke-contrast-ordinary`; both architecture builds
have zero warnings and all 26 unit tests pass. The ordinary complete x64 suite
also passes at `artifacts/smoke-contrast-physical-all`, including the shared
physical Tab/Escape entry in both themes with the repaired styles. Build/resource checks alone do
not establish a contrast pass.

This initial check covers the runner's configured contrast scheme and these
representatives. All four built-in themes, every component/state, focus pixels,
additional open overlays, Narrator and scaling remain acceptance gaps. The
[Windows contrast guidance](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/high-contrast-themes)
defines system color pairing; the wiki remote is unavailable, so this contract
is maintained in the repository.

The fixture now includes Bauhaus decorative imagery before the primary action,
without changing the Breadcrumb-to-Help keyboard entry path. It requires both
opaque and transparent image regions to become exact Window pixels in the
actual composed capture; Brand continues to require WindowText/alpha behavior.
These samples are retained in `contrast-brand-pixels.json` with a `bauhaus`
component marker. Fresh hosted acceptance for this addition is required.

Attention Surface is now included before the primary action, preserving the
Breadcrumb-to-Help keyboard entry order. Its check requires exact system Window
background and WindowText border/text, removal of the gradient from the rendered
surface, zero hover lift and no native shadow during actual contrast mode.
The label comparison reuses solid glyph positions captured before the mode
change, so already-correct pixels cannot define their own acceptance mask.
Restoration must return the exact producer image brush and original border/text
colors. Snapshots retain those values and the active image/shadow/lift state;
`contrast-attention-active.png` retains the rendered specimen. Hosted verification
of this new boundary is pending. The guarded local route refuses before changing
Windows mode at `artifacts/smoke-attention-contrast-local-refusal`.

Hosted run `38494432792` at `384b54b` fails Attention's unchanged glyph gate
identically on x64 and ARM64: 38/46 established pixels match exact Windows
foreground throughout the eight-second observation. Semantic paint and
gradient/lift/shadow suppression pass first; restoration returns the original
producer image, border and text. This is an open rendered-ink gap.

The fixture now saves the exact baseline and final glyph rasters from their
measured pixel buffers, plus the unchanged mask, each mismatched pixel's RGBA,
and before/after origin, dimensions, font, transform and scale values. Files use
`contrast-ink-<name>-before.png`, `-after.png` and `-details.json`. These diagnose
paint, layout and rasterization differences without deriving a replacement mask
from the post-transition result or relaxing the exact color/opacity assertion.

The verified ARM64 archive for run `38496131766` shows Attention's isolated
label transform changing from -1.27600098 to -1.77600098 as its ancestor layout
changes during contrast mode. Its displayed origin changes from Y=429.223999
to 392.223999: the same fractional phase, 37 whole pixels apart. Font, dimensions
and scale remain unchanged. Eight isolated-snapshot samples are partly covered
after the half-pixel local transform change, explaining why a local TextBlock
raster is insufficient evidence of displayed foreground adoption.

The glyph check now crops the actual composed client capture at each label's
physical bounds. It requires owned foreground, an unoccluded client and a desktop
composition flush, retains exact before/final label crops, and keeps the mask
established before the mode change. All those established positions must still
match exact Windows foreground with alpha 255 within eight seconds. The ordinary
keyboard-entry fixture also reads the composed Attention baseline without
changing Windows settings. Fresh hosted contrast verification is required; the
capture change does not assert that the observed displayed glyphs already pass.

The ordinary x64 full suite passes the composed Attention baseline reads in
both themes at `artifacts/smoke-notifications-theme-composed-ink-all`. This proves
the owned-screen capture, cropping and ink-mask path runs without changing
local Windows settings. It does not prove live contrast adoption; that requires
the fresh isolated hosted transition check.

Hosted run `38498921210` at `3f4c0e7` fails the composed Attention check at
46/124 positions on both architectures. The verified native ARM64 before/final
images show that all 124 pre-established positions match exact Windows
foreground at a one-pixel downward offset. That diagnostic offset is not used
to align the mask or relax acceptance. The label's logical origin retains its
fractional phase and its local transform changes by half a pixel; the physical
glyph/crop/baseline relationship remains an open positioning boundary.

Owned-UI diagnostics reproduce the compact content, foreground change,
gradient-to-solid background transition and half-pixel margin change without
changing Windows settings. All 124 established positions remain correct without
offset in both themes after two render frames. The half-pixel transform change
alone is therefore insufficient to explain the hosted displacement.

The capture previously read the label's bounds after awaiting asynchronous PNG
encoding. It now records bounds and geometry before capture, retains the later
pose in `contrast-ink-<name>-poses.jsonl`, and refuses a pose change during
encoding. Baseline capture waits for two actual render frames. This makes the
coordinate/pixel relationship reviewable; exact colors, the unchanged baseline
mask, owned-window requirements and the eight-second contrast deadline remain
required. Fresh hosted verification must establish whether this capture-boundary
correction resolves the failure.

The complete local x64 catalog passes the new baseline frame barrier and
pre-encoding coordinate reads in both themes at
`artifacts/smoke-attention-capture-pose-all`. Every ordinary baseline capture
reports a stable before/after pose. All 31 unit/resource tests pass. This
checkpoint changes catalog verification and diagnostics only; the library's
typography and rendering remain unchanged. Actual hosted contrast adoption
remains unverified until the fresh run completes.

Run `38502493745` at `4fde530` completes with the same 46/124 Attention failure
on both architectures. Its native ARM64 artifact has verified SHA-256
`d43d0c6bf5637bfa7e76f490b0f83344099abaefa37e5dc87271b5e8fa1f28ff`.
Every recorded pre/post-encoding label pose is stable. Before/after logical
origins are 429.223999 and 392.223999; local text transforms are -1.276001 and
-1.776001. The capture-timing correction therefore does not resolve the
positioning failure. No pixel offset or relaxed acceptance is introduced.
Snapshots additionally retain the natural metrics, baseline offset and each
owned ancestor's position, rounding and transform to distinguish the next
renderer investigation from an assumed capture race.
