---
type: dashboard
status: active
project: Igorogue
updated: 2026-07-18
---
# Project Dashboard

## Phase

`M0 Repository Bootstrap`からTLE-01〜15を含むM1 implementation workstreamまでは実装・review・CI・merge済み。DECISION-0005 Option 1でMOM／CTR migrationはM3へ確定。TASK-0030のindependent reviewによりM1 technical exitは`PASS`。TASK-0012の二人human evidenceは未確認だが、DECISION-0007のProject owner waiverによりGate 2 entryはopen。

## Sprint goal

Repair the Godot human-run Replay V3 evidence path, then repeat the incomplete M2 graybox UAT without treating Test 1 readability as gameplay-fun validation.

## Health

| Area | State | Note |
|---|---|---|
| Product vision | Green | Pillars documented |
| Rules | Green | DECISION-0006／0009／0010 are resolved; starter catalog display is synchronized with machine-readable content |
| Technical | Green | Repository bootstrap and runtime/export evidence complete |
| Content | Yellow | v0.2 candidates unvalidated |
| UX | Yellow | Test 1 side／orientation／intent／selection clear passed; Project owner reports the game is not yet enjoyable |
| Validation | Red | TASK-0042 result NOT PASSED; human-run replay and most fresh UAT paths missing |

## Blockers

- TASK-0042 confirmed that Godot discards the initial session／command-result chain and exposes no Replay V3 save／load route for the actual human run. M2 technical／E4 human／fun are `NOT PASSED` and Gate 3 is blocked.
- [[TASK-0043 Capture and Verify Godot Human Run Replay V3]] is in `review`; automated win／loss／repeat／save-race evidence is green, while graphical human UAT remains open.
- [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]] human evidence remains unverified, with Gate 2 progression separately authorized by DECISION-0007.

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
- PR #15 merge and post-merge main CI green at `6c34a4fffe00b0fbec9dc5dd3033d84c6229a56d`
- PR #16 merge and post-merge main CI green at `90dda9dd41b96864a24e19a7969285f56c4593b4`
- PR #17 merge and post-merge main CI green at `ad50fe7ae7a7170e308c322971380c4e66a2dcb0`
- PR #18 merge and post-merge main CI green at `ddccd57db12219847646d0b2de85c18b2c94b120`
- PR #19 merge and post-merge main CI green at `35139bedb927f4c15b4e62a02c423947d5bdb1da`
- [[TASK-0025 Gate 1 Deterministic Foundation Audit]] — fixed-baseline result `DECISION NEEDED`; Option 1 post-audit disposition recorded
- [[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]] — resolved／Option 1
- [[TASK-0027 Implement Temporary Liberty Domain Kernel]] — done
- [[TASK-0028 Implement Closed-Window Capture Benefits and TLE Boundary Pressure]] — done
- [[TASK-0029 Integrate Temporary Liberty Enemy Boundary and Golden Replay]] — done
- [[TASK-0030 Re-audit M1 Headless Rules Kernel Exit]] — done
- PR #20 merge and post-merge main CI green at `d1f69e10672ed7289c056cee32c4875964494fe4`
- [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]] — review; human sign-off evidence not retained
- [[DECISION-0007 Authorize Gate 2 Without Retained TASK-0012 Human Evidence]] — resolved owner waiver; not human evidence
- [[TASK-0031 Plan Gate 2 Core Duel Implementation]] — done; PR #21 merge／post-merge CI green
- [[TASK-0032 Implement Typed Core Duel Content Catalog]] — done; PR #22 merge／post-merge CI green
- [[TASK-0033 Implement Deterministic Battle Deck Hand and Qi Kernel]] — done; PR #23 merge／post-merge CI green
- [[TASK-0034 Implement Atomic Basic Stone Card Play]] — done; PR #24 merge／post-merge CI green
- [[TASK-0035 Implement Starter Stone Card Effects]] — done; PR #25 merge／post-merge CI green
- [[TASK-0036 Implement Starter Reinforce Effect]] — done; PR #26 merge／post-merge CI green
- [[TASK-0037 Implement Bandit Intent Planning and Execution]] — done; PR #27 merge `e98ac90`／post-merge CI run `29237842140` green
- [[DECISION-0006 Resolve M2 Starter Deck and Facility Scope]] — resolved／Option 1
- [[TASK-0038 Apply Resolved M2 Starter Deck and Facility Scope]] — done; PR #28 merge `6f84adcbc0b1deb70944e82648009eb53e1429a4`／post-merge CI run `29247035946` green
- [[TASK-0039 Integrate Headless Core Duel and Replay]] — done; PR #29 merge `60d8cc5958e38768f4077ee2f4d686526d5b25fe`／post-merge CI run `29252298693` green
- [[TASK-0040 Implement Core Duel Preview Queries]] — done; immutable speculative command reuse、625 tests、all wrappers、fixed source HEAD review approved、PR #30 source `eaa62531615eef7a10cfe1d16fe92318d45143c8` merged at `d8ccc08cf7fa3cc1a43046d128b2804b50b9d073`、post-merge CI run `29285926156` green
- [[TASK-0041 Build Playable Godot Core Duel Graybox]] — done; 653 tests、Godot smoke／Windows export、fixed-HEAD review、PR #31 merge `4a2745ca30990689789d60ef79e4721579b82bbe`、post-merge CI run `29292101348` green、Project owner visual approved
- [[TASK-0042 Validate M2 Core Duel Graybox]] — audit done; Test 1 four interaction checks PASS、M2 technical／E4 human／fun NOT PASSED、Gate 3 blocked; PR #33 merge `1d6b7c2e2ede5671e7d4736548e6728908fb7bf9`／post-merge CI run `29613756684` green
- PR #34 closed the TASK-0042 audit follow-up at source `9e684d71925057e120d240058bfa23da05abb4f1`／main merge `6a9c7dee394399bc499438f74c55e041c26e4be5`; PR CI run `29614924069` and post-merge main CI run `29620281041` passed all 3 jobs
- [[TASK-0043 Capture and Verify Godot Human Run Replay V3]] — code-bearing fixed HEAD `eaca6e5a7f97a8d0b4db168abd6ffa131a1032a3`、CI correction `a693505d0f82eeafde40182e1e4a1d13bcf64828`; automated loss／win／repeat／save-race evidence、all repository wrappers、PR #35 CI run `29622768204` passed、independent reviews approved; graphical human UAT remains open

## Next

1. Complete graphical human loss／win UAT and terminal Replay V3 overlay review for [[TASK-0043 Capture and Verify Godot Human Run Replay V3]].
2. Obtain human visual sign-off and merge judgment for Draft PR #35.
3. On the merged fixed HEAD, rerun starter six effects、atari／capture／territory、win／loss／restart／replay UAT and diagnose why gameplay is not yet enjoyable before Gate 3.
