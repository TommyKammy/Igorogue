---
type: dashboard
status: active
project: Igorogue
updated: 2026-07-11
---
# Project Dashboard

## Phase

`M0 Repository Bootstrap`からheadless battle state machineまでは実装・review・CI・merge済み。Gate 1のFAC-08／09 true replayに必要なauthorized facility build commandは実装・独立review・local validation・green PR #12 CI済みで、人間mergeを待つ。

## Sprint goal

Integrate authorized facility build into the deterministic headless battle session without duplicating Domain rules.

## Health

| Area | State | Note |
|---|---|---|
| Product vision | Green | Pillars documented |
| Rules | Yellow | P0 specs complete; FEAT-009 independent paper sign-off remains |
| Technical | Green | Repository bootstrap and runtime/export evidence complete |
| Content | Yellow | v0.2 candidates unvalidated |
| UX | Yellow | Mockup stage |
| Validation | Yellow | TASK-0010 post-merge main CI green; TASK-0024 238 tests、independent review、two closeout runs、PR #12 CI green |

## Blockers

- TASK-0009 awaits TASK-0024 and DECISION-0004 owner resolution; TASK-0011 awaits TASK-0009.
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
- immutable headless battle state machine with canonical placement／turn／pass commands and merged PR #11 evidence

## Next

1. Implement／review／merge [[TASK-0024 Authorized Facility Build Battle Command]]
2. Resolve [[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]] with the project owner
3. Execute [[TASK-0009 Golden Board Fixtures]] after TASK-0024 merge and DECISION-0004 resolution
4. Execute [[TASK-0011 Replay Round Trip Verification]] after TASK-0009 merge
5. FEAT-009 independent two-person sign-off
6. A-6 style data/document synchronization checker
