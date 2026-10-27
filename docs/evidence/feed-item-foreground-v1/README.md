# Feed Item timestamp and source-link hover

On 2026-10-27, physical hover in the source x64 catalog displayed the original
ISO timestamp in Orchard's tooltip. Hovering the source link then made its text
disappear on the dark Feed Column surface. The repaired component keeps the sea
accent and underlines the link during hover in both Dark and Light host themes.

- [Timestamp tooltip in Dark](timestamp-dark.jpg)
- [Disappearing link before](before-hover-dark.jpg) and [readable link after](after-hover-dark.jpg)
- [Readable link in a Light host](after-hover-light-host.jpg)

The hover checks did not invoke a source request. A subsequent physical click
on the hovered link passed the original fictional URL to the caller's status
handler. The fixture uses only local fictional data.
