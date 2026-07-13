---
type: task
id: TASK-0039
status: done
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0038]
updated: 2026-07-13
---
# TASK-0039 Integrate Headless Core Duel and Replay

## Outcome

resolved M2 starter recipe、deck／hand／qi、starter card play、Bandit plannerを一つのauthoritative headless battleへ接続し、StartBattleからwin／loss／restartまでversioned replay／goldenで再現する。

## Non-goals

- Godot UI、preview、Momentum／Brilliant／full counterattack、Invader、reward／shop／meta、fun claim。

## Allowed areas

- Application battle／replay。Application → Content project referenceは追加しない。
- Application／Domain／Contentを参照できるheadless host composition rootと限定integration adapter。
- integrationに必要な限定Domain canonical state。
- Application／Content／Architecture tests、新規golden、本TASK／status文書。
- TASK-0038が生成したresolved recipe／Domain definitions。

## Acceptance criteria

- DECISION-0006がresolvedであり、TASK-0038のmachine-readable starting recipe／starter scopeをcontent hashへbindする。
- StartBattleがseed、content snapshot、initial position、deck recipe、Bandit stateをexact-bindする。
- player turn start／PlayCard列／EndTurn／Bandit action／terminal／restartをApplication commandだけで完走する。
- battle startは初回player turn前に通常intentを生成し、enemy turn終了後に次回intentを生成する。player turn中のdisplayed `intent_id`固定とturn-end mandatory override previewをE3 integration testで確認する。
- replayの新state projection／schemaを旧v1／v2と並設し、cross-version／tamper／resource-limitをfail-closedで扱う。
- fixed scripted Core Duelが同一run 2回でbytes、state、facts、log checksum、terminalまで一致する。
- formal simulator／playable claimを行わない。

## Validation

- repository wrappers各2回、headless full-battle golden／replay round trip、v1／v2 regression。
- independent fixed-HEAD review、CI全job。

## Known issues

- 現行M2 catalogは未解決`DeferredPlayerChoice`を生成しない。将来contentがenemy boundaryで未解決choiceを生成した場合、aggregateはplayer turn開始時に自動選択せずfail-closedにするため、そのcontent導入前に正式choice command／log integrationが必要。

## Execution log

2026-07-13 — PR #28のhuman mergeを確認。TASK-0038のmain merge commit `6f84adcbc0b1deb70944e82648009eb53e1429a4`とpost-merge main CI run `29247035946`全3 job successによりdependency blockを解除し、TASKを`in_progress`へ遷移した。

2026-07-13 — generated `CoreDuelContentCatalog`、authoritative initial snapshot、replay metadataからimmutable bootstrapを構築し、resolved 6-type／12-card recipeをstable physical card IDへ展開した。deck shuffle後かつ初回player turn開始前にBandit normal intentを生成し、board、runtime、deck／hand／qi、RNG、resources、normal／bonus plans、restart countを`headless-core-duel-state-v1`へexact-bindした。

2026-07-13 — `CoreDuelBattleStateMachine`を実装し、PlayCard → EndPlayerTurn → Bandit action → next player turn／terminal → terminal-only restartをApplication commandだけで接続した。短命な既存card／Bandit projectionの内部logは破棄し、外側`CoreDuelBattleSession`だけがaccepted-only command logを所有する。player window中のdisplayed intent固定、query-only mandatory override preview、`mandatory_lethal_override` execution、rejected commandのstate／RNG／log exact no-opをintegration testで固定した。

2026-07-13 — replay schema 3／`headless-core-duel-state-v1`をschema 1／2と並設した。高位PlayCard／EndPlayerTurn／ResolveBanditEnemyAction／RestartBattleだけをcodecへ許可し、schema／projection混入、payload／integrity／semantic tamper、unknown low-level command、duplicate／unknown／trailing／depth、16 MiB、4096 attemptsをfail-closedで検証した。serializer Saveはoversize時にdestinationを変更しない。

2026-07-13 — win、turn-limit loss、restartを同一script 2回とfresh replay sessionで比較し、replay bytes、canonical state、ordered facts、accepted-only log、terminalが一致した。golden `tests/golden/v3/core_duel_turn_limit_loss.json`はseed `39039`、content hash `sha256:aa26362f6c4b1cdc9c8dc9336654bd20fe5379f622eef3fa992257db62d86832`、artifact SHA-256 `148d445842ba1dac19a4eca504c8fbf5ca835448726303160abe95f9a4c0ac08`を固定する。

