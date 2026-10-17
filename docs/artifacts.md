# Local artifact storage

Reuse the normal project publish directories for local iterations:

```powershell
dotnet publish samples/Orchard.Catalog/Orchard.Catalog.csproj -c Release -p:Platform=x64
dotnet publish samples/Orchard.Catalog/Orchard.Catalog.csproj -c Release -p:Platform=ARM64
```

The runnable applications are under
`samples/Orchard.Catalog/bin/<platform>/Release/net10.0-windows10.0.26100.0/win-<architecture>/publish`.
Build the architectures sequentially because their projects share restore state.
Do not create a new self-contained publish directory for every experiment.
Each copy includes the Windows App SDK and .NET runtime and is roughly 300 MB.

Keep the current package, release archives, independent consumers and reports
under `artifacts/current`. Reuse the x64 and ARM64 consumer output directories;
`scripts/verify-package.ps1` removes its own temporary source and package cache.
Create versioned archives for deliverable checkpoints, rather than every local
iteration. Replace obsolete deliverables after their replacement is verified.

Keep the reference and current screenshots and reports needed for active
comparisons. Retain actionable failure diagnostics. Historical binaries can be
rebuilt from Git; hundreds of old runtime copies are unnecessary. Record source
commit, command, architecture, result and hashes in the current manifest so a
successful old report cannot be mistaken for validation of newer bytes.

Check storage before and after validation. The intended current working set is
below 10 GiB, excluding shared SDK caches and the normal project `bin`/`obj`
directories. Investigate growth before starting another publish. The catalog
and two independent consumer applications account for only a few GiB; a large
increase indicates duplicate outputs or an isolated cache that was not removed.

## Resetting generated output

`artifacts` is ignored generated content. Removing it also removes screenshots,
failure captures, packages, archives and local verification reports. Committed
source, reference JSON and documentation remain in Git. Close applications using
these outputs before a complete reset; Windows leaves locked files behind.

From the repository root:

```powershell
Remove-Item -LiteralPath (Join-Path (Get-Location) 'artifacts') -Recurse -Force
```

The user requested a full local reset on 2026-10-17 after accumulated output
reached about 201 GiB. Evidence paths in older documentation are historical
records and may have been removed by that reset. Their recorded results do not
prove that the files are still available. The current checkpoint must identify
which deliverables and evidence have actually been regenerated.
