---
type: feature-spec
id: FEAT-009
status: accepted
project: Igorogue
updated: 2026-07-10
version: 1.0.3
supersedes: FEAT-007
---
# FEAT-009 Enemy Action Planning and Placement

## Player promise

敵は完全な囲碁AIではなく、固有の問題を盤面へ提示する決定論的な対戦相手である。

プレイヤーは敵ターン前に、次の情報を確認できる。

- 通常行動の意図カテゴリ
- 主対象となるグループ、領地、施設または交点
- 現在盤面での第1候補点と最大2つの代替候補点
- 対象が消えた場合に再選択される可能性
- 反攻による追加行動の有無と意図カテゴリ

同一ゲームバージョン、content hash、seed、盤面、敵状態から、敵は必ず同じ意図と着点を選ぶ。

## Scope

本仕様はv0.2およびM3で使用する次の敵を完全定義する。

- `enemy_bandit` 山賊棋士
- `enemy_invader` 侵入者

次の敵は本仕様の共通手順だけを参照し、固有行動表は別タスクで定義する。

- 商人ギルド
- 断ち切り僧
- 白眼亀

## Non-goals

- 19路盤相当の最善手探索
- 学習型AI
- 隠されたランダム着点
- 敵用デッキと手札
- コウ／盤面反復規則そのものの変更。合法性フィルターは[[ADR-0011 Battle-Local Stone Topology Repetition Ban]]を利用する
- 商人ギルド、断ち切り僧、白眼亀の完全行動表

## Terms

### Enemy action

敵行動1回は、原則として白石1個の配置と、その後の[[Combat Resolution Order]]全処理である。

### Planned intent

次の通常敵行動について保存する予告情報。

```text
intent_id
target_ref
primary_point
alternate_points[0..2]
retargetable
planned_from_state_checksum
```

### Target reference

対象を次の安定アンカーで参照する。

- 石グループ: 計画時のグループに属する最小座標の石
- 領地: 計画時の領地に属する最小座標の空点
- 施設: 施設instance IDと座標
- 交点: 座標

実行時にアンカーが対象種別を満たさなければ、対象は消失したものとして再選択する。

### Canonical point order

表示座標 `(x, y)` は[[Coordinate System and Initial Position]]のCanonicalPoint（各軸1〜7）を使い、比較は`(y昇順, x昇順)`とする。
内部座標が0-basedでも、候補生成・表示・保存・最終タイブレークはCanonicalPointへ正規化する。

### Lexicographic score

候補評価は加重和を使わず、表に記載した項目を上から順に比較する。
先の項目で差がついた時点で後続項目は比較しない。
最終項目は必ずCanonical point orderとする。

## Action budget

- 通常敵ターンの通常行動は1回。
- 反攻が予告済みなら、通常行動の完全解決後に追加行動を1回行う。
- v0.2の最大敵行動数は1敵ターン2回。
- 各行動後に勝敗を確認し、戦闘終了なら残り行動を実行しない。
- 通常行動と反攻行動の間で盤面、領地、施設、敵状態を再計算する。
- パスも行動予算を1消費する。

## White placement legality

全候補は、先に次の共通合法性を満たす必要がある。

1. 配置先のStone layerに石がない。[[ADR-0012 Facility Sites Are Empty Intersections]]により施設点も空点であり、合法配置の確定時に施設を破壊する。
2. 意図が許可する配置モードを満たす。
3. 白石を仮配置し、隣接黒グループを同時捕獲する。
4. 捕獲後、配置石を含む白グループの有効呼吸点が1以上である。
5. [[ADR-0011 Battle-Local Stone Topology Repetition Ban]]の履歴済み`StoneTopologyKey`を再現しない。
6. 王石捕獲、施設破壊、領地変化を含む即時結果をシミュレートできる。

### Enemy placement modes

| mode | 条件 |
|---|---|
| `white_frontline` | 白石に上下左右で隣接する空点 |
| `white_contact` | 白石と黒石の両方に上下左右で隣接する空点 |
| `white_terminal` | その配置で黒グループを即捕獲する空点。白前線外でもよい |
| `white_invasion` | 現在の黒領地内にある空点。白前線外でもよい |
| `white_facility_invasion` | 現在の黒領地内にある黒施設点 |

敵はデータで明示されたmode以外を利用しない。

## Planning timing and UI stability

