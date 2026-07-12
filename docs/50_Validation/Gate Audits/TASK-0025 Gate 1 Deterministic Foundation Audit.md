---
type: gate-audit
id: TASK-0025-GATE-1-AUDIT
status: decision_needed
project: Igorogue
milestone: M1
updated: 2026-07-12
fixed_main_head: 6398ec1e4f1e4ecf0c8eeaf71e33bb6ddeff6875
---
# TASK-0025 Gate 1 Deterministic Foundation Audit

## Result

`DECISION NEEDED`

Gate 1のordered implementation sequenceはfixed main HEADで完了している。しかしactiveな[[Golden Replay Index]]とAccepted fixture／ADR sourcesはMOM-01〜19、CTR-01〜25、TLE-01〜15をM1 shared Rules Kernel／golden replayへ移植すると明記する一方、activeな[[Codex Task Queue]]とAcceptedな[[Milestones and Exit Gates]]はMomentum／counterattackをGate 3／M3へ置いている。これはMOM／CTRのmilestone conflictである。

TLEには後工程へ置くsourceがなく、Acceptedな[[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]がM1移植を要求している。production lifecycleとE3 goldenがない現状は、conflictではなく独立したM1 implementation gapである。MOM／CTR conflictをowner decisionなしに片側へ合わせられず、TLE gapも残るため、M1 exitは`PASS`にできない。[[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]]の解決、TLEのbounded M1 follow-up、TASK-0012 human sign-offが揃うまでGate 1／M1をopenのまま維持し、M2 taskを`ready`にしない。

## Fixed baseline

- main HEAD: `6398ec1e4f1e4ecf0c8eeaf71e33bb6ddeff6875`
- PR #14 merged head: `abffb0f639918d3e30ec4a1ffdeb025e5f56f19a`
- PR #14 merge commit: `6398ec1e4f1e4ecf0c8eeaf71e33bb6ddeff6875`
- post-merge main CI run: `29171325730`
- CI jobs: Governance `86592900387`、Pure .NET `86592921178`、Godot／Windows export `86592965176`、全てsuccess
- content snapshot: `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`

## Exit evidence matrix

| Accepted statement／contract | Implemented artifact | Verification evidence | Class | Audit |
|---|---|---|---|---|
| 7×7 canonical board／point symmetry | `BoardGeometry`、`InitialPositionDefinition.Create`、`BoardState.FromInitialPosition` | COORD-01〜12、Domain tests、[[TASK-0003 Board Coordinates and Orthogonal Neighbours]] | E3 | PASS |
| orthogonal groups／unique real liberties | `StoneGroupAnalyzer`、`EffectiveLibertySnapshot` | Domain group／liberty tests、[[TASK-0004 Stone Groups and Unique Liberty Sets]] | E3 | PASS |
| hypothetical placement／simultaneous capture | `HypotheticalPlacementResolver` | capture ordering／input reversal tests、[[TASK-0005 Hypothetical Placement and Capture Resolution]] | E3 | PASS |
| suicide／terminal capture／repetition | `PlacementLegalityEvaluator`、`BattleRepetitionHistory` | KO-01〜07、illegal exact no-op tests、[[TASK-0006 Suicide Legality and Terminal Capture]] | E3 | PASS |
| king result／both-kings loss precedence | `KingCaptureResultEvaluator`、`BattleEndReasonRules` | king win／loss／both-captured tests、[[TASK-0007 King Capture and Battle Result]] | E3 | PASS |
| territory regions | `TerritoryAnalyzer`、`TerritoryDeltaResolver` | territory split／neutral／ordering tests、[[TASK-0008 Territory Region Calculation]] | E3 | PASS |
| facility site／runtime semantics | `FacilityState`、`FacilityRuntimeAnalyzer`、authorized build command | FAC-01〜09、[[TASK-0023 Implement Facility Runtime Semantics]]、[[TASK-0024 Authorized Facility Build Battle Command]] | E3 | PASS |
| seed／named RNG streams | `DeterministicRngState`、`AuthoritativeRngState`、`RngStream` | repeated seed／stream tests、[[TASK-0002 Deterministic RNG and Command Log]] | E3 | PASS |
| accepted-only command log | `OrderedCommandLog` | accepted／rejected chain tests、TASK-0002／TASK-0010／TASK-0011 | E3 | PASS |
| UIなし一戦処理 | `HeadlessBattleSession`／`HeadlessBattleStateMachine` | scripted authorized placement／enemy placement・pass、king terminal、20-turn loss tests、[[TASK-0010 Headless Battle State Machine]] | E3 | PASS WITH BOUNDARY |
| versioned golden board evidence | `tests/golden/v1/board_fixture_cases.json` | CORE／KO／FAC 19 cases、34 Application attempts、[[TASK-0009 Golden Board Fixtures]] | E3 | PASS |
| versioned replay round trip | `BattleReplayDocument`／serializer／runner | deterministic bytes、facts／state／log／terminal、tamper negatives、[[TASK-0011 Replay Round Trip Verification]] | E3 | PASS |
| formal board simulator | `tools/Igorogue.Sim.Cli` is bootstrap checksum smoke only | `tools/dev/sim-smoke` | N/A toolchain smoke | NOT PRODUCT EVIDENCE; explicit deferral required |
| card resolution named by Architecture | production card／deck／hand loopなし | no Gate 1 implementation task | none | M2 boundary; not an M1 PASS claim |
| MOM-01〜19 M1 migration | production Momentum state／resolver／goldenなし | specification checker only | E1 | CONFLICT／NOT IMPLEMENTED |
| CTR-01〜25 M1 migration | production counterattack／heat／Pending state／goldenなし | specification checker only | E1 | CONFLICT／NOT IMPLEMENTED |
| TLE-01〜15 M1 migration | production timed-liberty lifecycle／expiry sweep／goldenなし | specification checker only | E1 | M1 GAP／NOT IMPLEMENTED |
| M-1 two-human enemy fixture agreement | FEAT-009 machine fixtures complete | [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]] human sign-off pending | E1 | OPEN HUMAN GATE |

