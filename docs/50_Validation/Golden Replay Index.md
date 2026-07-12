---
type: golden-index
status: active
project: Igorogue
updated: 2026-07-12
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
- [[FEAT-011 Temporary Liberty Expiry Fixtures]]: TLE-01〜TLE-15
- `game_data/fixtures/temporary_liberty_expiry_fixtures.json`

M1では仕様checkerだけで完了とせず、共有Rules Kernelのunit evidence、event列、turn-boundary checksumをgolden化する。

## Versioned runtime evidence

- `tests/golden/v1/board_fixture_cases.json`は`headless-battle-state-v1`／replay schema 1の既存正本であり、TASK-0029では内容を変更しない。SHA-256は`b3e62c12574746233e1d829e4f30fcc179559cae017fcdd707a656e63b01655d`。
- `tests/golden/v2/temporary_liberty_cases.json`は`headless-battle-state-v2`／replay schema 2のTLE-01〜15 Application evidence。catalog SHA-256は`9f6486d9776ec05a0c6972f6fdb1ab6dfc49cdd5c653b05831a83216dea8d180`、source fixture SHA-256は`9f9a74ee9e1407c2b0882b6ccd1aa86ae950dd750fb0bfb4bc3bf12faae20e60`。
- schema 1はlegacy state v1だけ、schema 2はauthoritative state v2だけを受理する。両方向のprojection混入をfail-closedで拒否し、各schemaのvalid serialized bytesを共有しない。
- TLE v2はcanonical initial snapshot、通常Application command、各attempt boundary、ordered facts、terminal resultを固定する。TLE-12のmandatory expiry領地は`temporary_liberty_expiry` sourceかつimplicit Momentum非対象として記録する。
- TLE v2のclaimはMomentum event 0、Brilliant event 0、CTR-01〜25 coverage 0。`tools/dev/sim-smoke`のchecksumをformal board evidenceへ流用しない。

## M3への移植元

- [[FEAT-002 Momentum Gate Fixtures]]: MOM-01〜MOM-19
- `game_data/fixtures/momentum_gate_fixtures.json`
- [[FEAT-003 Counterattack Curve Fixtures]]: CTR-01〜CTR-25
- `game_data/fixtures/counterattack_curve_fixtures.json`

M3でMomentum／counterattackのproduction implementationを行うとき、仕様checkerの小型モデルを製品コードとして流用せず、共有Rules Kernelのunit／golden evidenceへ移植する。
