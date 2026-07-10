---
type: adr
status: accepted
project: Igorogue
updated: 2026-07-10
id: ADR-0001
---
# ADR-0001 Engine and Repository

## Status

Accepted on 2026-07-10 for M0 through M3.

This ADR selects the engine, production language, Rules Kernel boundary, repository host, CI shape, and version policy. It does **not** claim that the empty Godot project or .NET solution already exists; those artifacts are created and verified by [[TASK-0001 Decide Engine and Repository]].

## Context

Igorogue requires all of the following at the same time.

1. The live game, replay runner, and formal simulator use one deterministic Rules Kernel.
2. Most rule tests run without a graphics editor, GPU, audio device, or Godot process.
3. The presentation layer supports a dense 7×7 board, cards, intent previews, pixel-art animation, mouse, keyboard, and gamepad.
4. Codex can work in small Git worktrees with text-first source files and reliable command-line checks.
5. The first commercial target is desktop PC; web export is not a requirement through M6.
6. The project must remain affordable and maintainable for an independent developer.

The decision must avoid two opposite failures:

- binding game truth to engine nodes, which would make headless simulation slow and fragile;
- choosing a bare framework that makes the UI, layout, animation, accessibility, and content iteration cost dominate development.

## Hard gates

A candidate is rejected if it cannot satisfy all of these without maintaining a second rules implementation.

| Gate | Required result |
|---|---|
| Shared rules | One authoritative Rules Kernel serves UI, replay, Bot, and simulator |
| Headless tests | Domain and application tests run from a normal CLI process |
| Deterministic state | No engine type, frame delta, scene order, or renderer state is required to resolve a command |
| Desktop delivery | Windows is supported first; Linux and macOS remain practical secondary targets |
| 2D UI | Board, cards, overlays, animation, and integer-scaled pixel presentation are practical |
| Source control | Runtime code and scene/config data can be reviewed in Git |

Godot, Unity, and MonoGame pass these gates if they are used with the architecture defined below.

## Evaluation criteria

Scores use a 1–5 scale. Weighted totals are out of 100. The machine-readable source is `toolchain/engine_decision.json`.

| Criterion | Weight | Godot 4.7 .NET | Unity 6 | MonoGame 3.8.4 |
|---|---:|---:|---:|---:|
| Engine-independent Domain and headless execution | 22 | 5 | 4 | 5 |
| Deterministic tests, replay, and formal simulation | 16 | 5 | 4 | 5 |
| 2D UI and pixel-art iteration speed | 18 | 5 | 5 | 2 |
| Command-line CI, build, and export | 12 | 4 | 4 | 5 |
| Codex, Git diff, and worktree friendliness | 12 | 4 | 3 | 5 |
| Desktop distribution coverage | 8 | 4 | 5 | 4 |
| Licensing, cost predictability, and lock-in | 8 | 5 | 2 | 5 |
| Debugging and editor tooling | 4 | 4 | 5 | 3 |
| **Weighted total** | **100** | **92.8** | **80.4** | **86.0** |

### Godot 4.7 .NET

Strengths:

- Godot supplies a focused 2D editor, Control-node UI system, animation tools, input, audio, and desktop export without requiring Igorogue to build an editor or GUI toolkit.
- The .NET edition supports C#, while the Rules Kernel can remain ordinary engine-independent .NET assemblies.
- Godot supports headless command-line operation and command-line export, which fits CI and smoke testing.
- Viewport and integer scaling directly support the planned pixel-art presentation.
- The engine is MIT licensed, reducing revenue-threshold and subscription risk.

Risks:

- Godot C# requires the .NET editor build rather than the standard executable.
- Godot C# does not provide web export; this is accepted because PC is the declared target.
- `.tscn` and `.tres` are text, but complex hand edits can still be unsafe. Codex may only edit scene/resource files when the task explicitly authorizes it and a Godot smoke check follows.
- Godot types can leak into the Domain unless project references and architecture tests enforce the boundary.

### Unity 6

Strengths:

- Mature C# tooling, 2D features, profiler, editor ecosystem, desktop export, test framework, and command-line build support.

Reasons not selected:

- The editor and CI footprint are heavier for a 7×7 deterministic card/board game.
- Serialized scenes and imported asset metadata create more worktree and review noise.
- Licensing and subscription requirements introduce an avoidable business-policy dependency. Unity Personal remains usable under its current revenue/funding threshold, and the runtime fee was cancelled, but plan terms and pricing remain external operational constraints.
- Unity offers no decisive benefit over Godot for Igorogue's current PC-only 2D scope.