## UI-less battle boundary

M1の「UIなし一戦処理」はApplication APIとして、既承認済み黒／白placement、player turn end、enemy passを順序付きcommandとして注入し、王石captureまたは20-turn lossまで処理できる。これはE3 headless battle evidenceである。

一方、正式enemy planner、card／deck／hand／qi loop、Godot grayboxは含まれない。これらを「遊べる一戦」またはM2完成と呼ばない。

## Formal simulator boundary

[[Simulation Architecture]]の正式simulatorは同じApplication／Domain／ContentとBot policyを使う。現在の`Igorogue.Sim.Cli --smoke`は`BootstrapApplicationService`のchecksum一致だけを確認し、board battleを実行しない。

したがって本auditは`sim-smoke`をtoolchain determinism evidenceとしてだけ記録し、正式盤面simulation、balance、またはRules Kernel correctness evidenceとして扱わない。formal simulator接続はDecision解決後の別TASKで扱う。

## MOM／CTR milestone conflict

### M1 migrationを要求するsources

- activeな[[Golden Replay Index]] — MOM／CTRをM1 shared Rules Kernel event列／turn-boundary checksumへ移植。
- [[FEAT-002 Momentum Gate Fixtures]] — 仕様checkerをM1 unit／golden replayへ移植。
- [[FEAT-003 Counterattack Curve Fixtures]] — checker state machineを流用せず共有Kernelへ移植。

### 後工程へ置くsources

- activeな[[Codex Task Queue]] — Gate 3 Acceleration LabへMomentum、counterattackを配置。
- Acceptedな[[Milestones and Exit Gates]] — M3へ施設、余勢、触媒、反攻、妙手を配置。

[[TASK-0010 Headless Battle State Machine]]のMomentum／仮呼吸点／反攻Non-goalは、そのTASKがこれらを実装しないことを示す。milestone所属を変えるsourceではない。

## TLE M1 implementation gap

- Acceptedな[[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]] — TLE-01〜15をM1 Rules Kernel unit test／golden replayへ移植。
- Acceptedな[[FEAT-011 Temporary Liberty Expiry Fixtures]] — fixture checkerはM1 Rules Kernelの代替ではない。
- activeな[[Golden Replay Index]] — TLEをM1 migration catalogに含める。
- TLEを後工程へ置くAccepted milestone／ADRはない。TASK-0010のNon-goalはそのTASKの範囲限定であり、M1 requirementを移動しない。

### Production evidence

`src/`とproduct testsには`MomentumState`、counterattack／heat／Pending、timed temporary-liberty lifecycle／expiry sweepのproduction implementationがない。現golden catalogはCORE／KO／FAC 19 casesであり、MOM／CTR／TLE 59 fixturesを含まない。

### Bounded TLE follow-up proposal

TASK IDはDECISION-0005の解決時に割り当てる。Outcomeは、timed temporary-liberty effect state、enemy-turn-end expiry sweep、simultaneous mandatory capture／terminal orderingをproduction Rules Kernelへ追加し、TLE-01〜15をunit／golden replayのE3 evidenceへ移植することに限定する。

TLE-09／10／14／15は未実装のtrigger、予約resource／DeferredChoice、counterattack boundaryに依存する。follow-upはDECISION-0005で決まるMOM／CTR境界と一致する最小のproduction dependencyを明記し、仕様checkerをproduct evidenceとして流用しない。full MOM／CTR migrationはownerがM1に維持する場合だけ同一M1 workstreamに含める。

## Human-only gate

[[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]はmachine fixture 8件を満たすが、Accepted M-1 exit conditionである独立した人間2名の紙上結果一致がpendingである。Codex reviewで代替しない。

Gate 1 technical sequenceの完了と、このhuman-only prerequisiteを別表示する。Decisionを解決しても、human sign-off完了またはAccepted gate dependency変更なしにGate 2へ無条件移行しない。

## Smallest safe next action

1. [[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]]でMOM／CTRのmilestone境界を決める。
2. 現Accepted scopeに従い、TLE production lifecycle／E3 migrationのbounded M1 follow-upを追加する。
3. MOM／CTRを後工程へ移す場合はAccepted fixture／Feature Spec、Milestones、active index／queueを同一resolutionで同期する。M1へ維持する場合はproduction implementation／E3 migration taskを追加する。
4. TASK-0012の二人human sign-offを別gateとして完了する。
5. MOM／CTR decision、TLE M1 evidence、human sign-offが揃うまでTASK-0025を`blocked`、M2 taskをnot-readyに維持する。

## Scope verification

本auditはdocumentation／evidenceのみを変更する。production code、tests、`game_data/`、toolchain、package／project reference、Godot asset、Accepted rule／ADR／Feature Spec／Milestonesは変更しない。

## Validation evidence

- `tools/dev/check` ×2 — exit 0。全documentation／wikilink／content／fixture／governance check成功。
- `tools/dev/test` ×2 — exit 0。Domain 190、Application 54、Architecture 15、計259 tests、build warning 0／error 0。
- `tools/dev/sim-smoke` ×2 — exit 0。同一checksum `3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`。bootstrap determinism evidenceとしてのみ使用。
- `git diff --check` — exit 0。
