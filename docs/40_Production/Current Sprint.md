---
type: sprint
status: active
project: Igorogue
updated: 2026-07-13
sprint: S0
---
# Current Sprint

## Goal

Expose selected-card legality、capture、liberty／atari、territory／facility delta、king risk、and Bandit intent from the authoritative Core Duel aggregate without a second rules implementation.

## In review

- [[TASK-0040 Implement Core Duel Preview Queries]]
  - dependency TASK-0039 is done through PR #29 merge `60d8cc5958e38768f4077ee2f4d686526d5b25fe`／post-merge CI run `29252298693`
  - selected cardのCanonicalPoint／mode候補を同一immutable sessionからauthoritative command pathへ投機実行する
  - capture、effective liberty／atari、territory／facility delta、result checksumをread-only DTOへ投影する
  - stored normal／bonus intentとexisting mandatory overrideだけを投影し、full counterattack予測は行わない
  - parity、read-only、stale state／log、canonical enumeration、architecture evidenceを625 testsと全wrapper成功で固定済み
  - fixed source HEAD `ab600cd53e7fafa5976b1a381a4a19e672097977`の3系統independent reviewはfindingなしで全て`APPROVE`
  - Draft PR #30 CI run `29256594790`は全3 job success、human review／merge pending

## Open human evidence

- [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]] — `review`; worksheets／identities／results not retained
- [[DECISION-0007 Authorize Gate 2 Without Retained TASK-0012 Human Evidence]] — resolved; permits Gate 2 progression without claiming evidence completion

## Completed

- M-1 P0 design repair tasks TASK-0013 through TASK-0019
- [[TASK-0021 Prepare macOS Codex App Handoff]]
- [[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]]
- [[TASK-0001 Decide Engine and Repository]]
- [[TASK-0020 Review Repository Bootstrap Runtime Evidence]]
- [[TASK-0002 Deterministic RNG and Command Log]]
- [[TASK-0003 Board Coordinates and Orthogonal Neighbours]]
- [[TASK-0004 Stone Groups and Unique Liberty Sets]]
- [[TASK-0005 Hypothetical Placement and Capture Resolution]]
- [[TASK-0006 Suicide Legality and Terminal Capture]]
- [[TASK-0007 King Capture and Battle Result]]
- [[TASK-0008 Territory Region Calculation]]
- [[TASK-0023 Implement Facility Runtime Semantics]]
- [[TASK-0010 Headless Battle State Machine]]
- [[TASK-0024 Authorized Facility Build Battle Command]]
- [[TASK-0009 Golden Board Fixtures]]
- [[TASK-0011 Replay Round Trip Verification]]
- [[TASK-0025 Audit Gate 1 Deterministic Foundation Completion]]
- [[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]] — Option 1
- [[TASK-0026 Resolve M1 Momentum Counterattack Migration Boundary]]
- [[TASK-0027 Implement Temporary Liberty Domain Kernel]]
- [[TASK-0028 Implement Closed-Window Capture Benefits and TLE Boundary Pressure]]
- [[TASK-0029 Integrate Temporary Liberty Enemy Boundary and Golden Replay]]
- [[TASK-0030 Re-audit M1 Headless Rules Kernel Exit]] — M1 technical `PASS`; PR #20 merged／CI green
- [[TASK-0031 Plan Gate 2 Core Duel Implementation]] — PR #21 merged／post-merge CI green
- [[TASK-0032 Implement Typed Core Duel Content Catalog]] — PR #22 merged／post-merge CI green
- [[TASK-0033 Implement Deterministic Battle Deck Hand and Qi Kernel]] — PR #23 merged／post-merge CI green
- [[TASK-0034 Implement Atomic Basic Stone Card Play]] — PR #24 merged／post-merge CI green
- [[TASK-0035 Implement Starter Stone Card Effects]] — PR #25 merged／post-merge CI green
- [[TASK-0036 Implement Starter Reinforce Effect]] — PR #26 merged／post-merge CI green
- [[TASK-0037 Implement Bandit Intent Planning and Execution]] — PR #27 merged at `e98ac90`／post-merge CI green
- [[TASK-0038 Apply Resolved M2 Starter Deck and Facility Scope]] — PR #28 merged at `6f84adcbc0b1deb70944e82648009eb53e1429a4`／post-merge CI green
- [[TASK-0039 Integrate Headless Core Duel and Replay]] — PR #29 merged at `60d8cc5958e38768f4077ee2f4d686526d5b25fe`／post-merge CI green

## Next after TASK-0040

- Complete human review／merge of TASK-0040 Draft PR #30.
- [[TASK-0041 Build Playable Godot Core Duel Graybox]] becomes the next implementation task only after TASK-0040 is done.
- TASK-0042 remains blocked and advances only after TASK-0041.

## Implementation review questions

- Does every card candidate call the existing exact-bound command path without mutating the source session、RNG、log、or first-use state?
- Are canonical point／mode order、stable rejection reasons、accepted checksums、capture／territory／facility deltas、and effective-liberty projections exact?
- Does the battle projection expose only presentation-neutral snapshots while preserving stored normal／bonus intent and avoiding future-retarget prediction?
- Do stale state／log requests fail closed without returning renderable board、hand、or risk data?
- Are Godot rendering、Momentum／Brilliant、full counterattack preview、and player-visible rule changes still excluded?
