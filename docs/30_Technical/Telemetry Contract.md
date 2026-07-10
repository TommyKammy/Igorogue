---
type: telemetry
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Telemetry Contract

## 必須メタデータ

- build_id
- game_version
- content_hash
- rules_kernel_version
- seed
- policy_version（Botのみ）
- explosion_classifier_version

## ターン

- 気開始／終了
- ドロー枚数
- 使用カード
- 合法候補数
- 領地収入
- 施設数
- 余勢開始／終了、requested/applied/overflow、reason
- 余勢mode候補数、起点・中間・対象点、通常合法との重複
- 最前線距離before/after、解決後予測収入、前進ドロー可否・発火
- 捕獲グループ・石数
- 王石安全度
- 妙手倍率
- 反攻unit開始／終了、advance reason、delta、Pending生成・実行、overflow
- 熱crossing前後の妙手倍率、過伸展札数・cap、犠牲batch・remainder

## ラン

- 流儀、家元印、コミ
- 報酬提示・選択・スキップ
- 遺物取得・売却
- 初中規模成長
- 初真の爆発
- 直前3ターンの仕込みイベント
- 勝敗原因

個人情報を収集せず、オフラインでもローカルJSONへ出力できる設計にする。
