---
type: sprint
status: active
project: Igorogue
updated: 2026-07-13
sprint: S0
---
# Current Sprint

## Goal

Apply DECISION-0006 Option 1 as a canonical starter 6-type／12-card recipe and connect `card_development` through the existing authorized facility path.

## In review

- [[TASK-0038 Apply Resolved M2 Starter Deck and Facility Scope]]
  - dependency TASK-0037 is done through PR #27／post-merge CI run `29237842140`
  - [[DECISION-0006 Resolve M2 Starter Deck and Facility Scope]] resolved Option 1
  - exact recipeは`game_data/content/starting_decks.json`を参照する
  - `card_development`だけをM2 facility例外として既存authorized facility build commandへ接続する
  - keep Headless Core Duel／replay composition deferred to TASK-0039
  - 592-test candidate、content／Application／documentation pre-closeout reviews approved
  - fixed source HEAD `cd476d1` approved by independent Content、Application／Domain、documentation reviews

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

## Next after TASK-0038

- Human review／merge of TASK-0038 after fixed-HEAD approval and green CI.
- [[TASK-0039 Integrate Headless Core Duel and Replay]] becomes the next implementation task only after TASK-0038 is done.
- TASK-0040〜0042 remain blocked and advance only in dependency order.

## Implementation review questions

- Does `game_data/content/starting_decks.json` encode the exact six-ID／12-card multiset and reject malformed recipes fail-closed?
- Is the recipe canonical projection stable across JSON key／input enumeration order and bound into the content hash?
- Does Development reuse the authorized facility build command and existing territory／capacity／duplicate checks without a second facility rule?
- Are rejected Development plays exact no-ops for resources、zones、and facility state?
- Are non-Development facility cards unreachable in the M2 starter scope?
