---
type: task
id: TASK-0023
status: ready
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0008]
updated: 2026-07-11
---
# TASK-0023 Implement Facility Runtime Semantics

## Outcome

Stone layerと独立したimmutableなfacility stateを共有pure Domain Rules Kernelへ導入し、FAC-01〜09のfacility-side runtime semanticsを決定論的に解決する。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]
- [[Combat Resolution Order]]
- [[FEAT-001 Territory and Facilities]]
- [[ADR-0012 Facility Sites Are Empty Intersections]]
- [[ADR-0012 Facility Intersection Fixtures]]
- [[ADR-0011 Battle-Local Stone Topology Repetition Ban]]
- [[FEAT-002 Momentum]]（実装sourceではなく境界照合）
- [[TASK-0008 Territory Region Calculation]]
- [[TASK-0009 Golden Board Fixtures]]
- [[DECISION-0001 Insert Facility Runtime Task Before Golden Fixtures]]
- `game_data/fixtures/facility_intersection_fixtures.json`
- `game_data/balance/system.json`

## Non-goals

- PlayCard、気／cost予約、card zone、placement tag、MomentumState、余勢生成、前線前進ドロー。
- 個別facility効果、on-build／on-destroy／active trigger dispatch、relic capacity modifier、収入の気への適用。
- `TerritoryEstablished`、global event bus、Application BattleState、enemy AI、UI／preview、telemetry。
- replay serialization、golden replay、turn-boundary checksum、save/load、round trip。
- Accepted仕様、game_data、balance値、package、project reference、Godot assetの変更。
- instance ID allocator。caller-provided stable IDを検証・保持する。
- explicit-disable effectの付与／解除command。source保持とoperating-state導出だけを扱う。

## Allowed areas

- `src/Igorogue.Domain/Facilities/`配下のpure Domain facility state、typed policy、resolver、fact型。
- accepted `LegalPlacementCommit`をfacility-awareな結果へ統合するために必要な、既存Domain Board／placement APIへの最小変更。
- `tests/Igorogue.Domain.Tests/`と`tests/Igorogue.Architecture.Tests/`の本タスク向けテスト、canonical FAC fixture読取。
- 本TASK、関連Decision、production state文書の実行状態とEvidence同期。
- Application、Content、`game_data/`、package／project reference、Godot assetは変更しない。

## Acceptance criteria

- `FacilityInstance`とfacility stateがimmutableで、instance ID／point／build sequenceが一意、列挙とlookupがCanonical point order→ordinal ID順、確定石との共存を拒否する。
- authoritative facility fieldsとnext build sequenceにversioned canonical projectionがあり、入力順に依存しない。active／control-lost／over-capacityは派生値とする。
- facility state、`BoardState`、`TerritoryAnalysis`をexact snapshotへbindし、foreign／stale／cross-snapshot解決を拒否する。facility metadataはterritory、実呼吸点、`StoneTopologyKey`へ入力しない。
- operating stateはexplicit disableを優先し、それ以外はowner色のregion内だけactive。neutral／相手領地では`territory_control_lost`、復帰時は同一instance／owner／build sequenceのままactiveとなる。
- territory再計算ごとに全facilityをpointからcurrent regionへ再関連付けし、persistentな旧region IDを保持しない。installed／同名countは対象current region内だけを数え、split後は別regionを数えず、merge後だけ合算する。
- operating-state transitionはderived state／reasonが実際に変わる時だけfactを返す。active→disabledは`FacilityDisabled(territory_control_lost)`、disabled→activeは`FacilityActivated(territory_control_restored)`、不変なら0件、複数変化はpoint→ordinal instance ID順とする。
- 基本収入、基本capacity tier、slot cap、per-type limitはhard-codeせず、変更しない`system.json`由来の明示的immutable policy入力から計算する。explicitまたはterritory由来でdisabledのfacilityもinstalled／type countへ含め、over-capacityは既存facilityを停止しない。
- build validationはstone occupied、facility occupied、owned territory、capacity、type limitの順。illegalはfacility／board／history／factを変えず、legalはcaller-provided IDとnext sequenceでbuildし、`FacilityBuilt`→`FacilityActivated(built_in_controlled_territory)`を返す。territory、topology、repetition historyは変えない。
- facility踏破はaccepted `LegalPlacementCommit`だけから作れるfacility-aware composite resultへ、exact candidate／history／facility source snapshotをbindする。配置pointのfacilityだけをowner不問で除去し、`StonePlaced`→`GroupCaptured[]`→`FacilityDestroyed(stone_occupied)?`→`StoneTopologyRegistered`→king resultのpre-trigger順を一つのtyped ordered seamで保持する。illegal評価からresultを作れず、topology observationはplacement commit由来のexactly one件だけでfacility破壊は追加せず、破壊後に自動復元しない。
- FAC-01〜09のfacility-side runtime assertionをcanonical JSONからproduction Domain unit testsへ移植する。入力順反転、immutable collection、duplicate／stone coexistence、canonical fact順、stale／cross-snapshot、state transition idempotency、split／mergeのpoint reassociationとregion-local countも検証する。
- Domain以外へ施設ruleを重複実装せず、Godot型、filesystem、ambient RNGを導入しない。
- `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke` 2回を実行し、TASK Evidenceを更新する。

## Validation

- FAC-01〜09をcanonical JSONから読み、production Domain APIに対するunit parity testとして実行する。
- duplicate、stone coexistence、入力順反転、immutable collection、canonical fact順、foreign／stale／cross-snapshot rejectionを回帰テストする。
- facility-aware placement seamのpre-trigger順、illegal construction rejection、exactly-one topology observation、transition idempotency、split／merge時のpoint reassociationとregion-local installed／type countをunit／architecture testで固定する。
- `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を実行し、closeout前に3 commandを連続でもう1回実行する。
- 独立Codex reviewを実施し、root `CODE_REVIEW.md`のreview evidenceをTASKへ記録する。

## Execution log

2026-07-11 — TASK-0008の独立review、GitHub Actions run `29141650052`の全3 job成功、PR #9の人間merge `8a29a622f6f66ad0ed0d5048a2172e87b8a2424b`を確認。[[DECISION-0001 Insert Facility Runtime Task Before Golden Fixtures]]のsmallest safe operational resolutionにより、TASK-0008とTASK-0009の間へ挿入し`ready`へ遷移。

## Evidence

未作成。

## Known issues

[[DECISION-0002 Resolve Territory and Facility Event Order]]と[[DECISION-0003 Sequence Golden Replay After Battle State Machine]]は後続統合をblockするが、本タスクのpure Domain facility runtime／FAC unit parityはblockしない。TASK-0010はfacility-aware composite seamを使用し、raw placement commitからfacility順を独自publishしてはならない。
