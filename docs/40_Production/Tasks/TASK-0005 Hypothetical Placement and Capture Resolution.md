---
type: task
id: TASK-0005
status: review
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001]
updated: 2026-07-11
---
# TASK-0005 Hypothetical Placement and Capture Resolution

## Outcome

仮配置と相手同時捕獲を実装。

## Source of truth

- [[Rules Canon]]
- [[Combat Resolution Order]]
- [[FEAT-005 Sacrifice Triggers]]
- [[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]
- [[Architecture]]
- [[Determinism and Replay]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- 複数群同時捕獲と安定イベント順。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

2026-07-11 — TASK-0004の独立review、green CI、PR #5の人間mergeを確認し、直列Gate 1の次タスクとして`ready`へ遷移。

2026-07-11 — Rules Canon、Combat Resolution Order、FEAT-005、FEAT-011、Architecture、Determinism and Replay、後続TASK-0006／0007との境界を照合。通常配置captureも有効呼吸点0を条件とするため、real-only resolverは採用せず、配置後group snapshotへ厳密にbindされたimmutable effective-liberty factsを必須入力にする二段階の仮解決として着手。自殺手、反復、王石結果、trigger publishは先取りしない。

2026-07-11 — `HypotheticalPlacementResolver.TryCreate`を実装。occupied pointは`false`／null resultでfail closedし、成功時はsourceを変更せず、配置後board、exact group analysis、canonical anchor順に重複排除した隣接相手groupをimmutable snapshotとして返す。

2026-07-11 — exact `StoneGroupAnalysis` identityへbindする`EffectiveLibertySnapshot`を実装。全group coverageを必須とし、null、missing、duplicate、foreign group、負の最終countを拒否する。real-only default／overloadは持たず、実呼吸点と有効呼吸点の大小関係をresolver側で仮定しない。

2026-07-11 — `ResolveCaptures`を実装。同一の配置後snapshotで隣接相手groupの有効呼吸点0を全選択し、全stone unionを一度だけ除去する。結果board、capture後exact analysis、配置group、anchor順captured groups、`StonePlacedFact`から`GroupCapturedFact`へ続くcommit-ready ordered factsをimmutableに返す。source、配置後snapshot、facility、履歴、RNGは変更しない。

2026-07-11 — empty／occupied、KO-01単群capture、同一groupへの複数接触、2群3石同時capture、real 0／effective 1生存、real 3／effective 0 capture、非隣接0 group、FAC-04 self-zero候補、KO-02 raw recapture、king metadata、入力permutation、snapshot identity／完全性／immutabilityをunit test化。

2026-07-11 — コミット前API reviewで、capture後analysisの未保持と未承認の`effective >= real`制約を検出。exact post-capture analysisをresultへ保持し、effective countを非負だけに制約して両方修正。API再reviewとarchitecture／scope reviewはいずれもfindingなしで`APPROVE`。新たなDecision Neededはなし。

2026-07-11 — package、project reference、lock、Application、Content、game_data、Accepted仕様、Godot assetは変更していない。

2026-07-11 — 独立Codex closeout reviewでコードfindingなし。review questionのcapture条件が実呼吸点と誤記されていたMEDIUM findingと、TASK／dashboardの状態driftであるLOW findingを修正し、再確認`APPROVE`を得て`review`へ遷移。

## Evidence

- `tools/dev/check` — exit 0。documentation、wikilink、content、既存fixture、governance checkが成功。content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`。
- `tools/dev/test` — exit 0。exact .NET SDK `8.0.422`、locked restore、Release build、warning 0／error 0。Domain 83、Application 12、Architecture 5、合計100 testが成功。
- `tools/dev/sim-smoke`を2回実行 — 両方exit 0。同一の`checksum=3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`、同一content hash、`files=7`を確認。
- `tests/Igorogue.Domain.Tests/HypotheticalPlacementResolverTests.cs` — captureなし、occupied拒否、KO-01／KO-02 raw resolution、同一group dedupe、2群同時除去とfact順、effective補正の正負方向、非隣接group非掃引、self-zero候補、王石metadata、入力permutation、snapshot境界を確認。
- 読み取り専用API reviewとarchitecture／scope review — 初回2 findingを修正後、両方findingなしで`APPROVE`。review側でもgovernance、100/100 test、warning 0／error 0を確認。
- 独立Codex closeout review — `origin/main...HEAD`を正本仕様と直接照合し、コードfindingなし。governance、100/100 test、2回同一simulator checksumを独立確認。capture条件の文言と状態同期を文書follow-upとして指摘し、修正前`CHANGES REQUIRED`、修正後再確認はfindingなしで`APPROVE`。

## Known issues

TASK-0005範囲の既知defectはなし。

timed／continuous stateとstable stone instance IDから`EffectiveLibertySnapshot`を生成するDomain calculatorは、それらのruntime effectを導入する後続タスクへ延期する。Application／UIが有効呼吸点countを独自決定してはならない。

capture後の自group有効呼吸点、自殺手、terminal配置、`StoneTopologyKey`反復判定、不合法時のhypothetical board／ordered facts破棄はTASK-0006、王石勝敗はTASK-0007、正式なFEAT-005 `CaptureBatch`、trigger publish、carrier-effect removalは対応する後続統合へ明示的に延期する。
