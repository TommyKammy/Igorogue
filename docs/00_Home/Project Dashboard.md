---
type: dashboard
status: active
project: Igorogue
updated: 2026-07-11
---
# Project Dashboard

## Phase

`M0 Repository Bootstrap`からfacility runtimeまでは実装・review・CI・merge済み。Gate 1はheadless battle state machineへ進む。

## Sprint goal

Implement a deterministic scripted headless battle state machine over the shared Domain Rules Kernel.

## Health

| Area | State | Note |
|---|---|---|
| Product vision | Green | Pillars documented |
| Rules | Yellow | P0 specs complete; FEAT-009 independent paper sign-off remains |
| Technical | Green | Repository bootstrap and runtime/export evidence complete |
| Content | Yellow | v0.2 candidates unvalidated |
| UX | Yellow | Mockup stage |
| Validation | Yellow | Facility runtime merged with green post-merge CI; TASK-0010 in progress |

## Blockers

- TASK-0009 awaits TASK-0010; TASK-0011 awaits TASK-0010 and TASK-0009.
- FEAT-009 independent two-person paper sign-off remains human-only.

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
- immutable facility state, injected runtime policy, FAC-01〜09 production parity, typed build／placement／transition facts

## Next

1. Execute [[TASK-0010 Headless Battle State Machine]]
2. Execute [[TASK-0009 Golden Board Fixtures]] after TASK-0010 merge
3. Execute [[TASK-0011 Replay Round Trip Verification]] after TASK-0009 merge
4. FEAT-009 independent two-person sign-off
5. A-6 style data/document synchronization checker
