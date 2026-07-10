---
type: task
id: TASK-0009
status: blocked
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001]
updated: 2026-07-10
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

未着手。

## Evidence

未作成。

## Known issues

なし。

## Predefined specification sources

- FEAT-011 TLE-01〜TLE-15: 仮呼吸点同時失効、multiple group capture、王石gate、closed-window予約、mandatory topology revisit。
- M1ではevent sequenceとturn-boundary checksumをgolden化する。

