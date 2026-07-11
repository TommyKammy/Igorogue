---
type: roadmap
status: active
project: Igorogue
updated: 2026-07-11
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

Current: [[TASK-0003 Board Coordinates and Orthogonal Neighbours]].

1. [[TASK-0002 Deterministic RNG and Command Log]]
2. [[TASK-0003 Board Coordinates and Orthogonal Neighbours]]
3. [[TASK-0004 Stone Groups and Unique Liberty Sets]]
4. [[TASK-0005 Hypothetical Placement and Capture Resolution]]
5. [[TASK-0006 Suicide Legality and Terminal Capture]]
6. [[TASK-0007 King Capture and Battle Result]]
7. [[TASK-0008 Territory Region Calculation]]
8. [[TASK-0009 Golden Board Fixtures]]
9. [[TASK-0010 Headless Battle State Machine]]
10. [[TASK-0011 Replay Round Trip Verification]]

## Gate 2 — Core Duel

Only after M1 exit evidence:

- basic deck/hand/qi loop;
- 山賊棋士 intent execution;
- graybox 7×7 board;
- capture, territory, and risk previews;
- no meta progression and minimal relic content.

## Gate 3 — Acceleration Lab

Only after Core Duel shows meaningful per-turn decisions:

- Momentum;
- one facility engine;
- one sacrifice engine;
- limited catalysts;
- counterattack;
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
