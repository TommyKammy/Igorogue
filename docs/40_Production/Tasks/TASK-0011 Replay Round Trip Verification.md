---
type: task
id: TASK-0011
status: blocked
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001, TASK-0010, TASK-0009]
updated: 2026-07-11
---
# TASK-0011 Replay Round Trip Verification

## Outcome

コマンドログ保存・再生・checksum検査。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Allowed areas

- `src/Igorogue.Application/Replay/`のsave／load／replay orchestrationとschema validation。
- `tests/Igorogue.Application.Tests/`と`tests/golden/`のround-trip test。
- 本TASKとproduction state文書のEvidence同期。
- Domain rule、Content／`game_data/`、package／project reference、Godot assetは変更しない。

## Acceptance criteria

- 保存前後で最終状態一致。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Validation

- TASK-0009のgolden command列をserialize、deserialize、replayし、各boundary checksumとterminal resultを比較する。
- metadata／content hash／schema version／checksum mismatchをfail closedで検証する。
- `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を2回実行し、独立reviewを記録する。

## Execution log

2026-07-11 — [[DECISION-0003 Sequence Golden Replay After Battle State Machine]] Option 1に従い、TASK-0010とTASK-0009を明示dependencyへ追加。両taskのmergeまで`blocked`を維持する。

## Evidence

未作成。

## Known issues

TASK-0010とTASK-0009のmerge待ち。
