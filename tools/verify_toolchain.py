#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_PATH = ROOT / "toolchain/bootstrap_manifest.json"


class VerificationError(RuntimeError):
    pass


def load_manifest() -> dict[str, Any]:
    return json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))


def validate_dotnet_version(output: str, expected: str) -> str:
    actual = output.strip().splitlines()[0].strip() if output.strip() else ""
    if actual != expected:
        raise VerificationError(f"Expected .NET SDK {expected}, got {actual or '<empty>'}.")
    return actual


def validate_godot_version(output: str, expected: str) -> str:
    actual = output.strip().splitlines()[0].strip() if output.strip() else ""
    if "mono" not in actual.lower():
        raise VerificationError(
            f"Godot executable is not the .NET/Mono editor build: {actual or '<empty>'}.")

    expected_pattern = re.escape(expected.replace("-", "."))
    if not re.match(rf"^{expected_pattern}(?:\.mono)?(?:\.|$)", actual, flags=re.IGNORECASE):
        raise VerificationError(f"Expected Godot {expected} .NET, got {actual or '<empty>'}.")
    return actual


def resolve_command(env_name: str, candidates: list[str]) -> str:
    override = os.environ.get(env_name)
    if override:
        path = Path(override).expanduser()
        if not path.is_file():
            raise VerificationError(f"{env_name} does not point to a file: {path}")
        return str(path)

    for candidate in candidates:
        resolved = shutil.which(candidate)
        if resolved:
            return resolved
    raise VerificationError(
        f"No executable found for {env_name}. Set {env_name} explicitly.")


def run_version(command: str) -> str:
    result = subprocess.run(
        [command, "--version"],
        check=False,
        capture_output=True,
        text=True,
        timeout=30,
    )
    output = (result.stdout or result.stderr).strip()
    if result.returncode != 0:
        raise VerificationError(
            f"Version command failed ({result.returncode}): {command} --version\n{output}")
    return output


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify pinned Igorogue .NET and Godot tools.")
    parser.add_argument("--skip-dotnet", action="store_true")
    parser.add_argument("--skip-godot", action="store_true")
    parser.add_argument("--json", action="store_true", dest="json_output")
    args = parser.parse_args()

    manifest = load_manifest()
    result: dict[str, str] = {}
    try:
        if not args.skip_dotnet:
            dotnet = resolve_command("DOTNET_BIN", ["dotnet"])
            result["dotnet_path"] = dotnet
            result["dotnet_version"] = validate_dotnet_version(
                run_version(dotnet), manifest["dotnet"]["sdk_version"])

        if not args.skip_godot:
            godot = resolve_command(
                "GODOT_BIN",
                ["godot-mono", "godot4-mono", "godot4", "godot"],
            )
            result["godot_path"] = godot
            result["godot_version"] = validate_godot_version(
                run_version(godot), manifest["godot"]["version"])
    except (VerificationError, OSError, subprocess.SubprocessError) as error:
        if args.json_output:
            print(json.dumps({"ok": False, "error": str(error)}, sort_keys=True))
        else:
            print(f"Toolchain verification failed: {error}", file=sys.stderr)
        return 1

    if args.json_output:
        print(json.dumps({"ok": True, **result}, sort_keys=True))
    else:
        for key, value in sorted(result.items()):
            print(f"{key}={value}")
        print("Toolchain verification passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
