# Fixed Windows parity inventory

The source commits below freeze this goal. Upstream updates require explicit scope review.

Visual acceptance requires every component to match its Bleecker reference across every variant, size, theme and applicable interaction state. Compare typography, spacing, dimensions, colors, borders, radii, icons and motion against the rendered reference. Windows control defaults must not override the branded appearance, including nested popups and disabled/focus states. Preserve the explicitly required Windows contrast-color adaptation. A mismatch is an open acceptance gap, never an approved platform substitution.

| Reference | Commit |
| --- | --- |
| gaulatti/bleecker | a86059013cd59684ce452849278ea81e734f2066 |
| gaulatti/sabella | f5ac479bfceaa7eab23c1716ac47420ac53dae25 |
| gaulatti/thompson | 30a76c849e2486ba26b15b0c0c569fbde8c0606c |
| gaulatti/newsroom | 82cca98c4d3dfd1d65509f8d8c46e8fece0ead70 |

Machine-readable contracts, public symbols, literal variants, states, evidence paths and remaining gaps are recorded in [parity-inventory.json](parity-inventory.json). No component is complete until its evidence is populated and verified. Existing foundation styles cover only part of Button, Input, Select and typography.

Every interactive entry additionally requires hover, pressed, focus, selection and loading where applicable, keyboard activation/dismissal/focus restoration, long/multiline/non-Latin content, theme changes and reduced motion. Overlays must verify open-content theme changes. Collections must preserve selection across updates and sorting. Dialogs must explicitly protect unsaved edits.

Visual acceptance requires comparison with the rendered pinned Bleecker reference across every variant, size, theme and interaction state. Match typography, spacing, dimensions, colors, borders, radii, icons and motion. Native Windows semantics may be retained, but Windows defaults must not override the branded appearance. Token assertions and successful native-control interaction alone do not establish visual parity. Existing components remain in progress until this comparison is verified, including focus, loading and animated transitions.

The available development host is Intel x64. ARM64 compilation is verified separately; no ARM64 machine is currently available for runtime acceptance.

The visual audit tracks unresolved Windows defaults in current previews. Icon-button fixtures now use pinned Lucide vectors instead of SymbolIcon glyphs; other preview surfaces still require audit. The button loading indicator now uses the reference Lucide arc. Focus rings use Bleecker brushes and geometry, and the link uses the reference hover-only underline. The remaining focus matrix, state motion, shadows and typography still require full rendered comparison. These are tracked acceptance gaps, not approved visual substitutions. See [rendered comparison evidence](visual-audit.md).

