---
type: decision-needed
id: DECISION-0003
status: open
blocking: [TASK-0009]
updated: 2026-07-11
---
# DECISION-0003 Sequence Golden Replay After Battle State Machine

## Why work is blocked

[[TASK-0009 Golden Board Fixtures]]が要求するgolden replayはordered commands、turn-boundary checksum、terminal resultを必要とするが、その実行基盤である[[TASK-0010 Headless Battle State Machine]]が現queueでは後続に置かれている。

## Conflicting sources

現在の[[Codex Task Queue]]はTASK-0009をTASK-0010より前に置く。一方、`tests/golden/README.md`のgolden replay契約とTASK-0010のOutcomeから、true replay生成にはheadless state machineが先に必要である。

## Options

1. TASK-0023の後をTASK-0010、TASK-0009、TASK-0011の順へ並べ替える。
2. TASK-0009をdirect Domain golden snapshotへ縮小し、true replay casesをTASK-0011へ移す。

## Smallest safe default

TASK-0009を`blocked`に保ち、direct unit snapshotをgolden replayと呼ばない。TASK-0023は本decisionに依存しない。

## Owner decision

未決。
