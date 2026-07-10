---
type: sprint
status: active
project: Igorogue
updated: 2026-07-10
sprint: S0
---
# Current Sprint

## Goal

Complete the macOS Codex handoff and convert static repository bootstrap evidence into authentic clean-host runtime and CI evidence.

## Ready

- [[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]]

## In review

- [[TASK-0001 Decide Engine and Repository]]
  - solution, projects, wrappers, generated content, verifier, and CI definitions exist
  - runtime proof remains pending
- [[TASK-0020 Review Repository Bootstrap Runtime Evidence]]
  - closed by TASK-0022 when all local, clean-checkout, export, and CI criteria pass
- [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]
  - independent two-person paper sign-off pending

## Completed

- M-1 P0 design repair tasks TASK-0013 through TASK-0019
- [[TASK-0021 Prepare macOS Codex App Handoff]]

## Next after runtime evidence

- [[TASK-0002 Deterministic RNG and Command Log]]

## Review questions

- Are the exact pinned SDK and Godot .NET editor available on the Mac host?
- Are authentic package locks stable across two generations and a clean checkout?
- Do xUnit and simulator smoke run without source changes?
- Does Godot 4.7 .NET build and run headlessly on macOS?
- Can the Mac host cross-export the Windows debug build with matching templates?
- Are all CI jobs green on the reviewed commit?
