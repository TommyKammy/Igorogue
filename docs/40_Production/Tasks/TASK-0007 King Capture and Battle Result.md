---
type: task
id: TASK-0007
status: ready
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

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- 通常石捕獲と王石捕獲を区別。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

2026-07-11 — TASK-0006の独立review、green CI、PR #7の人間mergeを確認し、直列Gate 1の次タスクとして`ready`へ遷移。

## Evidence

未作成。

## Known issues

なし。
