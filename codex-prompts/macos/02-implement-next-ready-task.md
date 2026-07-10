# Codex Prompt — Implement the Next Explicitly Named Ready TASK

Implement only this TASK:

```text
<TASK FILE PATH>
```

Do not select another backlog item.

## Start

1. Read all active AGENTS instructions.
2. Read `CODEX_MAC_HANDOFF.md`, Source of Truth Map, Current Development State, the TASK, linked Feature Specs/ADRs, and `CODE_REVIEW.md`.
3. Confirm the TASK status is `ready` and every dependency is done.
4. Restate outcome, non-goals, acceptance criteria, allowed areas, and stop conditions.
5. List planned files and validation commands.
6. Stop before editing if any ambiguity changes player-visible behavior or accepted architecture.

## Implement

- Make the smallest accepted change.
- Add tests before or with behavior.
- Preserve deterministic ordering and checksum/replay semantics.
- Do not duplicate rules outside Domain/Application.
- Use `game_data/` for runtime values.
- Do not perform unrelated refactors.

## Validate

Run the TASK-specific checks plus:

```bash
tools/dev/check
tools/dev/test
git diff --check
```

Add simulator/Godot/export checks only when in scope.

## Closeout

Update the TASK Execution Log, Evidence, Known Issues, and status to `review`. Do not merge or mark `done`. Request an independent review using `03-independent-review.md`.
