---
type: task
id: TASK-0003
status: blocked
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001]
updated: 2026-07-10
---
# TASK-0003 Board Coordinates and Orthogonal Neighbours

## Outcome

7×7座標と上下左右隣接を純粋関数で実装。

## Source of truth

- [[Rules Canon]]
- [[Coordinate System and Initial Position]]
- [[Coordinate System and Initial Position Fixtures]]
- [[Architecture]]
- [[Determinism and Replay]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- CanonicalPoint `(1..7,1..7)`とInternalPoint `(0..6,0..6)`の往復変換。
- canonical index `0..48`との往復変換。
- 盤外値をclampせず拒否。
- 隅2、辺3、中央4の隣接テスト。
- `reflect(reflect(p)) == p`のproperty test。
- `standard_v0_2`初期盤面が色・役割交換付き点対称で、各王石グループが3石・実呼吸点7。
- COORD-01〜COORD-12を共有Rules Kernelのunit testへ移植。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

未着手。

## Evidence

未作成。

## Known issues

なし。
