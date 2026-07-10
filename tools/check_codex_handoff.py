#!/usr/bin/env python3
"""Validate the macOS Codex handoff contract."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

REQUIRED = [
    "CODEX_MAC_HANDOFF.md",
    "CODE_REVIEW.md",
    "handoff/FIRST_PROMPT.txt",
    "handoff/HANDOFF_MANIFEST.json",
    "docs/00_Home/Codex Mac Handoff.md",
    "docs/00_Home/Current Development State.md",
    "docs/30_Technical/macOS Development Host Setup.md",
    "docs/40_Production/Codex App Operating Procedure.md",
    "docs/40_Production/Codex Review and Merge Procedure.md",
    "docs/40_Production/Codex Stop and Escalation Rules.md",
    "docs/40_Production/Codex Task Queue.md",
    "docs/40_Production/Tasks/TASK-0022 Bootstrap macOS Host and Close Runtime Evidence.md",
    "codex-prompts/macos/00-first-session-read-only-audit.md",
    "codex-prompts/macos/01-task-0022-runtime-evidence.md",
    "src/AGENTS.md",
    "game/Igorogue.Godot/AGENTS.md",
    "docs/AGENTS.md",
    "tools/AGENTS.md",
    "tests/AGENTS.md",
]


def frontmatter_value(path: Path, key: str) -> str | None:
    text = path.read_text(encoding="utf-8")
    if not text.startswith("---\n"):
        return None
    _, body, *_ = text.split("---", 2)
    for line in body.splitlines():
        if ":" not in line:
            continue
        k, value = line.split(":", 1)
        if k.strip() == key:
            return value.strip()
    return None


def validate_gate_statuses(
    task_22_status: str | None,
    task_1_status: str | None,
    task_2_status: str | None,
) -> list[str]:
    errors: list[str] = []
    if task_22_status not in {"ready", "review", "done"}:
        errors.append(
            "TASK-0022 must be ready, review, or done during the macOS handoff lifecycle"
        )

    runtime_gate_closed = task_22_status == "done" and task_1_status == "done"
    expected_task_2_status = "ready" if runtime_gate_closed else "blocked"
    if task_2_status != expected_task_2_status:
        errors.append(
            f"TASK-0002 must be {expected_task_2_status} while TASK-0022 is "
            f"{task_22_status!r} and TASK-0001 is {task_1_status!r}"
        )
    return errors


def main() -> int:
    errors: list[str] = []
    for relative in REQUIRED:
        if not (ROOT / relative).is_file():
            errors.append(f"missing required handoff file: {relative}")

    manifest_path = ROOT / "handoff/HANDOFF_MANIFEST.json"
    if manifest_path.is_file():
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        expected = {
            "handoff_version": "v0.2.10_CODEX_MAC_HANDOFF",
            "current_gate_task": "TASK-0022",
            "blocked_gameplay_task": "TASK-0002",
            "product_rules_kernel_implemented": False,
            "abstract_proxy_evidence_level": "E2",
        }
        for key, value in expected.items():
            if manifest.get(key) != value:
                errors.append(f"handoff manifest {key}: expected {value!r}, got {manifest.get(key)!r}")

    task_22 = ROOT / "docs/40_Production/Tasks/TASK-0022 Bootstrap macOS Host and Close Runtime Evidence.md"
    task_1 = ROOT / "docs/40_Production/Tasks/TASK-0001 Decide Engine and Repository.md"
    task_2 = ROOT / "docs/40_Production/Tasks/TASK-0002 Deterministic RNG and Command Log.md"
    if task_22.is_file() and task_1.is_file() and task_2.is_file():
        errors.extend(
            validate_gate_statuses(
                frontmatter_value(task_22, "status"),
                frontmatter_value(task_1, "status"),
                frontmatter_value(task_2, "status"),
            )
        )

    root_agents = (ROOT / "AGENTS.md").read_text(encoding="utf-8") if (ROOT / "AGENTS.md").is_file() else ""
    for required_text in ("CODEX_MAC_HANDOFF.md", "CODE_REVIEW.md", "TASK-0022"):
        if required_text not in root_agents:
            errors.append(f"root AGENTS.md does not reference {required_text}")

    prompt_path = ROOT / "handoff/FIRST_PROMPT.txt"
    if prompt_path.is_file():
        prompt = prompt_path.read_text(encoding="utf-8")
        for required_text in ("READ-ONLY AUDIT", "Do not edit", "TASK-0022", "TASK-0002"):
            if required_text not in prompt:
                errors.append(f"first prompt missing safety phrase: {required_text}")

    if errors:
        print("Codex handoff checks failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    task_22_status = frontmatter_value(task_22, "status")
    task_2_status = frontmatter_value(task_2, "status")
    print(
        f"Codex handoff checks passed — {len(REQUIRED)} required files, "
        f"TASK-0022 {task_22_status}, TASK-0002 {task_2_status}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
