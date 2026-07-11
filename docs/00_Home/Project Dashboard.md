---
type: dashboard
status: active
project: Igorogue
updated: 2026-07-11
---
# Project Dashboard

## Phase

`M0 Repository Bootstrap`からterritory-region calculationまでは実装・review・CI・merge済み。Gate 1 facility runtime foundationへ進む。

## Sprint goal

Implement deterministic facility runtime semantics in the shared Domain Rules Kernel.

## Health

| Area | State | Note |
|---|---|---|
| Product vision | Green | Pillars documented |
| Rules | Yellow | P0 specs complete; FEAT-009 independent paper sign-off remains |
| Technical | Green | Repository bootstrap and runtime/export evidence complete |
| Content | Yellow | v0.2 candidates unvalidated |
| UX | Yellow | Mockup stage |
| Validation | Yellow | Territory calculation proven locally and in CI; facility runtime ready |

## Blockers

- TASK-0023 facility runtime is not implemented yet.
- TASK-0009 remains blocked by TASK-0023 and DECISION-0003; TASK-0010 remains blocked by TASK-0023 and DECISION-0002.

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
- deterministic stone-layer territory regions, canonical ordering, and FAC projection tests

## Next

1. [[TASK-0023 Implement Facility Runtime Semantics]]
2. Resolve [[DECISION-0003 Sequence Golden Replay After Battle State Machine]] before [[TASK-0009 Golden Board Fixtures]]
3. FEAT-009 independent two-person sign-off
4. A-6 style data/document synchronization checker
