---
type: adr
status: accepted
project: Igorogue
updated: 2026-07-10
id: ADR-0005
---
# ADR-0005 Shared Kernel for Game and Simulation

## Context

共通Rules Kernel

## Decision

ライブ、リプレイ、Bot、正式シミュレーターは同じDomain実装を使う。

## Consequences

- 実装、データ、テストはこの決定へ従う。
- 変更には後継ADRを作り、本ADRをsupersededにする。
