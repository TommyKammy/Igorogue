---
type: sprint
status: active
project: Igorogue
updated: 2026-07-11
sprint: S0
---
# Current Sprint

## Goal

Begin Gate 1 by implementing deterministic RNG streams and the ordered command-log contract after closing the macOS runtime gate.

## Ready

- なし

## In review

- [[TASK-0002 Deterministic RNG and Command Log]]
  - implementation evidence complete; independent review and CI pending
- [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]
  - independent two-person paper sign-off pending

## Completed

- M-1 P0 design repair tasks TASK-0013 through TASK-0019
- [[TASK-0021 Prepare macOS Codex App Handoff]]
- [[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]]
- [[TASK-0001 Decide Engine and Repository]]
- [[TASK-0020 Review Repository Bootstrap Runtime Evidence]]

## Next after TASK-0002

- [[TASK-0003 Board Coordinates and Orthogonal Neighbours]]

## Review questions

- Are gameplay/reward/cosmetic RNG streams isolated and versioned?
- Does the same seed and ordered input produce identical output and checksum?
- Does rejected input leave command-log and RNG state unchanged?
