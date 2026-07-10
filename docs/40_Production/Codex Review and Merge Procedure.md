---
type: process
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Codex Review and Merge Procedure

## Separation of roles

Implementation and independent review must use separate Codex tasks/conversations. Prefer a separate worktree for review when practical.

The reviewer reads the source-of-truth documents and diff directly. The implementer's summary is context, not evidence.

## Review inputs

- TASK note
- complete diff against base branch
- Rules Canon and linked Feature Specs/ADRs
- `CODE_REVIEW.md`
- test output and artifacts
- generated-content diff
- replay/checksum evidence when applicable

## Required review commands

At minimum:

```bash
git status --short
git diff --check
git diff --stat <BASE>...HEAD
git diff <BASE>...HEAD
tools/dev/check
tools/dev/test
```

Add simulator, Godot, export, or fixture commands required by the TASK.

## Codex review mode

Use `/review` when available, or paste `codex-prompts/macos/03-independent-review.md`.

Review against the intended base branch. Do not review only uncommitted fragments when the task includes committed changes.

## Merge gates

A change may be merged only when:

- no BLOCKER or HIGH finding remains;
- acceptance criteria are evidenced;
- TASK note is current;
- test outputs are current for the reviewed commit;
- generated files match generators;
- visual changes have human sign-off;
- no unexpected version/dependency changes exist;
- `main` is clean and up to date.

## Human-owned decisions

Only the human maintainer decides:

- whether to commit reviewed lock files;
- whether a design tradeoff is accepted;
- whether visual quality is acceptable;
- whether to merge to `main`;
- whether a technical TASK is `done`;
- whether a feature is `validated` for fun.

## Merge strategy

Use a normal reviewed branch merge. Avoid force pushes and history rewriting.

Suggested sequence:

```bash
git switch main
git status --short
git pull --ff-only   # only when a remote exists
git merge --no-ff task/TASK-XXXX-slug
tools/dev/check
tools/dev/test
git status --short
```

Godot or export tasks require their corresponding post-merge smoke.

## Post-merge record

Update:

- Current Sprint
- Backlog
- TASK status and evidence
- relevant release note
- any new risk or Decision Needed note

Archive the Codex task only after the branch is safely merged or intentionally abandoned.

## Rejection and rework

When review finds issues:

- keep the task in `review` or return it to `in_progress`;
- provide specific findings with file/line and acceptance criterion;
- use the same task/worktree for corrections;
- rerun the full affected validation, not only the new test;
- obtain a second review when the correction changes architecture or rules.
