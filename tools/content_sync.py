#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable

ROOT = Path(__file__).resolve().parents[1]
SOURCE_ROOT = ROOT / "game_data"
SOURCE_SUBDIRS = ("balance", "content")
DEFAULT_OUTPUTS = (
    ROOT / "build/generated_content",
    ROOT / "game/Igorogue.Godot/generated_content",
)


@dataclass(frozen=True)
class CanonicalFile:
    path: str
    content: bytes
    sha256: str


def canonical_json_bytes(value: Any) -> bytes:
    return (
        json.dumps(
            value,
            ensure_ascii=False,
            sort_keys=True,
            separators=(",", ":"),
            allow_nan=False,
        )
        + "\n"
    ).encode("utf-8")


def collect_files(source_root: Path = SOURCE_ROOT) -> list[CanonicalFile]:
    files: list[CanonicalFile] = []
    for subdir in SOURCE_SUBDIRS:
        directory = source_root / subdir
        if not directory.is_dir():
            raise FileNotFoundError(f"Missing runtime content directory: {directory}")

        for path in sorted(directory.rglob("*.json"), key=lambda item: item.as_posix()):
            relative = path.relative_to(source_root).as_posix()
            value = json.loads(path.read_text(encoding="utf-8"))
            content = canonical_json_bytes(value)
            digest = hashlib.sha256(content).hexdigest()
            files.append(CanonicalFile(relative, content, f"sha256:{digest}"))

    return sorted(files, key=lambda item: item.path)


def calculate_content_hash(files: Iterable[CanonicalFile]) -> str:
    digest = hashlib.sha256()
    for item in files:
        path_bytes = item.path.encode("utf-8")
        digest.update(len(path_bytes).to_bytes(4, "little"))
        digest.update(path_bytes)
        digest.update(len(item.content).to_bytes(8, "little"))
        digest.update(item.content)
    return f"sha256:{digest.hexdigest()}"


def build_manifest(files: list[CanonicalFile]) -> dict[str, Any]:
    return {
        "schema_version": 1,
        "content_hash": calculate_content_hash(files),
        "files": [
            {
                "path": item.path,
                "sha256": item.sha256,
                "bytes": len(item.content),
            }
            for item in files
        ],
    }


def expected_output(files: list[CanonicalFile]) -> dict[str, bytes]:
    manifest = canonical_json_bytes(build_manifest(files))
    result = {"content_manifest.json": manifest}
    for item in files:
        result[f"files/{item.path}"] = item.content
    return result


def write_output(output_root: Path, expected: dict[str, bytes]) -> None:
    if output_root.exists():
        shutil.rmtree(output_root)
    output_root.mkdir(parents=True, exist_ok=True)
    for relative, content in expected.items():
        path = output_root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(content)


def check_output(output_root: Path, expected: dict[str, bytes]) -> list[str]:
    errors: list[str] = []
    expected_paths = set(expected)
    actual_paths = {
        path.relative_to(output_root).as_posix()
        for path in output_root.rglob("*")
        if path.is_file() and path.name != ".gitkeep"
    } if output_root.exists() else set()

    for missing in sorted(expected_paths - actual_paths):
        errors.append(f"{output_root}: missing {missing}")
    for unexpected in sorted(actual_paths - expected_paths):
        errors.append(f"{output_root}: unexpected {unexpected}")
    for relative in sorted(expected_paths & actual_paths):
        if (output_root / relative).read_bytes() != expected[relative]:
            errors.append(f"{output_root}: stale {relative}")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate the canonical Igorogue runtime content snapshot.")
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--write", action="store_true", help="Rewrite all generated content outputs.")
    mode.add_argument("--check", action="store_true", help="Fail when generated content differs from game_data.")
    parser.add_argument("--print-hash", action="store_true", help="Print the canonical source content hash.")
    parser.add_argument("--output", action="append", type=Path, help="Override output directories (repeatable).")
    args = parser.parse_args()

    files = collect_files()
    expected = expected_output(files)
    outputs = tuple(args.output) if args.output else DEFAULT_OUTPUTS

    if args.write:
        for output in outputs:
            write_output(output, expected)
    else:
        errors: list[str] = []
        for output in outputs:
            errors.extend(check_output(output, expected))
        if errors:
            print("Content sync check failed:", file=sys.stderr)
            for error in errors:
                print(f"- {error}", file=sys.stderr)
            return 1

    manifest = build_manifest(files)
    if args.print_hash:
        print(manifest["content_hash"])
    else:
        action = "generated" if args.write else "verified"
        print(f"Content snapshot {action}: {manifest['content_hash']} ({len(files)} files)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
