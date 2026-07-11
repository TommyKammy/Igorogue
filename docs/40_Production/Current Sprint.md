---
type: sprint
status: active
project: Igorogue
updated: 2026-07-11
sprint: S0
---
# Current Sprint

## Goal

Implement hypothetical single-stone placement and stable simultaneous opponent-group capture.

## Ready

- [[TASK-0005 Hypothetical Placement and Capture Resolution]]

## In review

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
- [[TASK-0004 Stone Groups and Unique Liberty Sets]]

## Next after TASK-0005

- [[TASK-0006 Suicide Legality and Terminal Capture]]

## Review questions

- Is the placement applied to an immutable hypothetical board without changing the source board?
- Are all adjacent opponent groups with zero resulting real liberties selected from one snapshot and removed simultaneously?
- Are placement and captured-group facts returned in stable canonical order?
