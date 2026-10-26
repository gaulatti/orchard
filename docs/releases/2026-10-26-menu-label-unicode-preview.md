# Windows component preview: Menu Label Unicode casing

Menu Label's uppercase presentation now handles the full Unicode scalar range,
including characters outside the Basic Multilingual Plane. The source text and
caller-owned display metadata remain unchanged. This preview also carries the
earlier component-library repairs and the fictional Monitoring and Editorial
catalog demos.

Download the [preview release](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261026.5).

| Artifact | SHA-256 |
| --- | --- |
| `Gaulatti.Orchard.0.1.0-menu-label-unicode-v1-local.nupkg` | `70517d2696baad7b4c6d6ff1a34a1aa2e43683215a688c0f437ef431b929eea7` |
| `Gaulatti.Orchard.Catalog.0.1.0-menu-label-unicode-v1-local-win-x64.zip` | `59f93673d5661d8055cdd1ffeb1ae092b20246676ade51f5cb85183142ac6934` |
| `Gaulatti.Orchard.Catalog.0.1.0-menu-label-unicode-v1-local-win-ARM64.zip` | `fd25b6a69806581188fcce3452d8f486a05828afdb36f053033b936a93f9067a` |

The exact NuGet DLL passed the isolated x64 Menu Label cases, and its independent
consumer built for x64 and ARM64. The published x64 catalog passed 352 automation
name cases, 20 help-text cases, six Unicode Menu Label cases, 16 title-casing
checks, and 389 guard checks across eleven controls in light and dark themes.
Its hidden Monitoring and Editorial demo fixtures passed 66 fictional-data
checks. Both catalog publishes compiled 114 XBF files. Streaming verification
matched every ZIP entry to its published file: 664 x64 and 660 ARM64 entries.
These catalog checks used the published directory; extracted-archive native
replay and native ARM64 execution have not run. The checks did not use hardware
input or alter OS settings. These results do not establish full visual or
component acceptance.

The inventory remains 112 entries started/in progress and zero accepted. This
is an entries-started count, not a completion percentage. The wiki remote is
unavailable, so this release is documented in the repository. GitHub Actions
push runs are paused in the current workflow.
