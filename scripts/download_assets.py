#!/usr/bin/env python3
"""Download and safely extract external release artifacts."""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path, PurePosixPath
import shutil
import stat
import sys
from typing import Any, Dict, Iterable, List
import zipfile


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
MANIFESTS = {
    "datasets": (REPOSITORY_ROOT / "data" / "manifest.json", "datasets"),
    "models": (REPOSITORY_ROOT / "artifacts" / "manifest.json", "packages"),
}


def load_entries(kind: str) -> List[Dict[str, Any]]:
    selected = [kind] if kind != "all" else ["datasets", "models"]
    entries: List[Dict[str, Any]] = []
    for manifest_kind in selected:
        manifest_path, key = MANIFESTS[manifest_kind]
        payload = json.loads(manifest_path.read_text(encoding="utf-8"))
        for item in payload[key]:
            entry = dict(item)
            entry["manifest_kind"] = manifest_kind
            entries.append(entry)
    return entries


def safe_destination(relative_path: str) -> Path:
    destination = (REPOSITORY_ROOT / relative_path).resolve()
    try:
        destination.relative_to(REPOSITORY_ROOT)
    except ValueError as exc:
        raise ValueError(
            f"destination escapes the repository: {relative_path}"
        ) from exc
    return destination


def verify(entry: Dict[str, Any], path: Path) -> bool:
    if not path.is_file():
        print(f"MISSING {entry['id']}: {path}")
        return False

    expected_bytes = entry.get("expected_bytes")
    if expected_bytes is not None and path.stat().st_size != expected_bytes:
        print(
            f"SIZE_MISMATCH {entry['id']}: expected {expected_bytes}, "
            f"found {path.stat().st_size}"
        )
        return False

    print(f"VERIFIED {entry['id']}: size")
    return True


def ensure_download(
    entry: Dict[str, Any], destination: Path, overwrite: bool
) -> bool:
    file_id = entry.get("google_drive_file_id")
    if not file_id:
        manifest_path = MANIFESTS[entry["manifest_kind"]][0]
        print(
            f"LINK_PENDING {entry['id']}: add google_drive_file_id to "
            f"{manifest_path.relative_to(REPOSITORY_ROOT)}"
        )
        return False

    if destination.exists():
        if verify(entry, destination):
            return True
        if not overwrite:
            print(f"NOT_OVERWRITTEN {entry['id']}: use --overwrite")
            return False

    try:
        import gdown
    except ImportError:
        print(
            "gdown is required for downloads. Install it with: "
            "python -m pip install -r requirements-download.txt"
        )
        return False

    destination.parent.mkdir(parents=True, exist_ok=True)
    partial = destination.with_name(destination.name + ".part")
    if partial.exists():
        if not overwrite:
            print(f"PARTIAL_EXISTS {entry['id']}: {partial}")
            return False
        partial.unlink()

    print(f"DOWNLOADING {entry['id']} -> {destination}")
    result = gdown.download(id=file_id, output=str(partial), quiet=False)
    if result is None or not partial.is_file():
        print(
            f"DOWNLOAD_FAILED {entry['id']}: Google Drive may be rate-limited; "
            "use the download_page in the manifest for a manual download"
        )
        return False
    if not verify(entry, partial):
        print(f"INVALID_DOWNLOAD {entry['id']}: retained as {partial}")
        return False

    os.replace(partial, destination)
    print(f"READY {entry['id']}: {destination}")
    return True


def validate_member_path(member_name: str) -> None:
    member_path = PurePosixPath(member_name)
    if member_path.is_absolute() or ".." in member_path.parts:
        raise ValueError(f"unsafe ZIP member path: {member_name}")


def extract_members(
    package: Dict[str, Any], archive_path: Path, overwrite: bool
) -> bool:
    members = package.get("members", [])
    if not members:
        return True

    success = True
    try:
        with zipfile.ZipFile(archive_path, "r") as archive:
            for member in members:
                member_name = member["archive_path"]
                validate_member_path(member_name)
                try:
                    info = archive.getinfo(member_name)
                except KeyError:
                    print(f"ARCHIVE_MEMBER_MISSING {member['id']}: {member_name}")
                    success = False
                    continue

                unix_mode = (info.external_attr >> 16) & 0xFFFF
                if info.is_dir() or stat.S_ISLNK(unix_mode):
                    print(f"UNSAFE_ARCHIVE_MEMBER {member['id']}: {member_name}")
                    success = False
                    continue
                if info.file_size != member.get("expected_bytes"):
                    print(
                        f"ARCHIVE_MEMBER_SIZE_MISMATCH {member['id']}: expected "
                        f"{member.get('expected_bytes')}, found {info.file_size}"
                    )
                    success = False
                    continue

                destination = safe_destination(member["destination"])
                if destination.exists():
                    if verify(member, destination):
                        continue
                    if not overwrite:
                        print(f"NOT_OVERWRITTEN {member['id']}: use --overwrite")
                        success = False
                        continue

                destination.parent.mkdir(parents=True, exist_ok=True)
                partial = destination.with_name(destination.name + ".part")
                if partial.exists():
                    if not overwrite:
                        print(f"PARTIAL_EXISTS {member['id']}: {partial}")
                        success = False
                        continue
                    partial.unlink()

                print(f"EXTRACTING {member['id']} -> {destination}")
                with archive.open(info, "r") as source, partial.open("wb") as output:
                    shutil.copyfileobj(source, output, length=8 * 1024 * 1024)
                if not verify(member, partial):
                    print(f"INVALID_EXTRACTION {member['id']}: retained as {partial}")
                    success = False
                    continue
                os.replace(partial, destination)
                print(f"READY {member['id']}: {destination}")
    except (OSError, ValueError, zipfile.BadZipFile) as exc:
        print(f"ARCHIVE_ERROR {package['id']}: {exc}")
        return False
    return success


def verify_extracted_members(package: Dict[str, Any]) -> bool:
    success = True
    for member in package.get("members", []):
        success = verify(member, safe_destination(member["destination"])) and success
    return success


def select_entries(
    entries: Iterable[Dict[str, Any]], requested_ids: List[str]
) -> List[Dict[str, Any]]:
    entries = list(entries)
    if not requested_ids:
        return entries
    known = {entry["id"] for entry in entries}
    unknown = sorted(set(requested_ids) - known)
    if unknown:
        raise ValueError("unknown package or dataset id(s): " + ", ".join(unknown))
    return [entry for entry in entries if entry["id"] in requested_ids]


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Download, verify, and safely extract release artifacts"
    )
    parser.add_argument(
        "--kind", choices=("datasets", "models", "all"), default="all"
    )
    parser.add_argument(
        "--id", action="append", default=[], help="select a package or dataset id"
    )
    parser.add_argument(
        "--verify-only", action="store_true", help="do not download or extract"
    )
    parser.add_argument(
        "--overwrite", action="store_true", help="replace invalid or partial files"
    )
    args = parser.parse_args()

    try:
        entries = select_entries(load_entries(args.kind), args.id)
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        print(f"MANIFEST_ERROR: {exc}")
        return 2

    success = True
    for entry in entries:
        destination = safe_destination(entry["destination"])
        if args.verify_only:
            item_ok = verify(entry, destination)
            if item_ok:
                item_ok = verify_extracted_members(entry)
        else:
            item_ok = ensure_download(entry, destination, args.overwrite)
            if item_ok:
                item_ok = extract_members(entry, destination, args.overwrite)
        success = item_ok and success
    return 0 if success else 1


if __name__ == "__main__":
    sys.exit(main())
