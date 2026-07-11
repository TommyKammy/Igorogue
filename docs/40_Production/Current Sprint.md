---
type: sprint
status: active
project: Igorogue
updated: 2026-07-11
sprint: S0
---
# Current Sprint

## Goal

Review and land deterministic king-group capture results.

## Ready

- なし

## In review

- [[TASK-0007 King Capture and Battle Result]]
  - implementation evidence complete; independent review approved; CI and human merge pending
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

## Next after TASK-0007

- [[TASK-0008 Territory Region Calculation]]

## Review questions

- Does capturing a white king group produce a player win while capturing a black king group produces a player loss?
- Is ordinary capture distinguished from king capture without changing capture ordering?
- If both kings are present in one capture batch, does loss take precedence deterministically?
