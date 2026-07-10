---
type: golden-index
status: active
project: Igorogue
updated: 2026-07-10
---
# Golden Replay Index

実装後、以下のカテゴリで固定fixtureを作る。

- [[Coordinate System and Initial Position Fixtures|座標変換・点対称な初期盤面]]
- 単石捕獲
- [[ADR-0011 Board Repetition Fixtures|盤面反復・単純コウ]]
- 複数群同時捕獲
- 終着例外
- [[FEAT-011 Temporary Liberty Expiry Fixtures|仮呼吸点同時失効・王石gate・予約trigger]]
- 領地分割・結合
- 施設停止・再稼働・破壊
- [[FEAT-002 Momentum Gate Fixtures|余勢生成・消費・前進ドロー]]
- 妙手複合倍率
- 白王石厚み消費
- [[FEAT-003 Counterattack Curve Fixtures|反攻曲線・熱・Pending・overflow]]


## M1への移植元

- [[Coordinate System and Initial Position Fixtures]]: COORD-01〜COORD-12
- `game_data/fixtures/coordinate_system_fixtures.json`
- [[ADR-0011 Board Repetition Fixtures]]: KO-01〜KO-07
- `game_data/fixtures/board_repetition_fixtures.json`
- [[ADR-0012 Facility Intersection Fixtures]]: FAC-01〜FAC-09
- `game_data/fixtures/facility_intersection_fixtures.json`

- [[FEAT-002 Momentum Gate Fixtures]]: MOM-01〜MOM-19
- `game_data/fixtures/momentum_gate_fixtures.json`

M1では仕様checkerだけで完了とせず、共有Rules Kernelのイベント列とturn-boundary checksumをgolden化する。

- [[FEAT-003 Counterattack Curve Fixtures]]: CTR-01〜CTR-25
- `game_data/fixtures/counterattack_curve_fixtures.json`
- [[FEAT-011 Temporary Liberty Expiry Fixtures]]: TLE-01〜TLE-15
- `game_data/fixtures/temporary_liberty_expiry_fixtures.json`
