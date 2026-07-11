---
type: task
id: TASK-0003
status: review
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001]
updated: 2026-07-11
---
# TASK-0003 Board Coordinates and Orthogonal Neighbours

## Outcome

7×7座標と上下左右隣接を純粋関数で実装。

## Source of truth

- [[Rules Canon]]
- [[Coordinate System and Initial Position]]
- [[Coordinate System and Initial Position Fixtures]]
- [[Architecture]]
- [[Determinism and Replay]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- CanonicalPoint `(1..7,1..7)`とInternalPoint `(0..6,0..6)`の往復変換。
- canonical index `0..48`との往復変換。
- 盤外値をclampせず拒否。
- 隅2、辺3、中央4の隣接テスト。
- `reflect(reflect(p)) == p`のproperty test。
- `standard_v0_2`初期盤面が色・役割交換付き点対称で、各王石グループが3石・実呼吸点7。
- COORD-01〜COORD-12を共有Rules Kernelのunit testへ移植。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

2026-07-11 — TASK-0002の独立review、green CI、PR #3の人間mergeを確認し、直列Gate 1の次タスクとして`ready`へ遷移。

2026-07-11 — Accepted座標仕様、COORD-01〜COORD-12、`system.json`の`standard_v0_2`を照合し、Domainの純粋座標APIと外部入力型initial-position factoryの実装を開始。TASK-0004のgroup/liberty APIとTASK-0009のgolden replayは先取りしない。

2026-07-11 — `BoardGeometry`、`CanonicalPoint`、`InternalPoint`を実装。7×7契約をfail-closedで検証し、canonical index順の49点、1-based／0-based変換、盤面図row変換、点対称、canonical順の直交隣接をimmutableな純粋APIとして追加。

2026-07-11 — 外部dataからIDと石配置を受け取る汎用`InitialPositionDefinition`を実装。入力を防御copyしてcanonical point順へ正規化し、不正enum、重複点、盤外点を拒否する。productionコードへ`standard_v0_2`の6座標は直書きしていない。

2026-07-11 — COORD-01〜COORD-12と追加境界/property testを共有Domain testへ移植。`system.json`をtest入力として標準配置を構築し、色交換・同役割維持の点対称、各王石groupの3石連結・実呼吸点7をtest-only traversalで検証した。package、project reference、lock、game_data、Accepted仕様、Godot assetは変更していない。

## Evidence

- `tools/dev/check` — exit 0。documentation、wikilink、content、COORD-01〜COORD-12を含むgovernance checkが成功。content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`。
- `tools/dev/test` — exit 0。exact .NET SDK `8.0.422`、locked restore、Release build、warning 0／error 0。Domain 50、Application 12、Architecture 5、合計67 testが成功。
- `tools/dev/sim-smoke`を2回実行 — 両方exit 0。同一の`checksum=3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`、同一content hash、`files=7`を確認。
- `tests/Igorogue.Domain.Tests/BoardCoordinateTests.cs` — COORD-01〜09、全49点のCanonical/Internal/index往復、0／-1／6／8のboard-size拒否、各座標境界、隅2・辺3・中央4の隣接、斜め除外、全点のreflection involution、公開comparatorの`(y,x)`順を確認。
- `tests/Igorogue.Domain.Tests/InitialPositionFixtureTests.cs` — `game_data/balance/system.json`のboard sizeと`standard_v0_2`をDomain factoryへ入力し、COORD-10〜12のexact 6-stone set、同役割を保つ色交換点対称、中央空点、各色1王2護衛、3石連結、exact 7-liberty set、diagram row順を確認。
- 読み取り専用implementation reviewで、非7盤の公開生成、未検証comparator、未承認ID文字種制約、Domain内constructor bypassを検出して修正。再確認後の判定は`APPROVE`、残存findingなし。
- 独立Codex review — `origin/main...HEAD`の全14ファイルとAccepted仕様／runtime dataを照合し、findingなしで`APPROVE`。review側でもgovernance、67/67 test、2回同一simulator checksumを確認。

## Known issues

TASK-0003範囲の既知defectはなし。

productionのtyped Content mapperは本タスク範囲外であり、`InitialPositionDefinition`は検証済み外部dataを受け取る汎用factoryまで。group／unique-libertyのproduction APIはTASK-0004、`StoneTopologyKey`とgolden board fixtureはTASK-0009へ明示的に延期する。

`system.json`のsymmetry ID `point_reflection_with_color_and_role_swap`はopaque IDとして維持した。Accepted仕様・fixture・checkerに従い、実行契約は色を交換し役割を維持する。data tokenのrenameは仕様/data同期が必要なため本タスクでは行わない。
