---
type: task
id: TASK-0028
status: blocked
project: Igorogue
milestone: M1
priority: critical
dependencies: [TASK-0027]
updated: 2026-07-12
---
# TASK-0028 Implement Closed-Window Capture Benefits and TLE Boundary Pressure

## Outcome

共通`CaptureBatch`に対するdata-driven capture-trigger pipeline、closed-window resource state、TLEに必要な最小counterattack boundary primitiveをpure Domainへ追加し、TLE-09／10／15をproduction Rules KernelのE3 exact unit evidenceへ移植する。

## Source of truth

- [[Rules Canon]]
- [[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]
- [[FEAT-011 Temporary Liberty Expiry Fixtures]]
- [[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]
- [[FEAT-005 Sacrifice Triggers]]
- [[FEAT-003 Komi Counterattack and Heat]]
- [[ADR-0013 Baseline Pace and Burst-Driven Counterattack]]
- `game_data/fixtures/temporary_liberty_expiry_fixtures.json`

## Non-goals

- full FEAT-003／CTR-01〜25 migration。
- heat、attack overextension、battle-start komi reset、実反攻enemy action／intent planning。
- full card／deck／equipment runtime、board-mutating capture trigger。
- Momentum／Brilliant runtime、MOM-01〜19。
- facility／enemy passiveの具体的content implementation。
- Application battle boundary、golden replay、UI／Godot。
- `game_data/`数値／fixture expected value、Accepted ruleの変更。

## Allowed areas

- `src/Igorogue.Domain/Combat/`
- `src/Igorogue.Domain/`下の新規closed-window resource／counterattack-boundary領域。
- `tests/Igorogue.Domain.Tests/`
- Architecture testsの層／M3境界回帰。
- 本TASKとstatus／queue文書。
- `game_data/`はpolicy／fixtureのread-only照合だけとする。

## Acceptance criteria

- production `CaptureBatch`はcapture reason、boundary、capturing window、group／stone stable order、capturing color、king involvementをcanonicalに保持する。
- trigger入力はpre-resolved typed effect operationsとし、resolverにcontent ID別数値／test-only switchを直書きしない。
- trigger順はstandard accounting → source／armed effect → captured stone self → style／seal → relic → facility → enemy passive → sacrifice → score／telemetry。各カテゴリ内はAccepted stable orderを使う。
- `ClosedWindowResourceState`がreserved draw／qi、Soul、DeferredPlayerChoice、first-use flagsをimmutable／canonicalに保持する。
- terminal batchは全benefitを抑止し、TLE-07／08のterminal suppressionを回帰する。
- TLE-09をreserved draw 7、Soul 1、sacrifice remainder 2、counterattack delta 0、指定benefit event順でexact再現する。
- TLE-10をblack capture attribution、Soul 1、reserved draw 2、reserved qi 3、`seal_bone:qi_or_draw` DeferredChoice 1件でexact再現する。
- 最小counterattack boundary stateは`GaugeUnits`、`Pending`、`SacrificeStoneRemainder`をcanonicalに保持し、threshold、natural gain、stones-per-batch／units-per-batchをinjected policyから取得する。
- counterattack operationはsacrifice advance、enemy-turn-end advance、pending-at-start snapshot、consume／reprime onceに限定し、heat／overextension／intentを実装しない。
- TLE-15を`160 units → sacrifice +30 → enemy-turn-end +12 → 2 units, Pending=true`としてexact再現する。sacrifice advanceはexpiry benefit後、natural gain前に発行する。
- same input／reversed input enumerationでresource／facts／counterattack stateが一致する。
- production／test symbolがMOM／CTR全fixture coverageまたはfull FEAT-003実装をclaimしない。

## Validation

- TLE-09／10／15 exact fixture adapter、trigger source列挙順反転、terminal／ongoing、threshold／overflow／remainder境界をテストする。
- Architecture testでcontent-specific rule直書き、full CTR／Momentum先取り、Godot／host依存を拒否する。
- `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を各2回実行する。
- `git diff --check`とroot `CODE_REVIEW.md`に従う独立fixed-HEAD reviewを行う。

## Stop conditions

- fixtureを通すためcontent ID別のtest-only switchが必要。
- `game_data/`にないbalance値をproductionへ複製する必要がある。
- TLEのためにheat／overextension／CTR-01〜25の全体実装が必要。
- triggerからboard mutationが必要になる。
- Application境界／replayを先取りしないとgreenにできない。

## Execution log

2026-07-12 — DECISION-0005 Option 1のM1 TLE workstream第2段として[[TASK-0026 Resolve M1 Momentum Counterattack Migration Boundary]]が定義。[[TASK-0027 Implement Temporary Liberty Domain Kernel]]のhuman mergeまで`blocked`。

## Evidence

- [[FEAT-011 Temporary Liberty Expiry Fixtures]] TLE-09／10／15。
- [[TASK-0025 Gate 1 Deterministic Foundation Audit]] TLEのcapture-benefit／counterattack dependency。

## Known issues

TLE-14のenemy action boundaryと全15 casesのApplication golden／replay evidenceは[[TASK-0029 Integrate Temporary Liberty Enemy Boundary and Golden Replay]]の範囲である。本TASKだけでTLE migration完了またはM1 exitと扱わない。
