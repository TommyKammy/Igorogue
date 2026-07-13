---
type: task
id: TASK-0035
status: review
project: Igorogue
milestone: M2
priority: high
dependencies: [TASK-0034]
updated: 2026-07-13
---
# TASK-0035 Implement Starter Stone Card Effects

## Source of truth

- [[Rules Canon]]
- [[Initial Card Set]]
- [[Combat Resolution Order]]
- [[Deck and Card System]]
- [[FEAT-005 Sacrifice Triggers]]
- typed Core Duel content catalog

## Outcome

`card_extend`、`card_contact`、`card_lure_stone`をtyped ordered operationsとしてatomic PlayCardへ接続し、実呼吸点条件draw、敵アタリ条件qi、囮石予約draw／captured-stone triggerを既存Rules Kernelで解決する。

## Non-goals

- 補強／開拓、default deck recipe、Momentum、enemy planner、Godot。

## Allowed areas

- starter stone operationの限定Domain／Application integration。
- Domain／Application tests、本TASK／status文書。

## Acceptance criteria

- printed placement tags、stone kind、operation順をtyped content projectionから取得する。
- ノビは確定後の配置group実呼吸点、ツケは確定後の敵アタリ事実を共有kernelから評価する。
- 囮石はplayer windowの最低保証予約drawと、capture時のstone-instance source triggerを二重発火なく処理する。
- rejected commandは全resource／zone／trigger state exact no-op、terminal batchは後続利益を抑止する。
- same seed／state／commandsとinput enumeration reversalでcanonical state／factsが一致する。

## Validation

- repository wrappers、各card accepted／rejected／terminal／capture lifetime tests。
- independent fixed-HEAD review、CI全job。

## Known issues

PlayCardからactual enemy turn／replayまでのfull compositionはTASK-0039が所有する。TASK-0036の補強とtemporary-liberty付与は本TASKへ含めない。

## Integration boundary

- TASK-0034のstandalone PlayCardを拡張し、既存authoritative runtimeをlegacy `BattleState v1`から分離したexact-bound sidecarとして再利用する。`HeadlessBattleSession`、replay codec、enemy plannerへは接続しない。
- 囮石はaccepted PlayCardでstable stone instanceとcaptured-stone triggerを登録し、同じ既存`CaptureBenefitTriggerPlan`／`ClosedWindowCaptureBenefitResolver`による後続capture lifetimeを二段階E3 testで証明する。actual enemy commandとのcompositionはTASK-0039へ維持する。
- ツケの条件は、配置で隣接呼吸点を失った後も生存する白groupが確定後snapshotで有効呼吸点1になった事実に限定する。無関係な既存アタリは参照しない。
- printed `terminal`は通常のFrontline／Contact隣接gateを迂回する明示的な代替modeであり、既存`PlacementAccessMode.TerminalCapture`が同じ手の即時敵captureを必須とする。占有、自殺手、反復は禁止のまま維持する。
- 合法性と王石gate確定後、非終局時だけPlaceStone後のtyped operation順でdraw／qi／reserved drawを適用する。terminal batchは後続利益を抑止する。

## Execution log

2026-07-13 — PR #24 human mergeとpost-merge main CI全3 job successによりdependency TASK-0034が`done`。Project ownerの継続指示を本TASK選択として記録し、fixed main `69d686a5268c127d5ea2c3d3a6b0508b7d56b83c`から専用worktree／branchを作成して`in_progress`へ遷移した。

2026-07-13 — Rules Canon、Initial Card Set、Combat Resolution Order、FEAT-005、typed content、TASK-0034／0036／0039境界と既存M1 runtime／capture kernelを監査。full Headless／replay統合を先取りせず、runtime sidecar、runtime-aware placement共有、typed follow-up operation、囮石二段階capture lifetime proofへ限定した。

2026-07-13 — `CoreDuelContentCatalog`のStone型からBasic／Extend／Contact／Lureをoperation shapeで投影するcanonical catalogを実装した。card content IDによる分岐を置かず、printed placement tag、stone kind、effect／on-captured順をtyped definitionへ固定し、unsupported shapeはfail closedとした。

