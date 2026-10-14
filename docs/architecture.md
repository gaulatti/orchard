# Architecture and verification

## Platform ownership

The catalog smoke process has a six-minute overall deadline for the registered
Light/Dark suite, including the expanded physical Input corner matrix and shared
cursor lifecycle checks. Per-state settling deadlines and architecture gates
remain unchanged. A deadline expiry terminates that owned test process and fails
verification; it does not count as runtime acceptance.

The shared native-input session and Switch fixture invoke hosted welcome-window
preparation only when `CI=true`. Local runs acquire the catalog's own window
without invoking that CI-only operation. The preparation helper still rejects
non-CI callers; this keeps its foreign-window mutation restricted to isolated
hosted runners. All foreground, hit-target and actual-input checks remain active
locally.
The independent package consumer uses the same transient Alt press/release
when Windows rejects its initial foreground request. Its two-second acquisition
deadline includes that preparation. It records the actual foreground window
before and after, and still refuses component keyboard input until its own
visible window owns foreground input.
Composed captures likewise work locally, but require the foreground window to
belong to the running catalog process, remain entirely on its monitor and pass
the existing occlusion checks. They cannot capture another application's window.

Orchard is the Windows implementation of Gaulatti’s Bleecker design language.
It uses WinUI 3 and .NET, not an embedded browser. Sabella is the Apple library;
Linux has a separate future implementation. Shared branding and design tokens
do not imply shared platform control code.

The reusable library has no newsroom feature state, API clients, Cognito
configuration, credentials, SSE subscriptions, or application navigation.
Those belong to the future Windows newsroom application. The catalog is a
development executable and is not another user-facing newsroom product.

## Pinned foundation

| Dependency | Pin / owner |
| --- | --- |
| .NET SDK | `10.0.401`, `global.json` |
| Windows App SDK | `2.5.1`, `Directory.Packages.props` |
| Windows SDK build tools | `10.0.26100.7175`, `Directory.Packages.props` |
| MSTest | `4.5.1`, `Directory.Packages.props` |
| Bleecker tokens | Exact source commit/hash in `tokens/source-lock.json` |

The projects originated from Microsoft’s WinUI C# app/library templates
(`Microsoft.WindowsAppSDK.WinUI.CSharp.Templates` `0.0.7-alpha`). The template
package is a generation tool, not a runtime dependency; Orchard uses the
stable Windows App SDK pin above. The catalog replaces the template’s generic
page, assets, and packaging configuration.

## Token and control contracts

`scripts/generate-tokens.mjs` transforms the pinned platform-neutral source
into JSON, immutable C# dictionaries, and XAML resources. It verifies the
source hash and rejects unsupported color representations. Unknown color
schemes throw; no alternate palette is substituted.

Light maps to Bleecker’s light palette. WinUI’s `Default` theme dictionary
contains Bleecker’s dark palette. High contrast maps semantic resources to
Windows accessibility colors, including highlight and highlight text for
primary actions. Brand colors must not override the user’s high contrast
palette. Generated button styles and the foundation heading/body/button styles
disable WinUI's automatic text recoloring with `HighContrastAdjustment=None`
because their resources already supply system-aware pairs. This prevents a
primary-action label's mapped HighlightText foreground from being replaced by
white text on a dark backing. Resource completeness is tested; the live native
transition and paint verification is tracked in [contrast verification](contrast-verification.md).

Initial explicit styles retain WinUI’s native control templates, keyboard
behavior, focus handling, automation peers, and button state machines. Button
normal/hover/pressed/disabled brushes use the Bleecker palette; state opacity
is a Windows adaptation documented in the generator. Input styles provide
the baseline font, geometry, and surface. Full input/flyout state customization,
menus, tables, rich editors, notifications, navigation, and newsroom-specific
components are subsequent component work.

Primary and secondary button styles explicitly bind Background, Foreground and
BorderBrush to the generated theme resources. Inheriting these setters from the
SDK style can resolve the normal-state brushes in the SDK dictionary scope and
retain Windows accent colors. The native templates and their state machines remain
inherited; the published runtime regression checks the actual content presenter’s
normal, hover, pressed and disabled color/opacity in light and dark appearances.
These checks exercise visual states programmatically; physical input, Narrator and
Windows contrast verification remain separate acceptance requirements.

