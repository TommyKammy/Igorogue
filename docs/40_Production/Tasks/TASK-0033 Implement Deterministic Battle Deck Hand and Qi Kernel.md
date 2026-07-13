---
type: task
id: TASK-0033
status: review
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0032]
updated: 2026-07-13
---
# TASK-0033 Implement Deterministic Battle Deck Hand and Qi Kernel

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Deck and Card System]]
- [[Command Event Model]]
- [[DECISION-0006 Resolve M2 Starter Deck and Facility Scope]]
- [[TASK-0039 Integrate Headless Core Duel and Replay]]

## Outcome

外部注入されたcard instance multisetを使い、draw pile／hand／resolving／discard／exhaust、gameplay RNG shuffle／reshuffle、turn-start qi／draw、turn-end discardをpure deterministic Domain／Application stateとして実装する。

## Non-goals

- default starting recipe、card effect／target解決、PlayCard command、enemy planner、replay schema変更、Godot。
- DECISION-0006の選択、runtime値のコード直書き。

## Allowed areas

- `src/Igorogue.Domain/`のcard／turn state。
- integrationに必要な限定Application state。
- Domain／Application／Architecture tests、本TASK／status文書。

## Acceptance criteria

- card instance IDとcontent ID、全zone順、重複禁止、zone間排他をimmutable／canonicalに保持する。
- named `gameplay` RNGでbattle-start shuffleとdiscard reshuffleを行い、seed／消費順／no-op時非消費を固定する。
- draw不足時のdiscard reshuffle、空deck、resolving完了、exhaustをstable orderで処理する。使用済みcardは解決後もturn-endまでresolved stateとして保持し、turn endで残りhandとともにdiscardへ移す。
- 既存turn-start pipelineのturn-scoped flag reset → DeferredPlayerChoice → 領地再計算を保持し、その後にbase qi＋territory income＋reserved qi → base draw＋reserved drawの順で適用して予約値をexact resetする。
- base qi／drawはinjected policyを使い、`game_data`値をDomainへ直書きしない。
- state checksumがRNG、全zone、qi、reserved resourcesを含み、same seed／recipe／commandsで一致する。

## Validation

- `tools/dev/check`、`tools/dev/test`、`tools/dev/build`。
- shuffle／reshuffle golden sequence、input reversal、overflow／empty／duplicate negatives。
- independent fixed-HEAD review、CI全job。

## Integration boundary

- 本TASKはstandaloneなpure Domain／limited Application kernelを実装し、既存`headless-battle-state-v2`／replayへは接続しない。full headless compositionと新replay projectionはTASK-0039が所有する。
- `DeferredPlayerChoice`は選択結果を持たないため自動選択しない。turn-start kernelはcreated sequence順のchoice IDと、外部で承認済みのexact outcomeを照合する。outcome欠落／不一致ではterritory／qi／draw／RNGへ進まずexact no-opとする。
- 既存`ClosedWindowResourceState.FirstUseFlags`はbattle-scoped capture gateでありresetしない。turn-scoped resetは本TASKで導入するcard-turn stateだけを対象にする。

## Known issues

starting recipeは注入値でありDECISION-0006を先取りしない。正式なDeferred choice command／logとBattleState／replay統合はTASK-0039まで未接続。

## Execution log

2026-07-13 — PR #22 human mergeとpost-merge main CI全3 job successによりdependency TASK-0032が`done`。Project ownerの継続指示を本TASK選択として記録し、fixed main `0b4b8f5c1558e98051c758002269fca1994d5ca9`から専用worktree／branchを作成して`in_progress`へ遷移した。

2026-07-13 — Accepted sourceと既存state seamを監査。DECISION-0006を先取りしない外部注入recipe、named gameplay RNG、standalone card-turn checksumを採用し、既存BattleState v2／replayのcompositionはTASK-0039へ維持した。結果を持たないDeferredPlayerChoiceは自動選択せず、承認済みoutcome注入boundaryとして明記した。

2026-07-13 — immutable ordered draw／hand／resolving／discard／exhaust state、instance排他、canonical recipe shuffle、discard reshuffle、resolved保持、turn-end discard、exhaustをDomainへ実装した。Applicationにはinjected system policyとclosed-window resourcesを使うstandalone card-turn stateを追加し、turn flag reset → authorized deferred outcome → territory → qi／reserved reset → draw／reserved resetをimmutable snapshot順に固定した。

2026-07-13 — pre-commit independent reviewで、初版の計算順と公開stage traceが構造上一致しないMEDIUM findingを検出。outcomeをreserved resource snapshotへ反映してからterritoryを再計算し、qiとdrawの各適用直後に対応予約値を0へする構造へ修正した。再reviewは残存findingなしで`APPROVE`。

2026-07-13 — exact .NET SDK 8.0.422でgovernance／build／479 automated tests／simulation smokeを完了。Domain 308、Application 115、Architecture 56は全てsuccess、warning 0。Godot scene／resource／project／export設定は変更していない。

2026-07-13 — independent reviewerが実装commit `7d1a49d51fda4560d3d5c425be2ec4c972f69099`をclean worktreeのfixed HEADとして再監査。全wrapperを再実行し、残存findingなしで`APPROVE`。本TASKを`review`へ遷移した。

## Evidence

- PR #22 merge commit `0b4b8f5c1558e98051c758002269fca1994d5ca9`／post-merge main CI run `29214123093`全3 job success。
- `tools/dev/check` — documentation／content／deterministic fixture／repository governance全check success。
- `tools/dev/test` — Domain 308、Application 115、Architecture 56、合計479 tests success、warning 0。
- `tools/dev/build` — 8 projects build success、warning 0／error 0。
- `tools/dev/sim-smoke` — `IGOROGUE_SIM_SMOKE checksum=5f943a3cbc6847a14e841612c57d2d2cf4aef78d8b7441c0ff4d8b279113625c`、content `sha256:cd53980e2edd69ad14b3815c800a3c5aab119f21d95d724d083afa2920c15ad6`。
- Pre-commit independent review — pipeline順序のMEDIUM findingを修正後、残存findingなしで`APPROVE`。
- Fixed-HEAD independent review — `7d1a49d51fda4560d3d5c425be2ec4c972f69099`、clean worktree、全validation再実行、残存findingなしで`APPROVE`。
