---
type: task
id: TASK-0011
status: review
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001, TASK-0010, TASK-0009]
updated: 2026-07-12
---
# TASK-0011 Replay Round Trip Verification

## Outcome

コマンドログ保存・再生・checksum検査。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]
- [[TASK-0002 Deterministic RNG and Command Log]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Allowed areas

- `src/Igorogue.Application/Replay/`のsave／load／replay orchestrationとschema validation。
- `tests/Igorogue.Application.Tests/`と`tests/golden/`のround-trip test。
- 本TASKとproduction state文書のEvidence同期。
- Domain rule、Content／`game_data/`、package／project reference、Godot assetは変更しない。

## Acceptance criteria

- versioned replay documentはmetadata、initial／final state・log checksum、terminal result、順序付きsubmitted Application command attempts、全attempt fieldを覆うversioned chain checksum、全envelope fieldを拘束するdocument checksumをUTF-8でsave／loadする。
- 各attemptはsequence、command type／schema、canonical payload、before state／log、accepted／reason、after state／log、attempt chain checksumを保持する。rejected attemptも保持するがaccepted-only `OrderedCommandLog`へ追加しない。
- callerが同一version／content／seedから再構築したinitial `HeadlessBattleSession`をchecksumで拘束し、保存commandだけを`HeadlessBattleStateMachine.Execute`へ入力する。任意history注入、mid-state snapshot復元、board直接mutationを行わない。
- 19 golden casesの34 Application attemptsについて全boundary、ordered facts、RNGを含むstate checksum、accepted-only log count／checksum、final state、terminal resultが一致する。5 zero-command casesはinitial-state save／load evidenceでありtransition replayと呼ばない。
- KO-07のadapter-only silent-filter traceをproduction replay commandへ偽装せず、選択されたwhite commandだけを保存する。
- 現4 command型（stone placement、facility build、end turn、enemy pass）のcanonical codecを検証する。
- schema ID／version、metadata identity／algorithm、content hash、command type／schema／payload、sequence、各checksum、accepted／reason、terminal driftを最初の差分でfail closedにする。部分的な成功sessionを返さない。
- 同一documentは同一UTF-8 bytesとなり、serialize→deserialize→replayを2回行って同一結果を得る。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Validation

- TASK-0009のgolden command列をserialize、deserialize、replayし、各boundary checksumとterminal resultを比較する。
- metadata／content hash／schema version／checksum mismatchをfail closedで検証する。
- `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を2回実行し、独立reviewを記録する。

## Execution log

2026-07-11 — [[DECISION-0003 Sequence Golden Replay After Battle State Machine]] Option 1に従い、TASK-0010とTASK-0009を明示dependencyへ追加。両taskのmergeまで`blocked`を維持する。

2026-07-12 — PR #13 merge commit `b2bfceca8bf88046aa100d563b62c6697d1afcd6`とpost-merge main CI run `29155541603`成功を確認。全dependency完了により`ready`へ遷移した。

2026-07-12 — Project ownerの継続指示をTASK-0011選択として記録。Outcome、Non-goals、Allowed areas、Acceptance、Validationを再確認し、変更をApplication Replay save／load／runner、Application tests／golden adapter、TASK／status docsに限定して`in_progress`へ遷移した。

2026-07-12 — versioned replay document、4 typed command codecs、strict Stream serializer、caller-supplied initial sessionからのrunnerを実装。34 submitted attemptsをaccepted-only command logと分離し、per-attempt chain／document checksum、16 MiB／4096 attemptsのresource limitを追加した。

2026-07-12 — 19 golden casesをserialize→deserialize→replayし、34 Application attempts（28 accepted／6 rejected）、5 zero-command cases、KO-07の3 submitted commands、ordered facts、state／log checksum、terminal resultを同一run 2回とsetup列挙反転で固定した。accept→reject→accept、enemy pass、schema／metadata／command／checksum／terminal drift、short-read、oversize、semantic divergenceのnegative testsも追加した。

2026-07-12 — preliminary adversarial reviewの巨大JSON path／`Split` resource amplificationと、attempt途中のsemantic divergence test不足を採用。bounded duplicate diagnostic、Span codec parser、serialization budget、checksumを再署名したattempt 1 acceptance mismatch testへ修正し、再reviewはBLOCKER／HIGH／MEDIUM findingなしで`APPROVE`。

2026-07-12 — `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を固定worktreeで各2回実行し全成功。実装commitを固定して独立fixed-HEAD reviewへ渡す。

2026-07-12 — fixed HEAD `61670f1682bef7ff61c62e5b22ab5c1f34afd807`の独立reviewで、4096 attempts上限がDTO materialization後に評価されるresource amplificationをHIGHとして検出。指摘を採用し、root `attempts`配列長をduplicate再帰／DTO変換より前に検査する順序へ修正し、duplicateを含む4097-entry inputで固定した。

2026-07-12 — 修正fixed HEAD `32226c661d159fd14de9620b9c2d2cbb8b286e84`を同じ独立担当が再review。findingなし、reviewer側でも`tools/dev/check`／259 tests／`sim-smoke`、worktree cleanを確認し`APPROVE`。全AcceptanceとEvidenceを満たしたため、人間merge待ちの`review`へ遷移した。

2026-07-12 — closeout docs-only HEAD `236446c23e5d379992befca516234a59e3cd9ca1`も独立review findingなし、`APPROVE`。draft PR #14を作成し、initial CI run `29170368075`のGovernance、Pure .NET、Godot／Windows export全job成功とmergeable cleanを確認した。

## Evidence

- `src/Igorogue.Application/Replay/BattleReplayDocument.cs`／`BattleReplaySerializer.cs`／`BattleReplayRunner.cs` — immutable versioned envelope、attempt／document integrity、strict bounded Stream I/O、canonical initial sessionからのfail-closed replay。
- `src/Igorogue.Application/Replay/BattleReplayCommandCodec.cs`／`ReplayValidationException.cs` — stone placement、facility build、end turn、enemy passのSpan-based schema v1 codecとstable failure diagnostics。
- `tests/Igorogue.Application.Tests/GoldenBoardFixtureAdapter.cs`／`BattleReplayRoundTripTests.cs` — 19 cases、34 attempts（28 accepted／6 rejected）、5 zero-command cases、KO-07＝3 commands、4 codecs、facts／RNG state／log／terminal exact、deterministic UTF-8 bytes、2 replay runs、resource／tamper／semantic negativesを検証。
- `tools/dev/test` ×2 — exit 0。.NET SDK 8.0.422／macOS arm64、build警告0／error 0。Domain 190、Application 54、Architecture 15、計259 tests成功。
- `tools/dev/check` ×2 — exit 0。全governance／documentation／fixture check成功。content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`。
- `tools/dev/sim-smoke` ×2 — exit 0。両runでchecksum `3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`一致。
- preliminary independent contract／stream review — resource amplificationとrunner途中差分testの指摘を修正。再reviewはBLOCKER／HIGH／MEDIUM findingなし、`APPROVE`。
- 独立fixed-HEAD review — `61670f1`はattempt countの検査順にHIGH 1件で`CHANGES REQUIRED`。修正後`32226c661d159fd14de9620b9c2d2cbb8b286e84`はfindingなし、`APPROVE`。reviewer側でもcheck／259 tests／sim-smoke／clean worktreeを再確認。
- GitHub draft PR #14 initial CI run `29170368075` — Governance job `86590470442`、Pure .NET job `86590484518`、Godot .NET headless／Windows debug export job `86590543431`すべてsuccess。head `236446c23e5d379992befca516234a59e3cd9ca1`、mergeable clean。
- `git diff --check` — exit 0。

## Known issues

既知実装issueなし。initial board／facility／runtime policyはcallerがcanonical contentから再構築したsessionとして渡し、documentのinitial checksumで拘束する。standalone mid-battle snapshot saveは本TASKの非対象。
