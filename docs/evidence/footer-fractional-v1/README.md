# Footer fractional slot receipts

These small files are byte-identical copies of the hidden Footer public-contract
receipts indexed by `checkpoint.json`. They remain available after disposable
`artifacts/current` build trees are removed. The checkpoint records the original
paths and SHA-256 hashes; its copied runtime, audit, negative-audit and ARM64
build-log files have those same hashes.

The source-built x64 catalog and independently restored v4 NuGet consumer each
passed 12 contracts and 205 isolation guards. Their negative audits rejected 24
and 28 damaged receipts. The ARM64 catalog compiled but was not executed.
These records do not prove visible interaction, full raster parity, native ARM64
runtime or component acceptance. The package and runnable apps are not stored
in this small evidence archive.
