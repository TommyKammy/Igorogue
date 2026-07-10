---
type: task
id: TASK-0022
status: ready
project: Igorogue
milestone: M0
priority: P0
dependencies: [TASK-0019]
updated: 2026-07-10
---
# TASK-0022 Bootstrap macOS Host and Close Runtime Evidence

## Outcome

On the configured macOS development host, produce authentic runtime evidence for the v0.2.9 repository bootstrap, commit reviewed NuGet lock files, and close TASK-0001/TASK-0020 if every gate passes.

## Source of truth

- root `CODEX_MAC_HANDOFF.md`
- [[ADR-0001 Engine and Repository]]
- [[Engine Toolchain and Repository Layout]]
- [[Repository Bootstrap Status]]
- [[macOS Development Host Setup]]
- [[TASK-0001 Decide Engine and Repository]]
- [[TASK-0020 Review Repository Bootstrap Runtime Evidence]]

## Non-goals

- no gameplay code;
- no new packages;
- no package-version upgrades;
- no version-pin changes;
- no design changes;
- no Godot scene/resource edits;
- no broad repository refactoring.

## Allowed areas

- authentic `packages.lock.json` files;
- TASK-0001, TASK-0020, TASK-0022 evidence/status notes;
- `docs/50_Validation/Runtime Evidence/`;
- build/export outputs, which remain ignored except checksums or explicitly approved evidence;
- minimal bootstrap defect fixes only after a separate human approval.

## Stage 0 — read-only audit

Run the prompt in `codex-prompts/macos/00-first-session-read-only-audit.md`.

Do not edit files. If exact tools are absent, stop with a host-tool blocker report.

## Stage 1 — lock generation

After tool verification and human approval:

```bash
tools/dev/check
tools/dev/update-locks
find . -name packages.lock.json -print | sort
git diff -- ':(glob)**/packages.lock.json' Directory.Packages.props NuGet.Config
```

Generate twice; the second generation must create no diff.

Stop for human review before committing lock files.

## Stage 2 — local runtime evidence

After reviewed lock files are committed on the task branch:

```bash
tools/dev/restore
tools/dev/test
tools/dev/sim-smoke
tools/dev/sim-smoke
GODOT_BIN="<ABSOLUTE_PATH>" tools/dev/godot-smoke
GODOT_BIN="<ABSOLUTE_PATH>" tools/dev/export-windows
```

Record exact outputs and exit codes. Simulator checksums must match.

## Stage 3 — clean-checkout evidence

From the reviewed commit, use a disposable clone or worktree with no untracked prerequisites and rerun:

```bash
tools/dev/check
tools/dev/restore
tools/dev/test
tools/dev/sim-smoke
GODOT_BIN="<ABSOLUTE_PATH>" tools/dev/godot-smoke
GODOT_BIN="<ABSOLUTE_PATH>" tools/dev/export-windows
```

## Stage 4 — CI

Push the reviewed branch or open a pull request. All three jobs must be green:

- governance;
- pure .NET build/test/simulator;
- Godot headless smoke and Windows debug export.

If no remote/CI is configured, leave the task in `review`; do not claim full completion.

## Acceptance criteria

- exact tool verifier passes;
- all expected `packages.lock.json` files are authentic, reviewed, and committed;
- second lock generation is stable;
- locked restore succeeds;
- xUnit passes;
- two simulator smoke runs emit identical checksum/content hash;
- Godot headless smoke exits 0;
- Windows debug export and SHA-256 sidecar exist;
- the same sequence succeeds from a clean checkout;
- CI is green on the reviewed commit;
- runtime evidence redacts private absolute paths;
- TASK-0001 and TASK-0020 are updated truthfully.

## Validation evidence location

Create:

```text
docs/50_Validation/Runtime Evidence/TASK-0022 macOS Runtime Evidence.md
docs/50_Validation/Runtime Evidence/data/TASK-0022-command-log.txt
docs/50_Validation/Runtime Evidence/data/TASK-0022-artifact-hashes.json
```

Record:

- `sw_vers` summary;
- `uname -m`;
- exact tool versions;
- Git commit;
- content hash;
- commands, exit codes, checksums;
- CI run reference;
- untested or failed criteria.

Do not record username, machine serial, tokens, or private absolute paths.

## Stop conditions

- exact tool version missing;
- lock generation changes central package versions unexpectedly;
- build requires source changes outside bootstrap scope;
- simulator outputs differ;
- Godot standard edition is installed instead of .NET edition;
- export requires an unapproved preset edit;
- CI differs from local evidence.

## Execution log

Not started.

## Evidence

Not created.

## Known issues

Windows export on macOS requires the matching Godot export templates. CI remains a mandatory final gate.
