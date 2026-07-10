#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

REQUIRED_FILES = [
    "Igorogue.sln",
    "global.json",
    "Directory.Build.props",
    "Directory.Packages.props",
    "NuGet.Config",
    "src/Igorogue.Domain/Igorogue.Domain.csproj",
    "src/Igorogue.Application/Igorogue.Application.csproj",
    "src/Igorogue.Content/Igorogue.Content.csproj",
    "game/Igorogue.Godot/Igorogue.Godot.csproj",
    "game/Igorogue.Godot/Igorogue.Godot.sln",
    "game/Igorogue.Godot/project.godot",
    "game/Igorogue.Godot/export_presets.cfg",
    "game/Igorogue.Godot/Smoke/BootstrapSmoke.cs.uid",
    "game/Igorogue.Godot/Smoke/BootstrapSmoke.tscn",
    "tests/Igorogue.Domain.Tests/Igorogue.Domain.Tests.csproj",
    "tests/Igorogue.Application.Tests/Igorogue.Application.Tests.csproj",
    "tests/Igorogue.Architecture.Tests/Igorogue.Architecture.Tests.csproj",
    "tools/Igorogue.Sim.Cli/Igorogue.Sim.Cli.csproj",
    "tools/content_sync.py",
    "tools/verify_toolchain.py",
    "toolchain/bootstrap_manifest.json",
    ".github/workflows/ci.yml",
]

PROJECTS = [
    "src/Igorogue.Domain/Igorogue.Domain.csproj",
    "src/Igorogue.Application/Igorogue.Application.csproj",
    "src/Igorogue.Content/Igorogue.Content.csproj",
    "game/Igorogue.Godot/Igorogue.Godot.csproj",
    "tests/Igorogue.Domain.Tests/Igorogue.Domain.Tests.csproj",
    "tests/Igorogue.Application.Tests/Igorogue.Application.Tests.csproj",
    "tests/Igorogue.Architecture.Tests/Igorogue.Architecture.Tests.csproj",
    "tools/Igorogue.Sim.Cli/Igorogue.Sim.Cli.csproj",
]


def parse_xml(path: Path) -> ET.Element:
    try:
        return ET.parse(path).getroot()
    except ET.ParseError as error:
        raise ValueError(f"Invalid XML in {path.relative_to(ROOT)}: {error}") from error


def normalized_project_refs(path: Path) -> set[str]:
    root = parse_xml(path)
    result: set[str] = set()
    for element in root.iter("ProjectReference"):
        include = element.attrib.get("Include")
        if include:
            full = (path.parent / include).resolve()
            result.add(full.relative_to(ROOT.resolve()).as_posix())
    return result


def property_value(path: Path, name: str) -> str | None:
    root = parse_xml(path)
    for element in root.iter(name):
        if element.text and element.text.strip():
            return element.text.strip()
    return None


def package_versions(path: Path) -> dict[str, str]:
    root = parse_xml(path)
    result: dict[str, str] = {}
    for element in root.iter("PackageVersion"):
        include = element.attrib.get("Include")
        version = element.attrib.get("Version")
        if include and version:
            result[include] = version
    return result


