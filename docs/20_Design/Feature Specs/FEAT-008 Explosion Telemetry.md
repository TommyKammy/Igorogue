---
type: feature-spec
status: proposed
project: Igorogue
updated: 2026-07-10
---
# Explosion Telemetry

## Player promise

分析用の急加速分類を定義し、ゲームルールと分離する。

## Inputs

GameState、対象カード／敵意図、content data、順序付きコマンド。

## Resolution

詳細な判定順は[[Combat Resolution Order]]および[[Rules Canon]]に従う。

## UI

発動前の差分、対象、危険、資源変化を表示する。

## Telemetry

開始状態、コマンド、発行イベント、終了状態、seed、content hashを記録する。

## Acceptance criteria

- classifier_versionを保存。
- 絶対出力、直近比、盤面イベントを組み合わせる。
- 通常5枚使用だけで爆発扱いしない。

## Open questions

実装時に未確定があればDecision Neededへ移し、推測で確定しない。
