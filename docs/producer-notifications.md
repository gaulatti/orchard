# Component producer notifications

Select, Collection Filters, DataTable Pagination, DataTable Column Toggle and
DataTable Select Filter observe caller-owned models. A caller may publish their
`INotifyPropertyChanged`, `INotifyCollectionChanged` or table `Changed` event on
a worker thread. The component queues view work to its captured UI dispatcher
before reading `IsLoaded` or any other XAML property. Queue rejection is explicit.
Unloaded controls release their producer subscriptions; replacement tables keep
their own identity and state. Dependency properties themselves retain WinUI's
normal UI-thread requirement. Consumers must serialize model mutations; this
contract does not make collection enumeration and concurrent mutation safe.

The regression fixture in `tests/PackageConsumer/ProducerThreadingFixture.cs`
uses public Orchard APIs and is also compiled into the catalog. Its five cases
exercise 23 actual worker publications, including both Select and Header Select,
through the rendered value, prepared choices, original object tags and table
observer boundaries. It never opens a
popup, activates a window, requests focus, sends input or changes OS settings.
The saved Footer v1 package fails all five cases with `0x8001010E` at the old
pre-dispatch `IsLoaded` read. That failed baseline is preserved at
`artifacts/current/producer-threading-baseline-consumer-v2`.

Run a current catalog regression with:

```powershell
./scripts/verify-hidden-producer-threading.ps1 -AppDirectory <catalog-output> -Output <fresh-output>
```

After `verify-package.ps1 -BuildOnly` creates an independent package consumer,
run the same regression with `-HostKind PackageConsumer -Package <exact-nupkg>`
and its existing output directory. Use `-ExpectedArchitecture Arm64` only on a
native ARM64 host. The runner preserves the executed assemblies; the auditor
checks process exit, host/library hashes, worker identities, all five cases,
isolation, fresh restore identity and temporary-cache cleanup. The companion
negative audit rejects damaged receipts. These background regressions do not
prove pointer/keyboard behavior, visible raster parity or component acceptance.

This is component-library work. Both fictional-data composition demos remain
required. No backend or metrics surface is involved. The public wiki is currently
unavailable, so this contract is documented in the repository.

The current local package is `0.1.0-producer-threading-v1-local`, SHA-256
`d9f8ea8283d8011a9196358cec1c33174dadad7f6d4f76ba8b07833bf539f38c`.
The catalog and fresh isolated x64 consumer both pass all five cases, 23 worker
publications and 191 isolation guards. Their auditors reject 27 and 31 damaged
receipts respectively. Catalog and isolated consumer builds pass on x64 and
ARM64; ARM64 runtime remains unverified. Both temporary consumer restore caches
are removed. Footer remains unchanged: its separate catalog and package runs
each pass 144 layouts, 8,040 geometry checks with zero error, 1,434 paint checks
and seven lifetime/threading checks against this same library DLL. Those runs
retain their own exact host-assembly snapshots or published app directory.

Evidence is indexed by `artifacts/current/producer-threading-checkpoint.json`.
No component becomes accepted from these background checks. The inventory stays
at 88 in progress, 24 pending and zero accepted: 78.6% entries started. Both demos
remain in progress. Desktop input/raster, motion/scaling, broader component
matrices, final integration and native ARM64 acceptance remain open. No Actions
job, push, Narrator, system-setting change or pointer heartbeat was triggered.
