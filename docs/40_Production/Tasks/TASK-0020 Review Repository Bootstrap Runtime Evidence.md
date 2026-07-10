---
type: task
id: TASK-0020
status: blocked
project: Igorogue
milestone: M0
priority: P0
dependencies: [TASK-0001]
updated: 2026-07-10
---
# TASK-0020 Review Repository Bootstrap Runtime Evidence

## Outcome

Run the TASK-0001 bootstrap on the configured development host, commit authentic NuGet lock files, and attach build/test/headless/export evidence.

## Acceptance criteria

- exact tool verifier passes
- all `packages.lock.json` files are committed
- locked restore succeeds from a clean checkout
- xUnit tests pass
- simulator emits the same checksum twice
- Godot headless smoke exits 0
- Windows debug export exists and has a SHA-256 sidecar
- CI is green on the reviewed commit

## Non-goals

No gameplay code or design changes.

## Operational execution

The macOS handoff uses [[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]] as the ready host-execution wrapper for this evidence review. TASK-0020 remains blocked until TASK-0022 supplies local, clean-checkout, export, and CI evidence.
