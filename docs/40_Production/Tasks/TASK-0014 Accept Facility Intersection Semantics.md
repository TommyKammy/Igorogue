---
type: task
id: TASK-0014
status: done
project: Igorogue
milestone: M-1
priority: critical
dependencies: [TASK-0013]
updated: 2026-07-10
---
# TASK-0014 Accept Facility Intersection Semantics

## Outcome

施設交点が呼吸点・領地・石配置・捕獲へ与える意味を一義的にし、Rules Kernel、敵施設踏破、施設カードが同じ規則を参照できる状態にする。

## Source of truth

- [[Rules Canon]]
- [[ADR-0012 Facility Sites Are Empty Intersections]]
- [[FEAT-001 Territory and Facilities]]
- [[ADR-0012 Facility Intersection Fixtures]]
- [[Combat Resolution Order]]

## Non-goals

- 施設個別効果の製品コード実装
- 施設売却、移設、所有権奪取
- 運用容量による自動load shedding
- Rules Kernelの製品コード実装
- 敵施設建設
- A-4座標系、A-5余勢、A-7反攻数値の決定

## Acceptance criteria

- 施設をStone layerと別の空点上マーカーとしてAccepted化する。
- 施設点が実呼吸点、空点領域、領地サイズ、配置タグ、`StoneTopologyKey`で空点として扱われる。
- 合法な石配置だけが配置点施設を破壊し、不合法配置では施設状態とコストが不変である。
- 中立化停止、奪還再稼働、相手領地で非移転を定義する。
- 領地分割・結合時の座標再関連付けとcapacity超過を定義する。
- FEAT-001に擬似コードと3件以上のエッジケースがある。
- FAC-01〜FAC-09が機械検査を通る。
- [[Rules Canon]]、[[Combat Resolution Order]]、[[FEAT-009 Enemy Action Planning and Placement]]からAccepted決定を参照する。

## Allowed areas

- `docs/20_Design/Rules Canon.md`
- `docs/20_Design/Territory and Facilities.md`
- `docs/20_Design/Board and Placement.md`
- `docs/20_Design/Combat Resolution Order.md`
- `docs/20_Design/Glossary.md`
- `docs/20_Design/Initial Card Set.md`
- `docs/20_Design/Feature Specs/FEAT-001*`
- `docs/20_Design/Feature Specs/FEAT-009*`
- `docs/25_UIUX/Battle Screen Specification.md`
- `docs/30_Technical/Domain Model.md`
- `docs/30_Technical/Command Event Model.md`
- `docs/30_Technical/Determinism and Replay.md`
- `docs/30_Technical/Save Schema.md`
- `docs/30_Technical/Testing Strategy.md`
- `docs/40_Production/Tasks/TASK-0008*`
- `docs/40_Production/Tasks/TASK-0009*`
- `docs/50_Validation/Golden Replay Index.md`
- `docs/50_Validation/Spec Fixtures/`
- `docs/60_Decisions/ADRs/`
- `game_data/balance/system.json`
- `game_data/fixtures/`
- `tools/check_facility_semantics.py`
- 進捗、索引、manifest、release note

## Validation

```bash
python tools/check_facility_semantics.py
python tools/check_all.py
```

## Execution log

### 2026-07-10

- 施設を石と別レイヤーの空点上マーカーとするADR-0012をAccepted。
- FEAT-001を定型文から完全仕様へ置換し、領地計算、建設、停止、再稼働、直接踏破、capacity超過を定義。
- 容量を新規建設ゲートとし、分割後の既存over-capacity施設は自動停止しない方針を固定。
- Rules Canon、Combat Resolution、敵施設踏破、Domain、Replay、Save、UIを同期。
- FAC-01〜FAC-09の仕様fixtureと専用checkerを追加。
- `system.json`へ施設種別上限を移し、現在値の正本を明確化。

## Evidence

- `python tools/check_facility_semantics.py`: PASS。9 fixtures、ADR-0012、FEAT-001、Rules Canon、FEAT-009参照を検査。
- `python tools/check_all.py`: PASS。既存の敵行動、盤面反復、文書、content、抽象代理テストに回帰なし。
- FAC-04の期待理由を意図的に誤らせ、専用checkerがFAILする負のテストを実施。

## Known issues

- Rules Kernel未実装のため、fixtureは仕様checkerであり製品コードのgolden replayではない。
- 建設容量を意図的な領地分割で超過させる戦術が支配的になる可能性があり、M3でテレメトリ検証が必要。
- 一目領地の施設は通常の自殺手規則により敵が直接踏破できない場合がある。
- 施設の個別効果、明示的停止効果の重複、破壊報酬は後続Feature Specが必要。
