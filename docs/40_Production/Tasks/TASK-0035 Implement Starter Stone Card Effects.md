---
type: task
id: TASK-0035
status: blocked
project: Igorogue
milestone: M2
priority: high
dependencies: [TASK-0034]
updated: 2026-07-12
---
# TASK-0035 Implement Starter Stone Card Effects

## Outcome

`card_extend`、`card_contact`、`card_lure_stone`をtyped ordered operationsとしてatomic PlayCardへ接続し、実呼吸点条件draw、敵アタリ条件qi、囮石予約draw／captured-stone triggerを既存Rules Kernelで解決する。

## Non-goals

- 補強／開拓、default deck recipe、Momentum、enemy planner、Godot。

## Allowed areas

- starter stone operationの限定Domain／Application integration。
- Domain／Application tests、本TASK／status文書。

## Acceptance criteria

- printed placement tags、stone kind、operation順をtyped content projectionから取得する。
- ノビは確定後の配置group実呼吸点、ツケは確定後の敵アタリ事実を共有kernelから評価する。
- 囮石はplayer windowの最低保証予約drawと、capture時のstone-instance source triggerを二重発火なく処理する。
- rejected commandは全resource／zone／trigger state exact no-op、terminal batchは後続利益を抑止する。
- same seed／state／commandsとinput enumeration reversalでcanonical state／factsが一致する。

## Validation

- repository wrappers、各card accepted／rejected／terminal／capture lifetime tests。
- independent fixed-HEAD review、CI全job。

## Known issues

TASK-0034 mergeまで`blocked`。
