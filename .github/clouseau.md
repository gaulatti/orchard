# Orchard review contract

Orchard is Gaulatti's native Windows component library, built with C# and
WinUI 3. It implements Bleecker's visual language for Windows (Sabella owns
Apple platforms and Avelino owns Linux) and ships generated tokens, light/dark
and high-contrast themes, and a runnable Visual Studio catalog. This repository
is the component library only.

Preserve the generated token and palette outputs (their `--check` modes must
stay reproducible), the frozen reference evidence and its source audits, the
x64 and ARM64 build and package-consumer contracts, accessibility and high
contrast behaviour, and the component-library scope recorded in `AGENTS.md`.
Bug fixes, meaningful tests, accessibility corrections, documentation and
dependency maintenance serve this existing library without a new product
decision. A new component, a new target platform or a new visual exception
needs documentation or a linked requirement that establishes its need and
limits.

Assess purpose and evidence rather than the number of changed files or lines.
Ask gaulatti for a second opinion for a product-direction or authority decision.
Ordinary implementation uncertainty and repairable bugs belong to automated
investigation, not routine human code review. Changes to review controls (this
contract, `.github/clouseau.json`, the CI workflow, the local recipe) require
explicit owner confirmation.

Every material capability and mechanism must independently score at least 80
for fit, need and proportionality under Clouseau's shared protocol. A causal
technical finding at or above 70 blocks; unrelated features or good scores
cannot average away a defect. Integrity and review-completeness gates remain
independent.

Tests must assert actual behaviour. Reject fabricated evidence, disabled tests,
coverage pragmas or exclusions, swallowed errors and unconditional assertions.
The token checks, the Windows x64 build, tests, package consumer and catalog
verification, and the ARM64 build and runtime checks remain mandatory. The shared
coverage gate cannot measure .NET: it has no Cobertura format. The policy
therefore selects `manual` merge mode and the owner merges every pull request.
