---
type: task
id: TASK-0018
status: done
milestone: M-1
priority: P0
project: Igorogue
updated: 2026-07-10
---
# TASK-0018 Define Temporary Liberty Expiry and Capture Sweep

## Outcome

B-2の仮呼吸点失効、同時capture、王石結果、capture trigger、closed-window予約、反攻境界順をAccepted仕様とfixtureへ固定する。

## Source

- Fable 5 Action Backlog B-2
- [[Rules Canon]]
- [[Combat Resolution Order]]
- [[FEAT-005 Sacrifice Triggers]]
- [[FEAT-003 Komi Counterattack and Heat]]

## In scope

- ADR-0014
- FEAT-011
- FEAT-005実仕様化
- TLE-01〜TLE-15
- specification checker
- Rules / Domain / Event / Replay / Save / Testing同期

## Out of scope

- 製品Rules Kernel
- board-mutating capture trigger
- 種石成長
- 厚みトークンの個別獲得量調整
- 人間UX検証

## Acceptance evidence

- 全due effectを一括失効する。
- 全doomed groupを同一snapshotから同時captureする。
- 黒王石loss、白王石win、両王石lossをfixture化する。
- terminal batchの利益抑止をfixture化する。
- enemy-window draw / qi / choice予約をfixture化する。
- mandatory topology revisitをfixture化する。
- 失効capture由来の犠牲反攻が基礎反攻増加より先である。
- `python tools/check_all.py`が成功する。

## Evidence

- [[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]
- [[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]
- [[FEAT-011 Temporary Liberty Expiry Fixtures]]
- `game_data/fixtures/temporary_liberty_expiry_fixtures.json`
- `tools/check_temporary_liberty.py`
