---
type: sprint
status: active
project: Igorogue
updated: 2026-07-11
sprint: S0
---
# Current Sprint

## Goal

Continue Gate 1 by implementing canonical board coordinates, stable indexing, orthogonal neighbours, and the accepted point-symmetric initial position.

## Ready

- なし

## In review

- [[TASK-0003 Board Coordinates and Orthogonal Neighbours]]
  - implementation evidence complete; independent review and CI pending
- [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]
  - independent two-person paper sign-off pending

## Completed

- M-1 P0 design repair tasks TASK-0013 through TASK-0019
- [[TASK-0021 Prepare macOS Codex App Handoff]]
- [[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]]
- [[TASK-0001 Decide Engine and Repository]]
- [[TASK-0020 Review Repository Bootstrap Runtime Evidence]]
- [[TASK-0002 Deterministic RNG and Command Log]]

## Next after TASK-0003

- [[TASK-0004 Stone Groups and Unique Liberty Sets]]

## Review questions

- Are CanonicalPoint, InternalPoint, and canonical index conversions exact and reject out-of-range values?
- Are orthogonal neighbours returned in canonical point order with no diagonals?
- Does `standard_v0_2` satisfy role-aware reflection, connected three-stone king groups, and seven liberties per side?
