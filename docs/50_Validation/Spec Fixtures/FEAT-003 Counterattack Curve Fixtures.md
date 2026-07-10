---
type: spec-fixtures
id: FEAT-003-FIXTURES
status: accepted
project: Igorogue
updated: 2026-07-10
source_feature: FEAT-003
source_decision: ADR-0013
---
# FEAT-003 Counterattack Curve Fixtures

[[FEAT-003 Komi Counterattack and Heat]]と[[ADR-0013 Baseline Pace and Burst-Driven Counterattack]]を、製品Rules Kernel実装前に固定する仕様fixtureである。

機械可読正本:

```text
game_data/fixtures/counterattack_curve_fixtures.json
```

## Curve fixtures

`CTR-01`〜`CTR-10`はコミ0〜9の開始unit、敵ターン終了増加、自然到達ターン、最初の追加行動ターンを検証する。

| ID | コミ | 開始点 | 毎敵ターン | 閾値到達後 | 追加行動 |
|---|---:|---:|---:|---:|---:|
| CTR-01 | 0 | 10 | 6 | 15 | 16 |
| CTR-02 | 1 | 12 | 6.5 | 14 | 15 |
| CTR-03 | 2 | 14 | 7 | 13 | 14 |
| CTR-04 | 3 | 16 | 7.5 | 12 | 13 |
| CTR-05 | 4 | 18 | 8 | 11 | 12 |
| CTR-06 | 5 | 20 | 8.5 | 10 | 11 |
| CTR-07 | 6 | 22 | 9 | 9 | 10 |
| CTR-08 | 7 | 24 | 9.5 | 8 | 9 |
| CTR-09 | 8 | 26 | 10 | 8 | 9 |
| CTR-10 | 9 | 28 | 10.5 | 7 | 8 |

## Event fixtures

| ID | 検証内容 |
|---|---|
| CTR-11 | コミ0のx3初回crossingで熱24点 |
| CTR-12 | コミ9の熱42点で閾値を超え、残り2点・Pending |
| CTR-13 | 同一ターン2回目のx3以上では熱なし |
| CTR-14 | x3未満では熱なし |
| CTR-15 | 攻め碁流5攻撃札でも過伸展は一ターン24点cap |
| CTR-16 | 他流儀の攻撃札では過伸展なし |
| CTR-17 | 捨て石流2石捕獲は余り2、増加なし |
| CTR-18 | 余り2から1石捕獲で1 batch、15点 |
| CTR-19 | 7石捕獲で2 batch、余り1 |
| CTR-20 | Pendingなしで閾値超過時、100点を一度引く |
| CTR-21 | Pending中の増加は閾値を引かずoverflow保持 |
| CTR-22 | 反攻1回解決後、overflowから次ターンをprime |
| CTR-23 | 敵ターン終了増加で超えた反攻は次敵ターン |
| CTR-24 | 戦闘開始でコミ式へresetし、流儀counterを初期化 |
| CTR-25 | 戦闘終了後の増加は無視 |

## M1 migration

M1ではcheckerの小型状態機械を製品コードとして流用しない。共有Rules Kernelへ同じ入力と期待event列を移植し、turn-boundary checksumをgolden化する。
