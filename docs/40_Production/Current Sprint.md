---
type: sprint
status: active
project: Igorogue
updated: 2026-07-11
sprint: S0
---
# Current Sprint

## Goal

Implement the deterministic headless battle state machine.

## In review

- [[TASK-0010 Headless Battle State Machine]]
  - implementation、232 tests、independent `CODE_REVIEW.md` approval complete; CI／human merge pending
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
- [[TASK-0023 Implement Facility Runtime Semantics]]

## Next after TASK-0010

- [[TASK-0009 Golden Board Fixtures]]
- [[TASK-0011 Replay Round Trip Verification]]
- order fixed by resolved [[DECISION-0003 Sequence Golden Replay After Battle State Machine]]

## Review questions

- Does every accepted scripted placement use the shared Domain placement and facility-aware composite without duplicating rules?
- Are rejection, terminal state, phase changes, RNG state, and command log deterministic exact no-ops or transitions as specified?
- Is the selected `TerritoryEstablished -> FacilityDisabled / FacilityActivated` order stable and Momentum-free in M1?
