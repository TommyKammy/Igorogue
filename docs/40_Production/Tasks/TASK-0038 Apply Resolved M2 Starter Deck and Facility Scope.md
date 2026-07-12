---
type: task
id: TASK-0038
status: blocked
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0037]
updated: 2026-07-12
---
# TASK-0038 Apply Resolved M2 Starter Deck and Facility Scope

## Outcome

[[DECISION-0006 Resolve M2 Starter Deck and Facility Scope]]のowner-selected optionをAccepted milestone／graybox scope、machine-readable starting recipe、typed Content projectionへ一貫して適用し、選択に`card_development`が含まれる場合だけ既存facility kernelへeffectを接続する。

## Source of truth

- [[Milestones and Exit Gates]]
- [[Deck and Card System]]
- [[Initial Card Set]]
- [[v0.1.1 Graybox Scope]]
- [[DECISION-0006 Resolve M2 Starter Deck and Facility Scope]]
- `game_data/content/cards.json`
- card／recipe schemas

## Non-goals

- ownerに代わるoption選択、新規replacement card／数値設計、facility engine拡張、headless battle／replay、Godot。

## Allowed areas

- DECISION-0006で明示されたAccepted／proposed scope docs。
- `game_data/`のstarting recipeと必要最小schema／generated content。
- `src/Igorogue.Content/`のrecipe loader／validator／Domain definition conversion。
- Option 1で必要な`card_development`限定Domain／Application integration。
- Content／Domain／Application／Architecture tests、本TASK／status文書。

## Acceptance criteria

- DECISION-0006がresolvedで、選択optionと必要なAccepted Milestone／Deck and Card System／proposed graybox scopeの更新が同一commitに含まれる。Option 3は初期12枚ruleを明示的にsupersedeする。
- exact card ID → count multisetをmachine-readable正本へ置き、合計枚数、unknown／duplicate／zero count、starter scopeをfail-closedで検証する。
- typed recipe／resolved starter scopeをcontent hashへbindし、JSON key順／input enumeration reversalでcanonical resultが一致する。
- resolved recipeに`card_development`を含む場合だけ、Accepted M2／M3 scopeへ限定例外を記録し、既存authorized facility build commandとcapacity／duplicate／territory checksへ接続して第二のfacility ruleを作らない。
- resolved recipeが`card_development`を除外する場合はDevelopmentをM2 reachable contentへ含めず、未選択facility behaviorをproduction実装しない。
- runtime valueをコードへ複製しない。Developmentを含むrecipeでのrejected commandはresource／zone／facility state exact no-opとする。

## Validation

- repository wrappers、recipe malformed／hash／enumeration-order tests。
- Developmentを含むrecipeではfacility build accepted／negative／ordering tests。含まないrecipeではDevelopment unreachable tests。
- independent fixed-HEAD review、CI全job。

## Known issues

TASK-0037 mergeとDECISION-0006 owner resolutionまで`blocked`。