### MonoGame 3.8.4

Strengths:

- Pure C#, excellent control over deterministic architecture, straightforward CLI tests, compact source control footprint, and open-source licensing.

Reasons not selected:

- Igorogue has UI-heavy card interactions, layered board previews, intent overlays, animation sequencing, localization, accessibility, and multiple menus.
- MonoGame would require substantially more custom layout, widgets, editor tooling, input focus, animation orchestration, and asset workflow before the core game can be tested.
- Its architectural advantages are retained by placing Igorogue's Domain in plain .NET while using Godot only as the presentation shell.

## Decision

### Engine

Use **Godot 4.7 stable, .NET edition**.

- M0 pins `4.7-stable` in `.godot-version` and downloads or verifies the matching official .NET editor and export templates.
- Release candidates, betas, dev snapshots, and Godot 4.8 are prohibited on `main`.
- A 4.7 maintenance update may be adopted by a toolchain task after all Domain tests, Godot smoke tests, golden replays, and desktop export checks pass.
- Moving to another minor or major Godot line requires a successor ADR.

### Language and runtime

Use **C# 12 on .NET 8 LTS** for production runtime code through M3.

- `Igorogue.Domain`, `Igorogue.Application`, content loading, replay, Bot policies, and the formal simulator are C#.
- The Godot presentation adapter is C#.
- Runtime GDScript is not used through M3. A narrowly scoped editor-only script or plugin requires an explicit task and may not contain game rules.
- The exact .NET 8 SDK patch is pinned by `global.json` during TASK-0001 and updated only through a toolchain task.

### Renderer and logical presentation

Use Godot's **Compatibility renderer** for M0–M3.

- The existing 480×270 logical UI canvas remains the design baseline.
- Use viewport stretching with integer scaling and letterboxing when the window is not an integer multiple.
- Rendering and animation never influence Domain command resolution.

### Repository and hosting

Use a single **Git repository hosted privately on GitHub** until the demo release decision.

- `main` is the protected integration branch.
- One approved TASK maps to one Codex thread, one worktree, and one short-lived branch.
- Pull requests require automated checks and human review before merge.
- Obsidian notes, source, tests, schemas, content, and build scripts live in the same repository.
- Use Git LFS only for large source-art/audio formats such as `.aseprite`, `.psd`, `.wav`, `.flac`, and video. Small runtime PNGs remain normal Git objects unless repository growth proves otherwise.
- No Git submodules or runtime package marketplace dependencies are allowed through M3 without a separate ADR or dependency review.

## Repository layout

TASK-0001 creates this minimum structure.

```text
Igorogue.sln
├── src/
│   ├── Igorogue.Domain/             # Pure deterministic rules; no Godot references
│   ├── Igorogue.Application/        # Commands, orchestration, battle/run state machines
│   └── Igorogue.Content/            # Runtime DTOs, validation, content hashing
├── game/
│   └── Igorogue.Godot/              # project.godot, scenes, C# presentation adapters
├── tests/
│   ├── Igorogue.Domain.Tests/
│   ├── Igorogue.Application.Tests/
│   └── Igorogue.Architecture.Tests/
├── tools/
│   ├── Igorogue.Sim.Cli/            # Formal headless simulator using Application
│   └── existing Python governance tools
├── game_data/                        # Runtime content source of truth
├── docs/                             # Obsidian Vault
└── build/                            # Generated artifacts; ignored by Git
```

Projects may be split further after M3 only when a measured build, ownership, or dependency problem justifies it.

## Dependency rule

```text
Igorogue.Domain
      ↑
Igorogue.Application ← Igorogue.Content
      ↑                    ↑
Igorogue.Sim.Cli       Igorogue.Godot
```

The actual project references must preserve these constraints:

- `Igorogue.Domain` references no Godot package and no other Igorogue runtime project.
- `Igorogue.Application` may reference Domain, but not Godot.
- `Igorogue.Content` may reference Domain value types, but not Application or Godot.
- `Igorogue.Godot` may reference Application, Content, and Domain.
- `Igorogue.Sim.Cli` may reference Application, Content, and Domain.
- Tests may reference the project under test, never the reverse.

Domain code must not use:

- `Godot.*` types;
- `Node`, `Resource`, `Vector2`, or engine object instance IDs;
- frame delta, wall-clock time, locale, renderer state, filesystem enumeration order, or engine RNG;
- floating-point values for authoritative gameplay counters when an integer or fixed-point representation is defined;
- unordered collection iteration as a tie-breaker.

