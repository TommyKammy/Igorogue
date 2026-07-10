---
type: spec-fixture
status: active
project: Igorogue
feature: FEAT-009
updated: 2026-07-10
---
# FEAT-009 Enemy Decision Fixtures

座標契約は[[Coordinate System and Initial Position]]を参照する。

[[FEAT-009 Enemy Action Planning and Placement]]の行動表を、Rules Kernel実装前に紙上検証するためのフィクスチャ。

## Review protocol

1. レビュアーはFEAT-009と当該ケースだけを読む。
2. 候補生成済み表から、強制上書き、意図優先度、辞書式score、Canonical point orderを順に適用する。
3. `Expected`を隠した状態で意図と着点を記録する。
4. 2名が同じ結果ならHuman sign-offへ記入する。
5. 不一致時は推測で合わせず、FEAT-009を修正する。

機械検査用の同値データは`game_data/fixtures/enemy_behavior_decision_fixtures.json`。

## F09-01 山賊棋士・初期盤面の進行

初期盤面から、黒が第1ターンに`(4,2)`へ打つものとする。戦闘開始時の計画はプレイヤー操作前に生成済み。

### 計画時候補

`capture_black_king`、`defend_white_king`、`capture_non_king`、`pressure_black_king`は候補なし。

| point | 黒王石呼吸点までの距離 | 配置後白グループ呼吸点 | 中央距離 |
|---|---:|---:|---:|
| (6,4) | 4 | 9 | 2 |
| (4,6) | 4 | 9 | 2 |
| (5,5) | 4 | 8 | 2 |

**Expected**: `advance_toward_black_king` at `(6,4)`。`(6,4)`と`(4,6)`の最終同率を`(y,x)`昇順で解決する。

### 紙上3行動

| moment | player fixed action | expected enemy plan / action |
|---|---|---|
| 戦闘開始 | 未行動 | `advance_toward_black_king (6,4)` |
| 敵1解決後 | 黒 `(4,2)` | 次計画 `advance_toward_black_king (6,3)` |
| 敵2解決後 | 黒 `(5,2)` | 次計画 `pressure_black_king (6,2)` |

## F09-02 山賊棋士・捕獲は圧迫より優先

強制上書き条件なし。候補生成結果を次とする。

| intent | point | sort key（小さいほど優先） |
|---|---|---|
| `capture_non_king` | (4,3) | [-2, 1, -3, 3, 4] |
| `capture_non_king` | (5,4) | [-1, 0, -4, 4, 5] |
| `pressure_black_king` | (3,2) | [2, -4, -1, 2, 3] |

**Expected**: 優先意図`capture_non_king`、着点`(4,3)`。2石捕獲を1石捕獲より優先する。

## F09-03 山賊棋士・処刑の強制上書き

計画意図は`advance_toward_black_king`。プレイヤー行動後、黒王石を捕獲する候補が発生した。

| intent | point | sort key |
|---|---|---|
| `capture_black_king` | (2,3) | [-3, -2, 3, 2] |
| `advance_toward_black_king` | (5,4) | [2, -5, 1, 4, 5] |

**Expected**: `capture_black_king (2,3)`。`override_reason = mandatory_lethal_override`。

## F09-04 侵入者・施設踏破の領地順位

活動中アンカーなし、cooldown 0、遠隔侵入未使用。

| intent | point | 対象領地収入 | 稼働施設 | 領地サイズ | 配置後呼吸点 |
|---|---|---:|---:|---:|---:|
| `trample_facility` | (3,3) | 4 | 2 | 6 | 3 |
| `trample_facility` | (6,2) | 3 | 1 | 8 | 4 |
| `invade_largest_territory` | (4,4) | 4 | 2 | 6 | 4 |

**Expected**: `trample_facility (3,3)`。同じ高収入領地では、施設を直接破壊できる意図が領地侵入より先。

## F09-05 侵入者・侵入石の延命

活動中アンカーを含む白グループが存在する。同時に別領地の施設踏破候補も存在する。

| intent | point | sort key |
|---|---|---|
| `escape_active_invasion` | (4,5) | [-4, 1, 0, 5, 4] |
| `escape_active_invasion` | (5,4) | [-3, 0, 0, 4, 5] |
| `trample_facility` | (2,6) | [-5, -2, -7, -3, 6, 2] |

**Expected**: `escape_active_invasion (4,5)`。計画優先度が施設価値より先に適用される。

## F09-06 侵入者・単石の囮を無視

強制上書き条件なし。1石捕獲候補と黒領地侵入候補がある。

| intent | point | captured stones / other key |
|---|---|---|
| `capture_non_king` | (3,4) | 1石。`opportunistic_capture_min_stones=2`未満のため候補から除外 |
| `invade_largest_territory` | (5,3) | 合法候補 |

**Expected**: `invade_largest_territory (5,3)`。

## F09-07 同意図内の再ターゲット

計画時は`trample_facility (3,3)`。プレイヤーが当該施設を自ら失い、点は施設対象でなくなった。別の合法な施設点`(6,2)`は残る。

**Expected**: `trample_facility (6,2)`。`EnemyIntentRetargeted`、`fallback_depth=0`。

## F09-08 fallbackとパス

計画意図`pressure_black_king`の候補が消滅した。

- `advance_toward_black_king`候補が1点以上: その最上位点へfallbackする。
- 白前線合法点も存在しない: `EnemyPassed`を発行する。

**Expected**: 同意図再選択、fallback、passの順序を飛ばさない。

## Human sign-off

| reviewer | date | cases | result | notes |
|---|---|---|---|---|
| Reviewer A |  | F09-01〜08 | pending |  |
| Reviewer B |  | F09-01〜08 | pending |  |

2名一致後、[[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]を`validated`へ移す。
