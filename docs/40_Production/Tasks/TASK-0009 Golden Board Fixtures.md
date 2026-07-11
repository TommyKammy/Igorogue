---
type: task
id: TASK-0009
status: blocked
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001, TASK-0023]
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

## Acceptance criteria

- 初期盤面、単純コウ、同時捕獲、施設付き領地、終着例外を最低1件ずつ含む。
- KO-01〜KO-07をRules Kernelのgolden replayへ移植する。
- FAC-01〜FAC-09をRules Kernelのunit testとgolden replayへ移植する。
- CIで全fixture一致。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

2026-07-11 — TASK-0008 closeout reviewで、FAC-01〜09の完全なunit／golden移植に必要なfacility runtime実装が現queueに存在しない計画gapを確認。専用task挿入またはacceptance分割のDecision Neededが解決するまで`blocked`を維持する。

2026-07-11 — [[DECISION-0001 Insert Facility Runtime Task Before Golden Fixtures]]のsmallest safe operational resolutionで専用[[TASK-0023 Implement Facility Runtime Semantics]]を挿入し、facility runtimeの未決を明示dependencyへ変換した。golden replayとheadless state machineの順序は[[DECISION-0003 Sequence Golden Replay After Battle State Machine]]が未解決のため`blocked`を維持する。

## Evidence

未作成。

## Known issues

FAC runtime依存はTASK-0023へ移管済みで、player-visible ruleの未決はない。TASK-0009を`ready`へ移すには、TASK-0023のreview／CI／人間mergeに加え、[[DECISION-0003 Sequence Golden Replay After Battle State Machine]]のowner decisionが必要である。

## Predefined specification sources

- FEAT-011 TLE-01〜TLE-15: 仮呼吸点同時失効、multiple group capture、王石gate、closed-window予約、mandatory topology revisit。
- M1ではevent sequenceとturn-boundary checksumをgolden化する。