## Data flow

`game_data/` remains the content source of truth outside the Godot project directory.

TASK-0001 creates a deterministic content-sync step that:

1. validates JSON and schemas;
2. computes the content hash;
3. copies an exact generated snapshot into `game/Igorogue.Godot/generated_content/`;
4. allows Godot export to include the same data used by tests and the simulator;
5. fails CI if the generated snapshot is stale.

The generated Godot content directory is not manually edited.

## Testing and CI contract

Use **xUnit** with the normal .NET test runner for Domain and Application tests. Do not make the core test suite depend on a Godot test plugin.

The planned command contract is:

```text
python tools/check_all.py
dotnet restore Igorogue.sln --locked-mode
dotnet build Igorogue.sln -c Release --no-restore
dotnet test Igorogue.sln -c Release --no-build
dotnet run --project tools/Igorogue.Sim.Cli -- --smoke
godot --headless --path game/Igorogue.Godot --build-solutions --quit
godot --headless --path game/Igorogue.Godot --editor --quit-after 1
```

TASK-0001 provides cross-platform wrapper scripts so CI and Codex do not depend on a developer-specific executable name. The wrapper reads `GODOT_BIN`, verifies the engine version, and rejects a non-.NET Godot executable.

CI is split into independent jobs:

1. documentation/content checks;
2. .NET restore, build, unit, property, and architecture tests;
3. formal simulator smoke run;
4. Godot headless import/build and one presentation smoke scene;
5. desktop export smoke on release branches or tags.

The first three jobs must not launch Godot.

## Codex operating constraints

- Prefer changes to C#, JSON, Markdown, schemas, and tests.
- Do not hand-edit `.tscn`, `.tres`, `project.godot`, or export presets unless the TASK explicitly lists them in scope.
- A task that changes a Godot scene or resource must include a headless import/build check and a human visual review note.
- Godot-generated `.godot/` import/cache data is ignored and never committed.
- The scene tree is presentation state, not authoritative game state.
- UI code submits typed commands and consumes immutable Domain events or snapshots; it never mutates board arrays directly.

## Version and dependency policy

- Pin the Godot stable line, .NET SDK patch, NuGet package versions, and export templates.
- Commit `global.json`, `Directory.Packages.props`, and NuGet lock files once TASK-0001 creates them.
- Prefer BCL and Godot built-ins. Every additional runtime dependency needs a short dependency record covering purpose, license, update owner, and removal path.
- No engine upgrade is merged solely because a newer release exists.
- Patch upgrades require unchanged golden replay hashes unless an accepted rule change explains the difference.

## Consequences

### Positive

- The core rules can be implemented and tested before the game UI exists.
- Formal simulation can scale through normal .NET processes instead of editor instances.
- Godot handles the UI-heavy parts that would make MonoGame expensive.
- The project avoids Unity subscription and policy exposure while retaining C#.
- Codex receives strong typed boundaries, command-line checks, and a text-first repository.

### Negative

- Two toolchains are required: the .NET SDK and Godot .NET editor/export templates.
- The team must actively prevent Godot types and scene state from leaking into Domain logic.
- C# Godot projects cannot target web under the current Godot platform support; adding web would force a new architecture decision.
- Godot scene files still need editor-based visual validation.
- The initial repository bootstrap is more structured than a single Godot project, but that cost is intentional to protect simulation and replay integrity.

## Revisit triggers

Create a successor ADR if any of the following occurs.

- M0 cannot reference the pure `net8.0` Domain projects from the pinned Godot .NET project.
- A clean CI runner cannot build the .NET solution and Godot project without interactive editor state.
- The Godot presentation layer requires a second rules implementation.
- M2 UI iteration is measurably slower than a tested alternative, not merely unfamiliar.
- A required commercial target is unsupported, especially web.
- Engine bugs block a milestone and no stable maintenance release or bounded workaround exists.
- Licensing or distribution conditions materially change.

## Official sources reviewed

The detailed source register is [[Official Engine Evaluation Sources]]. Key facts were checked against official pages on 2026-07-10:

- Godot official release archive and stable versions;
- Godot command-line headless/build/export documentation;
- Godot C#/.NET platform support;
- Godot pixel-art integer-scaling guidance;
- Godot MIT license;
- Unity 6 command-line CI documentation and 2026 plan terms;
- MonoGame Foundation project and release information.
