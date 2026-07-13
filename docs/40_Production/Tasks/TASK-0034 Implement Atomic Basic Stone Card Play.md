---
type: task
id: TASK-0034
status: in_progress
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0033]
updated: 2026-07-13
---
# TASK-0034 Implement Atomic Basic Stone Card Play

## Outcome

hand内の`card_basic_stone` instanceを指定して気コスト、target／mode、既存placement legality、zone移動を一つのApplication commandで原子的に解決し、PlayCard vertical proofを作る。

## Source of truth

- [[Rules Canon]]
- [[Combat Resolution Order]]
- [[Deck and Card System]]
- [[Command Event Model]]
- typed Core Duel content catalog

## Non-goals

- 他starter効果、default deck recipe、Momentum、enemy planner、preview、replay schema更新、Godot。

## Allowed areas

- pure Domain card-play seam。
- `src/Igorogue.Application/Battle/` command／state integration。
- Domain／Application／Architecture tests、本TASK／status文書。

## Acceptance criteria

- commandはcard instance ID、Canonical target、明示modeをbindし、hand外／stale／insufficient qi／target不正をexact no-opで拒否する。
- legality確定前にqi、card zone、RNG、board、historyを変更しない。
- accepted時だけqiを消費し、cardをhand → resolvingへ移して解決済みとしてturn-endまで保持し、既存authorized placement／capture／facility／territory／terminal pipelineを一度だけ通す。discardへの移動はAccepted Deck ruleどおりturn-end pipelineが行う。
- content ID別数値switchを置かず、typed operationを受ける。
- ordered facts、accepted-only command log、canonical checksumがsame inputで一致する。

## Validation

- `tools/dev/check`、`tools/dev/test`、`tools/dev/build`。
- accepted／rejected／stale／terminal／repetition／facility-point cases。
- independent fixed-HEAD review、CI全job。

## Integration boundary

- 本TASKはstandaloneな`CoreDuelCardPlayState`／session／commandを実装し、legacy `BattleState v1`を内部snapshotと既存placement authorityの再利用に限定して保持する。既存`HeadlessBattleSession`、replay codec／serializer、`BattleState v2` authoritative compositionへPlayCardを接続せず、full headless／replay compositionはTASK-0039が所有する。
- `PlaceStone(Basic)`だけのtyped operation shapeをcontent ID switchなしで受理する。追加operation、Lure、他placement tag semanticsは無視せずfail-closedとし、TASK-0035以降へ維持する。
- 本TASKのcapture範囲は既存placement／simultaneous capture／facility destruction／history／king gate／territory／facility reassociation factsまでとする。仕様が未確定のplayer-action window標準Soul／即時draw／qi／任意trigger orchestrationは暗黙実装せず、full authoritative compositionまで未接続として明記する。
- 既存authorized placement pathは同じ内部pipelineへ限定抽出して共有し、既存command surface／結果／replay schemaを変えない。

## Known issues

starter candidateの残りはTASK-0035／0036。player-action window capture benefitとfull headless／replay compositionは未接続であり、Rules Canon上の実装済みclaimを行わない。

## Execution log

2026-07-13 — PR #23 human mergeとpost-merge main CI全3 job successによりdependency TASK-0033が`done`。Project ownerの継続指示を本TASK選択として記録し、fixed main `9fe6c6323a24bed59d5ff731ad770afd68958868`から専用worktree／branchを作成して`in_progress`へ遷移した。

2026-07-13 — Accepted sourceとTASK-0039／0035境界を監査。PlayCard vertical proofをlegacy `BattleState v1` snapshotを再利用するstandalone composite stateとaccepted-only logに限定し、basic typed operation以外はfail-closed、既存Headless session／replay／v2 authoritative surfaceは未接続とした。既存player-action capture benefitの正式pipelineが存在しないため、本TASKではplacementからterritory／terminalまでを共有し、Soul／即時resource／任意triggerを暗黙決定しない境界を明記した。

2026-07-13 — content shapeから作るstate-bound `BasicStoneCardPlayDefinition`、card instance／Canonical target／explicit placement modeを持つ`PlayCardCommand`、composite state／session／resultを実装した。pre-authorization後に既存placement pathと共有する内部pipelineでlegalityを確定し、accepted時だけqi、hand → resolving → resolved、board／history／facility／territory／terminal、accepted-only logを一度にcommitする。rejected pathはsession／state／deck／qi／RNG／board／history／facility／logのexact reference no-opを維持する。

2026-07-13 — accepted、12 rejection境界、stale state／log、simultaneous capture、facility-point、territory／facility reassociation、repetition、terminal king capture／terminal後拒否、turn-end discard、content authority、same-input fact payload／state／log checksumをDomain／Application／Architecture testsで固定した。`tools/dev/check`、`tools/dev/build`、`tools/dev/test`、`tools/dev/sim-smoke`はすべてexit 0。

## Evidence

- PR #23 merge commit `9fe6c6323a24bed59d5ff731ad770afd68958868`／post-merge main CI run `29215583244`全3 job success。
- `tools/dev/check` — exit 0。documentation／wikilink／content／governanceとtool tests success。
- `tools/dev/build` — exit 0、warning 0、error 0、exact .NET SDK `8.0.422`。
- `tools/dev/test` — exit 0。Domain 314、Application 124、Architecture 58、合計496 tests、failure 0、skip 0。
- `tools/dev/sim-smoke` — exit 0。checksum `5f943a3cbc6847a14e841612c57d2d2cf4aef78d8b7441c0ff4d8b279113625c`、content hash `sha256:cd53980e2edd69ad14b3815c800a3c5aab119f21d95d724d083afa2920c15ad6`、7 files。