| Component contract | Windows implementation | Catalog / regression / runtime evidence | Remaining gap |
| --- | --- | --- | --- |
| brand-lockup | Pending | Pending | Full pinned contract |
| bauhaus-background | Pending | Pending | Full pinned contract |
| activity-item | Pending | Pending | Full pinned contract |
| attention-surface | Pending | Pending | Full pinned contract |
| button | `BleeckerButton`: seven variants, four sizes, commands/content, loading | Registered fixtures; x64 native command/loading/binding, CanExecute/template replacement and 7×4 state checks in both themes; isolated NuGet consumer; ARM64 build | In progress: focus/input, Narrator, contrast, scaling, motion, ARM64 runtime; see [usage](buttons.md) |
| button-group | `BleeckerButtonGroup` | Horizontal/vertical catalog fixtures; published x64 light/dark layout and group-role checks | In progress: edge fixtures, child restoration, keyboard, Narrator, contrast, scaling, ARM64 runtime; see [usage](button-group.md) |
| card | `BleeckerCard`: five variants, four padding sizes, consumer content | Registered fixtures; native surface, wrapping, child-action and shadow-layer checks; independent consumer | In progress: exact shadows/clipping, surface motion, overrides, contrast/accessibility/scaling, ARM64 runtime; see [usage](cards.md) |
| chart | Pending | Pending | Full pinned contract |
| chart-theme | Pending | Pending | Full pinned contract |
| line-chart | Pending | Pending | Full pinned contract |
| bar-chart | Pending | Pending | Full pinned contract |
| area-chart | Pending | Pending | Full pinned contract |
| pie-chart | Pending | Pending | Full pinned contract |
| donut-chart | Pending | Pending | Full pinned contract |
| sparkline | Pending | Pending | Full pinned contract |
| scatter-chart | Pending | Pending | Full pinned contract |
| radar-chart | Pending | Pending | Full pinned contract |
| radial-bar-chart | Pending | Pending | Full pinned contract |
| funnel-chart | Pending | Pending | Full pinned contract |
| sankey-chart | Pending | Pending | Full pinned contract |
| sunburst-chart | Pending | Pending | Full pinned contract |
| stat-card | Pending | Pending | Full pinned contract |
| dashboard-grid | Pending | Pending | Full pinned contract |
| dashboard-section | Pending | Pending | Full pinned contract |
| page-header | Pending | Pending | Full pinned contract |
| sidebar | Pending | Pending | Full pinned contract |
| data-table-toolbar | Pending | Pending | Full pinned contract |
| data-table-filters | Pending | Pending | Full pinned contract |
| data-table-column-toggle | Pending | Pending | Full pinned contract |
| data-table-pagination | Pending | Pending | Full pinned contract |
| skeleton | Pending | Pending | Full pinned contract |
| stepper | Pending | Pending | Full pinned contract |
| timeline | Pending | Pending | Full pinned contract |
| data-list | Pending | Pending | Full pinned contract |
| error-state | Pending | Pending | Full pinned contract |
| loading-overlay | Pending | Pending | Full pinned contract |
| date-range-picker | Pending | Pending | Full pinned contract |
| filter-chip | Pending | Pending | Full pinned contract |
| search-input | Pending | Pending | Full pinned contract |
| dropdown-menu | `BleeckerDropdownMenu`, native item styles and focus behavior | Registered root/selection/submenu fixture; light/dark x64 geometry, selection, invocation and popup captures | In progress: rich rows, shadow/motion, full physical input, contrast/scaling/Narrator and ARM64 runtime; see [menu gaps](menus.md) |
| metric | Pending | Pending | Full pinned contract |
| notification-badge | `BleeckerNotificationBadge` and consumer-owned notification items | Registered populated/empty/count/long-content fixtures; x64 light/dark popup geometry, command/collection updates, live appearance and physical keyboard checks; independent package consumer | In progress: full state/input matrix, shadow/motion, contrast/Narrator/scaling and ARM64 runtime; see [evidence and gaps](notifications.md) |
| accordion | Pending | Pending | Full pinned contract |
| alert | Pending | Pending | Full pinned contract |
| alert-dialog | Pending | Pending | Full pinned contract |
| avatar | Pending | Pending | Full pinned contract |
| breadcrumb | Pending | Pending | Full pinned contract |
| context-menu | Pending | Pending | Full pinned contract |
| date-picker | Pending | Pending | Full pinned contract |
| dialog-close-button | Pending | Pending | Full pinned contract |
| drawer | Pending | Pending | Full pinned contract |
| empty | Pending | Pending | Full pinned contract |
| eyebrow | Pending | Pending | Full pinned contract |
| hover-card | Pending | Pending | Full pinned contract |
| input | Pending | Pending | Full pinned contract |
| kbd | Pending | Pending | Full pinned contract |
| label | `BleeckerLabel`, explicit typography and input association | Default, error styling, disabled, empty, wrapped, non-Latin and rich content | In progress: [preview](labels.md), physical input, exact rendering, contrast/Narrator/scaling and ARM64; [reference](label-reference.md) |
| menubar | Pending | Pending | Full pinned contract |
| navigation-menu | Pending | Pending | Full pinned contract |
| otp-input | Pending | Pending | Full pinned contract |
| popover | Pending | Pending | Full pinned contract |
| progress | Pending | Pending | Full pinned contract |
| progress-stack | Pending | Pending | Full pinned contract |
| radio-group | Pending | Pending | Full pinned contract |
| checkbox | Pending | Pending | Full pinned contract |
| scroll-area | Pending | Pending | Full pinned contract |
| separator | Pending | Pending | Full pinned contract |
| sheet | Pending | Pending | Full pinned contract |
| slider | Pending | Pending | Full pinned contract |
| sonner | Pending | Pending | Full pinned contract |
| switch | Pending | Pending | Full pinned contract |
| textarea | Pending | Pending | Full pinned contract |
| toggle | Six native `ToggleButton` styles | Registered size/variant fixtures; x64 automation/state checks; desktop pointer and Space/Tab; light/dark captures | In progress: Narrator, contrast, scaling, reduced motion, binding/command regressions, ARM64 runtime; see [usage](toggles.md) |
| toggle-group | `BleeckerToggleGroup` and native toggle items; optional single/multiple selection | Registered binding, collection, disabled and constrained fixtures; x64 UIA and physical input; package consumer; ARM64 build | In progress: Narrator, contrast, scaling, motion, broader RTL/error fixtures and ARM64 runtime; see [usage](toggle-groups.md) |
| tooltip | `BleeckerTooltip` and `BleeckerTooltipService`, app-constrained popup | Four directions, four collision flips, 7px clearance, wrapping and light/dark body/font/palette/role checks; bidirectional transit geometry and dark/top pointer transit; [reference audit](tooltip-reference.md) | In progress: full physical transit matrix, motion/timing, modal/corner cases, theme/scaling/contrast/Narrator and ARM64 runtime; see [gaps](tooltips.md) |
| feed-item | Pending | Pending | Full pinned contract |
| field | Pending | Pending | Full pinned contract |
| header-select | Pending | Pending | Full pinned contract |
| hero-carousel | Pending | Pending | Full pinned contract |
| field-help-tooltip | `BleeckerFieldHelpTooltip`, native button with explicit vector template | Inline label and empty text, branded state values, keyboard help, accessible name and live text | In progress: exact motion comparison, physical input, rendered parity, contrast/Narrator/scaling and ARM64; see [usage](field-help-tooltips.md) |
| icon-badge | Pending | Pending | Full pinned contract |
| icon-button | `BleeckerIconButton`: three variants and three sizes | Nine registered combinations; x64 dimensions, names/roles, command and state checks; light/dark captures; package fixture; ARM64 build | In progress: physical input, tooltip, Narrator, contrast, scaling, motion and ARM64 runtime; see [usage](icon-buttons.md) |
| loading-spinner | `BleeckerLoadingSpinner`: three sizes, themed three-quarter ring | Registered fixture; x64 geometry, theme, UIA, reduced-motion and lifecycle checks; independent package consumer | In progress: normal/live motion, contrast, Narrator, scaling/composition, ARM64 runtime; see [usage](loading-spinner.md) |
| media-library | Pending | Pending | Full pinned contract |
| modal | Pending | Pending | Full pinned contract |
| nav-menu | Pending | Pending | Full pinned contract |
| pagination | Pending | Pending | Full pinned contract |
| collection-filters | Pending | Pending | Full pinned contract |
| command-spotlight | Pending | Pending | Full pinned contract |
| select | Pending | Pending | Full pinned contract |
| file-input | Pending | Pending | Full pinned contract |
| section-header | Pending | Pending | Full pinned contract |
| panel | Pending | Pending | Full pinned contract |
| status-badge | Pending | Pending | Full pinned contract |
| table | Pending | Pending | Full pinned contract |
| data-table | Pending | Pending | Full pinned contract |
| tabs | Pending | Pending | Full pinned contract |
| theme-toggle | Pending | Pending | Full pinned contract |
| app-shell | Pending | Pending | Full pinned contract |
| admin-shell | Pending | Pending | Full pinned contract |
| auth-shell | Pending | Pending | Full pinned contract |
| detail-layout | Pending | Pending | Full pinned contract |
| footer | Pending | Pending | Full pinned contract |
| feed-column | Pending | Pending | Full pinned contract |
| feed-grid | Pending | Pending | Full pinned contract |
| header | Pending | Pending | Full pinned contract |
| page-frame | Pending | Pending | Full pinned contract |
| panel-layout | Pending | Pending | Full pinned contract |

