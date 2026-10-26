# Constrained rich content

`--probe-hidden-rich-content-layout` adds focused Metric and Stat Card coverage
using the existing published library. It changes no component implementation,
public API, token or rendering algorithm.

Metric has 60 cases across 72, 140 and 320 DIP widths, both themes and both text
directions. Both rich prefix and suffix are present. Initial, long, empty and
restored values plus live child-size changes must retain the original children
and bindings. The actual slot hosts must align with their native inline spacers
within 0.05 DIP. The retained source run has zero placement error.

Stat Card has 60 corresponding cases with original rich value, icon and footer
elements. Long title, summary and child-size changes must preserve their state,
visual ancestry and mounted lifecycle. These checks do not compare source
wrapping, clipping or complete rendered glyphs.

Lifecycle observers are attached before mounting. The fixture records two
initial Metric child loads and three Stat Card child loads, then requires zero
additional load/unload events during updates. It also checks actual visual-tree
ancestry and loaded roots, owners and children. Earlier diagnostic runs attached
observers after mounting and reported false child IsLoaded values; those runs
remain retained and do not establish a component failure.

```powershell
./scripts/verify-rich-content-layout.ps1 -AppDirectory <frozen-app> -Output <fresh-output>
```

The fixture uses an owned hidden native XAML island and no hardware input,
activation or OS setting changes. Native checks are x64; architecture builds
are recorded separately. The published library package remains unchanged.
The new command is available in current source and will enter the downloadable
catalog at the next component-delivery milestone. Existing public catalog
artifacts must not be described as containing this command yet.
