---
type: feature-spec
status: proposed
project: Igorogue
updated: 2026-07-10
---
# Reward Weighting

## Player promise

ソフト誘導3択と救済を定義する。

## Inputs

GameState、対象カード／敵意図、content data、順序付きコマンド。

## Resolution

詳細な判定順は[[Combat Resolution Order]]および[[Rules Canon]]に従う。

## UI

発動前の差分、対象、危険、資源変化を表示する。

## Telemetry

開始状態、コマンド、発行イベント、終了状態、seed、content hashを記録する。

## Acceptance criteria

- 同一seedとcontent hashで同一候補。
- 救済は関連性のみ保証し、レア度を保証しない。
- スキップを常に許可。

## Open questions

実装時に未確定があればDecision Neededへ移し、推測で確定しない。
