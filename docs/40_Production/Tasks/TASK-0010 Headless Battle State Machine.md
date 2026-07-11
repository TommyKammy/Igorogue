---
type: task
id: TASK-0010
status: blocked
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001, TASK-0023]
updated: 2026-07-11
---
# TASK-0010 Headless Battle State Machine

## Outcome

ターンとコマンドの最小状態機械。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]
- [[DECISION-0002 Resolve Territory and Facility Event Order]]
- [[TASK-0023 Implement Facility Runtime Semantics]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- UIなしで決着まで実行可能。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

2026-07-11 — [[DECISION-0002 Resolve Territory and Facility Event Order]]のglobal event order決定と、TASK-0023が提供するfacility-aware composite placement seamを待つため`blocked`を維持。raw `LegalPlacementCommit`からfacility順を独自publishしてはならない。

## Evidence

未作成。

## Known issues

[[DECISION-0002 Resolve Territory and Facility Event Order]]のowner decisionが必要。TASK-0023 merge前はfacility-aware state transitionを統合できない。
