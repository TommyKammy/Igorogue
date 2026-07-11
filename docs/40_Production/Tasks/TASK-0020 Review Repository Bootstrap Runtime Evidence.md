---
type: task
id: TASK-0020
status: done
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

The macOS handoff used [[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]] as the host-execution wrapper. TASK-0022 supplied every required local, clean-checkout, export, review, and CI artifact.

## Execution log

2026-07-11:

- All eight authentic NuGet locks were reviewed and committed.
- Local and detached clean-worktree governance, locked restore, xUnit, repeated simulator checksum, Godot .NET smoke, and managed Windows export passed.
- The approved managed-export bootstrap defect was corrected and verified without changing version pins or package versions.

## Evidence

- [[TASK-0022 macOS Runtime Evidence]]
- Runtime implementation commit: `c1e1998d34f7e9abbb8962b7cc34897ebd9675a1`
- Lock-file commit: `70f0eec`
- Final CI run `29128583728`: passed at `a09e2d3d4425566a458a987556db3429d24076c1`
- PR #2 merged as `b7d421d2e7f644366f8b186ccd7d4c333ef35f65`

## Known issues

- None. Non-blocking action-version deprecation warnings remain tracked by the bootstrap evidence.
