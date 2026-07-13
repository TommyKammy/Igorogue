---
type: sprint
status: active
project: Igorogue
updated: 2026-07-13
sprint: S0
---
# Current Sprint

## Goal

Complete fixed-HEAD review and CI for TASK-0037's integrated Bandit candidate／planning／execution implementation.

## In review

- [[TASK-0037 Implement Bandit Intent Planning and Execution]]
  - dependency TASK-0036 is done through PR #26／post-merge CI
  - read-only audit confirmed the existing placement／effective-liberty／repetition／facility／territory kernels are reusable
  - [[DECISION-0009 Resolve Bandit Multi-Group Capture Target Reference]] resolved Option 1
  - shared runtime placement evaluation and detached Application lifecycle are integrated with 565 tests
  - [[DECISION-0010 Resolve Bandit Advance With Zero Real King Liberties]] Option 1 and real=0／effective=1 E3 evidence are complete
  - fixed-HEAD independent review／CI remain
  - keep replay／full telemetry composition deferred to TASK-0039

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

## Next after TASK-0037

- Human review／merge of TASK-0037 after fixed-HEAD approval and green CI.
- [[DECISION-0006 Resolve M2 Starter Deck and Facility Scope]] — resolve before TASK-0038 applies the starting recipe／Development scope.
- TASK-0038〜0042 remain blocked and advance only in dependency order.

## Implementation review questions

- Does multi-group capture select the largest group, then king distance, then canonical anchor exactly once?
- Is F09-02 kept as E1 comparator evidence while a reachable board test supplies E3 coverage?
- Are planning and execution both based on the shared runtime placement/effective-liberty/repetition kernel?
- Does player-turn preview preserve the stored intent and retarget only at execution?
- When the black king has zero real liberties but survives through effective liberties, does advance rank by distance to the king group's stones and retain all later tie-breaks?
- Are existing replay v2／BattleState projections unchanged for TASK-0039?
