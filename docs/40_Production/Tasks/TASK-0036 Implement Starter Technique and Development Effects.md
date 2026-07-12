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
# TASK-0036 Implement Starter Technique and Development Effects

## Outcome

`card_reinforce`と`card_development`のcandidate effectsをatomic PlayCardへ接続し、friendly-group targeting、timed temporary liberty、conditional draw、authorized facility buildを共有kernelで解決する。

## Non-goals

- card_developmentのdefault deck採用決定、施設engine拡張、Momentum、enemy planner、Godot。

## Allowed areas

- technique／territory starter operationの限定Domain／Application integration。
- Domain／Application tests、本TASK／status文書。

## Acceptance criteria

- 補強はtarget groupとstable stone anchorをcommand時stateへbindし、既存TLE lifecycleでenemy-turn-end expiryを設定する。
- targetがアタリだった場合のdrawをplayer windowで正しく処理し、stale／foreign targetをexact no-opで拒否する。
- 開拓は既存authorized facility build commandとcapacity／duplicate／territory checksを再利用し、第二のfacility ruleを作らない。
- candidate effect実装とdefault M2 recipe採用を分離し、DECISION-0006を先取りしない。
- canonical state／facts／command logが同一入力で一致する。

## Validation

- repository wrappers、target lifecycle、TLE expiry、facility build negative／ordering tests。
- independent fixed-HEAD review、CI全job。

## Known issues

TASK-0035 mergeまで`blocked`。
