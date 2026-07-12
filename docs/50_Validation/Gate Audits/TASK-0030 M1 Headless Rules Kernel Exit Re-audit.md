---
type: gate-audit
id: TASK-0030-M1-EXIT-REAUDIT
status: pass
project: Igorogue
milestone: M1
updated: 2026-07-12
fixed_main_head: 35139bedb927f4c15b4e62a02c423947d5bdb1da
---
# TASK-0030 M1 Headless Rules Kernel Exit Re-audit

## Result

`M1 TECHNICAL EXIT: PASS`

`GATE 2 ENTRY: BLOCKED`

Fixed main HEAD `35139bedb927f4c15b4e62a02c423947d5bdb1da`では、[[Milestones and Exit Gates]]がM1へ要求するboard／liberty／capture／territory、seed／command log／replay、UIなし一戦処理、timed temporary-liberty expiry sweepとTLE-01〜15の共有Rules Kernel unit／golden evidenceを、production Domain／ApplicationとE3 automated evidenceへ矛盾なく追跡できる。

歴史的な[[TASK-0025 Gate 1 Deterministic Foundation Audit]]の`DECISION NEEDED`は、[[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]] Option 1によりMOM-01〜19／CTR-01〜25をM3へ同期し、TLE-01〜15をM1へ維持することで解決済みである。TASK-0027〜0029がTLE production／E3 gapを直列に閉じ、全3 TASKがhuman merge済み、fixed main CIもgreenである。したがってM1 technical exitは`PASS`とする。

