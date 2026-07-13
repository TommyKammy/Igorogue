---
type: sprint
status: active
project: Igorogue
updated: 2026-07-13
sprint: S0
---
# Current Sprint

## Goal

Review and merge TASK-0036's typed Reinforce targeting, pre-grant conditional draw, and temporary-liberty lifecycle without selecting the unresolved starter recipe.

## In review

- [[TASK-0036 Implement Starter Reinforce Effect]]
  - bind friendly target group and canonical stable stone anchor to command-time state
  - resolve draw-if-atari before granting +1 timed temporary liberty
  - prove expiry／merge-following／stale／foreign／deterministic behavior through the shared kernel
  - keep Headless／replay composition deferred to TASK-0039
  - fixed-HEAD primary／secondary reviews approved; awaiting PR review／human merge

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

## Next after TASK-0036

- [[TASK-0037 Implement Bandit Intent Planning and Execution]] — remains blocked until TASK-0036 human merge.
- [[DECISION-0006 Resolve M2 Starter Deck and Facility Scope]] — resolve before TASK-0038 applies the starting recipe／Development scope.
- TASK-0037〜0042 remain blocked and advance only in dependency order.

## Review questions

- Is Reinforce projected from the typed FriendlyGroup／DrawIfTargetAtari／TemporaryLiberty operation shape without a content-ID switch?
- Is the command-time friendly group bound to its canonical stable stone instance and rejected exactly when stale or foreign?
- Is effective liberty 1 measured before grant, with draw fully resolved before +1 temporary liberty is published?
- Does the effect follow its anchor through a merge and expire at the immediately following enemy-turn-end boundary?
- Do rejected commands preserve every resource／zone／runtime／trigger reference and avoid log／RNG mutation?
- Are Development、default recipe、Headless／replay composition、enemy planner、Godot still deferred to their owning tasks?
