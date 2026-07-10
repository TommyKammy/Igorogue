# Igorogue — macOS Codex App Development Handoff

- Handoff package: `v0.2.10_CODEX_MAC_HANDOFF`
- Baseline: `v0.2.9_REPOSITORY_BOOTSTRAP`
- Date: 2026-07-10
- Development host: macOS
- Development agent: Codex in the ChatGPT desktop app
- Current evidence level: static repository evidence only
- First authorized execution task: `TASK-0022`

## 1. What is being handed over

Igorogue is a PC roguelite combining Go-inspired liberties, capture, and territory with deckbuilding and production-engine acceleration.

The repository currently contains:

- accepted player-visible rules and design decisions;
- deterministic specification fixtures for coordinates, repetition, facilities, Momentum, counterattack, enemy intent, and temporary-liberty expiry;
- a Godot 4.7 stable .NET / C# 12 repository skeleton;
- pure .NET Domain, Application, and Content projects;
- an initial formal simulator smoke entry point;
- governance checks, content synchronization, CI definitions, and Codex instructions;
- UI/UX mockups and an Obsidian design/production Vault.

The repository does **not** yet contain the real gameplay Rules Kernel. The current C# code is a bootstrap smoke scaffold.

## 2. Current gate

Do not begin gameplay implementation yet.

The immediate goal is to prove that the accepted toolchain and repository bootstrap execute on the macOS host:

1. exact .NET SDK verification;
2. authentic NuGet lock generation;
3. locked restore;
4. build and xUnit tests;
5. formal simulator smoke;
6. Godot .NET headless build and smoke;
7. Windows debug cross-export with SHA-256;
8. green CI on the reviewed commit.

Use `docs/40_Production/Tasks/TASK-0022 Bootstrap macOS Host and Close Runtime Evidence.md`.

`TASK-0002 Deterministic RNG and Command Log` remains blocked until this gate is closed.

## 3. Safe extraction and Git baseline

Extract this package into a normal local development directory, preferably outside iCloud Drive, Dropbox, or another continuously synchronized folder.

Suggested location:

```bash
mkdir -p "$HOME/Developer"
cd "$HOME/Developer"
unzip /path/to/Igorogue_Design_Development_Vault_v0.2.10_CODEX_MAC_HANDOFF.zip
cd Igorogue_Project
```

Confirm wrapper permissions:

```bash
ls -l tools/dev/check tools/dev/test tools/ci/install_godot.sh
```

If the archive is not already a Git repository, create the immutable handoff baseline before opening worktrees:

```bash
git init -b main
git add .
git commit -m "chore(repo): import Igorogue v0.2.10 Codex handoff baseline"
git status --short
```

The final command must produce no output.

Do not import the historical bundle directory as the active repository. Use this package's `Igorogue_Project` root.

## 4. Exact local tools

The accepted versions are authoritative:

```text
Godot: 4.7-stable .NET editor
Renderer: Compatibility
.NET SDK: 8.0.422 exactly
Target framework: net8.0
C#: 12.0
Python: 3.12 recommended for parity with CI
```

Do not change `global.json`, `.godot-version`, `.dotnet-version`, `Directory.Packages.props`, or `toolchain/*.json` merely because another version is easier to install.

Read `docs/30_Technical/macOS Development Host Setup.md` before installing or changing tools.

## 5. Opening the project in the Codex app

1. Open the ChatGPT desktop app on macOS.
2. Choose Codex and open the `Igorogue_Project` folder.
3. Use **Local** for the initial host/runtime evidence task because it relies on locally installed SDKs, the Godot application bundle, and human review of generated lock files.
4. Paste the contents of `handoff/FIRST_PROMPT.txt`.
5. Do not authorize edits until the read-only audit reports the expected repository root, instruction chain, Git state, and tool availability.

After TASK-0022 is complete, use one Codex worktree per independent TASK. Worktrees require this folder to be a Git repository.

## 6. Task model

Each Codex task must follow this lifecycle:

```text
ready TASK
→ read source of truth
→ restate outcome/non-goals/acceptance
→ list planned files and validation
→ implement minimum change
→ add tests/evidence
→ run checks
→ update TASK note
→ independent review
→ human merge decision
```

One conversation/worktree equals one TASK. Do not mix design repair, gameplay implementation, balance tuning, and UI polish in one task.

Use the prompts under `codex-prompts/macos/`.

## 7. Human approval gates

Codex must stop and request approval before:

- installing or upgrading SDKs, engines, packages, or system tools;
- changing accepted version pins;
- adding a production dependency;
- changing an accepted ADR or player-visible rule outside explicit task scope;
- editing `.tscn`, `.tres`, `project.godot`, or export presets without explicit authorization;
- committing generated lock files before a human has reviewed the diff;
- merging to `main`;
- deleting or rewriting Git history;
- treating abstract-proxy results as product balance evidence.

## 8. Current technical queue

After TASK-0022 is fully complete and TASK-0001 is closed:

1. `TASK-0002` — deterministic RNG and command log;
2. `TASK-0003` — coordinates and orthogonal neighbours;
3. `TASK-0004` — stone groups and unique liberty sets;
4. `TASK-0005` — hypothetical placement and capture resolution;
5. `TASK-0006` — suicide legality and terminal capture;
6. `TASK-0007` — king capture and battle result;
7. `TASK-0008` — territory region calculation;
8. `TASK-0009` — golden board fixtures;
9. `TASK-0010` — headless battle state machine;
10. `TASK-0011` — replay round trip.

Run these serially until Domain interfaces stabilize. Do not parallelize a foundation task with its consumer.

## 9. Evidence standard

A task is not complete because code was written.

Minimum evidence:

- exact commands and exit codes;
- relevant test output;
- Git diff reviewed against source-of-truth documents;
- deterministic seed/checksum evidence where applicable;
- TASK Execution Log and Evidence updated;
- known limitations stated honestly.

Gameplay-fun claims require human playtest evidence. Abstract proxy simulations remain E2 design evidence only.

## 10. Read order for Codex

1. `AGENTS.md`
2. this file
3. `docs/00_Home/Source of Truth Map.md`
4. `docs/00_Home/Current Development State.md`
5. the selected TASK note
6. all Feature Specs and ADRs linked by that TASK
7. `CODE_REVIEW.md` before closeout

## 11. Key documents

- `docs/00_Home/Codex Mac Handoff.md`
- `docs/30_Technical/macOS Development Host Setup.md`
- `docs/40_Production/Codex App Operating Procedure.md`
- `docs/40_Production/Codex Review and Merge Procedure.md`
- `docs/40_Production/Codex Stop and Escalation Rules.md`
- `docs/40_Production/Codex Task Queue.md`
- `CODE_REVIEW.md`
- `codex-prompts/macos/`

## 12. Definition of a successful handoff

The handoff is successful when a fresh Codex session can:

- identify TASK-0022 as the only immediately authorized execution task;
- explain why gameplay implementation is blocked;
- run the read-only audit without modifying files;
- execute the runtime-evidence task without changing version pins;
- produce reviewable evidence and stop at human approval gates;
- move subsequent independent TASKs into isolated worktrees.
