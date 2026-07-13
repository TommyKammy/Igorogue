---
type: roadmap
status: active
project: Igorogue
updated: 2026-07-13
---
# Codex Task Queue

## Gate 0 — macOS runtime proof

### Completed

- [[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]]

### Closed artifacts

- [[TASK-0001 Decide Engine and Repository]]
- [[TASK-0020 Review Repository Bootstrap Runtime Evidence]]

### Human-only evidence and owner waiver

- [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]] — `review`; two-person paper evidence is not retained
- [[DECISION-0007 Authorize Gate 2 Without Retained TASK-0012 Human Evidence]] — resolved; Project owner waived the Gate 2 evidence prerequisite without certifying execution／agreement

## Gate 1 — deterministic foundation

Run serially unless a later architecture review explicitly permits parallel work.

Status: technical work complete. [[TASK-0030 Re-audit M1 Headless Rules Kernel Exit]] established M1 technical exit `PASS`, PR #20 was human-merged, and post-merge CI is green. TASK-0012 evidence remains unverified and is tracked separately from the owner waiver.

1. [[TASK-0002 Deterministic RNG and Command Log]]
2. [[TASK-0003 Board Coordinates and Orthogonal Neighbours]]
3. [[TASK-0004 Stone Groups and Unique Liberty Sets]]
4. [[TASK-0005 Hypothetical Placement and Capture Resolution]]
5. [[TASK-0006 Suicide Legality and Terminal Capture]]
6. [[TASK-0007 King Capture and Battle Result]]
7. [[TASK-0008 Territory Region Calculation]]
8. [[TASK-0023 Implement Facility Runtime Semantics]]
9. [[TASK-0010 Headless Battle State Machine]] — done
10. [[TASK-0024 Authorized Facility Build Battle Command]] — done
11. [[TASK-0009 Golden Board Fixtures]] — done
12. [[TASK-0011 Replay Round Trip Verification]] — done
13. [[TASK-0025 Audit Gate 1 Deterministic Foundation Completion]] — done
14. [[TASK-0026 Resolve M1 Momentum Counterattack Migration Boundary]] — done
15. [[TASK-0027 Implement Temporary Liberty Domain Kernel]] — done
16. [[TASK-0028 Implement Closed-Window Capture Benefits and TLE Boundary Pressure]] — done
17. [[TASK-0029 Integrate Temporary Liberty Enemy Boundary and Golden Replay]] — done
18. [[TASK-0030 Re-audit M1 Headless Rules Kernel Exit]] — done

[[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]] resolved Option 1. TLE work runs strictly TASK-0027 → TASK-0028 → TASK-0029; do not parallelize their state contracts. After each human merge, only the immediate successor may move from `blocked` to `ready`. The specification checker remains E1 and cannot substitute for production E3 evidence.

[[DECISION-0003 Sequence Golden Replay After Battle State Machine]] resolved the post-TASK-0023 order as TASK-0010→TASK-0009→TASK-0011.
The reachability audit inserted TASK-0024 between TASK-0010 and TASK-0009. [[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]] resolved the exact-fixture evidence contract as Option 1.

## Gate 2 — Core Duel

Entry is owner-authorized open because M1 exit evidence including TLE-01〜15 E3 migration is complete and DECISION-0007 explicitly waives the unverified TASK-0012 human-evidence prerequisite.

Current: [[TASK-0039 Integrate Headless Core Duel and Replay]] (`review`; TASK-0038 is merged through PR #28／post-merge CI, and Core Duel aggregate／replay schema 3 evidence plus fixed source HEAD review are complete; CI／human merge pending).

1. [[TASK-0031 Plan Gate 2 Core Duel Implementation]] — done
2. [[TASK-0032 Implement Typed Core Duel Content Catalog]] — done
3. [[TASK-0033 Implement Deterministic Battle Deck Hand and Qi Kernel]] — done
4. [[TASK-0034 Implement Atomic Basic Stone Card Play]] — done
5. [[TASK-0035 Implement Starter Stone Card Effects]] — done
6. [[TASK-0036 Implement Starter Reinforce Effect]] — done
7. [[TASK-0037 Implement Bandit Intent Planning and Execution]] — done
8. [[TASK-0038 Apply Resolved M2 Starter Deck and Facility Scope]] — done
9. [[TASK-0039 Integrate Headless Core Duel and Replay]] — review／current; CI／human merge pending
10. [[TASK-0040 Implement Core Duel Preview Queries]] — blocked by TASK-0039
11. [[TASK-0041 Build Playable Godot Core Duel Graybox]] — blocked by TASK-0040
12. [[TASK-0042 Validate M2 Core Duel Graybox]] — blocked by TASK-0041

DECISION-0009／0010 Option 1 are implemented and TASK-0037 is done. DECISION-0006 resolved Option 1: TASK-0038 writes the starter 6-type／12-card default recipe, records Development as the only M2 facility exception, and preserves broad facility expansion for M3. No meta progression or broad relic／facility content enters Gate 2.

## Gate 3 — Acceleration Lab

Only after Core Duel shows meaningful per-turn decisions:

- Momentum;
- MOM-01〜19 production Rules Kernel unit／golden migration;
- one facility engine;
- one sacrifice engine;
- limited catalysts;
- counterattack;
- CTR-01〜25 production Rules Kernel unit／golden migration;
- two styles and two enemies.

## Items not to start early

- full 30+ card pool;
- final art and audio;
- meta unlock economy;
- rank/stake system;
- Act 2;
- broad enemy roster;
- proxy-model balance tuning;
- Steam Deck final polish.

## Worktree concurrency rule

At most two worktrees initially:

- one implementation task;
- one independent review or documentation task.

Increase concurrency only after the repository bootstrap and first three Domain tasks complete without merge conflicts or evidence drift.
