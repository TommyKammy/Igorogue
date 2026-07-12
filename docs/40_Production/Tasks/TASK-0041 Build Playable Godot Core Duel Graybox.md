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
# TASK-0041 Build Playable Godot Core Duel Graybox

## Outcome

Godot 4.7 .NETで7×7盤、手札、気、Bandit intent、turn／result、card target、End Turn、battle restartを操作できる最小grayboxを作り、Application command／queryだけへ接続する。

## Source of truth

- [[UI UX Overview]]
- [[Battle Screen Specification]]
- [[Interaction and Input]]
- [[Coordinate System and Initial Position]]
- [[v0.1.1 Graybox Scope]]

Battle Screen、Interaction、graybox scopeは`proposed`のvisual／interaction referenceとしてのみ使う。本TASKはそれらをAccepted ruleへ昇格させず、player-visible rule conflictがあればDecision Neededで停止する。

## Non-goals

- final art／audio、run map／shop／meta、Momentum／Brilliant／full counterattack UI、Invader、broad accessibility polish。

## Allowed areas

- `game/Igorogue.Godot/`のC# presentation code。
- 本TASKが明示的に必要とする`.tscn`、`.tres`、`project.godot`入力設定。
- Godot／Application integration tests、本TASK／status文書。

## Acceptance criteria

- board orientationは左下(1,1)／右上(7,7)、石／領地／アタリ／capture可能点をquery projectionから描画する。
- hand／qi／turn／intent／primary・alternate points／battle resultを表示する。
- mouseでcard選択 → target hover → confirm、End Turn、restartを操作できる。Godotはstateを直接変更しない。
- scene／resource編集はGodot headless parse/build、bootstrap smoke、Windows exportを通す。
- human visual reviewでlayout、focus、pixel scaling、危険表示、coordinate orientationを確認する。

## Validation

- repository wrappers、Godot headless smoke、Windows debug export、scene parse。
- human visual review、independent fixed-HEAD review、CI全job。

## Known issues

TASK-0040 mergeまで`blocked`。visible scopeはgrayboxでありfinal presentationではない。
