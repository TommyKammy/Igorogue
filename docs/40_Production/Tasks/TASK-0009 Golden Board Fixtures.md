---
type: task
id: TASK-0009
status: blocked
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
- KO-01〜KO-07をRules Kernelのgolden replayへ移植する。
- FAC-01〜FAC-09をRules Kernelのunit testとgolden replayへ移植する。
- CIで全fixture一致。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Validation

- TASK-0010／0024のcanonical command／state checksum／terminal resultを使い、direct Domain snapshotをgolden replayと呼ばない。
- fixture input順反転、expected fact順、turn-boundary checksum、terminal resultをCIで固定する。
- `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を2回実行し、独立reviewを記録する。

## Execution log

2026-07-11 — TASK-0008 closeout reviewで、FAC-01〜09の完全なunit／golden移植に必要なfacility runtime実装が現queueに存在しない計画gapを確認。専用task挿入またはacceptance分割のDecision Neededが解決するまで`blocked`を維持する。

2026-07-11 — [[DECISION-0001 Insert Facility Runtime Task Before Golden Fixtures]]のsmallest safe operational resolutionで専用[[TASK-0023 Implement Facility Runtime Semantics]]を挿入し、facility runtimeの未決を明示dependencyへ変換した。golden replayとheadless state machineの順序は[[DECISION-0003 Sequence Golden Replay After Battle State Machine]]が未解決のため`blocked`を維持する。

2026-07-11 — TASK-0023 mergeと[[DECISION-0003 Sequence Golden Replay After Battle State Machine]] Option 1解決を反映。true replayの実行基盤であるTASK-0010をdependencyへ追加し、同taskのmergeまで`blocked`を維持する。

2026-07-11 — TASK-0010のPR #11人間mergeとpost-merge main CI成功を確認。到達性監査でKO-03／04／07、FAC-05／08／09は全fixture inputを現Application commandとして実行できないことを確認した。FAC buildの[[TASK-0024 Authorized Facility Build Battle Command]]をdependencyへ追加し、[[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]]のowner decisionまで`blocked`を維持する。Acceptanceは変更していない。

## Evidence

未作成。

## Known issues

[[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]]がopenである。TASK-0009を`ready`へ移すにはowner decisionとTASK-0024のreview／CI／人間mergeが必要である。

## Predefined specification sources

- FEAT-011 TLE-01〜TLE-15: 仮呼吸点同時失効、multiple group capture、王石gate、closed-window予約、mandatory topology revisit。
- M1ではevent sequenceとturn-boundary checksumをgolden化する。
