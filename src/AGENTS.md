# AGENTS.md — Pure .NET source

These instructions extend the repository root guidance for `src/`.

- `Igorogue.Domain` is the gameplay authority and must not reference Godot, filesystem UI state, wall-clock time, or ambient randomness.
- `Igorogue.Application` orchestrates typed commands and ordered events; it must not duplicate Domain rules.
- `Igorogue.Content` validates generated content and must not choose gameplay outcomes.
- Every outcome-affecting order must be explicit and covered by tests.
- Public state intended for replay/checksum must have canonical serialization.
- Do not introduce a package or project reference without explicit TASK scope and human approval.
- Run architecture and Domain/Application tests for every source change.
