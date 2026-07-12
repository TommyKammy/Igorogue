---
type: task
id: TASK-0042
status: blocked
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0041]
updated: 2026-07-12
---
# TASK-0042 Validate M2 Core Duel Graybox

## Outcome

M2 Core Duelをfresh startからBandit win／loss、restart、replayまで人間操作し、technical evidenceとUATを分離して`PASS`／`NOT PASSED`／`DECISION NEEDED`を記録する。

## Non-goals

- balance tuning、content追加、Gate 3実装、funを自動testだけで証明すること。

## Allowed areas

- M2 validation report、playtest report、TASK／status文書。
- UATで再現したbugのbounded follow-up TASK proposal。
- production修正は別TASKとする。

## Acceptance criteria

- fresh launchからcard play、target preview、End Turn、Bandit action、terminal、restartを完走する。
- accepted command replayが同一terminal resultへ到達する。
- coordinate orientation、アタリ／capture／territory／intent表示、mouse focusをhuman reviewする。
- [[Milestones and Exit Gates]]のM2各項目を、7×7 UI、DECISION-0006でresolvedしたstarter set／exact recipe、山賊棋士、intent／アタリ／capture／territory表示へ一行ずつ追跡するexit matrixを作る。
- resolved recipeへ含まれる各card content IDが実際にdraw／select可能であり、各効果を少なくとも一度human操作できることをseed／content hash付きで記録する。除外カードがある場合はDECISION-0006のscopeと一致させる。
- Banditに対するwin pathとloss pathをそれぞれ完走し、seed、content hash、terminal result、replay evidenceを記録する。
- technical passと「面白い」のE4 claimを分離し、未検証を明示する。
- blockerがあれば再現steps／seed／content hashと最小follow-upを記録する。

## Validation

- repository wrappers、Godot smoke／export、full-battle replay。
- conversational UAT、human visual review、independent evidence review、CI全job。

## Known issues

TASK-0041 human mergeまで`blocked`。
