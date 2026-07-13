---
type: sprint
status: active
project: Igorogue
updated: 2026-07-14
sprint: S0
---
# Current Sprint

## Goal

Build the minimal playable Godot 4.7 .NET Core Duel graybox on Application commands／queries without duplicating rules or runtime values in presentation code.

## In progress

- [[TASK-0041 Build Playable Godot Core Duel Graybox]]
  - dependency TASK-0040 is done through PR #30 source HEAD `eaa62531615eef7a10cfe1d16fe92318d45143c8`／main merge `d8ccc08cf7fa3cc1a43046d128b2804b50b9d073`／post-merge CI run `29285926156` all 3 jobs success
  - 7×7 board、hand、qi、turn／result、Bandit intent、card target、End Turn、restartをApplication command／queryだけへ接続する
  - start auditでproduction標準初期snapshot factoryの欠落を確認し、2026-07-14 Project ownerがpure typed Domain／Content／Application startup seamと対応testを限定承認
  - startup seamは既存Accepted ruleと`game_data/`を使用し、player-visible rule／runtime valueを変更しない
  - typed startup、query-only rendering、command-only input、terminal／restart smokeを実装済み、653 tests、sim／Godot smoke、Windows exportは成功
  - independent fixed-HEAD reviewは完了済み。initial／selected-hoverの480×270 Codex visual QAも実施済みで、Project owner human visual reviewとPR CIは未完了

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
- [[TASK-0040 Implement Core Duel Preview Queries]] — PR #30 source `eaa62531615eef7a10cfe1d16fe92318d45143c8` merged at `d8ccc08cf7fa3cc1a43046d128b2804b50b9d073`／post-merge CI run `29285926156` green

## Next

- Complete [[TASK-0041 Build Playable Godot Core Duel Graybox]] implementation、runtime／visual validation、independent review、and human merge.
- [[TASK-0042 Validate M2 Core Duel Graybox]] remains blocked and advances only after TASK-0041 human merge.

## Implementation review questions

- Does startup obtain the standard authoritative initial snapshot through the bounded production factory without copying board／policy／runtime values into Godot?
- Does Godot render only query projections and mutate battle state only through accepted Application commands?
- Is canonical orientation left-bottom `(1,1)`／right-top `(7,7)` preserved for drawing、hover、and confirm input?
- Are hand、qi、turn／result、Bandit primary／alternate intent、legality、capture、liberty／atari、and danger states visible at 480×270 integer-scaled layout?
- Do headless parse／build、bootstrap smoke、Windows export、human visual review、and fixed-HEAD independent review all pass without expanding final-art or full-counterattack scope?
