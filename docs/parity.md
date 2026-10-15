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

The available development host is Intel x64. ARM64 compilation is verified separately. Earlier native `windows-11-arm` runs passed baseline fixtures and the independent package consumer with process/OS checks; those historical runs do not verify later components or corrections. See [runtime evidence](runtime-evidence.md). The latest recorded native ARM64 sequence retains an unresolved Input exterior-cursor failure. Push-triggered Actions are paused at the user's request, so new native ARM64 runtime evidence remains pending. Full visual, input, accessibility and scaling acceptance is still required, and no local ARM64 machine is available for manual Narrator/contrast checks.

The visual audit tracks unresolved Windows defaults in current previews. Icon-button fixtures now use pinned Lucide vectors instead of SymbolIcon glyphs; other preview surfaces still require audit. The button loading indicator now uses the reference Lucide arc. Focus rings use Bleecker brushes and geometry, and the link uses the reference hover-only underline. The remaining focus matrix, state motion, shadows and typography still require full rendered comparison. These are tracked acceptance gaps, not approved visual substitutions. See [rendered comparison evidence](visual-audit.md).

| Component contract | Windows implementation | Catalog / regression / runtime evidence | Remaining gap |
| --- | --- | --- | --- |
| brand-lockup | `BleeckerBrandLockup`, typed consumer link context and live native image effects | Registered size/name, custom RGBA/link, disabled/command and non-Latin fixtures; focused x64 initial checks | In progress: [source contract and API](brand-lockup-reference.md); rendered parity, live/lifecycle/RTL, motion/reversal, error/override, package, contrast/Narrator/scaling and ARM64 acceptance |
| bauhaus-background | `BleeckerBauhausBackground`: four variants and live native image effects | Registered RGBA, square/tall and missing-image fixtures; x64 blend-pixel, source, geometry, motion-preference and UIA checks | In progress: [source contract and API](bauhaus-background-reference.md); full raster/motion, mutations, contrast/Narrator/scaling, packaging and ARM64 acceptance |
| activity-item | `BleeckerActivityItem`: exact strings, small Avatar, source typography and hover | Registered normal/empty/multilingual/RTL fixtures; frozen browser layout; x64/ARM64 pointer/Tab/UIA/lifecycle, motion-preference and independent-consumer checks pass at `64a2b27` | In progress: [source contract and API](activity-item-reference.md); baseline/raster, complete motion, constrained/override matrix and contrast/Narrator/scaling |
| attention-surface | `BleeckerAttentionSurface`: five elements, densities, source color model and consumer content/actions | 143 frozen-helper/browser numeric cases; 86 initial x64 source-layout cases and native action/style/hover checks | In progress: [source contract and API](attention-surface-reference.md); full raster/motion, constraints/lifecycle/overrides, contrast/Narrator/scaling, package and ARM64 acceptance |
| button | `BleeckerButton`: seven variants, four sizes, commands/content, loading | Registered fixtures; x64 native command/loading/binding, CanExecute/template replacement and 7×4 state checks in both themes; isolated NuGet consumer; ARM64 build | In progress: focus/input, Narrator, contrast, scaling, motion, remaining ARM64 state checks; see [usage](buttons.md) |
| button-group | `BleeckerButtonGroup` | Horizontal/vertical catalog fixtures; published x64 light/dark layout and group-role checks | In progress: edge fixtures, child restoration, keyboard, Narrator, contrast, scaling, remaining ARM64 state checks; see [usage](button-group.md) |
| card | `BleeckerCard`: five variants, four padding sizes, consumer content | Registered fixtures; native surface, wrapping, child-action and shadow-layer checks; independent consumer | In progress: exact shadows/clipping, surface motion, overrides, contrast/accessibility/scaling, remaining ARM64 state checks; see [usage](cards.md) |
| chart | Pending | Pending | Full pinned contract |
| chart-theme | `BleeckerChartTheme` and `BleeckerChartSeries`: scoped resource keys, source palette selection, immutable style metadata and consumer-owned series | Registered role/palette/brand, series-paint and axis/grid/tooltip surface fixture; frozen metadata regressions | In progress: [contract and API](chart-theme-reference.md); actual chart consumers, tooltip effects and full platform acceptance |
| line-chart | `BleeckerLineChart`: consumer records/series, monotone curves, grid, axes, legend and category inspection | Registered fixture; frozen curve/domain/data cases, 60 placement cases, 22 chart and 26 tooltip specimens, composed blur/shadow profiles; independent consumer | In progress: [API and evidence](line-chart-reference.md); complete visual/motion/lifecycle, tooltip raster and platform acceptance |
| sparkline | `BleeckerSparkline`: compact consumer records/key, monotone curves, optional paint and raw category inspection | Registered 36-case frozen fixture, negative-domain axes, physical input/binding/remount gates and independent consumer | In progress: [source contract and evidence](sparkline-reference.md); full raster/motion/data matrix and platform acceptance |
| stat-card | `BleeckerStatCard` and `BleeckerStatTrend`: consumer value/templates, trend/description precedence, icon, history and footer composition | Registered 80-case layout/content and 16-case typography references, composed clipping/occupied-row and native binding/input/lifecycle/template gates; independent consumer | In progress: [contract and evidence](stat-card-reference.md); exact glyph/raster/motion, full content/override and platform acceptance |
| bar-chart | `BleeckerBarChart`: consumer grouped bars, both layouts, axes, grid/legend and category inspection | Registered 68-case frozen geometry/palette/tick/cursor/tooltip fixture, source wrapping baselines and native live-data/input/lifecycle checks | In progress: [source contract and evidence](bar-chart-reference.md); full raster/motion/content/override/RTL and platform acceptance |
| area-chart | `BleeckerAreaChart`: overlapping/stacked monotone areas, ranges, axes and native inspection | Registered 80-case source fixture and live data/input/lifecycle checks | In progress: [source contract and acceptance gaps](area-chart-reference.md) |
| pie-chart | `BleeckerPieChart` and consumer-owned `BleeckerPieChartDatum` | Registered shared 84-case Pie/Donut source fixture and native lifecycle checks | In progress: [source contract and acceptance gaps](pie-chart-reference.md) |
| donut-chart | `BleeckerDonutChart`: source ring and independent center text | Shared Pie/Donut fixture | In progress: [source contract and acceptance gaps](pie-chart-reference.md) |
| scatter-chart | `BleeckerScatterChart`, typed series and independent consumer collections | Registered 64-case source fixture plus native input/data/lifecycle checks | In progress: [source contract and acceptance gaps](scatter-chart-reference.md) |
| radar-chart | Pending | Pending | Full pinned contract |
| radial-bar-chart | Pending | Pending | Full pinned contract |
| funnel-chart | Pending | Pending | Full pinned contract |
| sankey-chart | Pending | Pending | Full pinned contract |
| sunburst-chart | Pending | Pending | Full pinned contract |
| dashboard-grid | `BleeckerDashboardGrid` and `BleeckerDashboardSpan`: viewport columns, responsive/full spans and consumer children | Registered 96-case frozen layout, mutation/keyboard fixture and independent consumer; monitoring/editorial compositions | In progress: [source contract and evidence](dashboard-grid-reference.md); full intrinsic/override matrix, raster, contrast/Narrator/scaling and platform acceptance |
| dashboard-section | `BleeckerDashboardSection`: consumer content, optional title/description/action and viewport-responsive header | Registered 50-case frozen layout, binding/lifecycle/keyboard fixture and independent consumer | In progress: [source contract and evidence](dashboard-section-reference.md); exact raster, minimum-content/override matrix, contrast/Narrator/scaling and full platform acceptance |
| page-header | `BleeckerPageHeader`: consumer title, description, breadcrumbs, wrapping actions and Back commands/routes | Registered sixteen-case viewport/RTL and content/lifecycle/keyboard fixture; independent package consumer | In progress: [source contract and API](page-header-reference.md); exact text balancing/raster, minimum-content constraints, contrast/Narrator/scaling and full platform acceptance |
| sidebar | Pending | Pending | Full pinned contract |
| data-table-toolbar | Pending | Pending | Full pinned contract |
| data-table-filters | Pending | Pending | Full pinned contract |
| data-table-column-toggle | Pending | Pending | Full pinned contract |
| data-table-pagination | Pending | Pending | Full pinned contract |
| skeleton | `BleeckerSkeleton`, `BleeckerSkeletonCard`, `BleeckerSkeletonTable`: shapes, count-driven card/table compositions | Registered nine-case fixture; x64/ARM64 geometry, count/opacity bindings, rendered/empty pixels, ancestor visibility/lifecycle and clean consumers pass at `71a7fb8` | In progress: native/browser raster parity, exact pulse timing, contrast/Narrator/scaling; see [contract and usage](skeleton-reference.md) |
| stepper | Pending | Pending | Full pinned contract |
| timeline | Pending | Pending | Full pinned contract |
| data-list | Pending | Pending | Full pinned contract |
| error-state | `BleeckerErrorState`: consumer-owned summaries, icon content and optional retry commands | Registered normal/empty/long/RTL and native retry fixtures; x64/ARM64 runtime, contrast and independent consumers pass at `64f4f25` | In progress: complete glyph/icon raster comparison, scaling, Narrator, contrast schemes and lifecycle/template matrix; see [contract and evidence](error-states.md) |
| loading-overlay | Pending | Pending | Full pinned contract |
| date-range-picker | Pending | Pending | Full pinned contract |
| filter-chip | Pending | Pending | Full pinned contract |
| search-input | Pending | Pending | Full pinned contract |
| dropdown-menu | `BleeckerDropdownMenu`, native item styles and focus behavior | Registered root/selection/submenu fixture; light/dark x64 geometry, selection, invocation and popup captures | In progress: rich rows, shadow/motion, full physical input, contrast/scaling/Narrator and remaining ARM64 state checks; see [menu gaps](menus.md) |
| metric | Pending | Pending | Full pinned contract |
| notification-badge | `BleeckerNotificationBadge` and consumer-owned notification items | Registered populated/empty/count/long-content fixtures; x64 light/dark popup geometry, command/collection updates, live appearance and physical keyboard checks; independent package consumer | In progress: full state/input matrix, shadow/motion, contrast/Narrator/scaling and remaining ARM64 state checks; see [evidence and gaps](notifications.md) |
| accordion | Pending | Pending | Full pinned contract |
| alert | `BleeckerAlert`, `BleeckerAlertContainer`: window-owned typed notifications and shared stream | Registered four-type fixture; x64 native paint/position, Tab/Shift+Tab/Space, held/cancelled press, duration and lifecycle checks; clean package consumer | In progress: [contract and usage](alert-reference.md); rendered reference, premium hover easing, full motion, disabled/contrast/Narrator/scaling and ARM64 |
| alert-dialog | Pending | Pending | Full pinned contract |
| avatar | `BleeckerAvatar`, `BleeckerAvatarGroup`, `BleeckerAvatarData`: five sizes, images/initials/silhouettes and consumer collections | Registered Avatars fixture; source Light/Dark audit; x64/ARM64 geometry, rendered pixels, collection/lifecycle and independent package checks pass at `2f265e3` | In progress: native/browser raster parity, contrast/Narrator/scaling, constrained widths and overrides; see [contract and usage](avatar-reference.md) |
| breadcrumb | `BleeckerBreadcrumb` and consumer-owned `BleeckerBreadcrumbItem` | x64/ARM64 wrapping, threshold, template/collection, held/cancelled press, keyboard expansion/focus and unload/reload checks; independent consumers pass at `7c4c272` | In progress: complete visuals/motion, remaining focus, scaling, contrast and Narrator; see [reference/API](breadcrumb-reference.md) |
| context-menu | Pending | Pending | Full pinned contract |
| date-picker | Pending | Pending | Full pinned contract |
| dialog-close-button | Pending | Pending | Full pinned contract |
| drawer | Pending | Pending | Full pinned contract |
| empty | Pending | Pending | Full pinned contract |
| eyebrow | `BleeckerEyebrow`: three tones and optional rule | Registered Eyebrows fixture and independent consumer; runtime verification pending | In progress: [usage and gaps](eyebrows.md); Unicode/rich casing, exact rendering, accessibility, isolated scaling and ARM64 runtime |
| hover-card | Pending | Pending | Full pinned contract |
| input | `BleeckerInput`: three sizes, adornments, validation and native editing | Registered light/dark fixtures; native editing, motion, typography, 24 reading-direction and 72 physical-adornment cases; independent consumer | In progress: [reference comparison](input-reference.md), fallback typography, scaling (225% Japanese viewport failure), contrast/Narrator, full motion parity and remaining ARM64 state checks; see [usage](inputs.md) |
| kbd | `BleeckerKbd`: single content and observable key combinations | Registered Keyboard shortcuts fixture and independent consumer; runtime verification pending | In progress: [usage and gaps](keyboard.md), [pinned contract](kbd-reference.md); exact rendering, lifecycle, accessibility, scaling and ARM64 runtime |
| label | `BleeckerLabel`, explicit typography and input association | Default, error styling, disabled, empty, wrapped, non-Latin and rich content | In progress: [preview](labels.md), physical input, exact rendering, contrast/Narrator/scaling and ARM64; [reference](label-reference.md) |
| menubar | Pending | Pending | Full pinned contract |
| navigation-menu | Pending | Pending | Full pinned contract |
| otp-input | Pending | Pending | Full pinned contract |
| popover | Pending | Pending | Full pinned contract |
| progress | `BleeckerProgress`: three sizes, four variants and optional label | Registered size/variant matrix and independent consumer; runtime verification pending | In progress: [usage and gaps](progress.md); exact rendering, motion, lifecycle, edge values, accessibility, scaling and ARM64 runtime |
| progress-stack | Pending | Pending | Full pinned contract |
| radio-group | Pending | Pending | Full pinned contract |
| checkbox | `BleeckerCheckbox`: native selection with consumer label/description content | Registered Checkboxes fixture and independent package consumer; runtime verification pending | In progress: [usage and gaps](checkboxes.md); exact rendering, input/cursor/focus, lifecycle, accessibility, scaling, motion and x64/ARM64 runtime |
| scroll-area | Pending | Pending | Full pinned contract |
| separator | `BleeckerSeparator`: horizontal/vertical, decorative or semantic | Registered geometry, theme, live orientation and automation fixture; independent consumer | In progress: [contract and evidence](separator.md); complete raster, contrast/Narrator/scaling, keyboard traversal and ARM64 acceptance |
| sheet | Pending | Pending | Full pinned contract |
| slider | Pending | Pending | Full pinned contract |
| sonner | Pending | Pending | Full pinned contract |
| switch | `BleeckerSwitch`: native binary toggle and optional label | x64 light/dark native layout, UIA/binding/lifecycle, live appearance, physical Space/Tab/track/label/RTL, reduced motion and independent consumer passed; composed screenshots retained | In progress: [usage and gaps](switches.md); Japanese fallback width, exact raster/motion/drag/cursor/focus, contrast/Narrator/scaling and remaining ARM64 desktop checks |
| textarea | Pending | Pending | Full pinned contract |
| toggle | Six native `ToggleButton` styles | Registered size/variant fixtures; x64 automation/state checks; desktop pointer and Space/Tab; light/dark captures | In progress: Narrator, contrast, scaling, reduced motion, binding/command regressions, remaining ARM64 state checks; see [usage](toggles.md) |
| toggle-group | `BleeckerToggleGroup` and native toggle items; optional single/multiple selection | Registered binding, collection, disabled and constrained fixtures; x64 UIA and physical input; package consumer; ARM64 build | In progress: Narrator, contrast, scaling, motion, broader RTL/error fixtures and remaining ARM64 state checks; see [usage](toggle-groups.md) |
| tooltip | `BleeckerTooltip` and `BleeckerTooltipService`, app-constrained popup | Four directions, four collision flips, 7px clearance, wrapping and light/dark body/font/palette/role checks; bidirectional transit geometry and dark/top pointer transit; [reference audit](tooltip-reference.md) | In progress: full physical transit matrix, motion/timing, modal/corner cases, theme/scaling/contrast/Narrator and remaining ARM64 state checks; see [gaps](tooltips.md) |
| feed-item | Pending | Pending | Full pinned contract |
| field | Pending | Pending | Full pinned contract |
| header-select | Pending | Pending | Full pinned contract |
| hero-carousel | Pending | Pending | Full pinned contract |
| field-help-tooltip | `BleeckerFieldHelpTooltip`, native button with explicit vector template | Inline label and empty text, branded state values, keyboard help, accessible name and live text | In progress: exact motion comparison, physical input, rendered parity, contrast/Narrator/scaling and ARM64; see [usage](field-help-tooltips.md) |
| icon-badge | Pending | Pending | Full pinned contract |
| icon-button | `BleeckerIconButton`: three variants and three sizes | Nine registered combinations; x64 dimensions, names/roles, command and state checks; light/dark captures; package fixture; ARM64 build | In progress: physical input, tooltip, Narrator, contrast, scaling, motion and remaining ARM64 state checks; see [usage](icon-buttons.md) |
| loading-spinner | `BleeckerLoadingSpinner`: three sizes, themed three-quarter ring | Registered fixture; x64 geometry, theme, UIA, reduced-motion and lifecycle checks; independent package consumer | In progress: normal/live motion, contrast, Narrator, scaling/composition, remaining ARM64 state checks; see [usage](loading-spinner.md) |
| media-library | Pending | Pending | Full pinned contract |
| modal | Pending | Pending | Full pinned contract |
| nav-menu | Pending | Pending | Full pinned contract |
| pagination | Pending | Pending | Full pinned contract |
| collection-filters | Pending | Pending | Full pinned contract |
| command-spotlight | Pending | Pending | Full pinned contract |
| select | Pending | Pending | Full pinned contract |
| file-input | Pending | Pending | Full pinned contract |
| section-header | `BleeckerSectionHeader`: title, optional eyebrow/description and native level-two heading | Registered 24-case frozen layout and live binding/lifecycle/keyboard fixture; independent consumer | In progress: [source contract and evidence](section-header-reference.md); exact text raster/balancing, complete overrides, contrast/Narrator/scaling and full platform acceptance |
| panel | Pending | Pending | Full pinned contract |
| status-badge | `BleeckerStatusBadge`: five variants, consumer label/description and constrained reflow | Registered palette/layout/automation and live binding fixtures; independent consumer | In progress: [contract and evidence](status-badge.md); complete shaping/gamut/raster, contrast/Narrator/scaling and ARM64 acceptance |
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
- x64 and ARM64 builds and runtime evidence are separate requirements. The registered fixture/runtime baseline passes on both architectures; full platform acceptance remains open as described in [runtime evidence](runtime-evidence.md).
- Accessibility requires UI Automation plus Narrator and OS contrast verification; performance budgets must be set before large-data implementation and measured with hardware/OS/workload evidence.
- Independent package consumer, versioned NuGet and runnable catalog, matching documentation, remote main and required CI are still required.

The GitHub wiki could not be retrieved. Documentation remains in this repository. Thompson is a supplementary contract reference; its touch-specific behavior does not override desktop Windows conventions. Sabella television/mobile-specific types are outside this Windows scope. Web providers and implementation utilities map to native infrastructure rather than separate visual controls. No networking, CMS schemas or product workflow logic enters Orchard.

