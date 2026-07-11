---
type: task
id: TASK-0004
status: ready
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

## Evidence

未作成。

## Known issues

なし。