1. 戦闘開始時、初回プレイヤーターンの前に通常行動を計画する。
2. 敵ターン終了後、次回通常行動を計画する。
3. 反攻が次の敵ターンに発生すると確定した時点で、追加行動も別枠で計画する。
4. プレイヤーターン中、表示中の`intent_id`は変更しない。
5. プレイヤーの操作により対象や着点が不合法になった場合、実行時に同意図内で再評価する。
6. 強制上書きが発生する場合、ターン終了前プレビューに「処刑」または「王石防衛」を明示する。

UI表現は次の通り。

- 第1候補点: 実線ハイライト
- 代替候補点: 点線ハイライト、最大2点
- 対象: グループ／領地の輪郭
- 再選択可能: 循環矢印アイコン
- 強制上書き: 赤い処刑アイコンまたは青い防衛アイコン

## Common execution pipeline

```text
function execute_enemy_action(state, enemy, planned_intent):
    refresh_enemy_state(state, enemy)

    lethal = ranked_candidates("capture_black_king")
    if lethal is not empty:
        return resolve(lethal[0], reason="mandatory_lethal_override")

    if white_king_effective_liberties <= enemy.defense_threshold:
        defense = ranked_candidates("defend_white_king")
        if defense is not empty:
            return resolve(defense[0], reason="mandatory_defense_override")

    candidate = first_candidate_for_planned_target(planned_intent)
    if candidate exists:
        return resolve(candidate, reason="planned_target")

    candidate = first_candidate_for_same_intent_any_target(planned_intent.intent_id)
    if candidate exists:
        emit EnemyIntentRetargeted
        return resolve(candidate, reason="same_intent_retarget")

    for fallback_intent in fallback_chain(planned_intent.intent_id):
        candidate = first_ranked_candidate(fallback_intent)
        if candidate exists:
            emit EnemyIntentRetargeted
            return resolve(candidate, reason="fallback")

    emit EnemyPassed
    return pass
```

強制上書きは、通常行動と反攻行動の両方で毎回再判定する。

## Common intent definitions

### `capture_black_king`

**Condition**: 1手で黒王石グループを捕獲する合法点が1つ以上。

**Modes**: `white_terminal`, `white_frontline`, `white_contact`。

**Candidate order**:

1. 同時に捕獲する黒石総数が多い
2. 配置後の白配置グループ有効呼吸点が多い
3. Canonical point order

### `defend_white_king`

**Condition**: 白王石グループの有効呼吸点が敵データの`defense_threshold`以下。

候補は次のいずれかを満たす合法点。

- 白王石グループへ連結し、配置後の白王石グループ有効呼吸点を増やす
- 白王石グループに隣接していた黒石を捕獲し、結果として呼吸点を増やす

**Modes**: `white_frontline`, `white_contact`, `white_terminal`。

**Candidate order**:

1. 配置後の白王石グループ有効呼吸点が多い
2. 捕獲する黒石総数が多い
3. 連結する別白グループ数が多い
4. Canonical point order

### `capture_non_king`

**Condition**: 敵ごとの`opportunistic_capture_min_stones`以上の王石を含まない黒石を1手で捕獲する合法点がある。

**Modes**: `white_terminal`, `white_frontline`, `white_contact`。

**Candidate order**:

1. 捕獲する黒石総数が多い
2. 捕獲対象と黒王石グループとの最小マンハッタン距離が短い
3. 配置後の白配置グループ有効呼吸点が多い
4. Canonical point order

### `pressure_black_king`

**Condition**: 黒王石グループの現在の実呼吸点へ置けて、黒王石を捕獲せず有効呼吸点を減らす合法点がある。

**Modes**: `white_frontline`, `white_contact`。

**Candidate order**:

1. 配置後の黒王石グループ有効呼吸点が少ない
2. 配置後の白配置グループ有効呼吸点が多い
3. 連結する別白グループ数が多い
4. Canonical point order

### `advance_toward_black_king`

**Condition**: 白前線の合法点がある。

**Mode**: `white_frontline`。

**Candidate order**:

1. 配置点から黒王石グループの実呼吸点までの最小マンハッタン距離が短い
2. 配置後の白配置グループ有効呼吸点が多い
3. 盤中央 `(4,4)` までのマンハッタン距離が短い
4. Canonical point order

## Enemy table: 山賊棋士

### Identity

山賊棋士は、アタリを放置すると必ず食べるチュートリアル攻撃敵である。
単石の囮も捕獲するため、捨て石の基本を学習できる。

### Parameters

```text
defense_threshold = 2
opportunistic_capture_min_stones = 1
normal_actions = 1
counterattack_bonus_actions = 1
```

### Plan priority