2026-07-13 — pre-review validationを2巡実行。exact .NET SDK 8.0.422／Godot 4.7 stable .NETを確認し、locked restore、build、607 tests、governance、sim smoke、Godot headless smoke、Windows debug export、`git diff --check`が各2回exit 0。buildはwarning 0／error 0、testsはDomain 355／Application 175／Architecture 77。Godot scene／resource／project／export presetは変更していない。

2026-07-13 — fixed source HEAD `eb23f3d4a955e190e55d958286d7cd19bdeb1c3e`をbase `6f84adcbc0b1deb70944e82648009eb53e1429a4`と比較。独立Battle／Application、replay／security、documentation／golden／architecture reviewはいずれもfindingなしで`APPROVE`。reviewer validationでは607 tests、governance、relevant smoke、golden SHA、clean worktreeを分担確認し、TASKをCI／human merge待ちの`review`へ遷移した。

2026-07-13 — PR #29のhuman mergeを確認。source HEAD `afe3bc1ce64f4d4ebd240147053552ac1f848cae`はmain merge commit `60d8cc5958e38768f4077ee2f4d686526d5b25fe`へ取り込まれ、post-merge main CI run `29252298693`は全3 job success。dependency consumerであるTASK-0040を開始可能としてTASKを`done`へ遷移した。

## Evidence

- PR #28 human merge／main merge commit `6f84adcbc0b1deb70944e82648009eb53e1429a4`／post-merge main CI run `29247035946`全3 job success。
- `CoreDuelBattleStateMachineTests` — generated catalog／12 physical card ID、seed／content／initial bind、PlayCard → EndTurn → Bandit → next turn、displayed intent固定、mandatory lethal override、terminal／restart、2-run determinism。
- `BattleReplayV3RoundTripTests` — schema 3 round trip、v1／v2 regression、cross-version／tamper／semantic drift／low-level command rejection、16 MiB／4096-attempt limits、atomic Save、rejected restart exact no-op。
- `CoreDuelBattleGoldenTests`／`tests/golden/v3/core_duel_turn_limit_loss.json` — terminal card victory、turn-limit loss、restart前後をproduction schema 3で再生。golden SHA-256 `148d445842ba1dac19a4eca504c8fbf5ca835448726303160abe95f9a4c0ac08`。
- repository wrapper 2巡: `tools/dev/verify-tools`、`tools/dev/restore`、`tools/dev/build`、`tools/dev/test`、`tools/dev/check`、`tools/dev/sim-smoke`、`tools/dev/godot-smoke`、`tools/dev/export-windows`は全てexit 0。`git diff --check`も2回exit 0。
- exact toolchain: .NET SDK `8.0.422`、Godot `4.7.stable.mono.official.5b4e0cb0f`。build warning 0／error 0。Domain 355／Application 175／Architecture 77、計607 tests pass。
- content snapshot `sha256:aa26362f6c4b1cdc9c8dc9336654bd20fe5379f622eef3fa992257db62d86832`（8 files）。sim／Godot smoke checksum `36ca153c20b82b2220c82b787c229d22f255fee7c42fed9c5ce7753ae0ff7bf1`。Windows debug export SHA-256 `68e6955b3d399ec1c181e13e975e7226c97645638ab3786c9ee5b5b2d567a656`。
- Application → Content project reference、Godot型のDomain／Application導入、Godot scene／resource／project／export preset変更はいずれもなし。
- fixed source HEAD `eb23f3d4a955e190e55d958286d7cd19bdeb1c3e`／base `6f84adcbc0b1deb70944e82648009eb53e1429a4`。独立Battle／Application、replay／security、documentation／golden／architecture reviewsはいずれもfindingなしで`APPROVE`。
- PR #29 source HEAD `afe3bc1ce64f4d4ebd240147053552ac1f848cae`／main merge commit `60d8cc5958e38768f4077ee2f4d686526d5b25fe`／post-merge main CI run `29252298693`全3 job success。
