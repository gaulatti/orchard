# Orchard group-language preview

This local preview packages the reusable WinUI component library and runnable
catalog from source commit `ae539bccbe9dc90de46494f5d471ae06d6d344f3`.
Command Spotlight now displays caller-owned group headings with its live
`Language`; changing `istanbul` from `tr-TR` to `en-US` changes the heading from
`İSTANBUL` to `ISTANBUL` without changing the group value or automation name.

| Artifact under `artifacts/current` | SHA-256 |
| --- | --- |
| `Gaulatti.Orchard.0.1.0-group-language-v1-local.nupkg` | `9f0c17f709433a8be223737081bdb1d844602d185bcebccdafd8d9fe597b7fc2` |
| `Gaulatti.Orchard.Catalog.0.1.0-group-language-v1-local-win-x64.zip` | `bb5fafd0d2c30a87340ec519347f39e87e85c9229dc741c70c707661cbfac6ec` |
| `Gaulatti.Orchard.Catalog.0.1.0-group-language-v1-local-win-ARM64.zip` | `378a81263b4358cd3a38c6469d7b7178cd4cd59db8fbe9283061d2565ff92252` |

The x64 and ARM64 catalog publishes each contain 114 compiled XAML fixtures.
Extraction verified all 664 x64 and 660 ARM64 files byte for byte. The extracted
x64 catalog passed 66 hidden Monitoring and Editorial demo checks, validated
all 653 embedded reference resources, and passed the Command Spotlight native
audit with 100 source states and 24 interaction contracts. A fresh independent
x64 NuGet consumer passed the same 24 contracts; the ARM64 catalog built and
its apphost identifies as ARM64. The portable casing tests passed 10/10.

These are preview artifacts. Foreground catalog use, native ARM64 execution and
full component acceptance remain unverified. The inventory remains 112 entries
started and zero accepted; entries started is not a completion percentage.
