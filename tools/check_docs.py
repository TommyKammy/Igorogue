#!/usr/bin/env python3
"""Minimal governance checks for the Obsidian project notes."""

from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"

REQUIRED_TASK_FIELDS = {"type", "id", "status", "milestone", "priority", "updated"}
REQUIRED_ADR_FIELDS = {"type", "id", "status", "updated"}
VALID_TASK_STATUS = {"backlog", "ready", "in_progress", "review", "blocked", "done", "validated"}
VALID_ADR_STATUS = {"proposed", "accepted", "superseded", "rejected"}


def frontmatter(path: Path) -> dict[str, str]:
    text = path.read_text(encoding="utf-8")
    if not text.startswith("---\n"):
        return {}
    try:
        _, body, _ = text.split("---", 2)
    except ValueError:
        return {}
    data: dict[str, str] = {}
    for raw in body.splitlines():
        line = raw.strip()
        if not line or line.startswith("#") or ":" not in line:
            continue
        key, value = line.split(":", 1)
        data[key.strip()] = value.strip()
    return data


def check_note(path: Path, required: set[str], valid_status: set[str]) -> list[str]:
    errors: list[str] = []
    fm = frontmatter(path)
    missing = required - fm.keys()
    if missing:
        errors.append(f"{path.relative_to(ROOT)} missing frontmatter: {sorted(missing)}")
    status = fm.get("status")
    if status and status not in valid_status:
        errors.append(f"{path.relative_to(ROOT)} invalid status: {status}")
    note_id = fm.get("id")
    if note_id and note_id not in path.name:
        errors.append(f"{path.relative_to(ROOT)} filename does not contain id {note_id}")
    return errors


def main() -> int:
    errors: list[str] = []

    for path in (DOCS / "40_Production" / "Tasks").glob("TASK-*.md"):
        errors.extend(check_note(path, REQUIRED_TASK_FIELDS, VALID_TASK_STATUS))

    for path in (DOCS / "60_Decisions" / "ADRs").glob("ADR-*.md"):
        errors.extend(check_note(path, REQUIRED_ADR_FIELDS, VALID_ADR_STATUS))

    # Warn about unresolved TBDs in accepted notes.
    for path in DOCS.rglob("*.md"):
        fm = frontmatter(path)
        if fm.get("status") == "accepted" and re.search(r"\bTBD\b", path.read_text(encoding="utf-8")):
            errors.append(f"{path.relative_to(ROOT)} is accepted but still contains TBD")

    if errors:
        print("Documentation checks failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    print("Documentation checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
