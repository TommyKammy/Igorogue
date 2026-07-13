---
type: task
id: TASK-0038
status: review
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0037]
updated: 2026-07-13
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
- `game_data/content/starting_decks.json`
- `docs/30_Technical/Schemas/starting_deck.schema.json`

## Non-goals

- ownerに代わるoption選択、新規replacement card／数値設計、facility engine拡張、headless battle／replay、Godot。

## Allowed areas

- DECISION-0006で明示されたAccepted／proposed scope docs。
- `game_data/`のstarting recipeと必要最小schema／generated content。
- `src/Igorogue.Content/`のrecipe loader／validator／Domain definition conversion。
- resolved recipeに`card_development`を含む場合に必要な限定Domain／Application integration。
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

starting recipeから物理`BattleCardInstance`列を作るinstance ID規約とStartBattle／replay compositionは、本TASKのcounts projectionを入力としてTASK-0039で実装する。

## Execution log

2026-07-13 — PR #27 human mergeを確認。TASK-0037の最終source HEAD `1046cd47ede578c08f4d4ba2982c0d36e449411b`、main merge commit `e98ac90`、post-merge main CI run `29237842140`全3 job successによりdependency blockを解除した。

2026-07-13 — Project ownerが「DECISION-0006もOption 1で進めて」と明示。starter 6種類／12枚のexact recipe、`card_development` 1種限定のM2 facility例外、M3 broad facility scope維持をresolved正本としてTASKを`in_progress`へ遷移した。

2026-07-13 — Option 1の文書範囲をAccepted milestone、deck／card catalog、graybox scope、current statusへ同期。`tools/dev/check`はdocumentation、wikilink、content、全governance検査を含めexit 0。content snapshotは`sha256:aa26362f6c4b1cdc9c8dc9336654bd20fe5379f622eef3fa992257db62d86832`（8 files）。

2026-07-13 — pre-closeout文書reviewで、Accepted milestone／Current Sprintへのexact runtime count重複と、[[Initial Card Set]]の打石placement表示が`game_data/content/cards.json`の現行値を完全に示していない点を検出。exact multisetはmachine-readable正本への参照へ戻し、打石の表示を`frontline/terminal`へ同期した。runtime値、Accepted rule、card definitionは変更していない。

2026-07-13 — `starting_decks.json`／schema／generated snapshot、ordinal canonical typed recipe、Core Duel catalog projectionを実装。resolved 6 ID／12枚だけを選択し、missing／unknown／non-starter／duplicate／non-positive／total mismatch／extra propertyをfail-closedで検証した。JSON key／entry／definition enumeration reversalでも同じcanonical projectionになる。

2026-07-13 — content shapeから`StarterDevelopmentCardPlayDefinition`を構築し、Development playを既存`FacilityBuildEvaluator`へ接続。card cost／zone commitは合法なfacility evaluation後だけ行い、accepted fact順をQiChanged → FacilityBuilt → FacilityActivatedとした。territory／stone／occupied／capacity／type-limit／duplicate ID／placement-mode rejectionはcommand logを含むexact no-opとして検証した。

2026-07-13 — full pre-closeout suite成功。`tools/dev/build` warning 0／error 0、`tools/dev/test` Domain 355／Application 161／Architecture 76の計592、`tools/dev/check`、`tools/dev/sim-smoke`、`git diff --check`が全てexit 0。Content、Application／Domain、文書整合性の独立reviewは、文書2点の修正後に全て`APPROVE`。TASKを`review`へ遷移し、fixed-HEAD reviewとCIを待つ。

## Evidence

- PR #27 human merge／main merge commit `e98ac90`／post-merge main CI run `29237842140`全3 job success。
- Project ownerの2026-07-13の選択「DECISION-0006もOption 1で進めて」。
- `tools/dev/check` exit 0。documentation／wikilink／content／governance checks passed。content snapshot `sha256:aa26362f6c4b1cdc9c8dc9336654bd20fe5379f622eef3fa992257db62d86832`（8 files）。
- `tools/dev/build` exit 0、warning 0、error 0。`tools/dev/test` exit 0、Domain 355／Application 161／Architecture 76、計592 tests pass。
- `tools/dev/sim-smoke` exit 0。checksum `36ca153c20b82b2220c82b787c229d22f255fee7c42fed9c5ce7753ae0ff7bf1`、content hash `sha256:aa26362f6c4b1cdc9c8dc9336654bd20fe5379f622eef3fa992257db62d86832`、8 files。
- `git diff --check` exit 0。
- pre-closeout independent Content reviewはstarting recipe／schema／manifest binding／canonicalization／fail-closed projectionにfindingなしで`APPROVE`。非選択recipe bodyをtyped projectionしない挙動は既存Core Duel selective-loader boundaryと整合する。
- pre-closeout independent Application／Domain reviewはshared facility legality、exact binding、atomic commit、fact順、deterministic facility ID、rejection no-op、replay／Godot boundaryにfindingなしで`APPROVE`。
- pre-closeout independent documentation reviewのruntime値重複／catalog表示findingを修正し、再reviewはOption 1 scope、Development限定例外、source-of-truth、status同期にfindingなしで`APPROVE`。
