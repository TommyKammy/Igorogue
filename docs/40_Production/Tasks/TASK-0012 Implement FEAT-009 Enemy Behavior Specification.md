---
type: task
id: TASK-0012
status: done
project: Igorogue
milestone: M-1
priority: critical
dependencies: []
updated: 2026-07-12
---
# TASK-0012 Implement FEAT-009 Enemy Behavior Specification

## Outcome

山賊棋士と侵入者について、行動予算、白石配置合法性、意図計画、強制上書き、対象再選択、決定論的着点評価、fallbackを一義的に定義する。

## Source of truth

- [[Rules Canon]]
- [[FEAT-009 Enemy Action Planning and Placement]]
- [[Enemy Design and Intent]]
- `game_data/content/enemies.json`
- [[FEAT-009 Enemy Decision Fixtures]]

## Non-goals

- Rules Kernelのランタイム実装
- コウ／盤面反復規則の決定
- 施設点意味論の再設計
- 商人ギルド、断ち切り僧、白眼亀の完全行動表
- 敵バランスの人間プレイ検証

## Acceptance criteria

- 通常行動1回、反攻追加1回、最大2回の行動予算が定義される。
- 白前線、接触、終着、侵入、施設踏破の合法性が定義される。
- 計画時点、意図UI、処刑・防衛上書き、同意図再選択、fallbackが定義される。
- 全候補評価の最終同率がCanonical point orderで一意に決まる。
- 山賊棋士と侵入者の完全行動表が`enemies.json`と一致する。
- 8件の決定表フィクスチャが機械検査を通る。
- 独立した2名がフィクスチャを紙上解決し、同一結果を出す。

## Allowed areas

- `docs/20_Design/Rules Canon.md`
- `docs/20_Design/Enemy Design and Intent.md`
- `docs/20_Design/Enemy Catalog.md`
- `docs/20_Design/Feature Specs/FEAT-007*`
- `docs/20_Design/Feature Specs/FEAT-009*`
- `docs/30_Technical/Command Event Model.md`
- `docs/30_Technical/Domain Model.md`
- `docs/30_Technical/Schemas/enemy.schema.json`
- `docs/50_Validation/Spec Fixtures/`
- `game_data/content/enemies.json`
- `game_data/fixtures/`
- `tools/check_enemy_behaviors.py`
- 進捗・索引・manifest文書

## Validation

```bash
python tools/check_enemy_behaviors.py
python tools/check_all.py
```

人間検証は[[FEAT-009 Enemy Decision Fixtures]]のHuman sign-offを2名分記録する。

## Execution log

### 2026-07-10

- FEAT-009を新設し、FEAT-007をsupersededへ変更。
- 敵共通パイプラインと配置モードを定義。
- 山賊棋士と侵入者のパラメーター、優先度、score profile、fallbackを定義。
- `enemies.json`をデータ駆動形式へ更新し、未仕様敵をplaceholderとして明示。
- 8件の紙上／機械決定フィクスチャを追加。
- 専用検査`check_enemy_behaviors.py`を追加。
- Rules Canon、Domain/Eventモデル、索引を同期。

### 2026-07-12

- Project ownerが、TASK-0012の独立二人human sign-offは実施済みという前提で先へ進むよう明示した。この指示を、二人が独立にfixtureを紙上解決して同一結果へ到達したことに対するowner attestationとして記録し、本TASKを`done`へ遷移した。
- raw worksheetとsign-off identityはrepositoryへ保存されていない。Codexによる再現や人物情報を補っておらず、証跡の粒度はowner attestationに限定する。

## Evidence

- `python tools/check_enemy_behaviors.py`: PASS。指定済み敵2体、決定フィクスチャ8件。
- `python tools/check_all.py`: PASS。文書、Wikilink、content、敵行動、抽象モデル既存テストを通過。
- 紙上レビュー2名: Project owner attestationにより完了、2026-07-12。raw worksheet／identityはrepository未保存。

## Known issues

- A-2は[[ADR-0011 Battle-Local Stone Topology Repetition Ban]]としてAccepted。製品Kernel実装時に敵候補フィルターへ同fixtureを移植する。
- 施設点意味論は[[ADR-0012 Facility Sites Are Empty Intersections]]でAcceptedされ、`white_facility_invasion`は合法確定時だけ施設を破壊する。
- human sign-offのraw worksheet／identityはrepositoryに保存されていない。完了根拠は2026-07-12のProject owner attestationであり、Codex evidenceへの置換ではない。
- FEAT-009のproduction enemy planner／executionはM2の後続TASKで実装する。本TASKの`done`は仕様とhuman paper gateの完了を表し、runtime実装済みを意味しない。
