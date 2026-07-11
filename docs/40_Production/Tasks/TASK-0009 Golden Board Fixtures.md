---
type: task
id: TASK-0009
status: review
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001, TASK-0023, TASK-0010, TASK-0024]
updated: 2026-07-11
---
# TASK-0009 Golden Board Fixtures

## Outcome

代表盤面fixtureと期待イベントを保存。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]
- [[ADR-0011 Board Repetition Fixtures]]
- [[ADR-0012 Facility Intersection Fixtures]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Allowed areas

- `tests/golden/`のversioned replay fixtureと、fixtureを共有Rules Kernelへ入力するtest adapter。
- KO-01〜07、FAC-01〜09、初期盤面、同時捕獲、終着例外の既存fixture参照。
- 本TASKとproduction state文書のEvidence同期。
- production rule、Content／`game_data/`、package／project reference、Godot assetは変更しない。

## Acceptance criteria

- 初期盤面、単純コウ、同時捕獲、施設付き領地、終着例外を最低1件ずつ含む。
- versioned golden suiteはbuild/schema version、content hash、seed、source fixture／evidence分類、ordered scripted commands、initial／各attempt boundary state・log checksum、ordered facts、terminal resultを持つ。
- KO-01〜KO-07のexact Domain evidenceを維持し、[[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]]で定義したtrue replay／metadata normalization／silent-filter adapterをgolden suiteへ移植する。
- FAC-01〜FAC-09のexact Domain evidenceを維持し、同Decisionで定義したinitial-state evidence、canonical command replay、FAC-05 linked semantic replayをgolden suiteへ移植する。
- rejected commandはscripted inputへ含めるがaccepted-only `OrderedCommandLog`へ追加せず、state／log checksum exact no-opを固定する。KO-07の除外候補はbattle commandとして実行せず、silent traceと選択commandを分離する。
- CIで全fixture一致。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Validation

- TASK-0010／0024のcanonical command／state checksum／terminal resultを使い、direct Domain snapshotをgolden replayと呼ばない。
- fixture input順反転、同一run 2回、expected fact順、全command-boundary checksum、terminal resultをCIで固定する。candidate優先順とcommand順は意味を持つため反転しない。
- `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を2回実行し、独立reviewを記録する。

## Execution log

2026-07-11 — TASK-0008 closeout reviewで、FAC-01〜09の完全なunit／golden移植に必要なfacility runtime実装が現queueに存在しない計画gapを確認。専用task挿入またはacceptance分割のDecision Neededが解決するまで`blocked`を維持する。

2026-07-11 — [[DECISION-0001 Insert Facility Runtime Task Before Golden Fixtures]]のsmallest safe operational resolutionで専用[[TASK-0023 Implement Facility Runtime Semantics]]を挿入し、facility runtimeの未決を明示dependencyへ変換した。golden replayとheadless state machineの順序は[[DECISION-0003 Sequence Golden Replay After Battle State Machine]]が未解決のため`blocked`を維持する。

2026-07-11 — TASK-0023 mergeと[[DECISION-0003 Sequence Golden Replay After Battle State Machine]] Option 1解決を反映。true replayの実行基盤であるTASK-0010をdependencyへ追加し、同taskのmergeまで`blocked`を維持する。

2026-07-11 — TASK-0010のPR #11人間mergeとpost-merge main CI成功を確認。到達性監査でKO-03／04／07、FAC-05／08／09は全fixture inputを現Application commandとして実行できないことを確認した。FAC buildの[[TASK-0024 Authorized Facility Build Battle Command]]をdependencyへ追加し、[[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]]のowner decisionまで`blocked`を維持する。Acceptanceは変更していない。

2026-07-11 — PR #12の人間merge commit `acafc3215434fbafa3e2acbef19649ea9c0a66f4`とpost-merge main CI run `29153064894`の全3 job成功を確認。TASK-0024 dependencyを完了した。

2026-07-11 — Project ownerの継続指示を[[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]]推奨Option 1の選択として記録。ADR-0011／0012とgolden contractへevidence分類を同期し、Rules Canon／canonical fixture期待値を変更せず`ready`へ遷移した。

2026-07-11 — Outcome、Non-goals、Allowed areas、Acceptance、Validationを再確認。変更を`tests/golden/`、Application test adapter／tests、Accepted evidence分類、production status文書に限定して`in_progress`へ遷移した。

2026-07-11 — `tests/golden/v1/board_fixture_cases.json` schema v1、Application test adapter、contract／replay／determinism testsを実装。KO-01〜07、FAC-01〜09、canonical initial、simultaneous capture、terminal captureの19 cases／35 scripted boundariesを固定し、6 rejected commandsとKO-07 silent filterをaccepted-only logから分離した。

2026-07-11 — preliminary documentation auditのREADME content-hash契約漏れを修正。catalog content hash、runtime policy、source SHA-256、unit source実在、KO-07 chosen-point→submitted-command接続をcontract testで固定した。

2026-07-11 — preliminary code auditのKO-03／04 metadata normalization、KO-07 runtime handoff、全19 source／evidence mapping固定の指摘を採用。source metadata値をschemaへ構造化しpinned sourceと機械照合、silent choiceを直後のwhite commandへ引き渡して照合、全case mapping tableをexact固定した。content-hash指摘は先行修正で解消済み。

2026-07-11 — `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を各2回実行し全成功。実装とvalidation完了後、独立fixed-HEAD reviewへ渡すため`review`へ遷移した。

## Evidence

- `tests/golden/v1/board_fixture_cases.json` — schema v1、SHA-256 `5a0078fb9624ad9719266ab8be5a7d13aac54c2af453aca9216dfb6305d0e444`、19 cases、35 boundaries、6 rejected attempts、1 silent-filter boundary。
- `tests/Igorogue.Application.Tests/GoldenBoardFixtureAdapter.cs`／`GoldenBoardFixtureTests.cs` — source hash／evidence contract、全boundary checksum／ordered fact、terminal result、same-run twice、reversed setup enumerationを検証。
- `tools/dev/test` ×3 — exit 0。Domain 190、Application 36、Architecture 15、計241 tests成功。build警告0／error 0。
- `tools/dev/check` ×3 — exit 0。全governance／documentation／fixture check成功。content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`。
- `tools/dev/sim-smoke` ×2 — exit 0。両runでchecksum `3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`一致。

## Known issues

現時点の既知実装blockerなし。TASK-0011はTASK-0009の独立reviewと人間mergeまで開始しない。production rule、Content／`game_data/`、package／project reference、Godot assetは変更していない。

## Predefined specification sources

- FEAT-011 TLE-01〜TLE-15: 仮呼吸点同時失効、multiple group capture、王石gate、closed-window予約、mandatory topology revisit。
- M1ではevent sequenceとturn-boundary checksumをgolden化する。
