---
type: task
id: TASK-0021
status: done
project: Igorogue
milestone: M0
priority: P0
dependencies: [TASK-0019]
updated: 2026-07-10
---
# TASK-0021 Prepare macOS Codex App Handoff

## Outcome

Create a self-contained handoff package and operating instructions for continuing Igorogue development in the macOS Codex app.

## Source of truth

- [[ADR-0001 Engine and Repository]]
- [[Codex Operating Model]]
- [[Definition of Ready and Done]]
- [[Repository Bootstrap Status]]

## Non-goals

- no gameplay Rules Kernel implementation;
- no toolchain version changes;
- no balance changes;
- no claim that .NET/Godot runtime evidence exists.

## Acceptance criteria

- root handoff document;
- macOS host setup runbook;
- Codex Local/worktree procedure;
- independent review and merge procedure;
- stop/escalation rules;
- copy-paste prompts for first audit and TASK-0022;
- nested AGENTS instructions for specialized directories;
- a single ready operational task for the Mac host;
- package checks pass.

## Allowed areas

Documentation, Codex prompts, AGENTS instructions, project dashboards, package manifest, and handoff metadata.

## Validation

```bash
tools/dev/check
python3 tools/check_links.py
```

## Execution log

2026-07-10: Added v0.2.10 macOS Codex handoff package, operating procedures, task queue, first-session prompts, and nested instruction files.

## Evidence

- `CODEX_MAC_HANDOFF.md`
- `handoff/FIRST_PROMPT.txt`
- `docs/00_Home/Codex Mac Handoff.md`
- `docs/30_Technical/macOS Development Host Setup.md`
- `docs/40_Production/Codex App Operating Procedure.md`
- `docs/40_Production/Codex Review and Merge Procedure.md`
- `docs/40_Production/Codex Stop and Escalation Rules.md`
- `codex-prompts/macos/`

## Known issues

Actual macOS .NET/Godot runtime execution remains the responsibility of TASK-0022.
