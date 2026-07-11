---
type: task
id: TASK-0008
status: review
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001]
updated: 2026-07-11
---
# TASK-0008 Territory Region Calculation

## Outcome

空点領域と隣接色による領地判定。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]
- [[FEAT-001 Territory and Facilities]]
- [[ADR-0012 Facility Sites Are Empty Intersections]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- 隅、辺、中立、石なし領域。
- 施設点を通常の空点としてflood fill、領地サイズ、隣接色へ含める。
- 施設有無だけで領地結果と実呼吸点が変わらないunit test。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

2026-07-11 — TASK-0007の独立review、green CI、PR #8の人間mergeを確認し、直列Gate 1の次タスクとして`ready`へ遷移。

2026-07-11 — Rules Canon、Architecture、Determinism and Replay、FEAT-001、ADR-0012、FAC-01〜09を照合。石レイヤーだけを入力する共通pure Domain analyzerで、空点region、canonical anchor／point／region順、黒のみ／白のみ／両色または石なしの所有判定を実装する方針で着手。FAC-01／02／05／06／07／09の領地・呼吸点投影を移植し、FacilityInstance、建設・破壊・稼働、収入・容量、event、Application、replay、UIは先取りしない。TASK-0008内の仕様矛盾とDecision Neededはなし。

2026-07-11 — exact `BoardState`を入力する`TerritoryAnalyzer`と、immutableな`TerritoryAnalysis`／`TerritoryRegion`／`TerritoryOwner`を実装。全空点を重複なくregionへ割り当て、pointとregionをcanonical順へ固定し、occupied point lookupをnull、両色または隣接石なしをneutralとした。analysisはexact source boardへbindし、public constructorやfacility-aware overloadを持たない。

2026-07-11 — 空盤、全占有、隅、辺、内部の両色中立、斜め非連結、複数region partition、王石色、stone入力順反転、read-only／null境界をunit test化。canonical facility JSONをtest-only loaderで読み、FAC-01／02／05／06／07／09のstone-layer territory／real-liberty投影と施設metadata非依存を共有Rules Kernelで検証した。

2026-07-11 — precommit API reviewはfindingなしで`APPROVE`。determinism／spec reviewはコードfindingなしで、TASK-0009が要求するFAC-01〜09完全移植より前にfacility runtime taskが存在しない計画gapをKnown Issuesへ記録すべきというMEDIUM findingを検出。FAC投影とruntime統合を区別し、TASK-0009開始前のDecision Neededを明記して解消した。

2026-07-11 — package、project reference、lock、Application、Content、game_data、Accepted仕様、Godot assetは変更していない。基本収入、建設容量、FacilityInstance、建設・破壊・稼働、territory delta／facility event、Momentum、battle state、replay、UIは後続へ維持した。

2026-07-11 — commit `37e0c00`を対象に、実装担当とは別のCodexが`CODE_REVIEW.md`に従って親commitとの差分をTASK、Rules Canon、Architecture、Determinism and Replay、FEAT-001、ADR-0012、canonical FAC fixtureへ再照合。TASK-0008範囲のfindingなし、独立governance／160 test／2回同一sim checksumを確認した。TASK-0009前のfacility runtime計画gapをMEDIUM follow-upとして維持し、`APPROVE WITH FOLLOW-UP`で`review`へ遷移。CIと人間merge待ちとした。

## Evidence

- `tools/dev/check` — exit 0。documentation、wikilink、content、FAC-01〜09を含む既存fixture、governance checkが成功。content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`。
- `tools/dev/test` — exit 0。exact .NET SDK `8.0.422`、locked restore、Release build、warning 0／error 0。Domain 141、Application 12、Architecture 7、合計160 testが成功。
- `tools/dev/sim-smoke`を2回実行 — 両方exit 0。同一の`checksum=3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`、同一content hash、`files=7`を確認。
- `tests/Igorogue.Domain.Tests/TerritoryAnalyzerTests.cs` — 空盤neutral 49、全占有0、隅／辺、両色中立、斜め分離、canonical region／point順、全空点partition、occupied lookup、王石色、入力順不変、immutable collection、null境界を確認。
- `tests/Igorogue.Domain.Tests/TerritoryFacilityFixtureTests.cs` — canonical FAC-01〜09 inventoryを読み、FAC-01／02／05／06／07／09の領地・呼吸点投影をproduction analyzerで確認。施設pointがstone-emptyであり、施設metadataだけではterritory、`StoneTopologyKey`、実呼吸点へ入力できない境界を確認。
- `tests/Igorogue.Architecture.Tests/ArchitectureBoundaryTests.cs` — public analyzer入力が`BoardState`だけで、region／analysisを外部からforgeできないことを確認。
- 読み取り専用API review — public surface、immutability、exact source binding、lookup整合性、canonical順、neutral semantics、facility入力排除にfindingなしで`APPROVE`。独立実行でもgovernance、160/160 test、warning 0／error 0を確認。
- determinism／spec review — コードfindingなし。Known Issuesの計画gap記録をMEDIUM findingとして指摘し、FAC projectionとfacility runtime integrationを区別して解消。独立実行でもgovernance、160/160 test、2回同一sim checksumを確認。
- 独立Codex closeout review — commit `37e0c00`の親との差分を正本仕様へ直接照合し、TASK-0008範囲のBLOCKER／HIGH／MEDIUM／LOW findingなし。独立実行でもgovernance、160/160 test、warning 0／error 0、2回同一sim checksum、clean worktreeを確認。TASK-0009のfacility runtime計画gapだけを下流MEDIUM follow-upとして`APPROVE WITH FOLLOW-UP`。

## Known issues

TASK-0008範囲の既知defectはなし。

本タスクのproduction analyzerはstone-layer projectionだけを実装する。FAC-09のテストはfacility metadataがterritory／liberty APIへ入らない型境界の証拠であり、実`FacilityInstance`、build、destroy、operating state、capacity、eventとのruntime統合ではない。FAC-03／04／08と、FAC-05／06／07／09のfacility runtime側は未実装である。

現在の[[TASK-0009 Golden Board Fixtures]]はFAC-01〜09のunit test／golden replay完全移植を要求するが、その前にfacility runtimeを実装するTASKがない。TASK-0009を`ready`へ移す前に、専用facility runtime TASKを挿入するか、TASK-0009の受け入れ条件を実装済みprojectionと後続runtimeへ分割するDecision Neededが必要である。これはTASK-0008の領地規則・実装をblockしない。
