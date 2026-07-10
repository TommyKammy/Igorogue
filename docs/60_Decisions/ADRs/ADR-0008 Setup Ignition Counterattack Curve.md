---
type: adr
status: superseded
project: Igorogue
updated: 2026-07-10
id: ADR-0008
superseded_by: ADR-0013
---
# ADR-0008 Setup Ignition Counterattack Curve

## Context

仕込み・発火・反攻

## Decision

急加速を止めず、その直後の敵圧を増やして決着判断を作る。

## Superseded

[[ADR-0013 Baseline Pace and Burst-Driven Counterattack]]が数値、熱、pending、overflowをAccepted化したため、本ADRは履歴として保持する。

## Consequences

- 実装、データ、テストはこの決定へ従う。
- 変更には後継ADRを作り、本ADRをsupersededにする。
