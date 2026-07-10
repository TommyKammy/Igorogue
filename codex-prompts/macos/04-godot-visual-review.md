# Codex Prompt — Godot Technical and Human Visual Review

Review a TASK that changes Godot presentation assets.

## Technical checks

- Confirm the TASK explicitly authorized each `.tscn`, `.tres`, `project.godot`, or export preset change.
- Confirm gameplay rules remain in Domain/Application.
- Run:

```bash
GODOT_BIN="<ABSOLUTE_PATH>" tools/dev/godot-smoke
```

- Run export when required.
- Inspect scene/resource diffs for unstable generated noise.

## Visual checklist for the human

Provide a concise checklist and launch/open instructions covering:

- 480×270 logical resolution;
- integer scaling;
- board and card readability;
- focus/hover/target states;
- color-independent status cues;
- no clipped Japanese text;
- no animation hiding legality or capture information;
- mouse and gamepad navigation.

Codex may report technical success, but only the human records visual approval.
