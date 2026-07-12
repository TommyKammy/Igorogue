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

`game_data/`のstarter card候補6種、`enemy_bandit`、Core Duel system policy（base qi／base draw）を、content hashへbindされたimmutable／typed projectionとしてload／validateしてpure Domain definitionsへ変換し、後続Applicationへcontent ID別switchなしで渡せる境界を作る。

## Source of truth

- [[Architecture]]
- [[Engine Toolchain and Repository Layout]]
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

- `src/Igorogue.Content/`とContent → Domainのapproved project reference。
- content definitionに必要な最小`src/Igorogue.Domain/` value types。
- Content／Architecture tests。
- 本TASKとstatus文書。

## Acceptance criteria

- starter候補6種と山賊棋士のID、cost／type／target／placement tags／ordered operations、behavior parameters／priority／fallback、および`system.json`のbase qi／base drawをtyped immutable Domain definitionsへ欠落なく変換する。
- unknown／duplicate ID、unsupported operation shape、dangling reference、invalid stable orderをfail-closedで拒否する。
- canonical projectionとcontent hashが入力key順に依存せず、same snapshotから同一結果になる。
- ContentはAccepted architectureどおりpure Domain definitionsへ変換できるが、ルール結果を決めず、Application／Godot型へ依存しない。Domain definitionはfilesystem／JSON型へ依存しない。
- DECISION-0006のstarting recipe／facility採用を先取りしない。

## Validation

- `tools/dev/check`、`tools/dev/test`、`tools/dev/build`。
- malformed card／enemy／system policy negatives、input enumeration reversal、Content → Domain／Application非依存architecture boundary。
- independent fixed-HEAD review、CI全job。

## Known issues

TASK-0031 human mergeまで`blocked`。merge後に唯一の次production TASKとして`ready`へ遷移できる。
