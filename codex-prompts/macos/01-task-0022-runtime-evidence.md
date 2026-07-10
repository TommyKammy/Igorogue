# Codex Prompt — TASK-0022 macOS Runtime Evidence

Implement `docs/40_Production/Tasks/TASK-0022 Bootstrap macOS Host and Close Runtime Evidence.md`.

## Before any change

1. Read root `AGENTS.md`, `CODEX_MAC_HANDOFF.md`, Source of Truth Map, TASK-0022, ADR-0001, Repository Bootstrap Status, and `CODE_REVIEW.md`.
2. Confirm TASK-0022 is `ready`.
3. Restate outcome, non-goals, allowed areas, acceptance criteria, human approval gates, and stop conditions.
4. Show the planned commands and files.

## Hard restrictions

- Do not install or upgrade tools without explicit human approval.
- Do not change any version pin.
- Do not add/update packages.
- Do not edit gameplay code, scenes, resources, or export presets.
- Do not commit lock files until the human has reviewed their diff.
- Do not mark TASK-0001, TASK-0020, or TASK-0022 done without clean-checkout and CI evidence.

## Execute in stages

### Stage 0

Repeat the read-only audit. Stop if not ready.

### Stage 1

Run governance checks and generate authentic locks twice. Present the full lock-file diff and stop for human approval before commit.

### Stage 2

After approval/commit, run locked restore, build/tests, simulator smoke twice, Godot smoke, and Windows debug export. Record exact output and hashes.

### Stage 3

Repeat the required sequence from a clean checkout/worktree of the reviewed commit.

### Stage 4

Verify all CI jobs on the reviewed commit. If no remote CI exists, leave status in review and report that sole gap.

## Evidence

Create the files required by TASK-0022. Redact private paths and secrets. Update TASK notes truthfully.

## Final response

- result by acceptance criterion;
- files changed;
- commands and exit codes;
- simulator checksums;
- export hash;
- CI status;
- remaining blockers;
- recommended next status.
