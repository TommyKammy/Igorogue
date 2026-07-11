---
type: task
id: TASK-0008
status: ready
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001]
updated: 2026-07-11
---
# TASK-0008 Territory Region Calculation

## Outcome

空点領域と隣接色による領地判定。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]
- [[FEAT-001 Territory and Facilities]]
- [[ADR-0012 Facility Sites Are Empty Intersections]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- 隅、辺、中立、石なし領域。
- 施設点を通常の空点としてflood fill、領地サイズ、隣接色へ含める。
- 施設有無だけで領地結果と実呼吸点が変わらないunit test。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

2026-07-11 — TASK-0007の独立review、green CI、PR #8の人間mergeを確認し、直列Gate 1の次タスクとして`ready`へ遷移。

## Evidence

未作成。

## Known issues

なし。
