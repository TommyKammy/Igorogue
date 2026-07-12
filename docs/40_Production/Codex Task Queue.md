---
type: roadmap
status: active
project: Igorogue
updated: 2026-07-12
---
# Codex Task Queue

## Gate 0 — macOS runtime proof

### Completed

- [[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]]

### Closed artifacts

- [[TASK-0001 Decide Engine and Repository]]
- [[TASK-0020 Review Repository Bootstrap Runtime Evidence]]

### Human-only parallel action

- [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]] two-person paper fixture sign-off

## Gate 1 — deterministic foundation

Run serially unless a later architecture review explicitly permits parallel work.

Current: [[TASK-0029 Integrate Temporary Liberty Enemy Boundary and Golden Replay]] (`review`; Application enemy boundary and TLE-01〜15 versioned golden／replay evidence implemented; human merge pending).

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
17. [[TASK-0029 Integrate Temporary Liberty Enemy Boundary and Golden Replay]] — review／current

[[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]] resolved Option 1. TLE work runs strictly TASK-0027 → TASK-0028 → TASK-0029; do not parallelize their state contracts. After each human merge, only the immediate successor may move from `blocked` to `ready`. The specification checker remains E1 and cannot substitute for production E3 evidence.

[[DECISION-0003 Sequence Golden Replay After Battle State Machine]] resolved the post-TASK-0023 order as TASK-0010→TASK-0009→TASK-0011.
The reachability audit inserted TASK-0024 between TASK-0010 and TASK-0009. [[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]] resolved the exact-fixture evidence contract as Option 1.

## Gate 2 — Core Duel

Only after M1 exit evidence including TLE-01〜15 E3 migration and the required TASK-0012 human sign-off:

- basic deck/hand/qi loop;
- 山賊棋士 intent execution;
- graybox 7×7 board;
- capture, territory, and risk previews;
- no meta progression and minimal relic content.

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
