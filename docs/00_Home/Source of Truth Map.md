---
type: governance
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Source of Truth Map

| 情報 | 正本 | 補足 |
|---|---|---|
| プレイヤー可視ルール | [[Rules Canon]] | 理由はADR/Feature Spec |
| Engine／言語／repository／CI境界 | [[ADR-0001 Engine and Repository]] | current machine values are `toolchain/engine_decision.json` |
| 座標・初期配置契約 | [[Coordinate System and Initial Position]] | 現在値は`system.json` |
| 余勢の範囲・生成・配置gate | [[FEAT-002 Momentum]] | 数値は`system.json`、地合い流追加ruleは`styles.json` |
| 反攻・熱・Pending・overflow | [[FEAT-003 Komi Counterattack and Heat]] | 数値は`system.json`、判断は[[ADR-0013 Baseline Pace and Burst-Driven Counterattack]] |
| 機能の詳細と判定順 | Feature Specs | UI、エッジケース、計測を含む |
| 現在のカード等の数値 | `game_data/` | Markdownへ重複転記しない |
| 設計意図と健全範囲 | Obsidian | 数値そのものではなく理由 |
| 実装 | ソースコード | Accepted仕様へ従う |
| 作業状態 | TASKノート | Backlogは候補一覧のみ |
| 決定履歴 | ADR | Acceptedを無断変更しない |
| バランス証拠 | Simulation/Playtest Report | content hashとseedを含む |
| 再現 | Replay log + seed | バグ修正の必須証拠 |
| 過去の会話内シミュレーション | Archived Proxy Reports | 正式な再現性はない |

## 矛盾時

1. 実装を停止する。
2. Decision Neededノートを作る。
3. Accepted仕様を無断で上書きしない。
4. 決定後、仕様・データ・テストを同じ変更単位で更新する。