| priority | intent | selection condition | fallback |
|---:|---|---|---|
| 1 | `capture_non_king` | 1石以上を捕獲可能 | `pressure_black_king` |
| 2 | `pressure_black_king` | 黒王石の呼吸点を直接減らせる | `advance_toward_black_king` |
| 3 | `advance_toward_black_king` | 白前線合法点あり | `pass` |

`capture_black_king`と`defend_white_king`は計画優先度の外側にある強制上書き。
通常行動と反攻行動は同じ優先度を使う。

### Expected teaching result

- 自グループをアタリのまま終えると捕獲される
- 囮石を1個与えると山賊は取りに来る
- 白王石をアタリにすると山賊は防衛を優先する
- 捕獲機会がなければ黒王石へ最短で近づく

## Enemy table: 侵入者

### Identity

侵入者は、大きい黒領地と施設へ遠隔侵入し、次の行動で侵入石を延命する領地破壊敵である。
単石の囮は原則無視し、2石以上の捕獲だけを好機として扱う。

### Parameters

```text
defense_threshold = 1
opportunistic_capture_min_stones = 2
minimum_remote_invasion_territory_size = 2
remote_invasion_cooldown_enemy_turns = 1
normal_actions = 1
counterattack_bonus_actions = 1
remote_invasion_max_per_enemy_turn = 1
```

### Invasion state

```text
active_invasion_anchor: coordinate | null
remote_invasion_cooldown: integer >= 0
remote_invasion_used_this_turn: boolean
cooldown_set_this_turn: boolean
```

- `trample_facility`または`invade_largest_territory`解決後、配置点を`active_invasion_anchor`に保存する。
- 同時に`remote_invasion_cooldown = 1`、`remote_invasion_used_this_turn = true`とする。
- アンカー石が捕獲された、またはそのグループが白王石グループへ連結した場合、アンカーを消去する。
- `escape_active_invasion`成功後、アンカーを消去する。
- 敵ターン開始時にcooldownが正なら、その敵ターンは遠隔侵入不可。ターン終了時に1減らす。
- 当該敵ターン中に新しく設定されたcooldownは、その同じターン終了時には減らさない。
- 反攻行動を含め、1敵ターンに遠隔侵入は最大1回。
- アンカーが残る間、新しい遠隔侵入を計画しない。

### Black territory target order

1. 現在の実効気収入寄与が大きい
2. 稼働中施設数が多い
3. 領地サイズが大きい
4. 領地アンカーのCanonical point order

実効気収入寄与は、基本収入と現在稼働中の直接収入施設だけを含む。ドロー、魂、防御等の非気効果は含めない。

### `escape_active_invasion`

**Condition**: `active_invasion_anchor`を含む白グループが存在し、白王石グループへ未連結。

**Mode**: `white_frontline`。候補は対象グループの実呼吸点に限定。

**Candidate order**:

1. 配置後の対象グループ有効呼吸点が多い
2. 配置後に隣接する異なる黒グループ数が少ない
3. 配置後に黒石を捕獲する数が多い
4. Canonical point order

### `trample_facility`

**Condition**:

- `active_invasion_anchor`がnull
- cooldownが0
- 当該敵ターンに遠隔侵入未使用
- 現在の黒領地内に合法な施設点がある

**Mode**: `white_facility_invasion`。

**Candidate order**:

1. 対象領地順位
2. 対象領地内の稼働中施設数が多い
3. 配置後の白配置グループ有効呼吸点が多い
4. Canonical point order

施設の種類による隠し価値評価は行わない。同条件なら座標で決める。

### `invade_largest_territory`

**Condition**:

- `active_invasion_anchor`がnull
- cooldownが0
- 当該敵ターンに遠隔侵入未使用
- サイズ2以上の黒領地に合法点がある

**Mode**: `white_invasion`。

対象領地はBlack territory target orderで選ぶ。

**Candidate order within the selected territory**:

1. 配置後の白配置グループ有効呼吸点が多い
2. 領地境界までの領地内最短距離が長い
3. 隣接する異なる黒グループ数が少ない
4. Canonical point order

領地境界までの距離は、対象領地内だけを上下左右に移動し、対象外点へ出るまでの最短歩数。

### Invader plan priority

| priority | intent | selection condition | fallback |
|---:|---|---|---|
| 1 | `escape_active_invasion` | 活動中アンカーあり | `capture_non_king` |
| 2 | `trample_facility` | 遠隔侵入可能な施設あり | `capture_non_king` |
| 3 | `capture_non_king` | 2石以上を捕獲可能 | `invade_largest_territory` |
| 4 | `invade_largest_territory` | サイズ2以上の黒領地あり | `pressure_black_king` |
| 5 | `pressure_black_king` | 黒王石の呼吸点を減らせる | `advance_toward_black_king` |
| 6 | `advance_toward_black_king` | 白前線合法点あり | `pass` |

