# AuthShell container and root-size contract

This extends [AuthShell's contract](auth-shell.md) using the same pinned producer,
without changing the original 104-state reference. The new frozen
`tests/Reference/AuthShellLayout.json` SHA-256 is
`33e1c67660f4ff29b82372b3e4e696b2f3bd8b16061b3aa57c05ca0abcfe8cbd`.
It retains 132 original screenshots, 264 stable samples and eight histories.
Independent recapture matches every PNG and metadata field without exclusions.
The source auditor checks 7,072 geometry values and 212 actual pixel probes and
rejects 75 damaged examples. An RTL component is embedded in an LTR page host;
the component's own logical layout still follows its explicit direction.

Acceptance requires the native layout to preserve the producer's measured
geometry when the container width differs from the viewport, including
overflow. Responsive padding and aside visibility follow viewport breakpoints.
Grid fractions follow the container's content width. A zero-width access grid
track does not erase the section's horizontal padding: the actual access box
is at least twice that padding. The aside still paints above access content
where their boxes overlap. Start/end and RTL affect positions independently of
those minimum sizes.

For example, with a 1440px viewport, a 300px container, split/start layout and
16px root text, the source uses grid tracks of 384px and zero. Its access
section starts at x384 and is 128px wide because its two 64px paddings remain.
At 32px root text, the aside minimum becomes 768px and access retains 256px.
The corresponding RTL access boxes can extend to negative physical positions.

A centered shell is a block layout. Setting its height or minimum height above
the viewport leaves extra main background below the naturally sized access
section; it does not stretch that section. A split shell uses a grid, so its
row stretches to fill a taller main content box. In either layout, an explicit
short main height with a cleared minimum does not shrink the access section's
own viewport minimum; it overflows. Root padding and borders inset the child
layout and participate in outer sizing.

Histories verify retained caller editors while changing container width,
height, padding, border, side, direction and viewport breakpoint. Native
implementation now passes all 132 states and 32 behavior checks in the source
host, with 7,688 geometry/font checks and 212 actual palette probes. The native
auditor rejects 82 damaged cases. All 474 portable tests pass. The original
104-state/24-check suite also passes with the corrected component. Five new
portable regressions check frozen input integrity, padding overflow, block/grid
height differences, original editor state and invalid dimensions.

The default native style now uses left/top alignment so an explicit width
keeps the producer's block position. Caller alignment overrides remain
available. The registered **Auth shells** fixture includes a 300px split
container, a taller centered shell and a root padding/border example.

An exact PNG regression comparison against the previous source preview retains
76 differing pixels in one dark content-title image; all other 103 PNGs match
byte for byte. This reproduces the previously recorded small package-image
variation in a source-to-source comparison. Its rendering cause is still
unproven. The new 132-state browser/native image gate also fails: 1,594,803
pixels, maximum channel difference 250. Allowed channel differences remain
zero, and no original screenshot was changed.

The `0.1.0-auth-shell-layout-v1-local` preview passes the expanded 132-state and
32-check suite in source, a fresh x64 NuGet consumer and the extracted catalog.
All state and contract data agree, and all 132 component PNGs are byte-identical
between source/package and source/archive at zero pixel tolerance. Source and
package auditors each reject 82 damaged cases. Both architecture solution and
fresh isolated consumer builds pass. Both new consumer temporary roots were
removed while preserving shared dependencies. Both fictional component demos
pass in source and extracted catalog; all 1,360 archive files are byte verified.
All 474 portable tests and 10 Windows text tests pass.

The original 104-state/24-check suite remains a separate regression boundary
and passes in source, the fresh package and extracted archive. Its state and
contract data agree across hosts; its strict image comparisons are recorded
separately from the new suite. These checks do not establish physical input, OS display/text scaling,
performance or full visual acceptance, and do not change the remaining
library-wide acceptance gaps.
