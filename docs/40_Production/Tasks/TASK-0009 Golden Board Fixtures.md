---
type: task
id: TASK-0009
status: blocked
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001]
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

## Evidence

未作成。

## Known issues

FAC-03／04／08と、FAC-05／06／07／09のruntime部分には、未実装の`FacilityInstance`、build、destroy、operating state、capacity、event解決が必要である。TASK-0009を`ready`へ移す前に、専用facility runtime TASKを挿入するか、本タスクのacceptanceをstone-layer projectionと後続runtime goldenへ分割するDecision Neededが必要である。

## Predefined specification sources

- FEAT-011 TLE-01〜TLE-15: 仮呼吸点同時失効、multiple group capture、王石gate、closed-window予約、mandatory topology revisit。
- M1ではevent sequenceとturn-boundary checksumをgolden化する。
