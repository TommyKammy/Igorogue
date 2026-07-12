---
type: task
id: TASK-0041
status: blocked
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0040]
updated: 2026-07-12
---
# TASK-0041 Validate M2 Core Duel Graybox

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
- technical passと「面白い」のE4 claimを分離し、未検証を明示する。
- blockerがあれば再現steps／seed／content hashと最小follow-upを記録する。

## Validation

- repository wrappers、Godot smoke／export、full-battle replay。
- conversational UAT、human visual review、independent evidence review、CI全job。

## Known issues

TASK-0040 human mergeまで`blocked`。
