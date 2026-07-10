---
type: feature-spec
status: proposed
project: Igorogue
updated: 2026-07-10
---
# Brilliant Move Multiplier

## Player promise

盤面上の複合成果を倍率へ変換する。

## Inputs

GameState、対象カード／敵意図、content data、順序付きコマンド。

## Resolution

詳細な判定順は[[Combat Resolution Order]]および[[Rules Canon]]に従う。

## UI

発動前の差分、対象、危険、資源変化を表示する。

## Telemetry

開始状態、コマンド、発行イベント、終了状態、seed、content hashを記録する。

## Acceptance criteria

- 同一敵グループを同一ターンに複数回アタリ加算しない。
- 領地完成と捕獲の複合倍率順を固定。
- 棋譜片へ影響しない。

## Open questions

実装時に未確定があればDecision Neededへ移し、推測で確定しない。
