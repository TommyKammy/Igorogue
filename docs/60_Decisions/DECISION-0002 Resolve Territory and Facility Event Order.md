---
type: decision-needed
id: DECISION-0002
status: open
blocking: [TASK-0010]
updated: 2026-07-11
---
# DECISION-0002 Resolve Territory and Facility Event Order

## Why work is blocked

[[TASK-0010 Headless Battle State Machine]]でterritory delta、facility state change、Momentumを一つのordered event streamへ統合する前に、Accepted仕様間の順序を一義化する必要がある。

## Conflicting sources

[[FEAT-001 Territory and Facilities]]はterritory recalculation後にfacility state changes、`TerritoryEstablished`、Momentumの順を示す。[[FEAT-002 Momentum]]は`TerritoryEstablished`、facility state changes、Momentumの順を示す。[[Combat Resolution Order]]も両表現を一つに確定していない。

## Options

1. FEAT-001を正としてfacility state changesを`TerritoryEstablished`より前に置く。
2. FEAT-002を正として`TerritoryEstablished`をfacility state changesより前に置く。

## Smallest safe default

[[TASK-0023 Implement Facility Runtime Semantics]]はfacility factsの内部順だけを固定し、`TerritoryEstablished`とMomentumを実装しない。TASK-0010は本decision解決まで`blocked`を維持する。

## Owner decision

未決。
