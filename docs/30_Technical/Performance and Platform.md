---
type: technical-constraints
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Performance and Platform

## Platform target

- Engine: Godot 4.7 stable .NET, Compatibility renderer.
- Primary: Windows x86_64 desktop.
- Secondary after primary smoke: Linux x86_64 and macOS universal.
- Steam Deck-class 1280×800 usability is a validation target, not a separate runtime architecture.
- Web is out of scope through M6 because the selected Godot C# stack does not currently export C# projects to web.

## Pixel presentation

- Logical canvas: 480×270.
- Godot viewport stretch with integer scale and letterboxing.
- Gameplay hit targets and text follow the accessibility layout, not the physical sprite pixel size.

## Runtime constraints

- The 7×7 Domain is small, but legal-point previews and multi-trigger simulations are recalculated on state mutation, not every render frame.
- Presentation animation consumes an ordered event queue and may be skipped or accelerated without changing state.
- Headless Domain and simulator processes initialize no renderer or audio system.
- Formal batch simulation uses normal .NET processes and may parallelize independent seeds; a single run remains deterministic.
- Goal: at least 100,000 short battles can be scheduled in CI/research infrastructure without one Godot editor process per battle.

## Profiling gates

Do not optimize speculative Domain code before M1 fixtures exist. Measure separately:

- command validation;
- legal target generation;
- capture/territory resolution;
- content loading;
- Godot UI preview generation;
- animation and rendering.
