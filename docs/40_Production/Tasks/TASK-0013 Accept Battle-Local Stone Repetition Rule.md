---
type: task
id: TASK-0013
status: done
project: Igorogue
milestone: M-1
priority: critical
dependencies: []
updated: 2026-07-10
---
# TASK-0013 Accept Battle-Local Stone Repetition Rule

## Outcome

複数カード・反攻追加行動・捕獲報酬を含むIgorogueに対し、戦闘内の石配置循環を一義的に禁止する規則をAcceptedにする。

## Source of truth

- [[Rules Canon]]
- [[ADR-0011 Battle-Local Stone Topology Repetition Ban]]
- [[ADR-0011 Board Repetition Fixtures]]
- [[FEAT-009 Enemy Action Planning and Placement]]

## Non-goals

- 古典囲碁のコウ材、三コウ、長生の再現
- コウ特化カード・遺物
- Rules Kernelの製品コード実装
- 種石成長失敗時の再予約規則
- 施設交点の意味論

## Acceptance criteria

- `StoneTopologyKey`の含有・除外状態が一義的に定義される。
- 初期登録、履歴追加、戦闘間リセットが定義される。
- プレイヤー、敵候補、自動生成に同じ反復判定が適用される。
- 反復手ではコスト、カード移動、捕獲、トリガー、履歴が変化しない。
- [[ADR-0011 Battle-Local Stone Topology Repetition Ban]]がAcceptedである。
- 単純コウを含む7件の仕様fixtureが機械検査を通る。
- [[Rules Canon]]と[[Combat Resolution Order]]から本決定が参照される。

## Allowed areas

- `docs/20_Design/Rules Canon.md`
- `docs/20_Design/Board and Placement.md`
- `docs/20_Design/Combat Resolution Order.md`
- `docs/20_Design/Glossary.md`
- `docs/20_Design/Feature Specs/FEAT-009*`
- `docs/30_Technical/Domain Model.md`
- `docs/30_Technical/Command Event Model.md`
- `docs/30_Technical/Determinism and Replay.md`
- `docs/30_Technical/Save Schema.md`
- `docs/30_Technical/Testing Strategy.md`
- `docs/40_Production/Tasks/TASK-0006*`
- `docs/40_Production/Tasks/TASK-0009*`
- `docs/50_Validation/Spec Fixtures/`
- `docs/60_Decisions/ADRs/`
- `game_data/fixtures/`
- `tools/check_board_repetition.py`
- 進捗、索引、manifest、release note

## Validation

```bash
python tools/check_board_repetition.py
python tools/check_all.py
```

## Execution log

### 2026-07-10

- 戦闘内`StoneTopologyKey`反復禁止を選択し、ADR-0011をAccepted。
- 特殊石種と非石状態をキーから除外し、石色・王石位置だけを比較する方針を固定。
- Rules Canon、配置仕様、解決順、Domain、Replay、Save、Testingを同期。
- 単純コウ、特殊石回避、非石状態回避、別地点を挟む取り返し、敵候補除外を含む7 fixtureを追加。
- 専用検査`check_board_repetition.py`を`check_all.py`へ統合。

## Evidence

- `python tools/check_board_repetition.py`: PASS。7 fixtures、ADR/Rules Canon参照を検査。
- `python tools/check_all.py`: PASS。既存FEAT-009を含む全検査に回帰なし。

## Known issues

- Rules Kernel未実装のため、fixtureは仕様検査であり製品コードのgolden replayではない。
- 自動生成が反復で抑止された後の再予約・別点選択は各Feature Specで定義する必要がある。
- 将来のコウ特化システムはADR-0011の後継ADRを必要とする。
