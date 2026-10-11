# Architecture and verification

## Platform ownership

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
palette. High contrast resource completeness is tested; OS-level high contrast
switching still needs a Windows accessibility session.

Initial explicit styles retain WinUI’s native control templates, keyboard
behavior, focus handling, automation peers, and button state machines. Button
normal/hover/pressed/disabled brushes use the Bleecker palette; state opacity
is a Windows adaptation documented in the generator. Input styles provide
the baseline font, geometry, and surface. Full input/flyout state customization,
menus, tables, rich editors, notifications, navigation, and newsroom-specific
components are subsequent component work.

Use `ThemeResource` for color consumers and `StaticResource` for typography and
geometry. Load `XamlControlsResources` before Orchard resources. Font files
are copied to `Orchard/Assets/Fonts` in the consuming app output.

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

The GitHub workflow builds on `windows-2025`, runs MSTest contracts, publishes
the complete self-contained x64 catalog, and launches it with
`--smoke-output`. The catalog checks theme colors, font selection and presence,
native button invocation, input, disabled state, layout, and theme reset. It
renders the actual XAML tree using `RenderTargetBitmap` and writes PNGs and a
structured result report, including initialization failures before the window
loads. A render is also captured before theme assertions, so an incorrect
control color leaves visual evidence for investigation. Failure exits nonzero; the workflow never treats a
build as runtime proof.

Reproduce on Windows:

```powershell
dotnet publish samples/Orchard.Catalog/Orchard.Catalog.csproj -c Release -p:Platform=x64 -o artifacts/catalog
./scripts/smoke-catalog.ps1
```

The `orchard-windows-x64` workflow artifact contains the runnable output,
test reports, and native renders. The smoke check exercises automation
providers, not physical mouse/keyboard interaction. Font asset presence and
the selected family URI are checked; pixel-level typeface identity is not
asserted. This scaffold gate does not establish live-feed performance,
rich-document editing quality, screen-reader behavior, ARM64 runtime behavior,
or finished component parity.

The public repository’s wiki was unavailable during initialization. Setup and
architecture documentation therefore live in this repository; no wiki remote
or wiki content was fabricated.