2026-07-13 — M1のtemporary／continuous effective-liberty placement処理を共通runtime-aware pipelineへ抽出し、既存enemy actionとstandalone PlayCardの双方から利用した。PlayCard側はdetached exact-bound runtime sidecarを保持し、accepted placementだけがstone instance／used ID／resources／capture trigger planを一回更新する。Headless／replay schemaは変更していない。

2026-07-13 — Extendは確定後の配置group実呼吸点でdraw、Contactは今回の配置に隣接して呼吸点を失った生存白groupの確定後有効呼吸点1でqi、Lureは配置時reserved draw +1と同じstone-instance sourceの後続capture +2を既存closed-window resolverで処理する。terminal確定後は全follow-up／trigger登録を抑止した。

2026-07-13 — accepted／rejected／terminal／simultaneous capture／facility／territory回帰に加え、Extend閾値とdiscard reshuffle RNG、Contact両色隣接／影響group限定、Lure二段階capture lifetime／king suppression、catalog／runtime／board列挙反転をE3 testで固定した。precommit independent reviewの途中でtest-only RNG property誤記と1枚reshuffle非消費caseを修正し、再reviewはfindingなし、`APPROVE`。fixed-HEAD reviewはimplementation commit後に行う。

2026-07-13 — independent fixed-HEAD reviewがimplementation commit `2fe41322709b7e8f9cfe72932abc9e1dc53949b6`をbase `69d686a5268c127d5ea2c3d3a6b0508b7d56b83c`と比較。全Acceptance、accepted specs、runtime identity、exact no-op、terminal、determinism、scopeを再検証し、actionable findingなし、`APPROVE`。全wrapper greenのため本TASKを`review`へ遷移した。

2026-07-13 — PR #25 automated reviewのP2指摘2件を独立監査し、いずれも妥当と判定した。PlayCard捕獲時のcarrier removal factを既存enemy placementと同じ共有挿入順（最終`GroupCapturedFact`直後）で公開し、detached runtime sidecarを維持する6引数`BattleState.Start`から`initial.PlayerTurnIndex`を保持した。turn 7開始／accepted play保持とtimed＋continuous liberty相殺capture／removal fact順の回帰を追加した。

## Evidence

- PR #24 merge commit `69d686a5268c127d5ea2c3d3a6b0508b7d56b83c`／post-merge main CI run `29219574281`全3 job success。
- typed production snapshot projection — generated `CoreDuelContentCatalog`のStone型4件をBasic／Extend／Contact／Lureへexact投影し、content ID順／profile完全性を検証。
- runtime placement extraction — detached sidecar、used-ID no-op、timed effective-liberty、post-capture exact snapshots、列挙反転を直接検証。既存enemy state machine回帰を同じwrapper suiteで維持。
- card effect evidence — Extend threshold／draw／2枚discard reshuffle、Contact adjacency／affected surviving atari／terminal、Lure immediate +1／captured-source +2／king suppression、rejected exact no-op、accepted-only log／facts。
- `tools/dev/check` — exit 0。47 content IDs、content snapshot `sha256:cd53980e2edd69ad14b3815c800a3c5aab119f21d95d724d083afa2920c15ad6`。
- `tools/dev/test` — exit 0。.NET SDK 8.0.422、Domain 318、Application 139、Architecture 58、計515 tests、warning 0／error 0。
- `tools/dev/sim-smoke` — exit 0、`checksum=5f943a3cbc6847a14e841612c57d2d2cf4aef78d8b7441c0ff4d8b279113625c`。bootstrap determinism evidenceとしてのみ使用。
- `tools/dev/build` — exit 0、warning 0／error 0。`git diff --check` — exit 0。
- independent precommit review — test-only defects修正後actionable findingなし、`APPROVE`。
- implementation commit `2fe41322709b7e8f9cfe72932abc9e1dc53949b6` — typed starter-stone projection、shared runtime placement、Extend／Contact／Lure atomic effects、determinism／lifetime evidence。
- independent fixed-HEAD review — `2fe41322709b7e8f9cfe72932abc9e1dc53949b6`、base `69d686a5268c127d5ea2c3d3a6b0508b7d56b83c`、actionable findingなし、`APPROVE`。
- PR #25 review repair — carrier removal fact／snapshot turn indexのP2 2件を妥当と判定し、共有fact orderingとdetached startを維持した回帰2件で修正。
