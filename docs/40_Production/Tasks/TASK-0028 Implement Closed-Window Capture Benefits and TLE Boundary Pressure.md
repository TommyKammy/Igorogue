---
type: task
id: TASK-0028
status: done
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

2026-07-12 — PR #17を人間merge。merged head `f582134ba3d63c1188614d2aeed26f270d6f8422`、merge commit／fixed `origin/main` `ad50fe7ae7a7170e308c322971380c4e66a2dcb0`を確認し、唯一のdependencyが閉じたため本TASKを`ready`へ遷移した。

2026-07-12 — PR #17 post-merge main CI run `29183647493`のGovernance `86625797646`、Pure .NET `86625818827`、Godot／Windows export `86625880445`がすべてsuccessであることを確認した。Outcome、Non-goals、Acceptanceを再確認し、共通capture batch、pre-resolved typed operation、closed-window resource、最小counterattack boundaryだけをpure Domainへ追加する方針で本TASKを`in_progress`へ遷移した。

2026-07-12 — expiry resolutionが既存canonical v1／ordered factsを変更せず、reason、boundary、closed window、group／stone、capturing color、king involvementをfull stone-runtime projection込みで保持する共通`CaptureBatch`を公開するようにした。captureなしは`null`、terminal batchもbenefit処理前の観測用batchを保持する。

2026-07-12 — content解釈済みtyped operationとtyped source factoryを受けるclosed-window pipelineを実装した。source kindからglobal stage順を固定し、captured stoneはbatch membershipとgroup／point order、seal／relicはslot、facilityはpoint／instance、enemy passiveはIDへbindする。同一source、stone、sacrifice、event path、first-use flagの二重適用を拒否し、Domainにcontent ID／数値switchを置いていない。

2026-07-12 — reserved draw／qi、Soul、DeferredPlayerChoice、明示登録first-use flagをimmutable／canonical stateへ実装した。terminal gateはtrigger列挙より前に置き、TLE-07／08で全operationとstate mutationを抑止する。既存expiry側の`CaptureBenefitSuppressedFact`を一件だけ正本とし、pipeline側で重複発行しない。

2026-07-12 — injected threshold／natural gain／sacrifice policyだけを使う最小counterattack boundaryを実装した。stateはGauge／Pending／remainderをcanonical化し、pending generation tokenでforeign／再利用snapshotを拒否する。公開operationはsacrifice、enemy-turn-end、pending-at-start snapshot、consume／reprime onceの4種だけとした。

2026-07-12 — TLE-09／10／15 adapterをfixture inputと`game_data`からpre-resolveし、expected値を実行入力に使わずproduction pipelineへ移植した。TLE-09 draw 7／Soul 1／remainder 2／反攻+0とevent順、TLE-10 black attribution／Soul 1／draw 2／qi 3／choice 1件、TLE-15 160→+30→+12→2／Pendingをexact検証した。同一入力、trigger／group列挙反転、multi-group、terminal、threshold、overflow、remainder、canonical collision負例も追加した。

2026-07-12 — 独立API／rules preflightとdeterminism preflightで指摘されたterminal gate順、canonical field／TriggerId欠落、content ID拒否不足、first-use未宣言、source二重発火、Pending snapshot foreign／再利用を修正。両reviewの再確認は追加findingなし、`APPROVE`。

2026-07-12 — closeout候補に対して`tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を各2回連続実行し、全6 commandがexit 0。両runでtest count、content snapshot、simulation checksumが一致し、`tools/dev/build`と`git diff --check`もexit 0。

2026-07-12 — independent fixed-HEAD reviewがimplementation commit `7562f3430486db15047b7de28ca74627a5adba56`をbase `ad50fe7ae7a7170e308c322971380c4e66a2dcb0`と比較。正本、全Acceptance、TLE-09／10／15、scope、canonical／terminal／typed binding／pending tokenを照合し、actionable findingなし、`APPROVE`。repository wrapperもgreenのため本TASKを`review`へ遷移した。

2026-07-12 — PR #18を人間merge。merged head `fb53bc3f644f8d7a6498c46b12db56da33ed07c3`、merge commit／fixed `origin/main` `ddccd57db12219847646d0b2de85c18b2c94b120`を確認した。post-merge main CI run `29187053532`のGovernance `86634981261`、Pure .NET `86635001823`、Godot／Windows export `86635049311`がすべてsuccessのため、本TASKを`done`へ遷移した。

## Evidence

- [[FEAT-011 Temporary Liberty Expiry Fixtures]] TLE-09／10／15。
- [[TASK-0025 Gate 1 Deterministic Foundation Audit]] TLEのcapture-benefit／counterattack dependency。
- PR #17 merge commit `ad50fe7ae7a7170e308c322971380c4e66a2dcb0`／post-merge main CI run `29183647493`全3 job success。
- implementation commit `7562f3430486db15047b7de28ca74627a5adba56` — canonical common capture batch、typed closed-window pipeline、resource state、minimal counterattack boundary。
- fixture evidence — TLE-09／10／15のexact expected、TLE-07／08 terminal suppression、同一入力／逆列挙のresource・facts・counterattack canonical一致。仕様checkerはproduction evidenceとして呼んでいない。
- `tools/dev/check` ×2 — 連続exit 0。47 content IDs、content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`が両runで一致。
- `tools/dev/test` ×2 — 連続exit 0。.NET SDK 8.0.422、Domain 277、Application 54、Architecture 19、計350 tests、warning 0／error 0が両runで一致。
- `tools/dev/sim-smoke` ×2 — 連続exit 0。`checksum=3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`が両runで一致。bootstrap determinism evidenceとしてのみ使用。
- `tools/dev/build` — exit 0、warning 0／error 0。`git diff --check` — exit 0。
- Rules／API preflight、determinism preflight — 指摘修正後はいずれもfindingなし、`APPROVE`。
- independent implementation fixed-HEAD review — `7562f3430486db15047b7de28ca74627a5adba56`、base `ad50fe7ae7a7170e308c322971380c4e66a2dcb0`、actionable findingなし、`APPROVE`。
- PR #18 merged head `fb53bc3f644f8d7a6498c46b12db56da33ed07c3`／merge commit `ddccd57db12219847646d0b2de85c18b2c94b120`／post-merge main CI run `29187053532`全3 job success。

## Known issues

TLE-14のenemy action boundaryと全15 casesのApplication golden／replay evidenceは[[TASK-0029 Integrate Temporary Liberty Enemy Boundary and Golden Replay]]の範囲である。本TASKだけでTLE migration完了またはM1 exitと扱わない。

本TASKのcounterattack boundaryはTLEに必要なGauge／Pending／sacrifice remainderと4 operationだけであり、heat、overextension、battle reset、実反攻action／intent、CTR-01〜25のfull production migrationを実装済みとは扱わない。
