---
type: task
id: TASK-0036
status: blocked
project: Igorogue
milestone: M2
priority: high
dependencies: [TASK-0035]
updated: 2026-07-12
---
# TASK-0036 Implement Starter Reinforce Effect

## Outcome

`card_reinforce`をatomic PlayCardへ接続し、friendly-group targeting、timed temporary liberty、conditional drawを共有kernelで解決する。

## Non-goals

- `card_development`のeffect／default deck採用、施設engine拡張、Momentum、enemy planner、Godot。

## Allowed areas

- reinforce technique operationの限定Domain／Application integration。
- Domain／Application tests、本TASK／status文書。

## Acceptance criteria

- 補強はtarget groupとstable stone anchorをcommand時stateへbindし、既存TLE lifecycleでenemy-turn-end expiryを設定する。
- targetがアタリだった場合のdrawをplayer windowで正しく処理し、stale／foreign targetをexact no-opで拒否する。
- canonical state／facts／command logが同一入力で一致する。

## Validation

- repository wrappers、target lifecycle、TLE expiry、atari conditional draw、stale／foreign target negative tests。
- independent fixed-HEAD review、CI全job。

## Known issues

TASK-0035 mergeまで`blocked`。
