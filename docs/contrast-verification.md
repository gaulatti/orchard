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
