---
type: dashboard
status: active
project: Igorogue
updated: 2026-07-12
---
# Project Dashboard

## Phase

`M0 Repository Bootstrap`からGate 1 ordered implementation sequenceまでは実装・review・CI・merge済み。M1 exitはMOM／CTR milestone Decision、TLE runtime／golden gap、TASK-0012 human sign-off待ち。

## Sprint goal

Audit Gate 1／M1 exit evidence without overstating E1 fixtures or bootstrap simulator smoke.

## Health

| Area | State | Note |
|---|---|---|
| Product vision | Green | Pillars documented |
| Rules | Yellow | P0 specs complete; FEAT-009 independent paper sign-off remains |
| Technical | Green | Repository bootstrap and runtime/export evidence complete |
| Content | Yellow | v0.2 candidates unvalidated |
| UX | Yellow | Mockup stage |
| Validation | Yellow | Gate 1 technical sequence green; M1 exit blocked by DECISION-0005, TLE E3 gap, and TASK-0012 human sign-off |

## Blockers

- [[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]] — MOM／CTR M1 migration versus Gate 3／M3 conflict.
- TLE-01〜15 production lifecycle／E3 migration — missing M1 evidence under current Accepted scope.
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
- canonical authorized facility build command with merged PR #12 evidence
- schema-v1 golden suite with 19 cases、35 boundaries、source hashes、ordered facts、exact no-op rejection evidence
- versioned replay envelope with 34 submitted attempts、accepted-only log chain、strict bounded Stream I/O、typed replay and fail-closed integrity evidence
- PR #14 merge and post-merge main CI green at `6398ec1e4f1e4ecf0c8eeaf71e33bb6ddeff6875`
- [[TASK-0025 Gate 1 Deterministic Foundation Audit]] — technical sequence complete、M1 exit `DECISION NEEDED`

## Next

1. Resolve [[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]]
2. Define and complete the bounded TLE M1 production／E3 follow-up required by current Accepted scope
3. Complete FEAT-009 independent two-person sign-off
4. Re-run／close [[TASK-0025 Audit Gate 1 Deterministic Foundation Completion]]
5. Define Gate 2 implementation TASKs only after the gates above permit it