Use `ThemeResource` for color consumers and `StaticResource` for typography and
geometry. Load `XamlControlsResources` before Orchard resources. Font files
are copied to `Orchard/Assets/Fonts` in the consuming app output.

Disabled Input and Checkbox pointer behavior uses an internal observer shared
per XAML root. It queries the native pointer and performs at most one root hit
test per observation, then applies each control's own bounds, visibility, ancestor
clipping and occlusion. Checkbox supplies its rounded 20px box and uses the
browser's 1×1 pointer sample; Input currently supplies its control bounds.
Each control detaches on enable/unload, and Checkbox also detaches on
template replacement. One thread-local cursor owner prevents competing controls
from restoring stale overrides. The displayed override belongs to the root's
InputPointerSource, rather than the UI thread's Win32 cursor cache. Each root
retains cursor objects published by WinUI while excluding its own override
object, so release can restore the latest producer selection, including resource
cursors. Stationary enabling selects the control's explicit cursor. Routed
pointer observations coalesce one queued refresh after native cursor dispatch;
layout/render observations continue to handle stationary geometry changes.
Physical cursor gates require GetCursorInfo's visible global cursor handle.

The catalog keeps `EnableMsixTooling=true` and explicitly enables
`AppxGeneratePriEnabled` for the SDK’s application resource-index build
pipeline. `WindowsPackageType=None` still makes the app
unpackaged. Removing the tooling property can leave a compiling executable
without its application resources; CI explicitly requires the published
`resources.pri` before attempting a launch.

The catalog explicitly names its index `resources.pri`, omits the library-style
initial path prefix, and uses the SDK's default embedding of compiled XAML in
that index. Disabling XBF embedding creates references to loose files that the
unpackaged publish pipeline does not copy. These SDK properties keep
`ms-appx:///App.xaml` and the
catalog’s resource paths aligned with the unpackaged runtime lookup contract.

## Scaffold acceptance criteria

1. `Orchard.sln` contains a reusable library, a runnable catalog startup project,
   and tests, with Debug/Release x64 and ARM64 configurations.
2. A clean Windows restore/build compiles the solution without sibling
   repositories, credentials, or production services.
3. C# and XAML reproduce the pinned Bleecker palettes, geometry, and fonts;
   generated outputs cannot drift unnoticed.
4. The native catalog starts, resolves Orchard resource dictionaries and font
   assets, lays out controls, responds to button automation, accepts input,
   and changes between light, dark, and system appearance.
5. Runtime captures produce nonblank light/dark PNGs for visual inspection.
6. Setup, consumer resource paths, provenance, and verification limits are
   documented alongside the code.

## Verification

`samples/Orchard.Catalog/catalog-coverage.json` registers library styles and
public controls against catalog XAML fixtures. Contract tests discover styles
and public class declarations from library sources, compare the registration
sets, and require each registered fixture to actually reference/instantiate its
component. Registration proves fixture presence; it does not establish variant,
interaction or visual acceptance. Those remain tracked in the parity inventory.

The GitHub workflow builds on `windows-2025`, runs MSTest contracts, publishes
the complete self-contained x64 catalog, and launches it with
`--smoke-output`. The catalog checks theme colors, font selection and presence,
native button invocation, input, disabled state, layout, and theme reset. It
renders the actual XAML tree using `RenderTargetBitmap` and writes PNGs and a
structured result report, including initialization failures before the window
loads. A render is also captured before theme assertions, so an incorrect
control color leaves visual evidence for investigation. Failure exits nonzero; the workflow never treats a
build as runtime proof.

