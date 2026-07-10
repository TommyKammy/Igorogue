---
type: adr
status: accepted
project: Igorogue
updated: 2026-07-10
id: ADR-0010
---
# ADR-0010 Runtime Data Ownership

## Context

ランタイム値の正本

## Decision

現在値はgame_data、意図と範囲はObsidianへ置く。

## Consequences

- 実装、データ、テストはこの決定へ従う。
- 変更には後継ADRを作り、本ADRをsupersededにする。
