"""Materialize a catalog ZIP using verified identical files from an existing deployment."""

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
from zipfile import ZipFile


ROOT = Path(__file__).resolve().parents[1]
ARTIFACTS = (ROOT / "artifacts" / "current").resolve()


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--base", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
    archive = (ROOT / manifest["archive"]).resolve()
    base = args.base.resolve(strict=True)
    output = args.output.resolve()
    if output.parent != ARTIFACTS or output.exists():
        raise ValueError("Output must be a fresh direct child of artifacts/current")
    if base.parent != ARTIFACTS or base == output:
        raise ValueError("Base must be an existing direct child of artifacts/current")
    if sha256(archive) != manifest["archiveSha256"]:
        raise ValueError("Archive SHA-256 differs from the pinned manifest")

    entries = {item["path"]: item for item in manifest["files"]}
    changed = set(manifest["changedFromV7"])
    if len(entries) != manifest["fileCount"] or not changed <= entries.keys():
        raise ValueError("Manifest file count or changed-file list is invalid")
    for name in entries:
        path = PurePosixPath(name)
        if path.is_absolute() or ".." in path.parts or str(path) != name:
            raise ValueError(f"Unsafe archive path: {name}")

    # All checks happen before creating the output. No existing file is altered.
    for name, entry in entries.items():
        if name in changed:
            continue
        source = base.joinpath(*PurePosixPath(name).parts)
        if not source.is_file() or source.stat().st_size != entry["bytes"] or sha256(source) != entry["sha256"]:
            raise ValueError(f"Base file differs from the archive manifest: {name}")

    payloads = {}
    with ZipFile(archive) as zipped:
        names = {item.filename for item in zipped.infolist() if not item.is_dir()}
        if names != entries.keys():
            raise ValueError("Archive entries differ from the manifest")
        for name in changed:
            payload = zipped.read(name)
            entry = entries[name]
            if len(payload) != entry["bytes"] or hashlib.sha256(payload).hexdigest() != entry["sha256"]:
                raise ValueError(f"Changed archive entry differs from the manifest: {name}")
            payloads[name] = payload

    output.mkdir()
    for name in entries:
        destination = output.joinpath(*PurePosixPath(name).parts)
        destination.parent.mkdir(parents=True, exist_ok=True)
        if name in changed:
            destination.write_bytes(payloads[name])
        else:
            os.link(base.joinpath(*PurePosixPath(name).parts), destination)

    for name, entry in entries.items():
        destination = output.joinpath(*PurePosixPath(name).parts)
        if name in changed:
            if sha256(destination) != entry["sha256"]:
                raise ValueError(f"Written archive entry differs from the manifest: {name}")
        elif not os.path.samefile(base.joinpath(*PurePosixPath(name).parts), destination):
            raise ValueError(f"Unchanged file is not linked to the verified base: {name}")
    print(json.dumps({
        "format": "orchard-catalog-linked-materialization-v1",
        "archiveSha256": manifest["archiveSha256"],
        "output": str(output),
        "files": len(entries),
        "verifiedHardlinks": len(entries) - len(changed),
        "archivePayloadFiles": len(changed),
        "archivePayloadBytes": sum(len(value) for value in payloads.values()),
        "allFilesMatchArchiveManifest": True,
    }))


if __name__ == "__main__":
    main()
