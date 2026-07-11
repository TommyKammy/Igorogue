---
type: sprint
status: active
project: Igorogue
updated: 2026-07-11
sprint: S0
---
# Current Sprint

## Goal

Review and land same-color orthogonal groups and duplicate-free real-liberty sets.

## Ready

- なし

## In review

- [[TASK-0004 Stone Groups and Unique Liberty Sets]]
  - implementation evidence complete; independent Codex review approved; CI pending
- [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]
  - independent two-person paper sign-off pending

## Completed

- M-1 P0 design repair tasks TASK-0013 through TASK-0019
- [[TASK-0021 Prepare macOS Codex App Handoff]]
- [[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]]
- [[TASK-0001 Decide Engine and Repository]]
- [[TASK-0020 Review Repository Bootstrap Runtime Evidence]]
- [[TASK-0002 Deterministic RNG and Command Log]]
- [[TASK-0003 Board Coordinates and Orthogonal Neighbours]]

## Next after TASK-0004

- [[TASK-0005 Hypothetical Placement and Capture Resolution]]

## Review questions

- Are groups connected only through orthogonal same-color stones, never diagonals or opposing colors?
- Does each group expose every empty orthogonal neighbour exactly once in canonical point order?
- Are multiple groups and their anchors returned in a stable deterministic order?
