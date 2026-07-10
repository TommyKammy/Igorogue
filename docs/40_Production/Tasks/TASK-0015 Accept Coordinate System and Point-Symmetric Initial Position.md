---
type: task
id: TASK-0015
status: done
project: Igorogue
milestone: M-1
priority: critical
dependencies: [TASK-0014]
updated: 2026-07-10
---
# TASK-0015 Accept Coordinate System and Point-Symmetric Initial Position

## Outcome

7×7盤の表示座標、内部座標、盤面図、canonical point order、点対称変換、標準初期配置を一義的にし、Rules Kernel、fixture、敵タイブレーク、リプレイが同じ座標契約を参照できる状態にする。

## Source of truth

- [[Rules Canon]]
- [[Coordinate System and Initial Position]]
- [[Coordinate System and Initial Position Fixtures]]
- `game_data/balance/system.json`

## Non-goals

- Rules Kernelの製品コード実装
- 初期配置バリエーション
- 先手有利のバランス調整
- 敵タイブレーク方式の再設計
- A-5余勢、A-7反攻数値、B-2仮呼吸点失効順
- 7×7以外の盤面サイズ対応

## Acceptance criteria

- CanonicalPointは`(x,y)`、各軸1〜7、`x`左→右、`y`下→上と明記する。
- 盤面図は上から`y=7`、下が`y=1`と明記する。
- InternalPointは0〜6で、CanonicalPointとの純粋な変換式を定義する。
- Canonical point orderを`y`昇順、次に`x`昇順とし、linear indexを0〜48で定義する。
- 点対称変換`reflect(x,y)=(8-x,8-y)`を定義する。
- `standard_v0_2`初期配置が色・役割交換付き点対称である。
- 各色の初期王石グループが3石連結・実呼吸点7である。
- system data、Rules Canon、technical docs、FEAT-009、ADR-0011が同じ座標契約を参照する。
- COORD-01〜COORD-12が専用checkerを通る。
- 既存checkerのStoneTopologyKeyが盤面図行順ではなくCanonical point orderを使用する。

## Allowed areas

- `docs/20_Design/Rules Canon.md`
- `docs/20_Design/Board and Placement.md`
- `docs/20_Design/Coordinate System and Initial Position.md`
- `docs/20_Design/Glossary.md`
- `docs/20_Design/Feature Specs/FEAT-009*`
- `docs/25_UIUX/Battle Screen Specification.md`
- `docs/30_Technical/Domain Model.md`
- `docs/30_Technical/Command Event Model.md`
- `docs/30_Technical/Determinism and Replay.md`
- `docs/30_Technical/Save Schema.md`
- `docs/30_Technical/Testing Strategy.md`
- `docs/40_Production/Tasks/TASK-0003*`
- `docs/40_Production/Tasks/TASK-0009*`
- `docs/50_Validation/Golden Replay Index.md`
- `docs/50_Validation/Spec Fixtures/`
- `docs/60_Decisions/ADRs/ADR-0011*`
- `game_data/balance/system.json`
- `game_data/fixtures/coordinate_system_fixtures.json`
- `tools/check_coordinate_system.py`
- `tools/check_board_repetition.py`
- `tools/check_facility_semantics.py`
- 進捗、索引、manifest、release note

## Validation

```bash
python tools/check_coordinate_system.py
python tools/check_all.py
```

## Execution log

### 2026-07-10

- SPEC-COORD-001をAcceptedとして新設。
- CanonicalPoint、InternalPoint、盤面図行順、canonical index、隣接列挙順を固定。
- 標準初期配置`standard_v0_2`をsystem dataへ追加し、色・役割交換付き点対称を固定。
- 各初期王石グループが3石連結、実呼吸点7であることをfixture化。
- FEAT-009、ADR-0011、Domain、Replay、Save、UI、M1タスクを同期。
- 既存仕様checkerの`StoneTopologyKey`をCanonical point orderへ修正。
- COORD-01〜COORD-12と専用checkerを追加。

## Evidence

- `python tools/check_coordinate_system.py`: PASS。12 fixtures、system data、文書参照、初期点対称、各王石グループ実呼吸点7を検査。
- `python tools/check_all.py`: PASS。文書、リンク、content、敵行動、盤面反復、施設意味論、抽象代理テストに回帰なし。
- COORD-10の期待石数を意図的に変更し、専用checkerがFAILする負のテストを実施後、元へ戻して再PASS。

## Known issues

- Rules Kernel未実装のため、fixtureは仕様checkerであり製品コードのgolden replayではない。
- 幾何学的対称性は、プレイヤー先手・複数カード行動と敵1手の時間的対称性を保証しない。
- FEAT-009の最終座標タイブレークは左下側を優先し得るため、M3で行動偏りをテレメトリ確認する。
- 7×7以外へ拡張する場合は座標契約、反復キー、初期配置、fixtureを同時更新する必要がある。
