---
type: dashboard
status: active
project: Igorogue
updated: 2026-07-11
cssclasses:
  - igorogue-dashboard
---
# Igorogue Project Hub

> [!abstract] 現在の目的
> [[TASK-0010 Headless Battle State Machine]]の決定的Application境界は実装・独立review済み。CI／人間mergeで閉じる。

> [!success] 実装ゲート
> Gate 1は基盤と利用側を並列化せず、TASK-0002からTASK-0011まで依存順に進める。[[Current Development State]]と[[Codex Task Queue]]を参照。

## 正本

- [[Project Brief]]
- [[Design Pillars]]
- [[Rules Canon]]
- [[Integrated v0.2 Scope]]
- [[Source of Truth Map]]

## 開発

- [[Development Plan]]
- [[Milestones and Exit Gates]]
- [[Current Sprint]]
- [[Backlog]]
- [[Risk Register]]
- [[Codex Mac Handoff]]
- [[Codex App Operating Procedure]]
- [[Codex Task Queue]]
- [[Codex Operating Model]]

## 検証

- [[Balance Targets]]
- [[Validation Strategy]]
- [[Simulation Index]]
- [[Playtest Index]]
- [[SIM-0001 Reference Proxy Baseline]]

## UI/UX

- [[UI UX Overview]]
- [[Mockup Gallery]]
- [[Visual Style Guide]]

## Latest accepted technical decision

- [[ADR-0001 Engine and Repository]] — Godot 4.7 stable .NET／C#／pure .NET Rules Kernel
- [[Engine Toolchain and Repository Layout]]

## Remaining decisions

- ローカライズ対応範囲

## 直近タスク

- [[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]] — done
- [[TASK-0001 Decide Engine and Repository]] — done
- [[TASK-0020 Review Repository Bootstrap Runtime Evidence]] — done
- [[TASK-0002 Deterministic RNG and Command Log]] — done
- [[TASK-0003 Board Coordinates and Orthogonal Neighbours]] — done
- [[TASK-0004 Stone Groups and Unique Liberty Sets]] — done
- [[TASK-0005 Hypothetical Placement and Capture Resolution]] — done
- [[TASK-0006 Suicide Legality and Terminal Capture]] — done
- [[TASK-0007 King Capture and Battle Result]] — done
- [[TASK-0008 Territory Region Calculation]] — done
- [[DECISION-0001 Insert Facility Runtime Task Before Golden Fixtures]] — resolved
- [[TASK-0023 Implement Facility Runtime Semantics]] — done
- [[DECISION-0002 Resolve Territory and Facility Event Order]] — resolved
- [[DECISION-0003 Sequence Golden Replay After Battle State Machine]] — resolved
- [[TASK-0010 Headless Battle State Machine]] — review／current
- [[TASK-0009 Golden Board Fixtures]] — blocked／TASK-0010
- FEAT-009 independent two-person paper sign-off

## Latest design repairs

- [[FEAT-002 Momentum]]
- [[FEAT-002 Momentum Gate Fixtures]]
