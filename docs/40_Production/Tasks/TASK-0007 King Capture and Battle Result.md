---
type: task
id: TASK-0007
status: in_progress
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001]
updated: 2026-07-11
---
# TASK-0007 King Capture and Battle Result

## Outcome

王石群捕獲による勝敗。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]
- [[Combat Resolution Order]]
- [[FEAT-005 Sacrifice Triggers]]
- [[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]
- [[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- 通常石捕獲と王石捕獲を区別。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

2026-07-11 — TASK-0006の独立review、green CI、PR #7の人間mergeを確認し、直列Gate 1の次タスクとして`ready`へ遷移。

2026-07-11 — Rules Canon、Combat Resolution Order、FEAT-005、FEAT-011、ADR-0014、Architecture、Determinism and Replay、後続TASK-0008／0010との境界を照合。atomic batchのcaptured groupsを入力する共通pure evaluatorを唯一のauthorityとし、黒王石captureをloss優先、次に白王石captureをwin、それ以外をongoingとする。通常配置はtopology登録後の`LegalPlacementCommit`へderived resultを添付し、正式event publish、benefit suppression、expiry sweep、turn-limit loss、battle state transitionを先取りしない方針で着手。Decision Neededなし。

2026-07-11 — immutableな`KingCaptureResult`と共通pure evaluatorをDomainへ実装。atomic batch全体から黒／白王石capture flagを集約し、入力group順に依存せず、黒王石を含む場合はloss、黒王石なしで白王石を含む場合はwin、それ以外はongoingとするversioned canonical resultを生成する。

2026-07-11 — 通常配置ではexact accepted candidateを履歴へ登録した後の`LegalPlacementCommit`だけが公開resultを返すよう統合。通常石capture、白王石capture、黒王石capture、両王石同時capture、入力順反転、null境界、反復不合法な王石captureのcommit拒否をtest化した。

2026-07-11 — precommit API reviewでraw captured-group evaluatorがpublicで、合法commit前のhypothetical captureから終局resultを作れるMEDIUM findingを検出。evaluatorをDomain internalへ閉じ、型付きfriend testと公開API境界testを追加して解消した。将来のmandatory expiry commitは同じDomain assemblyからpure coreを再利用できる。

2026-07-11 — package、project reference、lock、Application、Content、game_data、Accepted仕様、Godot assetは変更していない。正式event publish、`CaptureBatch`、終局時benefit suppression、expiry sweep、20-turn loss、battle state transition、UI／replayは後続taskへ維持した。

## Evidence

- `tools/dev/check` — exit 0。documentation、wikilink、content、既存fixture、governance checkが成功。content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`。
- `tools/dev/test` — exit 0。exact .NET SDK `8.0.422`、locked restore、Release build、warning 0／error 0。Domain 119、Application 12、Architecture 6、合計137 testが成功。
- `tools/dev/sim-smoke`を2回実行 — 両方exit 0。同一の`checksum=3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`、同一content hash、`files=7`を確認。
- `tests/Igorogue.Domain.Tests/KingCaptureResultEvaluatorTests.cs` — empty／通常石captureのongoing、白王石groupのwin、黒王石groupのloss、両王石同時captureのloss優先と入力順不変、versioned canonical text、null境界をproduction evaluatorで確認。通常配置3経路では盤面、ordered capture fact、登録topology、履歴、残存王石とderived resultのbindingを確認。
- `tests/Igorogue.Domain.Tests/PlacementLegalityEvaluatorTests.cs` — 反復不合法な王石captureはaccepted candidateを持たず、`CommitLegalPlacement`が拒否し、履歴を変更しないことを確認。
- `tests/Igorogue.Architecture.Tests/ArchitectureBoundaryTests.cs` — raw evaluatorがpublic Domain APIへ露出しないことを確認。
- 読み取り専用API、determinism／spec、独立scope review — 初回MEDIUMの公開API findingを内部core化で修正。他のrule priority、両王石、commit順、決定論、後続scope境界にはP1／P2 findingなし。

## Known issues

TASK-0007範囲の既知defectはなし。

正式な`KingCaptured`／`BattleWon`／`BattleLost` event payloadと順序、`CaptureBatch`、終局batchのbenefit suppression、mandatory expiry sweep、20-turn loss、battle state transition、golden replay／checksum、UI表示は対応する後続taskへ延期する。通常配置以外のatomic capture commitは、盤面・topology観測を確定した後に同じinternal evaluatorを呼ぶ必要がある。
