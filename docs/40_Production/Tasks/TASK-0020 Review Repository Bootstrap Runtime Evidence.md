---
type: task
id: TASK-0020
status: blocked
project: Igorogue
milestone: M0
priority: P0
dependencies: [TASK-0001]
updated: 2026-07-11
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

## Execution log

2026-07-11:

- All eight authentic NuGet locks were reviewed and committed.
- Local and detached clean-worktree governance, locked restore, xUnit, repeated simulator checksum, Godot .NET smoke, and managed Windows export passed.
- The approved managed-export bootstrap defect was corrected and verified without changing version pins or package versions.

## Evidence

- [[TASK-0022 macOS Runtime Evidence]]
- Runtime implementation commit: `c1e1998d34f7e9abbb8962b7cc34897ebd9675a1`
- Lock-file commit: `70f0eec`
- CI run `29127553564`: passed at `d47cc671bf2a650570c47612e8a026a7c3e0b748`

## Known issues

- Automated runtime evidence is complete. Keep this task `blocked` until independent closeout approval and the human `review → done`/merge decisions close TASK-0022 and TASK-0001.