ただし、これはplayable game、M2 Graybox、formal board simulator、card loop、actual enemy plannerの完成を意味しない。さらに[[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]の独立した人間2名によるpaper fixture agreementがpendingであり、Codex reviewでは代替できない。この別prerequisiteが閉じるまでGate 2 entryと全Gate 2 TASKの`ready`化は`BLOCKED`を維持する。

## Fixed baseline

- fixed main HEAD／PR #19 merge commit: `35139bedb927f4c15b4e62a02c423947d5bdb1da`
- PR #19 merged head: `141f431c70cf905c2bb0af8b05e72ee382be8c6e`
- post-merge main CI run: `29190754762`
- CI jobs: Governance `86644893062`、Pure .NET `86644908211`、Godot／Windows export `86644948844`、全てsuccess
- content snapshot: `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`
- v1 board golden SHA-256: `b3e62c12574746233e1d829e4f30fcc179559cae017fcdd707a656e63b01655d`
- v2 TLE golden SHA-256: `9f6486d9776ec05a0c6972f6fdb1ab6dfc49cdd5c653b05831a83216dea8d180`
- TLE source fixture SHA-256: `9f9a74ee9e1407c2b0882b6ccd1aa86ae950dd750fb0bfb4bc3bf12faae20e60`

## Exit evidence matrix

| Accepted statement／contract | Implemented artifact | Verification evidence | Class | Audit |
|---|---|---|---|---|
| 7×7 canonical board／point symmetry | `BoardGeometry`、`InitialPositionDefinition.Create`、`BoardState.FromInitialPosition` | COORD-01〜12、Domain tests、[[TASK-0003 Board Coordinates and Orthogonal Neighbours]] | E3 | PASS |
| orthogonal groups／unique real liberties | `StoneGroupAnalyzer`、`EffectiveLibertySnapshot` | group／liberty tests、[[TASK-0004 Stone Groups and Unique Liberty Sets]] | E3 | PASS |
| hypothetical placement／simultaneous capture | `HypotheticalPlacementResolver` | capture ordering／input reversal tests、[[TASK-0005 Hypothetical Placement and Capture Resolution]] | E3 | PASS |
| suicide／terminal capture／battle-local repetition | `PlacementLegalityEvaluator`、`BattleRepetitionHistory` | KO-01〜07、illegal exact no-op tests、[[TASK-0006 Suicide Legality and Terminal Capture]] | E3 | PASS |
| king result／both-kings loss／turn-20 precedence | `KingCaptureResultEvaluator`、`BattleEndReasonRules` | king result、terminal、turn-20 tests、[[TASK-0007 King Capture and Battle Result]] | E3 | PASS |
| territory regions／typed establishment | `TerritoryAnalyzer`、`TerritoryDeltaResolver` | split／neutral／ordering／expiry-source tests、[[TASK-0008 Territory Region Calculation]] | E3 | PASS |
| facility site／runtime semantics | `FacilityState`、`FacilityRuntimeAnalyzer`、authorized build command | FAC-01〜09、[[TASK-0023 Implement Facility Runtime Semantics]]、[[TASK-0024 Authorized Facility Build Battle Command]] | E3 | PASS |
| seed／named RNG streams | `DeterministicRngState`、`AuthoritativeRngState`、`RngStream` | repeated seed／stream tests、[[TASK-0002 Deterministic RNG and Command Log]] | E3 | PASS |
| accepted-only command log | `OrderedCommandLog` | accepted／rejected chain tests、TASK-0002／0010／0011 | E3 | PASS |
| UIなし一戦処理 | `HeadlessBattleSession`、`HeadlessBattleStateMachine`、`AuthoritativeEnemyTurnStateMachine` | scripted authorized placement／pass、normal／bonus boundary、king terminal、turn-20 tests、TASK-0010／0029 | E3 | PASS WITH BOUNDARY |
| versioned board golden | `tests/golden/v1/board_fixture_cases.json` | CORE／KO／FAC 19 cases、Application boundary evidence、[[TASK-0009 Golden Board Fixtures]] | E3 | PASS |
| versioned replay round trip | replay schema 1／2 document、serializer、runner | deterministic bytes、state／facts／log／terminal、tamper／cross-version negatives、TASK-0011／0029 | E3 | PASS |
| timed temporary-liberty identity／effective liberty／expiry sweep | `StoneRuntimeState`、`TemporaryLibertyState`、`ContinuousLibertySnapshot`、`TemporaryLibertyExpiryResolver` | TLE Domain projection、enumeration reversal、simultaneous capture、king gate、topology tests、[[TASK-0027 Implement Temporary Liberty Domain Kernel]] | E3 | PASS |
| closed-window capture benefits／minimal boundary pressure | `CaptureBatch`、`CaptureBenefitTriggerPlan`、`ClosedWindowCaptureBenefitResolver`、`CounterattackBoundaryState` | TLE-09／10／15 exact resource／event／order tests、terminal suppression、source lifetime negatives、[[TASK-0028 Implement Closed-Window Capture Benefits and TLE Boundary Pressure]] | E3 | PASS |
| final enemy boundary／TLE-01〜15 golden replay | authoritative state v2、normal／bonus action boundary、replay schema 2、`tests/golden/v2/temporary_liberty_cases.json` | all 15 cases、same run twice、reversed setup、replay twice、TLE-12／13／14／terminal exact tests、[[TASK-0029 Integrate Temporary Liberty Enemy Boundary and Golden Replay]] | E3 | PASS |
| MOM-01〜19／CTR-01〜25 production migration | DECISION-0005 Option 1、M3 migration sources | active Feature Specs／fixture notes／Golden Replay Index／Milestones／queue all place production migration in M3 | E1 until M3 | NOT AN M1 REQUIREMENT |
| formal board simulator | `tools/Igorogue.Sim.Cli --smoke` remains bootstrap checksum only | `tools/dev/sim-smoke` | N/A toolchain smoke | NOT PRODUCT EVIDENCE; M4 boundary |
| card／deck／hand／qi loop、actual enemy planner、graybox UI | production implementationなし | no M1 completion claim | none | M2 boundary; not playable |
| M-1 two-human enemy fixture agreement | FEAT-009 machine fixtures complete | [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]] human sign-off pending | E1 | OPEN HUMAN GATE／GATE 2 BLOCKED |

## TLE-01〜15 production evidence

TASK-0027はstone instance identity、timed effect、continuous modifier、effective-liberty analysis、due-effect removal、single-snapshot simultaneous capture、king gate、mandatory repeated topology、TLE-01〜08／11〜13のDomain projectionをproduction kernelへ追加した。fixture expectedをproduction inputとして使わず、same input／reversed enumerationのcanonical一致を検証する。

TASK-0028はplacement／expiryを共通`CaptureBatch`へ正規化し、standard accountingからscore／telemetryまでのtyped closed-window benefit stage、reserved draw／qi、Soul、DeferredChoice、first-use flags、TLEに必要な最小counterattack boundaryを追加した。TLE-09／10／15のresource／fact／order、TLE-07／08 terminal suppression、standard Soul capとsource lifetimeをDomain testsで固定する。

