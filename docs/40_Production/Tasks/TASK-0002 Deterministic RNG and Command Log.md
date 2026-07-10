---
type: task
id: TASK-0002
status: blocked
project: Igorogue
milestone: M0
priority: high
dependencies: [TASK-0001]
updated: 2026-07-10
---
# TASK-0002 Deterministic RNG and Command Log

## Outcome

seed付きRNGと順序付きコマンドログの最小実装。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- 同一seedと入力で同一出力、checksum一致。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

未着手。

## Evidence

未作成。

## Known issues

なし。
