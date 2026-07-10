---
type: technical-spec
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Engine Toolchain and Repository Layout

## Purpose

This note operationalizes [[ADR-0001 Engine and Repository]]. ADR-0001 owns the decision; `toolchain/engine_decision.json` owns the current machine-readable toolchain selection; this note owns the intended repository and command contract.

## Selected stack

| Concern | Selection |
|---|---|
| Game engine | Godot 4.7 stable, .NET edition |
| Renderer | Compatibility |
| Production language | C# 12 |
| Runtime target | .NET 8 LTS (`net8.0`) |
| Unit/integration tests | xUnit through `dotnet test` |
| Runtime content | JSON validated from `game_data/` |
| Repository | Git, private GitHub host until demo decision |
| Primary target | Windows x86_64 |
| Secondary targets | Linux x86_64, macOS universal after primary smoke |
| Pixel presentation | 480×270 logical canvas, viewport stretch, integer scale |

## Authority boundary

Godot is a presentation and platform adapter, not the game authority.

```text
Input device / Bot
        ↓
Typed Application Command
        ↓
Igorogue.Application
        ↓
Igorogue.Domain
        ↓
Ordered Domain Events + immutable snapshot
   ├── Godot presentation
   ├── Replay writer
   ├── Telemetry
   └── Formal simulator
```

The following must be possible before M2:

```text
dotnet test
```

This command must validate board, capture, territory, cards, enemy intent, replay, and state-machine behavior without launching Godot.

## Minimum solution structure

```text
Igorogue.sln
src/
  Igorogue.Domain/
  Igorogue.Application/
  Igorogue.Content/
game/
  Igorogue.Godot/
tests/
  Igorogue.Domain.Tests/
  Igorogue.Application.Tests/
  Igorogue.Architecture.Tests/
tools/
  Igorogue.Sim.Cli/
```

### Igorogue.Domain

Owns deterministic value types and rule transformations.

Examples:

- CanonicalPoint
- BoardState
- Group and LibertySet
- TerritoryRegion
- BattleState
- command validation and resolution
- ordered Domain events
- deterministic RNG interfaces and stream state

It has no Godot reference and no file, clock, process, or UI access.

### Igorogue.Application

Owns use cases and state machines.

Examples:

- StartBattle
- PlayCard
- EndPlayerTurn
- ResolveEnemyTurn
- SaveReplay
- Run progression

It depends on Domain abstractions and receives content or persistence through explicit interfaces.

### Igorogue.Content

Owns DTOs, schema-aware loading, ID resolution, content hash, and conversion into Domain definitions. It reads the same JSON snapshot for game, tests, and simulator.

### Igorogue.Godot

Owns scenes, Control nodes, animation, input adapters, audio, localization presentation, and export configuration.

It may:

- create typed commands;
- render snapshots;
- animate ordered events;
- request content through the Application boundary.

It may not:

- calculate capture or territory independently;
- mutate authoritative board state;
- choose enemy moves outside FEAT-009;
- use frame order as a game rule.

### Igorogue.Sim.Cli

Runs battles and acts in normal console processes. It links the same Application, Domain, and Content projects as Godot. Its policy selects among legal commands exposed by the Application layer.

## Content synchronization

Because Godot exports resources under its project root while `game_data/` remains repository-level source of truth, M0 creates a deterministic generated snapshot.

```text
game_data/**/*.json
    ↓ validate + canonicalize + hash
build/generated_content/
    ↓ copy
 game/Igorogue.Godot/generated_content/
```

Rules:

- the generated directory carries a manifest and content hash;
- no human edits are accepted there;
- CI compares source and generated hashes;
- release builds regenerate content from a clean checkout;
- replay metadata stores the source content hash, not file timestamps.

## Toolchain pinning

### Godot

- `.godot-version` contains `4.7-stable`.
- M0 verifies the executable reports the exact allowed version and has .NET support.
- `GODOT_BIN` is the only supported executable override.
- export templates are pinned to the same engine version.

### .NET

- M0 creates `global.json` for a specific .NET 8 SDK patch.
- `rollForward` must not silently select a new major runtime.
- C# language version is explicitly pinned to 12.0.
- package versions use Central Package Management and lock files.

## Planned command wrappers

M0 supplies both PowerShell and POSIX wrappers with equivalent behavior.

```text
tools/dev/check
tools/dev/build
tools/dev/test
tools/dev/sim-smoke
tools/dev/godot-smoke
tools/dev/export-windows
```

The wrappers perform tool-version verification before invoking build commands. `AGENTS.md` references the wrappers after TASK-0001 is complete.

## CI stages

### Stage 1 — Governance

```text
python tools/check_all.py
```

### Stage 2 — Pure .NET

```text
dotnet restore Igorogue.sln --locked-mode
dotnet build Igorogue.sln -c Release --no-restore
dotnet test Igorogue.sln -c Release --no-build
dotnet run --project tools/Igorogue.Sim.Cli -- --smoke
```

### Stage 3 — Godot integration

```text
$GODOT_BIN --headless --path game/Igorogue.Godot --build-solutions --quit
$GODOT_BIN --headless --path game/Igorogue.Godot --editor --quit-after 1
```

### Stage 4 — Export smoke

A Windows release export runs for tags and release candidates. Linux follows after Windows works from a clean runner. macOS signing/notarization remains a later platform task.

## Architecture enforcement

`Igorogue.Architecture.Tests` must fail if:

- Domain references an assembly whose name begins with `Godot`;
- Domain or Application references the Godot presentation project;
- Godot is referenced by the simulator;
- a forbidden dependency direction appears;
- authoritative Domain types expose Godot types in public signatures.

The project files also enforce references structurally; the architecture test is a second line of defense.

## Godot source-control policy

Track:

- `project.godot`
- `.tscn`
- `.tres`
- C# source
- export presets without secrets
- imported source assets

Ignore:

- `.godot/`
- local editor settings
- generated build output
- generated content snapshot if CI regenerates it
- platform signing credentials

Codex may change `.tscn` or `.tres` only under an explicit UI task. Every such change requires a Godot headless parse/build check and a human visual review.

## Upgrade policy

### Maintenance patch in Godot 4.7 line

Requires:

- a toolchain task;
- official stable release only;
- pure .NET tests pass;
- Godot smoke and exports pass;
- golden replay hashes remain stable;
- visual regression checklist is completed.

### Godot 4.8 or another engine

Requires a successor ADR and a migration spike. The existence of a newer version is not sufficient justification.

## M0 exit evidence

TASK-0001 is complete only after a clean checkout demonstrates:

1. the empty Godot .NET project opens and runs;
2. a pure Domain xUnit test passes without Godot;
3. a Godot headless smoke scene loads the Application adapter and exits successfully;
4. a Windows debug export is created by command line;
5. the tool-version verifier rejects a wrong Godot version or the non-.NET editor;
6. all repository paths and commands documented here exist.