TASK-0029はexact authoritative initial snapshot、normal／bonus scripted enemy action、Pending consume／reprime、expiry、terminal gate、benefit、territory／facility、natural gain、planning traceをApplicationで順序付け、state／replay schema 2とTLE-01〜15 golden catalogを追加した。catalogはsource fixture SHA、runtime content hash、seed 42、initial state、commands、facts、state／log checksums、terminal resultを固定し、同一run 2回、初期列挙反転、save／load／runner 2回を検証する。

TLE goldenはMomentum event 0、Brilliant event 0、CTR-01〜25 coverage 0を明示する。TLEに必要なminimal boundary stateをfull Momentum／counterattack production migrationとして扱わない。

## UI-less battle boundary

M1の「UIなし一戦処理」は、Application commandとして黒／白のauthorized placement、player turn end、normal／bonus enemy placementまたはpassを順序付きで注入し、王石captureまたは20-turn lossまでimmutable state／ordered facts／accepted-only logを更新できる範囲で満たす。v2はtemporary-liberty、closed-window resource、minimal counterattack boundaryもinitial snapshotからexact-bindする。

正式なFEAT-009 enemy candidate ranking／intent execution、card／deck／hand／qi-spend loop、Godot board input／renderingは実装していない。そのため本結果を「人間が遊べる一戦」「M2 Graybox完成」と表現しない。

## Formal simulator boundary

[[Simulation Architecture]]のformal simulatorはlive gameと同じApplication／Domain／ContentとBot policyを使う。現在の`tools/dev/sim-smoke`はbootstrap checksum determinismだけを確認し、board battle、分布、balanceを検証しない。

Accepted [[Milestones and Exit Gates]]は正式シミュレーションをM4へ置くため、現状はM1 technical exitのgapではない。ただしCurrent Development Stateでは未実装を継続表示し、Rules Kernel correctness／balance evidenceとして引用しない。

## Technical exit and Gate 2 entry

M1 technical exitは共有Rules Kernel／Application／golden／replayのE3 evidenceに対する判定であり、`PASS`である。TASK-0012 human agreementはM-1から継続する別のGate 2 prerequisiteであり、pendingである。

したがって現在の状態を次のように分離する。

- M1 technical exit: `PASS`
- TASK-0012 two-human paper sign-off: `PENDING`
- Gate 2 entry: `BLOCKED`
- Gate 2 implementation TASK: `NOT READY`
- playable／fun validation: `NOT STARTED`

## Smallest safe next action

1. 独立した人間2名が[[FEAT-009 Enemy Decision Fixtures]]をexpected outputを読まずに解き、同一intent／placement結果を記録する。
2. TASK-0012へ2名分のhuman evidenceを記録し、人間判断でcloseする。
3. M1 technical PASSとTASK-0012完了が揃った後だけ、Gate 2 Core Duelのbounded implementation TASKを定義する。

## Scope verification

本re-auditはdocumentation／evidenceだけを変更する。production code、tests、`game_data/`、toolchain、package／project reference、Godot asset、Accepted Rules Canon／ADR／Feature Spec／Milestonesは変更しない。歴史的TASK-0025 audit reportも変更しない。

## Validation evidence

- `tools/dev/check` ×2 — exit 0。documentation／wikilink／content／fixture／governance check成功、47 content IDs、content snapshot一致。
- `tools/dev/test` ×2 — exit 0。.NET SDK 8.0.422、Domain 293、Application 105、Architecture 20、計418 tests、warning 0／error 0。
- `tools/dev/sim-smoke` ×2 — exit 0。同一checksum `3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`。bootstrap determinism evidenceとしてのみ使用。
- `tools/dev/build` — exit 0、warning 0／error 0。
- `git diff --check` — exit 0。
- independent fixed-HEAD review — substantive audit HEAD `d8f971c1fa594e2129fb31fdf5b75e6913cebc6e`をbase `35139bedb927f4c15b4e62a02c423947d5bdb1da`と比較。matrix、sources、artifact／tests、境界、docs-only scope、GitHub evidenceを直接照合し、actionable findingなし、`APPROVE`。
