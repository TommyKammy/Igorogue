---
type: decision-needed
id: DECISION-0006
status: open
blocking: [TASK-0038]
updated: 2026-07-12
---
# DECISION-0006 Resolve M2 Starter Deck and Facility Scope

## Why a decision is needed

M2 Core Duelのstarting contentを選ぶactive／proposed sourcesが、次の点を一義的にしていない。

- Accepted [[Milestones and Exit Gates]]はM2を「初期6カード」とするが、枚数か種類数かを明記しない。
- Accepted [[Deck and Card System]]は初期deckを12枚とする。
- proposed [[Initial Card Set]]はstarter 6種を`5 / 2 / 2 / 1 / 1 / 1`の12枚とし、`card_development`（開拓施設）を1枚含む。
- `game_data/content/cards.json`には同じstarter 6種があるが、starting deck multisetのmachine-readable正本はない。
- proposed [[v0.1.1 Graybox Scope]]は「基本12枚」と同時に施設を含めないとする。

starting recipe、開拓の採用、M2施設境界はプレイヤー可視のcontent scopeであり、実装都合では決めない。

## Options

1. **Recommended:** M2の「初期6カード」をstarter 6種類と解釈し、Initial Card Setの12枚recipe（`card_basic_stone: 5`、`card_extend: 2`、`card_contact: 2`、`card_reinforce: 1`、`card_development: 1`、`card_lure_stone: 1`）を採用する。`card_development`と既存facility kernelをM2の限定例外として許可し、Accepted [[Milestones and Exit Gates]]のM2／M3境界とproposed graybox scopeへ「開拓1種だけ」の例外を明記する。
2. 施設なしの12枚deckを維持する。`card_development`を除外し、既存の非施設starterから代替カードとexact multisetをownerが指定する。
3. M2を文字どおり6枚deckとし、starter 6種を各1枚採用する。Accepted Deck and Card Systemの初期12枚記述を明示的にsupersedeし、`card_development`を含むためOption 1と同じM2／M3 facility例外を明記する。

## Recommendation rationale

Option 1はAcceptedな12枚deck、既存の6 starter definitions、既実装facility kernel、proposed Initial Card Setのrecipeをそのまま利用し、新規カードや数値選定を増やさない。一方、Accepted MilestonesはfacilityをM3へ置き、proposed M2 grayboxも施設を除外するため、Option 1／3はM2のDevelopment 1種だけを許す限定例外を両sourceへ明記しなければならない。その更新を含めても、新規replacement cardを決めるOption 2より選択追加が少ない。

## Safe work before resolution

- starter候補6種と`enemy_bandit`をread-only typed Content projectionへ変換する。
- starting recipeを外部注入するgeneric deck／hand／qi kernelを実装する。
- `card_development`を除くcandidate card effectsと山賊棋士plannerを、default deck採用とは分離して実装する。

## Blocked work

- default M2 starting deck recipeの生成。
- `card_development`のproduction effect接続。
- playable Core Duelの`StartBattle` content selection。
- grayboxへ表示する最終starter deck／facility scopeの確定。

## Owner decision

Pending.
