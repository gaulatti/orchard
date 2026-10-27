# BleeckerFeedItem

`BleeckerFeedItem` presents consumer-owned author/avatar metadata, a localized
timestamp, relevance, plain content, language/category tags and a source link.
It composes the existing Avatar and Tooltip components. It adds no feed service,
selection model, filtering, persistence or publishing behavior.

```xml
<orchard:BleeckerFeedItem Author="{Binding Author}"
                         AvatarSource="{Binding Portrait}"
                         Content="{Binding Text}"
                         PostedAt="{Binding PostedAt}"
                         Relevance="{Binding Relevance}"
                         LanguageCode="{Binding Language}"
                         Categories="{Binding Categories}"
                         SourceUrl="{Binding SourceUrl}"
                         SourceCommand="{Binding OpenSource}"
                         SourceCommandParameter="{Binding}" />
```

All metadata properties are native dependency properties. `LanguageCode` is the
displayed language; inherited `Language` retains its native text/shaping role.
`Categories` retains the original non-null sequence of strings, observing its
collection notifications when supplied. Display categories trim ECMAScript
whitespace, use default lowercase and omit empty results; original values stay
unchanged. Duplicate display categories remain present. The constructor provides
an empty collection; a bound collection must resolve before the control mounts.

`Init` supplies the Avatar fallback. Otherwise the first author UTF-16 unit is
uppercased, with `?` for an empty trimmed author. The retained Avatar keeps its
source error latch across metadata/source changes. Relevance follows the source
round-to-nearest rule with ties toward positive infinity and preserves its
nonfinite display strings. Body text uses normal word wrapping and splits an
oversized word at grapheme boundaries; the author stays on one truncated line.

`PostedAt` accepts `DateTimeOffset` or a timestamp string. A valid timestamp from
zero through twelve hours old uses local hour/minute text; older and future
values use a localized day/month/year. ISO date-only values are UTC and local
date-time strings use the current time zone. Invalid strings display the
source's `Unknown time` label and retain their original tooltip text. The
tooltip otherwise carries the complete ISO UTC timestamp. As in the source,
formatting refreshes with component data/render updates. No periodic clock is
started. Pinned tests cover en-US, en-GB and pt-BR at the twelve-hour boundary.
Full ECMAScript legacy/extended-date parsing and other locale/calendar cases
remain acceptance gaps.

The link displays the parsed host or the original source's `Source article`
label for an invalid URL. Activating it runs `SourceCommand` with the original
parameter, then raises `SourceRequested` with the original source string. The
consumer routes or opens that destination. A rejected command, disabled control,
removed template or detached control emits no request. The passive row adds no
Tab stop; its source link has the native Hyperlink role, focus visuals and
original URL help metadata. Caller naming on the row remains native.

The Feed items catalog fixture uses fictional local metadata and records source
requests locally. Acceptance requires source layout/typography, long-word and
category wrapping, RTL, themed and hover paint, the original timestamp tooltip,
retained data/parts, image failure, command/request ownership, keyboard/focus,
visible raster, motion, actual scaling, native ARM64 and package integration.
The browser reference has 100 states, 64 full screenshots, 21 timestamp cases
and both actual timestamp tooltip bodies. Hidden native diagnostics use an
internal fixed clock; they do not change Windows time or establish visible
rendering/input. The component remains an in-progress preview.

Foreground x64 catalog input confirms pointer source requests use the original
URL, including an empty invalid URL, and keyboard Enter invokes the focused
source link. When a Feed Item is composed in a Feed Column, the column's dark
surface requests dark theme tokens even in a Light host. This corrects the
previous dark-on-dark text while preserving standalone Feed Item theming.
The visible before/after frames are in `docs/evidence/feed-light-contrast-v1/`.

The previous standalone x64 independent package run matches all 100 source rectangles and
checked paint properties with zero geometry error. Thirty ownership/lifecycle
checks cover original bindings and category collections, retained parts, command
rejection, four original-URL requests, Avatar's image error latch, caller names,
template/unload/remount resource cleanup and an unbounded native parent. Its
708 guards kept the window invisible without focus or physical input. Forty-three
corrupted receipts are rejected. All 243 portable tests and eight windowless
native font tests pass. The RTL timestamp regression preserves separately
rounded visual fragments; source physical-left link alignment and the physical
`margin-left: auto` relevance placement are retained under RTL.
The fresh `0.1.0-feed-contrast-v2-local` x64 package additionally verifies a
Feed Item inherits readable dark text tokens inside a Feed Column in a Light
host; it does not replace the standalone Feed Item package matrix.
