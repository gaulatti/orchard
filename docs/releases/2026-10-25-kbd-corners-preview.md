# Windows component preview: keyboard keycap corners

Changing a keycap's `CornerRadius` previously changed its border while leaving
the shadow rounded at 4px. `BleeckerKbd` now applies caller radius values and
bindings to the 1px downward shadow, including generated combination keycaps.
Square, asymmetric, overlapping and tiny keycaps update when resized,
retemplated or remounted. The catalog includes square, rounded and asymmetric
examples.

Download the [preview release](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261025.2).
It contains `Gaulatti.Orchard.0.1.0-kbd-corners-v1-local.nupkg` and the runnable
`Gaulatti.Orchard.Catalog.0.1.0-kbd-corners-v1-local-win-x64.zip`.

| Artifact | SHA-256 |
| --- | --- |
| NuGet package | `d75e0274cfecc522dd6bb579954e89d67d61db05d89983aa95a7c432dd6b6176` |
| Catalog archive | `e131e110ff30f987bdce47f2f26facfe55d2d91189ab1de0070caaaa6589a828` |

The source-tested x64/package library has SHA-256
`8abcabf13ce34b76349c34cb9ee828861207f5c821d75fb1a77f5963d75de136`.
The previous published library fails the square-shadow pixel regression. The
fixed source catalog, independent NuGet consumer and archive runtime each pass
32 radius/size/theme/direction states, four combination/template/remount contexts
and 257 isolation guards, with zero focus or activation. The unaffected default
Light/LTR keycap capture is unchanged. Captures retain the actual rendered square
corner pixels; the rounded cases also check the normalized shadow geometry.
Source and independent NuGet consumer builds pass for x64 and ARM64; this is
compilation evidence and does not establish native ARM64 execution.

Every catalog ZIP entry is checked against the frozen app. The replay directory
maps verified ZIP entries to identical independently frozen files, avoiding a
second full runtime copy. The verification and source-input manifests record
the exact binary, source and test identities.

Unchanged component evidence retains its original source/binary scope. The
[preceding Skeleton preview](2026-10-25-skeleton-content-preview.md) records its
496 portable tests and fictional Monitoring/Editorial demo checks; these are
not claimed as fresh runs of this package. Both demos remain included in the
catalog. Hosted Actions remain suppressed at the user's request.

Native ARM64 execution, actual display/text scaling, complete browser/native
visual review and final component acceptance remain open. No OS settings,
Narrator or physical input are used by the focused corner checks. The inventory
remains 112 entries started/in progress, zero pending and zero accepted, with
both required demos in progress. These are inventory counts, not a completion
estimate.
