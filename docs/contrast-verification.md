# Windows contrast verification

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
popup must stay open with keyboard focus during the transition, and the original
root/text palette must return afterward.

Reports retain before, active/failure and restored state in
`contrast-measurements.json`; PNGs retain the three page phases, active tooltip
and composed window. Mode restoration failures fail verification. Local refusal
has been verified with `highContrast=false`; native mode-transition verification
is pending. The ordinary complete x64 catalog still passes both themes/system
appearance under `artifacts/smoke-contrast-ordinary`; both architecture builds
have zero warnings and all 26 unit tests pass. Build/resource checks alone do
not establish a contrast pass.

This initial check covers the runner's configured contrast scheme and these
representatives. All four built-in themes, every component/state, focus pixels,
additional open overlays, Narrator and scaling remain acceptance gaps. The
[Windows contrast guidance](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/high-contrast-themes)
defines system color pairing; the wiki remote is unavailable, so this contract
is maintained in the repository.