def project_sdk(path: Path) -> str:
    return parse_xml(path).attrib.get("Sdk", "")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--strict-locks", action="store_true")
    args = parser.parse_args()
    errors: list[str] = []
    warnings: list[str] = []

    for relative in REQUIRED_FILES:
        if not (ROOT / relative).is_file():
            errors.append(f"missing required file: {relative}")

    if errors:
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    global_json = json.loads((ROOT / "global.json").read_text(encoding="utf-8"))
    manifest = json.loads((ROOT / "toolchain/bootstrap_manifest.json").read_text(encoding="utf-8"))
    if global_json.get("sdk", {}).get("version") != manifest["dotnet"]["sdk_version"]:
        errors.append("global.json and bootstrap manifest .NET versions differ")
    if (ROOT / ".dotnet-version").read_text(encoding="utf-8").strip() != manifest["dotnet"]["sdk_version"]:
        errors.append(".dotnet-version differs from bootstrap manifest")
    if (ROOT / ".godot-version").read_text(encoding="utf-8").strip() != manifest["godot"]["version"]:
        errors.append(".godot-version differs from bootstrap manifest")

    build_props = ROOT / "Directory.Build.props"
    if property_value(build_props, "LangVersion") != manifest["dotnet"]["csharp_language_version"]:
        errors.append("Directory.Build.props C# version differs from bootstrap manifest")
    package_map = package_versions(ROOT / "Directory.Packages.props")
    expected_test_package = manifest["tests"]["framework"]
    if package_map.get(expected_test_package) != manifest["tests"]["version"]:
        errors.append("xUnit package/version differs from bootstrap manifest")
    expected_godot_sdk = f"Godot.NET.Sdk/{manifest['godot']['package_version']}"
    if project_sdk(ROOT / "game/Igorogue.Godot/Igorogue.Godot.csproj") != expected_godot_sdk:
        errors.append("Godot project SDK differs from bootstrap manifest")
    godot_solution = (ROOT / "game/Igorogue.Godot/Igorogue.Godot.sln").read_text(encoding="utf-8")
    if "Igorogue.Godot.csproj" not in godot_solution:
        errors.append("Godot-local solution does not include Igorogue.Godot.csproj")
    for project in PROJECTS:
        if property_value(ROOT / project, "TargetFramework") != manifest["dotnet"]["target_framework"]:
            errors.append(f"target framework differs from bootstrap manifest: {project}")

    for project in PROJECTS:
        parse_xml(ROOT / project)
        if project.replace("/", os.sep) not in (ROOT / "Igorogue.sln").read_text(encoding="utf-8") and project not in (ROOT / "Igorogue.sln").read_text(encoding="utf-8"):
            errors.append(f"solution does not include project: {project}")

    domain_refs = normalized_project_refs(ROOT / "src/Igorogue.Domain/Igorogue.Domain.csproj")
    if domain_refs:
        errors.append(f"Domain must have no project references, got {sorted(domain_refs)}")
    app_refs = normalized_project_refs(ROOT / "src/Igorogue.Application/Igorogue.Application.csproj")
    if app_refs != {"src/Igorogue.Domain/Igorogue.Domain.csproj"}:
        errors.append(f"Application refs mismatch: {sorted(app_refs)}")
    content_refs = normalized_project_refs(ROOT / "src/Igorogue.Content/Igorogue.Content.csproj")
    if content_refs:
        errors.append(f"Content must have no project references, got {sorted(content_refs)}")
    sim_refs = normalized_project_refs(ROOT / "tools/Igorogue.Sim.Cli/Igorogue.Sim.Cli.csproj")
    if sim_refs != {
        "src/Igorogue.Application/Igorogue.Application.csproj",
        "src/Igorogue.Content/Igorogue.Content.csproj",
    }:
        errors.append(f"Sim.Cli refs mismatch: {sorted(sim_refs)}")

    godot_refs = normalized_project_refs(ROOT / "game/Igorogue.Godot/Igorogue.Godot.csproj")
    if godot_refs != {
        "src/Igorogue.Application/Igorogue.Application.csproj",
        "src/Igorogue.Content/Igorogue.Content.csproj",
    }:
        errors.append(f"Godot refs mismatch: {sorted(godot_refs)}")

    for directory in (ROOT / "src/Igorogue.Domain", ROOT / "src/Igorogue.Application"):
        for path in directory.rglob("*"):
            if path.is_file() and path.suffix in {".cs", ".csproj"}:
                if re.search(r"\bGodot(?:\.|\b)", path.read_text(encoding="utf-8")):
                    errors.append(f"Godot reference leaked into {path.relative_to(ROOT)}")

    project_settings = (ROOT / "game/Igorogue.Godot/project.godot").read_text(encoding="utf-8")
    required_settings = [
        'window/size/viewport_width=480',
        'window/size/viewport_height=270',
        'window/stretch/mode="viewport"',
        'window/stretch/scale_mode="integer"',
        'renderer/rendering_method="gl_compatibility"',
        'run/main_scene="res://Smoke/BootstrapSmoke.tscn"',
    ]
    for setting in required_settings:
        if setting not in project_settings:
            errors.append(f"project.godot missing setting: {setting}")

    generated_manifest = ROOT / "build/generated_content/content_manifest.json"
    godot_manifest = ROOT / "game/Igorogue.Godot/generated_content/content_manifest.json"
    if not generated_manifest.is_file() or not godot_manifest.is_file():
        errors.append("generated content manifests are missing")
    elif generated_manifest.read_bytes() != godot_manifest.read_bytes():
        errors.append("build and Godot generated content manifests differ")

    workflow = (ROOT / ".github/workflows/ci.yml").read_text(encoding="utf-8")
    for required_job in ("governance:", "dotnet:", "godot:"):
        if required_job not in workflow:
            errors.append(f"CI workflow missing job: {required_job[:-1]}")

    wrapper_names = [
        "check", "restore", "update-locks", "build", "test", "sim-smoke",
        "godot-smoke", "export-windows", "verify-tools", "content-sync",
    ]
    for name in wrapper_names:
        posix = ROOT / f"tools/dev/{name}"
        powershell = ROOT / f"tools/dev/{name}.ps1"
        if not posix.is_file() or not os.access(posix, os.X_OK):
            errors.append(f"missing executable POSIX wrapper: tools/dev/{name}")
        if not powershell.is_file():
            errors.append(f"missing PowerShell wrapper: tools/dev/{name}.ps1")

    lock_files = [ROOT / project.parent / "packages.lock.json" for project in map(Path, PROJECTS)]
    missing_locks = [str(path.relative_to(ROOT)) for path in lock_files if not path.is_file()]
    if missing_locks:
        message = "package lock files require first restore on a .NET host: " + ", ".join(missing_locks)
        if args.strict_locks:
            errors.append(message)
        else:
            warnings.append(message)

    if errors:
        print("Repository bootstrap checks failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    for warning in warnings:
        print(f"WARNING: {warning}")
    print(f"Repository bootstrap checks passed — {len(PROJECTS)} projects, {len(wrapper_names)} wrapper pairs")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
