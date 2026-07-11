---
type: decision-needed
id: DECISION-0003
status: resolved
blocking: []
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

## Resolution

2026-07-11 — Accepted ruleを変更しない最小の運用判断としてOption 1を採用した。TASK-0023 merge後は[[TASK-0010 Headless Battle State Machine]]、[[TASK-0009 Golden Board Fixtures]]、[[TASK-0011 Replay Round Trip Verification]]の順で進める。

TASK-0009のgolden replay acceptanceをdirect Domain snapshotへ縮小せず、TASK-0010が提供するordered command、turn-boundary checksum、terminal resultを利用する。保存・読込・round tripはTASK-0011に残す。
