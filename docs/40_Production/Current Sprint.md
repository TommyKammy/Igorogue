---
type: sprint
status: active
project: Igorogue
updated: 2026-07-13
sprint: S0
---
# Current Sprint

## Goal

Implement and review TASK-0034's atomic `card_basic_stone` PlayCard vertical proof without selecting the unresolved starter recipe.

## In progress

- [[TASK-0034 Implement Atomic Basic Stone Card Play]]
  - bind card instance、Canonical target、explicit mode、qi cost、existing placement pipeline
  - accepted-only log／facts and exact no-op rejection; replay schema remains deferred

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

## Next after TASK-0034

- [[TASK-0035 Implement Starter Stone Card Effects]] — remains blocked until TASK-0034 human merge.
- [[DECISION-0006 Resolve M2 Starter Deck and Facility Scope]] — resolve before TASK-0038 applies the starting recipe／Development scope.
- TASK-0035〜0042 remain blocked and advance only in dependency order.

## Review questions

- Does PlayCard reject stale、hand外、insufficient qi、invalid target without changing state／RNG／log?
- Does an accepted basic stone card use the existing placement／capture／facility／territory／terminal pipeline exactly once?
- Is qi consumed only after legality and the card retained as resolved until turn end?
- Is the typed operation injected without a content-ID switch or default recipe decision?
- Are other starter effects、Momentum、replay schema、Godot still deferred to their owning tasks?
