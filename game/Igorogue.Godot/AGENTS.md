# AGENTS.md — Godot presentation layer

These instructions extend the repository root guidance for `game/Igorogue.Godot/`.

- Godot presents events and submits Application commands; it does not decide legality, capture, territory, RNG, enemy intent, or rewards.
- No gameplay state may be mutated directly from a Node.
- `.tscn`, `.tres`, `project.godot`, and export preset edits require explicit TASK authorization.
- Every authorized scene/resource edit requires Godot headless parse/build and human visual review.
- Preserve 480×270 logical resolution, Compatibility renderer, integer scaling, and pixel-art filtering rules.
- Do not store machine-specific absolute paths in project files.
