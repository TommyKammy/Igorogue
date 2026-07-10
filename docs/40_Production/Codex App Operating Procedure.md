---
type: process
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Codex App Operating Procedure

## Purpose

Use Codex as an implementation agent without allowing it to silently become the game designer, balance authority, release manager, or source of truth.

## Project selection

Open the Git repository root containing `AGENTS.md` and `Igorogue.sln`. Do not open only `docs/`, `src/`, or an old bundle folder as the Codex project root.

## First session

Use Local mode and paste `handoff/FIRST_PROMPT.txt`.

The first session is read-only. It must report:

- repository root;
- active instruction files;
- current Git branch and cleanliness;
- current authorized task;
- exact tool availability;
- governance-check result;
- blockers.

Do not authorize implementation until the report is correct.

## One TASK per task/worktree

Naming:

```text
Codex task: [TASK-0002] Deterministic RNG and Command Log
Branch: task/TASK-0002-deterministic-rng-command-log
Commit: feat(determinism): TASK-0002 add rng and command log
```

A task may include tests and documentation required by its acceptance criteria. It may not absorb unrelated cleanup.

## Local versus Worktree

### Use Local for

- TASK-0022 host evidence;
- generating and reviewing package locks;
- visual Godot review;
- integration and merge validation;
- work requiring an installed application that is unavailable in a managed environment.

### Use a Codex-managed Worktree for

- isolated implementation tasks after TASK-0022;
- independent code review;
- documentation-only changes;
- parallel tasks with no shared dependency surface.

### Do not parallelize

- a foundational interface and its first consumer;
- Domain state and replay serialization of that unfinished state;
- capture resolution and enemy AI depending on capture;
- content schema and several content migrations at once.

## Start protocol

Codex must begin every implementation task by printing:

1. TASK ID and status;
2. outcome;
3. non-goals;
4. acceptance criteria;
5. source-of-truth files read;
6. planned files;
7. planned validation;
8. any ambiguity or stop condition.

If the TASK is not `ready`, Codex must not implement it.

## Implementation protocol

- Make the smallest change that satisfies the TASK.
- Prefer pure functions and explicit immutable snapshots in Domain.
- Use explicit stable ordering for all gameplay outcomes.
- Add negative and boundary tests, not only happy paths.
- Keep runtime values in `game_data/`.
- Update generated content using repository wrappers.
- Do not hand-edit generated files unless the generator is the task subject.
- Do not edit Accepted design decisions without scope.

## Validation protocol

Use repository wrappers:

```bash
tools/dev/check
tools/dev/test
tools/dev/sim-smoke
```

When Godot is in scope:

```bash
GODOT_BIN="<ABSOLUTE_PATH>" tools/dev/godot-smoke
```

For export tasks:

```bash
GODOT_BIN="<ABSOLUTE_PATH>" tools/dev/export-windows
```

Record exact commands and exit codes in the TASK note.

## Status transitions

```text
backlog/blocked
→ ready: dependencies and specification complete
→ in_progress: Codex begins implementation
→ review: implementation and evidence ready for independent review
→ done: required technical evidence accepted
→ validated: design hypothesis proven at required evidence level
```

Codex may move `ready → in_progress → review`. Human review controls `review → done` unless the TASK explicitly delegates it.

## Closeout protocol

Before reporting completion, Codex must:

- inspect `git diff --check`;
- inspect the complete diff, including generated files;
- run required tests;
- update TASK Execution Log, Evidence, and Known Issues;
- state whether design assumptions changed;
- state what was not tested;
- request independent review.

## Worktree handoff

When implementation needs foreground inspection or visual testing, use the app's Handoff flow to move the task to Local. Do not try to check out the same branch simultaneously in Local and a worktree.

## Prompt set

Use:

- `codex-prompts/macos/00-first-session-read-only-audit.md`
- `codex-prompts/macos/01-task-0022-runtime-evidence.md`
- `codex-prompts/macos/02-implement-next-ready-task.md`
- `codex-prompts/macos/03-independent-review.md`
- `codex-prompts/macos/04-godot-visual-review.md`
- `codex-prompts/macos/05-stop-and-escalate.md`
