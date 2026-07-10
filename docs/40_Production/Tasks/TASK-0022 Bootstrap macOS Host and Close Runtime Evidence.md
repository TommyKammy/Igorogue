---
type: task
id: TASK-0022
status: review
project: Igorogue
milestone: M0
priority: P0
dependencies: [TASK-0019]
updated: 2026-07-11
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

2026-07-11 — local and clean-checkout runtime gates passed; CI pending:

- Installed and verified .NET SDK 8.0.422 arm64 and Godot 4.7 stable .NET with matching export templates.
- Generated all eight authentic NuGet lock files twice; the second generation was stable. Human review preceded commit `70f0eec`.
- Passed governance, locked restore, Release build, 11 xUnit tests, repeated simulator smoke, Godot .NET smoke, and Windows debug export in the task worktree.
- Fixed an approved bootstrap defect in `c1e1998`: Godot now has its required local solution/UID; export must contain the managed assembly; platform restore locks cannot rewrite committed locks; CI archives the complete export.
- Repeated the full required sequence from a detached clean worktree at `c1e1998` with no tracked or untracked prerequisite.

2026-07-10 — CI preflight only; the macOS host evidence sequence has not started:

- Draft PR #1 imported the v0.2.10 bootstrap baseline and started the repository CI.
- The first run exposed `CS0120` in the bootstrap application test fixture because a nested instance property shadowed the outer test constant.
- After human approval, commit `14cf9c3` qualified the constant reference without changing test expectations or production behavior.
- The rerun completed governance, pure .NET build/test/simulator, Godot .NET headless smoke, and Windows debug export successfully.

## Evidence

- Runtime report: [[TASK-0022 macOS Runtime Evidence]]
- Runtime implementation commit: `c1e1998d34f7e9abbb8962b7cc34897ebd9675a1`
- Authentic lock commit: `70f0eec`
- Local and clean-checkout governance, locked restore, build, xUnit, simulator, Godot smoke, and managed Windows export: passed.
- Simulator checksum: `3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd` on both runs.
- Content hash: `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`.
- Clean-checkout Windows executable SHA-256: `0aeded8aaf1b7398549906215aa5ec1cbc16262055b3dba555d036b69fe71d5a`.
- Stage 4 CI: pending.

### Prior CI preflight

- Pull request: `https://github.com/TommyKammy/Igorogue/pull/1`
- Successful CI run: `https://github.com/TommyKammy/Igorogue/actions/runs/29098393726`
- Reviewed fix commit: `14cf9c3a38a1946c5cbc3888e447cff3e43b86ba`
- Governance and generated content: passed.
- Pinned .NET verification, lock generation, locked restore, build, xUnit, and simulator smoke: passed.
- Pinned Godot .NET verification, C# headless build, bootstrap scene, Windows debug export, and artifact upload: passed.

## Known issues

- Stage 4 CI has not yet run on the branch containing the committed locks and managed-export validation. TASK-0022 remains `review` until all three jobs are green.
- PowerShell wrapper parity was reviewed statically on macOS; `pwsh` is not installed on this host, so the PowerShell wrapper was not executed locally.
- Separate Windows debug exports were not byte-identical. TASK-0022 requires an integrity sidecar, not reproducible build bytes; simulator and Godot runtime checksums remained identical.
- CI reports non-blocking Node.js 20 deprecation warnings for current action versions.
