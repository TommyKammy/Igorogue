---
type: task
id: TASK-0004
status: done
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001]
updated: 2026-07-11
---
# TASK-0004 Stone Groups and Unique Liberty Sets

## Outcome

同色グループと重複なし呼吸点集合を実装。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- 共有呼吸点、斜め非連結、複数群。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

2026-07-11 — TASK-0003の独立review、green CI、PR #4の人間mergeを確認し、直列Gate 1の次タスクとして`ready`へ遷移。

2026-07-11 — Rules Canon、Architecture、Determinism and Replay、後続TASK-0005〜0009との境界を照合。Stone layerだけのimmutable snapshotから同色直交groupと一意な実呼吸点を導出し、配置・capture・合法性・王石結果・有効呼吸点modifierは先取りしない実装範囲で着手。

2026-07-11 — canonical index順のimmutable `BoardState`、`BoardStone`、initial-position mappingを実装。占有石を防御copyし、null、不正色、同一点の重複を拒否する。runtime metadataは色・王石flag・点だけに限定し、護衛は通常石へ写像する。

2026-07-11 — `StoneGroupAnalyzer`とimmutableな`StoneGroupAnalysis`／`StoneGroup`を実装。49点のcanonical走査、同色直交BFS、bool maskによる呼吸点一意化を使い、group anchor、group列、石列、実呼吸点列をcanonical point orderへ固定した。実呼吸点0のsnapshotも保持し、自動captureは行わない。

2026-07-11 — 空盤、全49点の単石境界、共有呼吸点を持つL字、斜め非連結4群、異色隣接、複数石の実呼吸点0、入力順permutation、重複／null／不正enum、公開collection不変性をunit test化。COORD-11の標準初期配置testをtest-only traversalからproduction analyzerへ置換し、各3石・7実呼吸点とanchor順を確認した。

2026-07-11 — package、project reference、lock、Application、Content、game_data、Accepted仕様、Godot assetは変更していない。コミット前API reviewとarchitecture／scope reviewはいずれもfindingなしで`APPROVE`。

2026-07-11 — 独立Codex closeout reviewでコードfindingなし。唯一のLOW findingだったTASK／dashboardの状態driftを修正し、再確認`APPROVE`を得て`review`へ遷移。

## Evidence

- `tools/dev/check` — exit 0。documentation、wikilink、content、既存fixture、governance checkが成功。content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`。
- `tools/dev/test` — exit 0。exact .NET SDK `8.0.422`、locked restore、Release build、warning 0／error 0。Domain 68、Application 12、Architecture 5、合計85 testが成功。
- `tools/dev/sim-smoke`を2回実行 — 両方exit 0。同一の`checksum=3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`、同一content hash、`files=7`を確認。
- `tests/Igorogue.Domain.Tests/StoneGroupAnalyzerTests.cs` — 共有呼吸点の一意化、斜め／異色非連結、複数groupのanchor順、実呼吸点0、全49点の隣接境界、入力permutationに対する完全analysis一致、immutable出力を確認。
- `tests/Igorogue.Domain.Tests/BoardStateTests.cs` — 入力防御copy、canonical順、初期王石／護衛mapping、同色／異色重複点拒否、null／不正色拒否、immutable占有石列を確認。
- `tests/Igorogue.Domain.Tests/InitialPositionFixtureTests.cs` — `standard_v0_2`をproduction `BoardState`／`StoneGroupAnalyzer`へ通し、2 group、anchor `(2,2)`／`(6,5)`、各3石・王石1・実呼吸点7を確認。
- 読み取り専用API reviewとarchitecture／scope review — 両方findingなしで`APPROVE`。review側でもgovernance、85/85 test、warning 0／error 0を確認。
- 独立Codex closeout review — `origin/main...HEAD`を正本仕様と直接照合し、コードfindingなし。governance、85/85 test、2回同一simulator checksumを独立確認。状態同期だけをLOW follow-upとして指摘し、修正前`APPROVE WITH FOLLOW-UP`、修正後再確認はfindingなしで`APPROVE`。
- GitHub Actions run `29134488480` — PR #5 head `ea3d49b9f662c6d01b8b8cf990736a26bdf8fb86`で全3 job成功。人間判断でPR #5をmergeし、merge commit `18d8ea0dfc84cc6b4d72f3b9d9301099e36b4ec8`を確認。

## Known issues

TASK-0004範囲の既知defectはなし。

仮配置・同時captureはTASK-0005、自殺手・盤面反復はTASK-0006、王石結果はTASK-0007、領地はTASK-0008、golden serializationはTASK-0009へ明示的に延期する。timed／continuous modifierを含む有効呼吸点とstable stone instance IDは、それらを要求する後続タスクまで導入しない。
