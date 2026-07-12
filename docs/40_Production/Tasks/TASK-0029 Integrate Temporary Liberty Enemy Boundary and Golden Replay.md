---
type: task
id: TASK-0029
status: review
project: Igorogue
milestone: M1
priority: critical
dependencies: [TASK-0009, TASK-0010, TASK-0011, TASK-0023, TASK-0024, TASK-0028]
updated: 2026-07-12
---
# TASK-0029 Integrate Temporary Liberty Enemy Boundary and Golden Replay

## Outcome

TASK-0027／0028のDomain kernelをheadless Application battleへ接続し、scripted normal／counterattack action後のexpiry boundary、canonical state／replay、TLE-01〜15のversioned golden evidenceを完成させる。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]
- [[Golden Replay Index]]
- [[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]
- [[FEAT-011 Temporary Liberty Expiry Fixtures]]
- [[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]
- [[FEAT-003 Komi Counterattack and Heat]]
- `game_data/fixtures/temporary_liberty_expiry_fixtures.json`
- [[TASK-0010 Headless Battle State Machine]]
- [[TASK-0011 Replay Round Trip Verification]]

## Non-goals

- FEAT-009 enemy candidate ranking／actual intent execution。
- full FEAT-003、CTR-01〜25、heat／overextension。
- MOM-01〜19、Momentum／Brilliant runtime。
- card／deck／hand／qi-spend loop。
- UI／Godot、formal board simulator、playable／fun claim。
- v1 golden／replayの意味変更／上書き。
- canonical TLE fixture／Accepted ruleの変更。

## Allowed areas

- `src/Igorogue.Application/Battle/`
- `src/Igorogue.Application/Replay/`
- integrationに必要な限定Domain修正。
- `tests/Igorogue.Application.Tests/`、`tests/Igorogue.Architecture.Tests/`。
- 新規versionの`tests/golden/`とfixture adapter。
- 本TASK、Golden Replay Index、status／queue文書。
- `game_data/fixtures/temporary_liberty_expiry_fixtures.json`はread-onlyとする。

## Acceptance criteria

- versioned battle initial snapshotがboard、facility、stone runtime、temporary／continuous liberty、closed-window resources、trigger setup、counterattack boundaryをexact-bindする。mid-run test mutationを使わない。
- v1 golden／replayを上書きせず、TLE authoritative stateを新規canonical projectionとして追加する。既存schemaの意味変更が必要なら実装前に停止する。
- state checksumがstone identity、temporary effects、sweep marker、reserved draw／qi、Soul／DeferredChoice／first-use flags、minimal counterattack boundary fieldsをstable orderで含む。
- player turn終了時に当該enemy turnのPendingをsnapshotする。normal action後、snapshotがtrueならscripted authorized placement／passをちょうど1回追加し、それまでtemporary libertyをexpireしない。
- 最終enemy action後のTLE処理順はPending consume／overflow reprime → expiry sweep → king gate → nonterminal benefit → territory／facility → natural counterattack gain → planning boundaryとする。既存TASK-0010／`BattleEndReasonRules`のturn-limit terminal契約とprecedenceは意味変更せず、Accepted順と両立しない場合は実装前に停止する。
- normal／bonus actionまたはexpiryでterminalになったら、残りのaction／benefit／natural gainを抑止する。turn-limitを含む既存terminal precedenceを回帰維持する。
- actual plannerは未実装のため`EnemyIntentPlanned`を偽装発行せず、typed boundary-stage traceでTLE-14のphase orderをexact検証する。
- TLE-13はdue effect 0でsweep eventを0件とする。
- TLE-01〜15の全caseがproduction Domain evidenceを持ち、Application goldenはcanonical initial snapshot／正規commandだけで到達する。
- source fixture SHA、seed、ordered commands／facts、各boundary state／log checksum、terminal resultを固定し、同一run 2回で一致する。
- 新version replayのsave／load／runner round trip、tamper／metadata／checksum rejectionを追加し、既存v1 golden／replayを回帰維持する。
- TLE goldenはMomentum／Brilliant events 0件、CTR-01〜25 coverage claim 0件と明示する。
- `tools/dev/sim-smoke`をformal board simulation evidenceとして扱わない。

## Validation

- TLE-01〜15全fixture、同一run 2回、initial setup列挙順反転を検証する。
- normal／bonus pass／placement、terminal precedence、turn 20、stale command exact no-opをテストする。
- v1 regression、新version replay round trip、tamper negativesを実行する。
- `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を各2回実行する。
- `git diff --check`、docs／golden／schema diff、root `CODE_REVIEW.md`に従う独立fixed-HEAD review、CI全jobを確認する。

## Stop conditions

- canonical fixtureをproduction initial snapshot／commandで表現できず、direct Domain state mutationが必要。
- v1 schemaを意味変更または破棄する必要がある。
- actual enemy planner、full CTR、Momentumを実装しないとTLE evidenceを作れない。
- Accepted event orderと既存turn-limit contractが矛盾する。
- player-visible rule変更が必要。

## Execution log

2026-07-12 — DECISION-0005 Option 1のM1 TLE workstream第3段として[[TASK-0026 Resolve M1 Momentum Counterattack Migration Boundary]]が定義。[[TASK-0028 Implement Closed-Window Capture Benefits and TLE Boundary Pressure]]のhuman mergeまで`blocked`。

2026-07-12 — PR #18を人間merge。merged head `fb53bc3f644f8d7a6498c46b12db56da33ed07c3`、merge commit／fixed `origin/main` `ddccd57db12219847646d0b2de85c18b2c94b120`を確認し、唯一の未完dependencyが閉じたため本TASKを`ready`へ遷移した。

2026-07-12 — PR #18 post-merge main CI run `29187053532`のGovernance `86634981261`、Pure .NET `86635001823`、Godot／Windows export `86635049311`がすべてsuccessであることを確認した。Outcome、Non-goals、Acceptanceを再確認し、v1 state／replayを変更せずv2 authoritative initial snapshot、normal／bonus enemy substage、expiry boundary、new-version replay／goldenを並設する方針で本TASKを`in_progress`へ遷移した。baseline `tools/dev/check`と350 testsはexit 0。

2026-07-12 — board／facility／stone identity／temporary・continuous liberty／history／closed-window resource／conditional trigger plan／counterattack policy・stateをexact-bindするauthoritative initial snapshotと`headless-battle-state-v2`を追加した。foreign runtime／facility／history、dangling captured-stone・facility source、未宣言first-use flag、policy不整合、overdue effectをconstruction boundaryで拒否し、入力列挙反転で同じcanonical checksumになることを固定した。

2026-07-12 — player turn終了時のPending snapshot、normal action、任意のbonus action、consume／overflow reprime、expiry、terminal gate、benefit、territory／facility、natural gain、planning trace、turn-limitのApplication順を実装した。actual plannerを偽装せずtyped stage factだけを発行し、pass／runtime placement、turn 20、stale exact no-op、normal／bonus／expiry terminal precedenceを回帰した。

2026-07-12 — 独立rules preflightの指摘を受け、normal／bonus placement captureも共通closed-window`CaptureBatch`へ正規化した。初期trigger setupをbatch-independent conditional planとし、captured source、captured white group、non-king black、any captureをbatchごとにmaterializeする。terminal placementはtrigger選択前に`BatchStarted → CaptureBenefitSuppressed → BatchResolved`を発行し、normalからbonus／expiryへfirst-useとsacrifice stateを一度だけ継承する。

2026-07-12 — mandatory expiryの黒領地差分をtyped `TemporaryLibertyExpiry` source、reason `temporary_liberty_expired`、`ImplicitMomentumEligible=false`で発行し、benefit後／facility reassociation前へ配置した。TLE-12は(4,4) black／size 1／basic income 1とMomentum／Brilliant event 0をApplication state／factで固定した。

2026-07-12 — replay schema 2を新設し、authoritative state projection、metadata、attempt chain、terminal、16 MiB／4096 attempts、JSON duplicate／unknown／trailing／depth制限をfail-closedで検証した。全TLE caseをsave／load／runner 2回でbytes、state、log、factsまで照合する。独立security preflightの指摘を受け、schema 1のvalid bytes／legacy behaviorを変えずv2 projectionを逆方向でも拒否し、v2でunsupported facility-build rejected attemptも完全round tripするようにした。再確認はfindingなし、`APPROVED`。

2026-07-12 — `tests/golden/v2/temporary_liberty_cases.json`へTLE-01〜15を生成した。source expectedをinitial inputへ使わず、source fixture／runtime content SHA、seed 42、canonical initial、全command boundary、ordered facts、terminalを固定した。catalog SHA-256 `9f6486d9776ec05a0c6972f6fdb1ab6dfc49cdd5c653b05831a83216dea8d180`、source fixture SHA-256 `9f9a74ee9e1407c2b0882b6ccd1aa86ae950dd750fb0bfb4bc3bf12faae20e60`。v1 golden／`game_data/`は未変更。

2026-07-12 — fixed-HEAD reviewで`arm_next_capture`がfuture captureにも残る点と、captured stone instance IDを後から再利用できる点を検出した。armed captureへ宣言済みfirst-use flagを付けて最初のeligible batchで消費し、2 enemy boundaryで再発火しないことを固定した。authoritative runtimeへbattle-lifetime used stone ID registryをcanonical化し、live／capturedを問わずID再利用を`stone_instance_already_used` exact no-opで拒否する。

2026-07-12 — 同reviewで通常capture Soulの戦闘上限とstatic source lifetimeを追加確認した。standard accounting専用typed operationへSoul単位／group数／injected limitをbindし、resource stateへclaimed countをcanonical化した。上限到達後もcaptured-stone追加Soulは発火する。CapturedStoneSelfの誤条件、uncapped standard Soul、foreign standard operationをconstruction時に拒否し、facility triggerはcurrent activeかつmandatory placement後も同一instanceがsurviveする場合だけ選択する。

2026-07-12 — fixed-HEAD reviewでstandard accountingを異なるsource IDから複数投入できる点と、公開resolverへbatch-derived white group数を偽装できる点を検出した。conditional plan／resolverの双方でstandard sourceを一意化し、resolverではstandard reward operationを最大1件、申告group数をactual black-captures-white group数とexact照合して、state mutation前に拒否する負例を追加した。

2026-07-12 — closeout候補に対して`tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を各2回連続実行し、全6 commandがexit 0。両runでcontent snapshot、418 tests、simulation checksumが一致し、`tools/dev/build`と`git diff --check`もexit 0。

2026-07-12 — independent fixed-HEAD reviewがimplementation commit `5984d373e70f91a5d9bed23a5b703cb068713ed1`をbase `ddccd57db12219847646d0b2de85c18b2c94b120`と比較。正本、全Acceptance、runtime／replay／golden境界、trigger lifetime、standard accounting、negative testsを照合し、actionable findingなし、`APPROVED`。repository wrapperもgreenのため本TASKを`review`へ遷移した。

## Evidence

- [[FEAT-011 Temporary Liberty Expiry Fixtures]] TLE-01〜15。
- [[TASK-0011 Replay Round Trip Verification]] v1 replay／golden integrity contract。
- PR #18 merge commit `ddccd57db12219847646d0b2de85c18b2c94b120`／post-merge main CI run `29187053532`全3 job success。
- implementation commits — `70c5e589e073146517bd95a7e4cca2af0b64ba7a`、`5921a4fc305e71297355d8cb39f3db67b3c664bc`、`ca720cf0aadaf144b660a5a5a20fe6604b54ceb6`、`5984d373e70f91a5d9bed23a5b703cb068713ed1`。
- authoritative boundary evidence — exact initial snapshot、normal／bonus pass・placement、conditional capture benefits、terminal／turn-limit precedence、TLE-13 no Domain sweep events、TLE-14 exact typed stage trace。
- replay v2 evidence — schema／projection専用integrity、tamper・resource limits・cross-version rejection、unsupported facility attempt exact no-op、TLE-01〜15の二重round trip。
- golden v2 — `tests/golden/v2/temporary_liberty_cases.json`、catalog SHA-256 `9f6486d9776ec05a0c6972f6fdb1ab6dfc49cdd5c653b05831a83216dea8d180`、source fixture SHA-256 `9f9a74ee9e1407c2b0882b6ccd1aa86ae950dd750fb0bfb4bc3bf12faae20e60`、Momentum／Brilliant／CTR coverage claim各0。
- rules preflight — placement capture pipeline、expiry conditional trigger、facility command surface、territory source、initial exact-bindingの指摘を修正。security preflight — inverse projection guard／rejected facility attempt保持を修正後、findingなし`APPROVED`。
- `tools/dev/check` ×2 — 連続exit 0。47 content IDs、content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`が両runで一致。
- `tools/dev/test` ×2 — 連続exit 0。.NET SDK 8.0.422、Domain 293、Application 105、Architecture 20、計418 tests、warning 0／error 0が両runで一致。
- `tools/dev/sim-smoke` ×2 — 連続exit 0。`checksum=3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`が両runで一致。bootstrap determinism evidenceとしてのみ使用。
- `tools/dev/build` — exit 0、warning 0／error 0。`git diff --check` — exit 0。
- independent implementation fixed-HEAD review — `5984d373e70f91a5d9bed23a5b703cb068713ed1`、base `ddccd57db12219847646d0b2de85c18b2c94b120`、actionable findingなし、`APPROVED`。

## Known issues

本TASKが完了しても、[[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]の二人human sign-offなしにGate 2へ進まない。formal simulator／Godot graybox／playable状態も後続missionである。
