# Local artifact storage

The current published package and x64/ARM64 runnable catalogs are
[preview 2026-10-29.32](releases/2026-10-29-preview-32.md). Its exact NuGet DLL,
archive bytes, x64 consumer/runtime checks, ARM64 build/consumer compile, and
fictional demos are recorded in
`artifacts/current/context-menu-native-candidate/release-v32-checkpoint.json`.
Native ARM64 execution and remaining component acceptance remain open. The previous
[published v7](releases/2026-10-27-preview-v7.md) contains the crash fix and
its own separately qualified bytes. The older unpublished local checkpoint is
[components v7](releases/2026-10-27-components-v7-local.md); its distinct
version, hashes, runtime checks and failed ARM64 consumer publish are recorded
there.

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
archive. The Catalog project copies compiled XAML (`.xbf`) into `publish`; the
packaging script checks that every source fixture, including both fictional
Monitoring and Editorial demos, has a matching compiled file before archiving.
The catalog publish target copies compiled XAML only from the build output root
and `Demos` directory. Repeated publishes must not copy an earlier `publish`
tree into `publish/publish`; the packaging script rejects nested output.
If an older build leaves a nested `publish/publish` directory containing files,
remove that generated subdirectory before packaging. The packer tolerates empty
nested directories left by repeated publishes and still rejects nested files.
Verify the extracted application and required component demos before claiming
runtime delivery.

Keep the current package, release archives, independent consumers and reports
under `artifacts/current`. Reuse the x64 and ARM64 consumer output directories;
`scripts/verify-package.ps1` removes its own temporary source and package cache.
The Media Library package payload check compares its XAML paths and hashes with
the tracked `src/Orchard` sources; its theme count follows that source inventory.
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
The v7 independent ARM64 restore plus self-contained publish exhausted C: with
less than 3 GB free after catalog publishing. Do not start that verifier again
on this disk without materially more free space or an explicitly selected
larger volume.
Its failed run removed the temporary restore workspace, but a partial app
directory remains until cleanup is permitted.
The separate `-Platform ARM64 -CompileOnly` verifier path uses a framework-dependent
build and a fresh isolated restore, then removes its temporary workspace. The
v7 compile-only consumer succeeded with about 53 MiB of app output; it does not produce
a runnable, self-contained ARM64 qualification or native runtime evidence.

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

On 2026-10-26, retained `artifacts/current` outputs measured about 396 GiB in
logical file lengths across 3,326 test-run directories, far above the intended
working set. That sum includes hard-linked negative-audit cases and therefore
overstates physical disk use. The Hero Carousel negative auditor removes each
proven rejection immediately; if an assertion fails, it retains only that case
for diagnosis. This prevents one late failure from retaining dozens of earlier
hard-link trees. Old successful runs and their copied application binaries
still need pruning or a full artifact reset. After any
reset, regenerate the evidence needed for active acceptance checks rather than
claiming that old checkpoint paths still exist.

The Media Library negative auditor likewise removes its copied counterexample
directory after a successful run and leaves a failing copy available for
diagnosis. This prevents the normal audit from retaining a second copy of all
100 rendered states.

The Popover and Scroll Area source negative auditors release each copied case
after its expected rejection. If an unexpected check fails, the current case
remains available for diagnosis. Rechecks of existing native captures rejected
14 Popover and 28 Scroll Area cases without retaining successful scratch copies.
Older negative-audit output directories are historical generated artifacts and
are not removed by this change.

The File Input negative auditor follows the same rule: its copied native
capture is removed after the audit receipt is written successfully, while an
unexpected failure keeps the copy for diagnosis. Older successful
`*-counterexamples-v1` directories predate this cleanup and remain disposable
generated output.
