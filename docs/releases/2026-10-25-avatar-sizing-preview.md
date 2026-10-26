# Windows component preview: Avatar caller sizing

Avatar template application replaced caller width/height/font-size bindings:
a requested 74×42 avatar with 17px text became 40×40 with 14px text and lost all
three bindings. `BleeckerAvatar` now supplies size defaults without replacing
caller values, bindings or explicit/BasedOn/implicit styles. Clearing an override
restores the applicable style or current size recipe. The empty silhouette's
viewport uses 60% of each actual dimension, including non-square caller bounds.
The catalog includes bound sizing/font examples and an empty non-square avatar.

Download the [preview release](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261025.4).

| Artifact | SHA-256 |
| --- | --- |
| `Gaulatti.Orchard.0.1.0-avatar-sizing-v1-local.nupkg` | `f1d2c3c2fc3b76b1f6a30b756ee5ee68b705689cd4d8d60c0ac546fbf24a6be8` |
| `Gaulatti.Orchard.Catalog.0.1.0-avatar-sizing-v1-local-win-x64.zip` | `ec8a80c63e8bd34c4c4d61793aea849257c28c8079734fba1ce6838a49e10f8a` |

The source-tested x64/package library has SHA-256
`f24689be249bf6aa4b166b98eaf6db6f3ed518bc93c38de08ab6e595fbfb509f`.
The published preceding package fails the binding-loss regression. Focused
qualification covers all five default sizes, initials/silhouettes, caller
bindings and equal-value locals, explicit/BasedOn/implicit styles, updates,
clearing, template replacement/remounts, and retained default/styled group
positions. The ten retained Light/LTR default images are unchanged. Scoped
results and architecture builds are recorded separately in verification.json.

The current source, exact NuGet consumer and archive runtime each pass280 sizing
states and32 caller/group lifecycle contracts in both themes and directions.
Each run records1,757 isolation guards with zero focus or activation. Source
and independent NuGet consumer compilation pass for x64 and ARM64; native ARM64
execution remains unverified for this repair.

Every ZIP entry is verified against the frozen app. The replay directory maps
verified ZIP entries to identical independently frozen files, avoiding another
full runtime copy. Source-input and verification manifests record exact identities.
Unchanged component/demo suites retain their previous source and binary scope;
they are not reported as fresh runs of this package. Both fictional Monitoring
and Editorial demos remain included. Hosted Actions remain suppressed.

Native ARM64 execution, actual display/text scaling, complete visual/image
acceptance and remaining component-library acceptance stay open. No OS settings
or physical input are used by the focused tests. Narrator and OS contrast
campaigns are excluded. The inventory remains112 entries started/in progress,
zero pending and zero accepted, with both required demos in progress. These
counts are not a completion estimate.
