---
type: task
id: TASK-0029
status: blocked
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

## Evidence

- [[FEAT-011 Temporary Liberty Expiry Fixtures]] TLE-01〜15。
- [[TASK-0011 Replay Round Trip Verification]] v1 replay／golden integrity contract。

## Known issues

本TASKが完了しても、[[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]の二人human sign-offなしにGate 2へ進まない。formal simulator／Godot graybox／playable状態も後続missionである。
