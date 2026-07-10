---
type: adr
status: accepted
project: Igorogue
updated: 2026-07-10
id: ADR-0002
---
# ADR-0002 Deterministic Gameplay and Replay

## Context

決定論的ゲームプレイ

## Decision

seed、コマンドログ、content hash、checksumを必須とする。

## Consequences

- 実装、データ、テストはこの決定へ従う。
- 変更には後継ADRを作り、本ADRをsupersededにする。
