---
type: technical-architecture
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Architecture

The selected stack and repository boundary are defined by [[ADR-0001 Engine and Repository]] and [[Engine Toolchain and Repository Layout]].

## Core flow

```text
Player/Bot Command
       ↓
Igorogue.Application
       ↓
Igorogue.Domain Rules Kernel
       ↓
Ordered Domain Events + immutable snapshot
 ├─ Godot 4.7 .NET presentation
 ├─ Replay Writer
 ├─ Telemetry
 └─ Igorogue.Sim.Cli
```

## Layers

### Domain — `src/Igorogue.Domain`

Board, groups, liberties, capture, territory, card resolution, deterministic state, victory. Pure `net8.0`; no Godot reference, filesystem, UI, frame delta, or engine RNG.

### Application — `src/Igorogue.Application`

Battle/run state machines, typed commands, use cases, interfaces for content, persistence, replay, and policy input. Depends on Domain only.

### Content — `src/Igorogue.Content`

Loads and validates the canonical `game_data/` snapshot, resolves content IDs, and calculates content hashes. It does not implement gameplay rules.

### Presentation — `game/Igorogue.Godot`

Godot scenes, Control nodes, board/card rendering, animation, input, audio, and export. It submits commands and renders results; it never mutates authoritative Domain state.

### Tools — `tools/Igorogue.Sim.Cli` and Python governance tools

The formal simulator links the same Application, Domain, and Content assemblies as the live game. `tools/abstract_sim/` remains a non-authoritative proxy.

## Enforced dependency direction

```text
Domain ← Application ← Godot
   ↑          ↑
 Content ─────┘
   ↑
Sim.Cli
```

Project references and architecture tests enforce the exact directions in ADR-0001.

## Prohibited patterns

- UI nodes directly editing board arrays.
- Godot types in Domain or Application public APIs.
- Enemy AI maintaining a second capture or territory implementation.
- Simulator-only simplified production rules.
- Gameplay decisions based on frame order, delta time, scene-tree order, or unordered collection iteration.
- Runtime content values duplicated as C# constants when `game_data/` is the source of truth.
