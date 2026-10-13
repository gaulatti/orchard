# Catalog navigation

The left component list filters titles case-insensitively as the user types.
Choose a result to show that fixture. Clearing the search restores the full list.
An unmatched query shows an explicit message and leaves the current fixture
visible. The selected entry is highlighted; selecting it again preserves its
scroll position and current keyboard focus.

Only the selected fixture is mounted in the content host. Switching fixtures
unloads the previous visual tree, resets the content scroll position and loads
the selected fixture. Fixture objects are retained, so local example text and
selection survive navigation. This is visual-tree isolation, not deferred object
construction or a claim of bounded large-data virtualization.

The appearance selector stays outside the content host. Both navigation and
content consume Orchard's fonts, colors and controls. The sidebar uses existing
Bleecker button previews and the Input preview; their outstanding component
parity gaps still apply.

Breadcrumb is registered after Avatars, with source threshold boundaries,
independent wrapping, custom content, keyboard expansion and consumer updates.

The full runtime suite uses the same fixture selection path before exercising
each registered component and capturing its result. Navigation checks exercise
search, native button activation, no-result messaging, clearing search, retained
consumer text and unloading of the previous fixture. The focused Input mode
selects Input through the same catalog path.

The focused tooltip mode (`-Component tooltips`) also navigates away and back
between appearances, exercising theme propagation after owner reattachment.

Verification on the Intel x64 host passes the full light/dark runtime suite with
Windows Animation effects off (`artifacts/smoke-navigation-fixed`) and on
(`artifacts/smoke-navigation-motion`). Both Input themes capture intermediate
color and opacity frames with animations enabled. This verifies the existing
motion checks; it does not establish a performance budget or exact browser
animation parity. The original Windows setting was restored.

Actual keyboard input verifies mixed-case search followed by Tab/Enter
activation (`artifacts/navigation-keyboard-input.jpg`). An unmatched query keeps
Input visible and shows the explicit message (`artifacts/navigation-no-results.jpg`).
Clearing search restores the list, and pointer activation returns to Foundation.
The baseline unit/coverage suite passes all 19 tests, and x64 publish and ARM64
compilation pass. The full navigation/catalog fixture checks now also pass on
native ARM64; see [runtime evidence](runtime-evidence.md) for architecture
reports and the remaining platform acceptance.

Text-size/density controls, the remaining unimplemented inventory and full
keyboard/Narrator/responsive acceptance are still required. Catalog navigation
does not make any component's visual or interaction acceptance complete.

The **Windows contrast** page is a registered cross-component verification
fixture. Normal navigation displays the semantic-color specimens; only its
guarded CI smoke entry point changes the isolated runner's actual mode. See
[contrast verification](contrast-verification.md) for scope and remaining gaps.

The Switches page adds off/on, both disabled states, a named unlabeled switch,
long/Japanese/RTL labels and a consumer-owned two-way Boolean binding. Its
registered runtime checks and remaining acceptance are documented in
[switches](switches.md).