強制上書きは山賊棋士と同じ。反攻行動でも同じ優先度を使うが、遠隔侵入回数上限とcooldownを共有する。

## Intent generation

```text
function plan_enemy_action(state, enemy):
    refresh_enemy_state(state, enemy)

    if candidates("capture_black_king"):
        return make_plan("capture_black_king")

    if white_king_effective_liberties <= enemy.defense_threshold
       and candidates("defend_white_king"):
        return make_plan("defend_white_king")

    for intent_id in enemy.plan_priority:
        candidates = ranked_candidates(intent_id)
        if candidates is not empty:
            return make_plan(
                intent_id,
                target_ref = candidates[0].target_ref,
                primary_point = candidates[0].point,
                alternate_points = candidates[1:3],
                retargetable = true
            )

    return make_plan("pass", retargetable = false)
```

## Domain events

最低限、次のDomain Eventを発行する。

```text
EnemyIntentPlanned
EnemyIntentRetargeted
EnemyActionStarted
EnemyActionResolved
EnemyPassed
EnemyInvasionAnchorSet
EnemyInvasionAnchorCleared
EnemyRemoteInvasionCooldownChanged
```

既存の`StonePlaced`、`GroupCaptured`、`KingCaptured`、`FacilityDestroyed`等は通常のRules Kernelから発行する。

## Telemetry

敵行動ごとに次を記録する。

```text
enemy_id
enemy_behavior_version
enemy_turn_index
action_index
is_counterattack_action
planned_intent_id
executed_intent_id
override_reason
target_ref_before
target_ref_after
preview_points
executed_point
candidate_count
fallback_depth
board_checksum_before
board_checksum_after
```

M3では追加で以下を集計する。

- 予告通りの意図が実行された割合
- 同意図内再選択率
- 強制処刑率
- 強制防衛率
- パス率
- 山賊が囮石を捕獲した割合
- 侵入者の遠隔侵入後生存ターン数
- 侵入により停止・破壊された施設数

## Edge cases

1. **通常行動で戦闘終了**: 反攻行動は破棄する。
2. **複数の王石捕獲点**: `capture_black_king`の候補順で1点を選ぶ。
3. **計画対象グループの合流**: アンカー石を含む現在グループを対象とする。
4. **計画対象グループの消滅**: 同意図で全対象を再検索する。
5. **施設が計画後に破壊済み**: `trample_facility`を同意図で再選択し、候補なしならfallback。
6. **侵入領地が計画後に中立化**: 同じ黒領地ではないため対象消失。別黒領地へ再選択する。
7. **遠隔侵入直後の反攻**: 同じ敵ターンでは2回目の遠隔侵入を禁止し、`escape_active_invasion`以下へ進む。
8. **白王石防衛候補なし**: 強制防衛を諦め、計画意図を実行する。敗北可能性を隠さない。
9. **候補完全枯渇**: パスし、`EnemyPassed`を発行する。
10. **黒王石捕獲と白王石危機が同時**: 黒王石捕獲を優先し即勝利する。

## Acceptance criteria

- 行動予算、白石配置合法性、計画時点、強制上書き、再選択、fallbackが一義的。
- すべての候補順がLexicographic scoreとCanonical point orderで決定論的。
- 山賊棋士は単石捕獲、王石圧迫、前線展開を完全定義。
- 侵入者は施設踏破、最大領地侵入、侵入石延命、遠隔侵入cooldownを完全定義。
- `game_data/content/enemies.json`が本仕様のID、優先度、パラメーターを参照する。
- [[Rules Canon]]、[[Enemy Design and Intent]]、[[Enemy Catalog]]から本仕様へ参照がある。
- [[FEAT-009 Enemy Decision Fixtures]]の全ケースで、独立した2名が同一の意図と着点を選ぶ。

## Dependencies and deferred decisions

- 盤面反復禁止は[[ADR-0011 Battle-Local Stone Topology Repetition Ban]]を利用する。本仕様独自のコウ例外は作らない。反復候補はLexicographic score適用前に除外する。
- 施設点の意味論は[[ADR-0012 Facility Sites Are Empty Intersections]]と[[FEAT-001 Territory and Facilities]]を利用する。不合法候補の仮実行では施設を破壊しない。
- 商人ギルド、断ち切り僧、白眼亀は`implementation_status: placeholder`のままM3-A対象外とする。
