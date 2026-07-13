---
type: sprint
status: active
project: Igorogue
updated: 2026-07-13
sprint: S0
---
# Current Sprint

## Goal

Connect the resolved Core Duel recipe、starter-card turn、Bandit intent、terminal result、and restart through one deterministic headless aggregate and replay schema 3.

## In progress

- [[TASK-0039 Integrate Headless Core Duel and Replay]]
  - dependency TASK-0038 is done through PR #28 merge `6f84adcbc0b1deb70944e82648009eb53e1429a4`／post-merge CI run `29247035946`
  - resolved 12-card recipeをcanonical physical instanceへ展開し、seed／content／initial snapshot／Banditをexact-bindする
  - outer `CoreDuelBattleSession`だけがauthoritative command logを所有する
  - PlayCard → EndPlayerTurn → Bandit action → next player turn／terminal／restartをApplication commandで接続する
  - replay schema 3は`headless-core-duel-state-v1`だけを受理し、schema 1／2を変更しない
  - Godot UI／preview query projection／formal simulator／playable claimは対象外

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

## Next after TASK-0039

- Human review／merge of TASK-0039 after fixed-HEAD approval and green CI.
- [[TASK-0040 Implement Core Duel Preview Queries]] becomes the next implementation task only after TASK-0039 is done.
- TASK-0041〜0042 remain blocked and advance only in dependency order.

## Implementation review questions

- Does the aggregate keep board、runtime、deck／hand／qi、RNG、resources、Bandit plans、and one outer log exact-bound at every phase?
- Is the initial intent planned before the first player-turn start and kept fixed through the player window, while mandatory override preview remains a query-only no-op?
- Do fixed win、loss、and restart paths reproduce state、facts、accepted-only log、terminal、and replay bytes?
- Does schema 3 reject cross-version input、tamper、unknown low-level commands、oversized input、and excessive attempts without changing schema 1／2?
- Are Godot UI、formal simulator、and gameplay-fun claims still excluded?