| brand-mark (Sabella) | Pending | Pending | Full pinned desktop contract |
| secure-field (Sabella) | Pending | Pending | Full pinned desktop contract |
| segmented-control (Sabella) | Pending | Pending | Full pinned desktop contract |
| app-tab-bar (Sabella) | Pending | Pending | Full pinned desktop contract |

## Compositions

Monitoring: suite-selector slot, sidebar, live columns, attention cards, search, stable selection and processing review. Editorial: filters, sortable/selectable rows, page editing, media/category/author selection and inline confirmation. Both use fictional local data, without product APIs or authentication.

## Verification and release acceptance

- Foundation: published and Visual Studio startup, fonts/resources, light/dark/system/Windows contrast, open flyouts, token drift, text/display scaling and reduced motion.
- Every public visual control/style must have registered catalog coverage and appropriate boundary regression checks.
- x64 and ARM64 builds and runtime evidence are separate requirements. ARM64 support remains unverified until exercised on ARM hardware.
- Accessibility requires UI Automation plus Narrator and OS contrast verification; performance budgets must be set before large-data implementation and measured with hardware/OS/workload evidence.
- Independent package consumer, versioned NuGet and runnable catalog, matching documentation, remote main and required CI are still required.

The GitHub wiki could not be retrieved. Documentation remains in this repository. Thompson is a supplementary contract reference; its touch-specific behavior does not override desktop Windows conventions. Sabella television/mobile-specific types are outside this Windows scope. Web providers and implementation utilities map to native infrastructure rather than separate visual controls. No networking, CMS schemas or product workflow logic enters Orchard.

