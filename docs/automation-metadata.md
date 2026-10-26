# Caller-owned automation metadata

Avatar, Label, Eyebrow, Menu Label, Metric, Stat Card, Error State, Page Header
and Footer now preserve caller automation names before generating a default.
This matches the established Kbd and Status Badge contract.

`AutomationProperties.Name` may be a nonempty or empty string, a live binding,
or a value supplied by an explicit, BasedOn or ancestor implicit style. Clearing
an override reveals the next applicable value, then the existing generated name.
An associated label supplies its own name when no caller Name overrides it;
an empty associated label remains empty. Template replacement and remounting
must retain caller bindings. Error State also preserves empty caller HelpText
values and bindings instead of restoring its Description.

```xml
<orchard:BleeckerLabel Content="Display text" AutomationProperties.Name="" />
<orchard:BleeckerErrorState Title="Unavailable"
    Description="Fictional local error" AutomationProperties.HelpText="" />
```

The internal helper only resolves caller metadata. Each component keeps its
existing generated name, role and rendering behavior. Components with an
intentional decorative role keep their existing role rules.

The shared `--probe-hidden-display-metadata` fixture exercises eleven controls
in both themes: 352 name cases and 20 help-text cases, including the unchanged
Kbd and Status Badge behavior. Its expected generated names come from the
retained pre-repair native baseline. The baseline contains 142 name failures
across nine controls and six Error State HelpText failures. The Footer fixture
supplies the documented required Brand model.

```powershell
./scripts/verify-display-metadata.ps1 -AppDirectory <frozen-app> -Output <fresh-output>
```

These checks use hidden native windows and real automation peers, without
hardware input, Narrator or OS setting changes. They establish the scoped
metadata contract; they do not establish complete component visual, interaction,
scaling or native ARM64 acceptance.
