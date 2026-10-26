# Windows component preview: caller-owned display metadata

Avatar, Label, Eyebrow, Menu Label, Metric, Stat Card, Error State, Page Header
and Footer now preserve caller automation names before generating defaults.
Empty values, live bindings, explicit/BasedOn/ancestor styles and associated
labels remain authoritative. Clearing an override restores the applicable name.
Error State also preserves caller-empty HelpText instead of restoring Description.
Template replacement and remounting retain caller bindings. Generated defaults
and rendering algorithms are unchanged.

Download the [preview release](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261026.4).

| Artifact | SHA-256 |
| --- | --- |
| `Gaulatti.Orchard.0.1.0-display-metadata-v1-local.nupkg` | `12fd24bee6f65b3606289cea9b08128efa86e6f42f01880619edf542e51f0aef` |
| `Gaulatti.Orchard.Catalog.0.1.0-display-metadata-v1-local-win-x64.zip` | `9990fd50911fbf73165a8d9eb75ddbe92d58721d35385493c6842eea1693059c` |

Library SHA-256:
`5cc92ba21798ae61014c639207784fe0ac034a5abb8408b1dc005c0ff30bf4fa`.
Source, exact independent NuGet and catalog archive hosts each pass 352 name and
20 help-text cases across eleven controls in both themes. Kbd and Status Badge
serve as unchanged guard cases. All 22 original generated name/help defaults and
layout measurements are retained; this is not a pixel-equality claim. The
pre-repair baseline records 142 name failures across nine controls and six Error
State HelpText failures. An earlier incomplete test omitted Footer's required
Brand; that setup failure is retained separately, and the final fixture supplies
the documented input.

Source and independent 160-file consumers compile for x64 and ARM64. Runtime
checks are hidden x64 runs with no hardware input, activation or OS setting
changes. These results establish the scoped metadata behavior. Native ARM64
execution of the repair, remaining interactions, scaling, full visual review and
performance acceptance stay open. Unrelated suites retain their original source
and binary identities. Strict historical raster failures remain recorded.

All 550 catalog ZIP entries are verified. Runtime replay maps entries to
byte-identical independently frozen files instead of duplicating the full
extraction. Both fictional Monitoring and Editorial component demos remain
included. Actions are suppressed during publication and prior settings restored.
The wiki is unavailable; documentation is maintained in the repository.

The inventory remains 112 entries started/in progress, zero pending and zero
accepted; both required demos remain in progress. These are not completion
estimates. Narrator and OS contrast campaigns remain excluded.
