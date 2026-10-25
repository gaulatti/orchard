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

The catalog embeds its reference JSON as gzip data generated under `obj` by
`CatalogReferences.targets`, using the installed .NET SDK. Authoritative JSON
files and logical resource names stay unchanged. Catalog fixtures read through
`CatalogReferenceResources.Open`, which restores the original bytes in seekable,
read-only streams; PNG and binary resources pass through unchanged. No reference,
fixture or component demo is removed. This keeps large JSON captures from adding
about 66 MB to every catalog assembly copy. The reusable library and NuGet payload
are unaffected.

After changing this resource path, run the catalog with
`--verify-catalog-reference-resources --verify-output <report.json>` and audit
the report against the original resource names, lengths and hashes with
`scripts/verify-catalog-reference-resources.mjs`. Also exercise both fictional
demos from the extracted catalog archive. A build alone does not verify decoding
or delivery.

Pass the clean `publish` directory itself to `scripts/package-catalog.ps1`.
Do not archive its parent build directory: that directory can contain an older
nested `publish` tree and would bundle stale application binaries alongside the
current application. The packaging script rejects that input before writing an
archive. Verify the extracted application and required component demos before
claiming runtime delivery.

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
