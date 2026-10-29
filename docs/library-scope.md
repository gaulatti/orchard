# Orchard component-library scope

User-confirmed scope, 2026-10-18:

Complete Orchard exclusively as a reusable C# / WinUI 3 UI component library.
Implement the fixed Bleecker/Sabella Windows component inventory, public APIs,
tokens and themes, component interaction and visual behavior, catalog fixtures,
regression coverage, usage documentation, and independently consumable NuGet and
runnable catalog artifacts. Keep verification focused on these library outcomes.

The monitoring and editorial catalog screens remain required demonstrations of
the components. They use fictional local data and illustrate composition,
selection, filtering, editing controls, and other library interactions. Their
appearance as application screens does not put these demos outside scope.

Work outside the component library and its demonstrations is forbidden. Do not
build newsroom/CMS products, backend services, product APIs, authentication,
production publishing workflows, or unrelated platform implementations.

Narrator and Windows/OS high-contrast test campaigns are excluded. Do not launch
Narrator or change system accessibility settings for this work. Existing public
component accessibility metadata and theme support remain intact. Older
acceptance lists and historical reports mentioning those campaigns do not
reinstate them as completion requirements.

The user resumed component-library work on 2026-10-18. The pointer heartbeat
stays paused; the resumption does not restart it or authorize Narrator or OS
contrast testing.

The saved Codex goal is active. The available goal API can change status but
cannot edit an existing objective, so its old wording has not been replaced.
This user correction governs the work and must be carried into any resumed goal.

The 2026-10-29 milestone follows the [component-library acceptance contract](acceptance-contract.md):
x64 runtime acceptance only, ARM64 build-only and runtime-unverified, and no
raster/pixel parity gate. Each component receives pass/fail results for Light
and Dark state rendering, keyboard/pointer/focus, 150% scaling without clipping,
reduced motion, and package-consumer compilation. No new evidence folders are
needed. The release lists accepted components explicitly.

The user requires an explicit check against overengineering (2026-10-24).
Before adding implementation, abstractions or verification, identify the
concrete component requirement or observed defect it addresses. Prefer the
smallest complete repair and focused regression coverage, reuse valid evidence
for unaffected paths, and batch package/catalog qualification at meaningful
milestones. Further investigation needs a plausible effect on a required
observable outcome. Preserve existing failures and evidence; byte-identical
raster research is not a public-component delivery prerequisite.
