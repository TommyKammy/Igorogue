---
type: dashboard
status: active
project: Igorogue
updated: 2026-07-11
---
# Project Dashboard

## Phase

`M0 Repository Bootstrap`からking capture resultまでは実装・review・CI・merge済み。Gate 1 board Rules Kernel workが継続中。

## Sprint goal

Implement deterministic territory-region calculation in the shared Domain Rules Kernel.

## Health

| Area | State | Note |
|---|---|---|
| Product vision | Green | Pillars documented |
| Rules | Yellow | P0 specs complete; FEAT-009 independent paper sign-off remains |
| Technical | Green | Repository bootstrap and runtime/export evidence complete |
| Content | Yellow | v0.2 candidates unvalidated |
| UX | Yellow | Mockup stage |
| Validation | Yellow | Placement legality, KO-01〜KO-07, and king result proven locally and in CI; territory calculation ready |

## Blockers

- Territory-region calculation is not implemented yet.

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
- deterministic same-color groups and duplicate-free real-liberty sets
- immutable hypothetical placement and effective-liberty simultaneous capture
- canonical `StoneTopologyKey` and immutable battle-local repetition history
- suicide／terminal legality and KO-01〜KO-07 through the shared Rules Kernel
- atomic legal commit binding for board, ordered facts, and next history
- versioned king-capture result, shared pure evaluator, and legal-commit result binding

## Next

1. [[TASK-0008 Territory Region Calculation]]
2. [[TASK-0009 Golden Board Fixtures]]
3. FEAT-009 independent two-person sign-off
4. A-6 style data/document synchronization checker
