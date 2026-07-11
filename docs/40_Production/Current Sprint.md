---
type: sprint
status: active
project: Igorogue
updated: 2026-07-11
sprint: S0
---
# Current Sprint

## Goal

Implement deterministic facility runtime semantics.

## In review

- [[TASK-0023 Implement Facility Runtime Semantics]]
  - independent `CODE_REVIEW.md` approval, local validation, and green PR #10 CI complete; human merge pending
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
- [[TASK-0005 Hypothetical Placement and Capture Resolution]]
- [[TASK-0006 Suicide Legality and Terminal Capture]]
- [[TASK-0007 King Capture and Battle Result]]
- [[TASK-0008 Territory Region Calculation]]

## Next sequencing decision after TASK-0023

- [[DECISION-0003 Sequence Golden Replay After Battle State Machine]]
- [[TASK-0009 Golden Board Fixtures]] or [[TASK-0010 Headless Battle State Machine]]
  - owner decision pending; neither task is selected while DECISION-0003 remains open

## Review questions

- Are facility instances immutable, unique, canonically ordered, and never committed beneath stones?
- Does only an accepted legal placement destroy the placement-point facility while rejected placement is a complete no-op?
- Do neutralization, opponent control, restoration, capacity, over-capacity, and build limits match FAC-01〜09 without changing stone topology?
