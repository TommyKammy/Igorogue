---
type: task
id: TASK-0005
status: ready
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001]
updated: 2026-07-11
---
# TASK-0005 Hypothetical Placement and Capture Resolution

## Outcome

仮配置と相手同時捕獲を実装。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- 複数群同時捕獲と安定イベント順。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

2026-07-11 — TASK-0004の独立review、green CI、PR #5の人間mergeを確認し、直列Gate 1の次タスクとして`ready`へ遷移。

## Evidence

未作成。

## Known issues

なし。
