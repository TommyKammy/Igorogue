---
type: sprint
status: active
project: Igorogue
updated: 2026-07-12
sprint: S0
---
# Current Sprint

## Goal

Implement deterministic replay save／load／verification over the versioned golden command suite.

## In review

- [[TASK-0011 Replay Round Trip Verification]]
  - 19 cases／34 Application attempts、accepted-only log chain、fail-closed schema／metadata／checksum verification、double validation／fixed-HEAD review approved
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
- [[TASK-0010 Headless Battle State Machine]]
- [[TASK-0024 Authorized Facility Build Battle Command]]
- [[TASK-0009 Golden Board Fixtures]]

## Next after TASK-0011

- Gate 1 deterministic foundation completion audit

## Review questions

- Does every replay execute only typed Application commands from a canonical initial session?
- Are submitted attempts and the accepted-only log chain distinct and integrity-checked?
- Do schema、metadata、content、checksum、acceptance／reason、terminal drifts fail closed at the first mismatch?
