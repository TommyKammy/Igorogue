---
type: task
id: TASK-0027
status: review
project: Igorogue
milestone: M1
priority: critical
dependencies: [TASK-0004, TASK-0005, TASK-0006, TASK-0007, TASK-0008, TASK-0026]
updated: 2026-07-12
---
# TASK-0027 Implement Temporary Liberty Domain Kernel

## Outcome

stable stone instance identity、timed／continuous effective-liberty state、grant／carrier removal、同時expiry capture、mandatory topology observation、king gateをpure Domainへ実装し、TLE-01〜08／11〜13をproduction Rules KernelのE3 exact unit evidenceへ移植する。

## Source of truth

- [[Rules Canon]]
- [[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]
- [[FEAT-011 Temporary Liberty Expiry Fixtures]]
- [[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]
- [[ADR-0011 Battle-Local Stone Topology Repetition Ban]]
- [[Determinism and Replay]]
- `game_data/fixtures/temporary_liberty_expiry_fixtures.json`

## Non-goals

- capture benefit、予約draw／qi、Soul、DeferredPlayerChoice。
- Counterattack state／反攻追加敵行動。
- Application enemy boundary、golden replay、replay schema。
- `PlayCard`、deck／hand、霊泉等のcontent生成runtime。
- Momentum／妙手、enemy planner、UI／Godot。
- Accepted rule／ADR／fixture valueの変更。

## Allowed areas

- `src/Igorogue.Domain/Board/`
- `src/Igorogue.Domain/Combat/`
- `src/Igorogue.Domain/`下の新規temporary-liberty／stone-runtime領域。
- `tests/Igorogue.Domain.Tests/`
- Architecture testsのDomain boundary回帰。
- 本TASKとstatus／queue文書。
- `game_data/`はread-onlyとし、値／fixtureを変更しない。

## Acceptance criteria

- `BoardState`のoccupied pointsへexact-bindされたimmutable stone-runtime stateが、全stoneの一意instance ID、kind／effect metadata、next sequenceをcanonical orderで保持する。`StoneTopologyKey`は従来どおり空／色／王石だけを含む。
- `TemporaryLibertyState`がeffect ID、amount、owner、anchor stone instance、source、created sequence、expiry enemy-turn indexを検証／canonical化する。
- grantは現在groupのCanonical point order先頭stone instanceへanchorし、sweep開始後のgrantは次のenemy-turn boundaryへ送る。
- effective libertiesはunique real＋active timed＋continuous modifierを一度ずつ合成し、stack、future effect、merge追従、continuous残存を満たす。
- due effect 0件はexact no-op。due effectを全削除後、石を除去する前の一snapshotからdoomed groupsを確定し、group anchor／stone pointのstable orderで事実を発行しつつ同時除去する。
- 通常captureでanchorが消えたeffectは同一resolutionで`carrier_removed`となり、後のexpiry eventを発行しない。
- mandatory captureは既出topologyでもordered observationへ追加し、`first_seen=false`、Seen集合不変とする。test-only state mutationを使わない。
- 黒王石loss優先、白王石win、両王石lossを同時batchで一義にし、terminal batchは後続benefit pipelineを呼ばないseamを持つ。
- TLE-12は既存`TerritoryAnalyzer`を使い、新領地を反映するがMomentum／Brilliant factを発行しない。
- TLE-01〜08／11〜13のcanonical input／payload／expected resultのうち、本TASKが所有するstone runtime、effective liberty、expiry capture、topology、king gate、territory projectionをproduction Domain resolverでexact検証する。TLE-07のarmed payloadは欠落なくfixture adapterへ保持し、terminal suppressionにより後続continuationを取得できないことまでをexact検証するが、reserved draw／Soul／counterattack deltaのproduction projectionは明示的Non-goalとしてTASK-0028へ残す。仕様checkerをproduction evidenceとして呼ばない。
- same input、input enumeration reversal、same seedでstate／events／checksum projectionが一致する。

## Validation

- TLE-01〜08／11〜13 fixture adapter、effect／group列挙順反転、同一入力2回、foreign snapshot／duplicate ID rejectionをテストする。
- 既存board／capture／repetition／king／territory testsを回帰する。
- `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を各2回実行する。
- `git diff --check`とroot `CODE_REVIEW.md`に従う独立fixed-HEAD reviewを行う。

## Stop conditions

- stone identityまたはcontinuous source lifetimeにAccepted仕様上の新たな判断が必要。
- mandatory historyをtest-only mutationなしで表現できない。
- [[TASK-0028 Implement Closed-Window Capture Benefits and TLE Boundary Pressure]]／[[TASK-0029 Integrate Temporary Liberty Enemy Boundary and Golden Replay]]の範囲を先取りしないとgreenにできない。
- Accepted ADR／Rules Canonと矛盾する。

## Execution log

2026-07-12 — DECISION-0005 Option 1のM1 TLE workstream第1段として[[TASK-0026 Resolve M1 Momentum Counterattack Migration Boundary]]が定義。TASK-0026のhuman mergeまで`blocked`。

2026-07-12 — PR #16を人間merge。merged head `eae7b616768ee7934795f31eec133a3940607390`、merge commit／fixed `origin/main` `90dda9dd41b96864a24e19a7969285f56c4593b4`を確認し、全dependencyが閉じたため本TASKを`ready`へ遷移した。

2026-07-12 — Outcome、Non-goals、Acceptanceを再確認。`BoardStone`を変更せずexact-bound stone-runtime stateを追加し、continuous modifierは永続sourceから導出済みのactive snapshotとして受け取る最小Domain APIに限定した。TLE-09／10／14／15とApplication接続を後続へ残し、本TASKを`in_progress`へ遷移した。

2026-07-12 — PR #16 post-merge main CI run `29180540418`のGovernance `86617175981`、Pure .NET `86617194216`、Godot／Windows export `86617238452`がすべてsuccessであることを確認した。

2026-07-12 — TLE-07のfixture期待値には本TASKのNon-goalであるreserved draw／Soul／counterattack deltaが含まれるため、Acceptanceの「exact」をTASK-0027所有projectionへ明示化した。fixtureの`style_id`／`equipped_seals`／3 deltaはadapterで保持し、production Domainはterminal king gateからbenefit continuationを公開しないところまでを検証する。delta生成を仮実装せずTASK-0028へ残すためのscope clarificationであり、Accepted rule／fixture valueは変更していない。

2026-07-12 — occupied boardへexact-bindする`StoneRuntimeState`、stable instance identity、timed effect／derived continuous snapshot、real＋timed＋continuous内訳をpure Domainへ実装した。grantはcanonical group anchorを使い、sweep後grantはnon-forgeable exact-state windowを要求して次boundaryへ送る。

2026-07-12 — due effect一括除去後の一snapshotでdoomed groupを確定し、同時capture、carrier cleanup、mandatory topology observation、king gate／terminal benefit suppression、capture後territory projectionを一resolutionへ実装した。canonical state／fact projectionとSHA-256 checksumを追加した。

2026-07-12 — normal placementのcarrier removalを`LegalPlacementCommit`とcapture／post-capture双方のexact temporary-liberty analysisへbindした。real-liberty-only capture、差し替えsuicide snapshot、foreign runtime、stale sweep state、foreign／terminal windowを拒否する境界テストを追加した。

2026-07-12 — TLE-01〜08／11〜13の11 casesをproduction resolverへ移植し、same input、effect／modifier列挙反転、canonical event ordering、既出topology mandatory capture、terminal gate、territory、due 0 exact no-opを検証した。仕様checkerはproduction evidenceとして呼んでいない。

2026-07-12 — 独立preflight／API・determinism監査で指摘されたcommit binding、TLE-07 evidence境界、sweep state invariantを修正。再監査はいずれも追加findingなし、`APPROVE`。repository wrapper検証もgreenのため本TASKを`review`へ遷移した。

2026-07-12 — closeout候補に対して`tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を各2回連続実行し、全6 commandがexit 0。両runでtest count／content snapshot／simulation checksumが一致した。

2026-07-12 — independent implementation fixed-HEAD focused reviewが`6427287e8e8bc69d7de080ca2e1788052f8ea63e`を`origin/main` `90dda9dd41b96864a24e19a7969285f56c4593b4`と比較。全Acceptance、TLE-01〜08／11〜13 production evidence、Non-goalsを照合し、findingなし、`APPROVE`。後続のreview-evidence文書commitを含むPR最終treeのreview結果は、commit自身への循環参照を避けてPR #17のreview threadへ外部記録する。

## Evidence

- [[FEAT-011 Temporary Liberty Expiry Fixtures]] TLE-01〜08／11〜13。
- [[TASK-0025 Gate 1 Deterministic Foundation Audit]] TLE M1 implementation gap。
- PR #16 merge commit `90dda9dd41b96864a24e19a7969285f56c4593b4`／post-merge main CI run `29180540418`全3 job success。
- production Domain — immutable stone runtime、temporary／continuous effective-liberty analysis、grant／carrier removal、simultaneous expiry resolution、mandatory topology commit、king／benefit gate、territory projection。
- fixture evidence — TLE-01〜08／11〜13の11 cases。same input／逆列挙のcanonical resolution・checksum一致、TLE-11 restored production history、TLE-13 exact reference／event no-op。
- `tools/dev/check` ×2 — 連続exit 0。47 content IDs、content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`が両runで一致。
- `tools/dev/test` ×2 — 連続exit 0。.NET SDK 8.0.422、Domain 253、Application 54、Architecture 16、計323 tests、warning 0／error 0が両runで一致。
- `tools/dev/sim-smoke` ×2 — 連続exit 0。`checksum=3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`が両runで一致。bootstrap determinism evidenceとしてのみ使用。
- `git diff --check` — exit 0。Rules Canon、Accepted ADR／Feature Spec、`game_data/`、Application、Godot assetに変更なし。
- independent preflight／API・determinism review — exact snapshot、ordering、state invariant、scope evidenceを再監査し、追加findingなし、`APPROVE`。
- independent implementation fixed-HEAD review — `6427287e8e8bc69d7de080ca2e1788052f8ea63e`、base `90dda9dd41b96864a24e19a7969285f56c4593b4`、findingなし、`APPROVE`。PR最終treeのreview resultはPR #17 threadへ外部記録する。

## Known issues

TLE-09／10のcapture benefitとTLE-14／15のenemy-turn／counterattack boundaryは後続TASKであり、本TASKのE3 evidenceだけでTLE-01〜15移植完了またはM1 exitと扱わない。

TLE-07の`style_id`、`equipped_seals`、期待値`reserved_draw_delta`／`soul_delta`／`counterattack_delta_units`はfixture adapterで欠落なくparseし、armed fixtureがterminal king gateでbenefit continuationを取得できないことまでを本TASKのE3 evidenceとする。各deltaを生成するcapture-benefit／resource／counterattack pipeline自体は明示的Non-goalであり、値0のproduction projection検証は[[TASK-0028 Implement Closed-Window Capture Benefits and TLE Boundary Pressure]]へ残す。
