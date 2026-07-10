---
type: validation-strategy
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Validation Strategy

## 三段階

1. 抽象代理モデル：高速な仮説選別。
2. 正式盤面シミュレーター：分布と外れ値。
3. 人間プレイテスト：理解、爽快感、納得感。

## 分離する問い

- 正しいか：自動テスト。
- 再現できるか：リプレイ。
- 健全な分布か：シミュレーション。
- 面白いか：プレイテスト。

## 比較方法

変更前後で同じCalibration seedを使い、最後に未使用Exploration seedで評価する。
