---
type: task
id: TASK-0006
status: ready
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001]
updated: 2026-07-11
---
# TASK-0006 Suicide Legality and Terminal Capture

## Outcome

自殺手禁止とカード指定終着例外。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]
- [[ADR-0011 Battle-Local Stone Topology Repetition Ban]]
- [[ADR-0011 Board Repetition Fixtures]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- 捕獲で呼吸点が生まれる手は合法。
- 履歴済み`StoneTopologyKey`を再現する配置は不合法。
- 反復不合法手でコスト、盤面、カード、トリガー、履歴が変わらない。
- KO-01〜KO-07を共有Rules Kernelへ移植する。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

2026-07-11 — TASK-0005の独立review、green CI、PR #6の人間mergeを確認し、直列Gate 1の次タスクとして`ready`へ遷移。

## Evidence

未作成。

## Known issues

盤面反復の設計規則はAccepted。製品実装はTASK-0005の仮配置・捕獲解決に依存する。
