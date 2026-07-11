---
type: dashboard
status: active
project: Igorogue
updated: 2026-07-11
---
# Project Dashboard

## Phase

`M0 Repository Bootstrap` and the TASK-0002 determinism foundation are complete. Gate 1 board Rules Kernel work is active.

## Sprint goal

Implement canonical 7×7 coordinates, stable indexing, orthogonal neighbours, and the accepted point-symmetric initial position in the shared Domain Rules Kernel.

## Health

| Area | State | Note |
|---|---|---|
| Product vision | Green | Pillars documented |
| Rules | Yellow | P0 specs complete; FEAT-009 independent paper sign-off remains |
| Technical | Green | Repository bootstrap and runtime/export evidence complete |
| Content | Yellow | v0.2 candidates unvalidated |
| UX | Yellow | Mockup stage |
| Validation | Yellow | Determinism and board geometry tested; groups/capture pending |

## Blockers

- Group, liberty, placement, and capture Rules Kernel is not implemented yet.

## Current evidence

- all M-1 deterministic design fixtures
- Accepted Godot 4.7 .NET architecture
- solution and 8-project repository bootstrap
- content synchronization and exact tool verifier
- architecture boundary tests
- GitHub Actions pipeline definition
- authentic NuGet locks and clean-checkout runtime evidence
- managed Windows debug export and final green CI
- versioned RNG streams and ordered command-log checksums
- canonical 7×7 geometry and standard initial-position invariants

## Next

1. [[TASK-0003 Board Coordinates and Orthogonal Neighbours]]
2. [[TASK-0004 Stone Groups and Unique Liberty Sets]]
3. FEAT-009 independent two-person sign-off
4. A-6 style data/document synchronization checker
