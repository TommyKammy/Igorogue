---
type: task
id: TASK-0027
status: blocked
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
- TLE-01〜08／11〜13のcanonical input／payload／expected resultをproduction Domain resolverでexact検証する。仕様checkerをproduction evidenceとして呼ばない。
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

## Evidence

- [[FEAT-011 Temporary Liberty Expiry Fixtures]] TLE-01〜08／11〜13。
- [[TASK-0025 Gate 1 Deterministic Foundation Audit]] TLE M1 implementation gap。

## Known issues

TLE-09／10のcapture benefitとTLE-14／15のenemy-turn／counterattack boundaryは後続TASKであり、本TASKのE3 evidenceだけでTLE-01〜15移植完了またはM1 exitと扱わない。
