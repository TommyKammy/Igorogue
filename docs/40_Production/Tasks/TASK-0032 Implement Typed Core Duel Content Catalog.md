---
type: task
id: TASK-0032
status: blocked
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0031]
updated: 2026-07-12
---
# TASK-0032 Implement Typed Core Duel Content Catalog

## Outcome

`game_data/`のstarter card候補6種と`enemy_bandit`を、content hashへbindされたimmutable／typed Content projectionとしてload／validateし、後続Domain／Applicationへcontent ID別switchなしで渡せる境界を作る。

## Source of truth

- [[Architecture]]
- [[Deck and Card System]]
- [[FEAT-009 Enemy Action Planning and Placement]]
- `game_data/content/cards.json`
- `game_data/content/enemies.json`
- `game_data/balance/system.json`
- card／enemy schemas

## Non-goals

- starting deck recipeの選択、card effect解決、deck／hand state、enemy ranking、Application command、replay、Godot。
- `game_data/`値、schema意味、Accepted ruleの変更。

## Allowed areas

- `src/Igorogue.Content/`。
- Content／Architecture tests。
- 本TASKとstatus文書。

## Acceptance criteria

- starter候補6種と山賊棋士のID、cost／type／target／placement tags／ordered operations、behavior parameters／priority／fallbackをtyped immutable projectionへ欠落なく変換する。
- unknown／duplicate ID、unsupported operation shape、dangling reference、invalid stable orderをfail-closedで拒否する。
- canonical projectionとcontent hashが入力key順に依存せず、same snapshotから同一結果になる。
- Contentはルール結果を決めず、Domain／Application／Godot型へ依存しない。
- DECISION-0006のstarting recipe／facility採用を先取りしない。

## Validation

- `tools/dev/check`、`tools/dev/test`、`tools/dev/build`。
- malformed content negatives、input enumeration reversal、architecture boundary。
- independent fixed-HEAD review、CI全job。

## Known issues

TASK-0031 human mergeまで`blocked`。merge後に唯一の次production TASKとして`ready`へ遷移できる。