The `arm64-runtime` job uses GitHub's native `windows-11-arm` runner, publishes
the ARM64 catalog and runs the same fixture matrix plus the independent ARM64
package consumer. Runtime reports contain process architecture, OS architecture,
Windows/.NET versions and catalog display/text scale and animation preference.
`smoke-catalog.ps1 -ExpectedArchitecture Arm64` requires both process and OS to
be ARM64; the x64 job similarly requires X64. The package verifier requires the
selected process architecture and requires an ARM64 OS for ARM64 runtime work.
The separate `arm64-build` job retains cross-compilation coverage on x64.
GitHub documents the runner and free public-repository availability in its
[hosted-runner reference](https://docs.github.com/en/actions/reference/runners/github-hosted-runners).
The first native ARM64 runtime run and the repeat with fractional Progress
geometry passed the catalog and independent consumer. Reports prove native
process/OS architecture; see [runtime evidence](runtime-evidence.md) for the
retained captures, hashes and remaining platform acceptance.

Reproduce on Windows:

```powershell
dotnet publish samples/Orchard.Catalog/Orchard.Catalog.csproj -c Release -p:Platform=x64 -o artifacts/catalog
./scripts/smoke-catalog.ps1
```

The full catalog process has a six-minute timeout. Run motion sampling without
concurrent compilation; an overloaded UI thread can miss an entire short
transition. Field-help checks retain intermediate-frame assertions and write
per-frame color/scale evidence before requesting static captures.

The `orchard-windows-x64` workflow artifact contains the runnable output,
test reports, and native renders. The smoke check exercises automation
providers, not physical mouse/keyboard interaction. Font asset presence and
the selected family URI are checked; pixel-level typeface identity is not
asserted. This scaffold gate does not establish live-feed performance,
rich-document editing quality, screen-reader behavior,
or finished component parity.

## Independent package verification

`dotnet pack` includes the compiled library/resource index, XAML, one copy of each
bundled font, OFL licenses, Bleecker attribution and a package README.
The managed library targets AnyCPU while the applications select x64 or ARM64.
The package verifier inspects the PE header and rejects an architecture-specific
library in the common NuGet `lib` directory.
`buildTransitive/Gaulatti.Orchard.targets` copies font assets to the same
`Orchard/Assets/Fonts` URI paths for package consumers as project consumers.

`scripts/verify-package.ps1` checks the archive, creates a new consumer outside
the checkout, uses a fresh isolated NuGet cache, restores the exact package via
source mapping and verifies its SHA-256. The consumer has its own pinned SDK
configuration and package references. It publishes and runs a small native shell
with all six foundation styles, tests light/dark colors and button invocation,
checks bundled fonts/licenses, and captures native renders. CI retains the
package, runnable consumer and reports under `artifacts`. Each invocation removes
its own temporary source, feed, build workspace and isolated package cache in a
`finally` block, including restore/build failures and `-BuildOnly` returns.
`cleanup.json` records the generated workspace path and confirms its removal;
that path is diagnostic metadata, not a retained checkout. Cleanup refuses paths
outside the exact generated child of the temporary directory and reparse-point
roots. A cleanup failure fails verification rather than silently accumulating
another cache. Package isolation, source mapping, SHA-256, architecture and native
runtime checks remain required.
Successful `result.json` reports are written only after cleanup has completed;
a cleanup failure cannot leave a new successful verification report.
`scripts/verify-package-cleanup.ps1` also invokes the real verifier with a
forbidden project reference and a malformed project that fails `dotnet restore`.
Both must propagate their expected failure, retain a cleanup report, leave no
temporary consumer directory and produce no successful result. These regressions
run after package verification in all three Windows CI jobs.
Local failure regressions pass in `artifacts/package-cleanup-regression`; the
fresh ARM64 `-BuildOnly` consumer also publishes successfully and removes its
cache in `artifacts/package-consumer-cache-cleanup-buildonly`. The local native
x64 consumer restores and publishes, then fails its existing foreground guard
while Windows Terminal remains foreground. Its `cleanup.json` still confirms
removal, and the failed runtime report remains in
`artifacts/package-consumer-cache-cleanup`. No foreground assertion is relaxed.
CI also builds the solution and clean package consumer for ARM64 on its x64
runner. `-Platform ARM64 -BuildOnly` records `runtimeVerified: false`; it never
counts cross-compilation as ARM64 runtime acceptance. Run the same command
without `-BuildOnly` in a Windows ARM64 session to exercise that runtime.
The native ARM64 workflow runs this command without `-BuildOnly` and retains
the runnable catalog, consumer, package, captures and reports in the separate
`orchard-windows-arm64-runtime` artifact. These runtime checks cover the
implemented fixtures; they do not establish full visual or platform acceptance.

This verifies the existing foundation through a package boundary. The consumer
will need the completed Orchard application shell and additional representative
components as the inventory is implemented. It does not prove full parity,
Narrator acceptance or a published NuGet feed release.

The public repository’s wiki was unavailable during initialization. Setup and
architecture documentation therefore live in this repository; no wiki remote
or wiki content was fabricated.
