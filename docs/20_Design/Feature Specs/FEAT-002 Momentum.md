---
type: feature-spec
id: FEAT-002
status: accepted
project: Igorogue
updated: 2026-07-10
version: 1.0.0
---
# FEAT-002 Momentum

## Player promise

余勢は、盤面上で作った領地経済を前線アクセスへ変換する、全流儀共通の戦闘資源である。
領地を仕込み、必要な札へ余勢を使うことで、通常より一歩遠くへ展開できる。
地合い流は施設建設を追加生成源に持つが、余勢の所持・消費権を独占しない。

プレイヤーはカード確定前に、次を確認できる。

- 現在余勢と、使用後・解決後の予測値
- 通常合法点と余勢によって追加される合法点の差
- 余勢到達点の起点石と中間点
- その手で前線前進ドローが発生するか
- 不合法理由と、余勢・気が消費されないこと

## Scope

本仕様は次を定義する。

- 余勢が全流儀共通のbattle-scoped resourceであること
- 初期値、上限、ターンをまたぐ保持、戦闘間リセット
- 全流儀共通の領地生成源
- 地合い流だけが持つ施設建設生成源
- 余勢を使用できるカード条件
- `momentum_reach`の幾何条件と通常配置との優先関係
- 消費、生成、前線前進ドローの判定順
- UI、イベント、テレメトリ、fixture

## Non-goals

- 敵側の余勢
- 余勢をラン間へ持ち越すこと
- 斜め、曲線、石越しの余勢配置
- 余勢を気や魂へ直接変換する汎用ルール
- 一間トビ、辺打ち、侵入等の印刷済み配置タグを余勢で強化すること
- 余勢上限を超えた分の自動報酬
- 数値バランスの最終確定

## Terms

### Global battle resource

`MomentumState.amount`としてプレイヤー側に一つだけ存在する。
領地、施設、石グループ、カードinstanceへ紐付かない。
どの流儀でも同じ資源を生成・所持・消費できる。

### Atomic resolution

一つの`PlayCard`、自動黒石生成、または明示的な単一効果を、必須の石配置、捕獲、領地再計算、施設状態変化まで解決する単位。
同一atomic resolutionで複数の黒領地が成立しても、普遍生成源は一度だけ発火する。

### Black territory established

atomic resolutionの前後で、少なくとも一つの交点が「黒領地ではない」から「黒領地」へ変化し、かつ変化のsource actorまたはeffect ownerが黒であること。

これは初回囲い、拡張、結合、奪還を含む。
戦闘開始時の既存領地、収入計算だけ、敵白行動だけによる偶発的な所有変化は含まない。

### Momentum-eligible card

次のすべてを満たすカード。

```text
type == stone
printed placement_tags contains frontline
main effect contains exactly one place_stone for black
```

現在の候補では、打石、ノビ、血石、種石が該当する。
ツケ、囮石、一間トビ、辺這い、忍び、施設札、手筋札は該当しない。

### Momentum reach

余勢1を使って選べる、印刷済み配置タグとは別の代替配置mode。
配置札の効果本文は変更しない。

## Resource lifecycle

```text
battle start:
    amount = 0
    approach_draw_used_this_player_turn = false

player turn start:
    amount is preserved
    approach_draw_used_this_player_turn = false

enemy turn / turn end:
    amount is preserved

battle end:
    amount = 0
```

- 初期値0。
- 上限2。
- 未使用分はターン終了時に消滅しない。
- 戦闘をまたいで持ち越さない。
- 上限超過分は失われ、別効果へ変換しない。

現在値の機械可読正本は`game_data/balance/system.json`の`momentum`である。

## Generation

### Universal source: black territory established

全流儀共通。

```text
if atomic resolution is black-owned
and at least one point changed from non-black-territory to black-territory:
    generate 1 momentum
```

- 一つのatomic resolutionにつき最大1。
- 新規黒領地が複数region、複数pointあっても1。
- 余勢を使った配置で領地を作った場合も生成するため、消費直後に1を取り戻せる。
- 自動黒石生成が条件を満たした場合も生成できる。
- 敵白行動だけで黒領地が偶発的に成立しても生成しない。

### Territory style bonus source: facility built

地合い流`style_territory`だけが持つ追加source。

```text
on successful black FacilityBuilt command:
    generate 1 momentum
```

- 一つの建設commandにつき最大1。
- 建設が容量、対象、コスト等で不合法なら生成しない。
- 他流儀は施設を建てられるが、施設建設だけでは余勢を得ない。
- 施設の再稼働、停止解除、領地奪還は施設建設ではないため、このsourceを発火しない。

`Styles.md`と`styles.json`では、このrule IDを`facility_build_grants_momentum`とする。

### Explicit content source

カード、遺物、将来の流儀が明示的な`gain_momentum`効果を持つ場合は、その効果文に従う。
暗黙の生成源を追加してはならない。

### Cap and overflow

```text
applied = min(requested, cap - current)
overflow = requested - applied
new_amount = current + applied
```

`overflow`はテレメトリへ記録するが、トリガーや報酬にはしない。

## Eligible placement geometry

プレイヤーがeligible cardを選び、余勢が1以上ある場合、`momentum_reach`を任意選択できる。

対象点`T`が余勢到達点になる条件は、次のすべてである。

1. `T`のStone layerが空。
2. 黒石`S`が存在し、`S`と`T`は同じxまたは同じy上で、Manhattan distanceが2。
3. `S`と`T`の中間点`M`のStone layerが空。
4. `T`が、そのカードの印刷済み配置タグだけでは合法点でない。
5. 仮配置、同時捕獲、自殺手、盤面反復、カード固有条件を含む完全合法性を通る。

```text
S . T     legal geometry
S B T     blocked; midpoint has a stone
S W T     blocked; midpoint has a stone
S
.
T         legal geometry
```

- 斜めdistance 2は不可。
- 曲がったL字は不可。
- 中間点または対象点の施設はStone layer上の空点なので、幾何条件を妨げない。
- 対象点に施設がある場合、合法配置確定後に[[ADR-0012 Facility Sites Are Empty Intersections]]どおり破壊する。
- 複数の起点石が成立しても、消費は1。

## Normal placement takes precedence

同じ対象点がカードの印刷済み配置タグで合法なら、通常modeとして扱う。

- 余勢候補として表示しない。
- 余勢を消費しない。
- プレイヤーが意図的に余勢を捨てる選択は提供しない。

これにより、別の黒石に隣接して通常前線になっている点へ、遠い起点石を理由に余勢を浪費できない。

## Validation and consumption

```text
select eligible card
→ optionally select momentum_reach mode
→ derive momentum-only candidate points
→ choose point
→ preview full placement, captures, suicide and repetition
→ if illegal: reject without changing qi, card zone or momentum
→ reserve qi cost and momentum 1
→ commit command
→ emit MomentumChanged(reason=spent_for_momentum_reach)
→ resolve card and stone placement
```

- 余勢は合法コマンド確定時に1だけ消費する。
- 対象選択キャンセル、不合法、自殺手、盤面反復では消費しない。
- カードの気コストも同じく消費しない。
- 配置後のカード効果は通常どおり解決する。例として、ノビは余勢配置後の実呼吸点条件でドローできる。

## Post-resolution generation order

余勢配置が黒領地を成立させた場合、消費後に普遍sourceから再生成できる。

```text
MomentumChanged(-1, spent_for_momentum_reach)
→ card / StonePlaced / GroupCaptured
→ facility destruction and StoneTopologyRegistered
→ battle result check
→ capture and card triggers
→ territory recalculation
→ TerritoryEstablished facts
→ facility state changes
→ MomentumChanged(+1, black_territory_established) if eligible
→ evaluate momentum approach draw
→ brilliant / remaining resource effects
```

戦闘終了が確定した場合、前線前進ドローは実行しない。

## Momentum approach draw

一つのプレイヤーターンにつき最大1回、余勢配置を経済から攻撃への明示的な橋にする。

成功した`momentum_reach`配置の前後で、次を計算する。

```text
front_distance = minimum Manhattan distance
                 from any black stone
                 to the white king point
```

次のすべてを満たす場合、即時1ドローする。

1. 実際に余勢1を消費して配置した。
2. 配置後`front_distance`が配置前より小さい。
3. atomic resolution後の予測黒領地収入が4以上。
4. このプレイヤーターンにまだ余勢前進ドローを使っていない。
5. 戦闘が終了していない。

`予測黒領地収入`は、次のターン開始時に盤面が変わらなければ得る、黒領地の基本収入とactive施設修正の合計であり、基礎気3を含まない。

- 対象点が白王石へ近くても、盤面上の最前線距離が更新されない場合はドローしない。
- 領地収入が配置によって4へ到達した場合は、その同じ解決で条件を満たせる。
- 2回目以降の余勢配置は可能だが、追加ドローは発生しない。
- 判定flagは次のプレイヤーターン開始時にリセットする。

## Event payloads

```text
MomentumChanged(
    old_amount,
    new_amount,
    requested_delta,
    applied_delta,
    overflow,
    reason,
    source_id,
    command_id
)

MomentumApproachDrawTriggered(
    command_id,
    point,
    front_distance_before,
    front_distance_after,
    projected_black_income
)
```

最低reason集合:

```text
battle_reset
black_territory_established
facility_built_by_territory_style
explicit_content_effect
spent_for_momentum_reach
```

## UI

- 余勢は左側resource railへ`0/2`、`1/2`、`2/2`で表示する。
- eligible card選択時、通常合法点と余勢追加点を別glyphで表示する。
- 余勢追加点へカーソルを合わせると、起点石、中間点、消費後余勢を表示する。
- 前線前進ドロー条件を満たす点には、カード1枚の予告iconを表示する。
- 通常合法点では余勢iconを表示しない。
- 不合法点では余勢・気の消費予告を出さず、理由を表示する。
- 上限時の生成予告は`余勢+0（上限）`と表示できる。

## Telemetry

最低限、次を記録する。

```text
momentum_before
momentum_requested_delta
momentum_applied_delta
momentum_overflow
momentum_after
momentum_reason
style_id
source_command_id
card_id
placement_mode
source_points
midpoint
chosen_point
normal_legal_without_momentum
full_legality_result
front_distance_before
front_distance_after
projected_black_income
approach_draw_eligible
approach_draw_triggered
approach_draw_already_used
```

M3では次を集計する。

- 流儀別の余勢生成源割合
- 生成した余勢の消費率と上限overflow率
- 余勢を1ターン以上保持した割合
- 余勢配置が最前線を更新した割合
- 前線前進ドロー発火率
- 地合い流の施設建設生成が爆発へ寄与した割合
- 余勢なし、余勢ありでの領地→攻撃切替ターン差
- 同じ初手列への収束度

## Edge cases

1. **複数領地同時成立**: 一つの配置で2地域以上が黒領地になっても普遍生成は1。
2. **奪還**: 非黒領地から黒領地へ戻した黒行動は普遍生成対象。
3. **敵による偶発成立**: 白行動だけで黒領地が成立しても生成しない。
4. **上限中の生成**: 余勢2で+1が起きても2のまま。overflow 1を記録する。
5. **ターンまたぎ**: 余勢2でターン終了しても次ターン開始時2。前進ドローflagだけをリセットする。
6. **通常点との重複**: 遠い起点から幾何条件を満たしても、別黒石に隣接して通常合法なら余勢を使わない。
7. **中間施設**: 中間点に施設だけがある場合、石レイヤーは空なので余勢到達可能。
8. **対象施設**: 対象点の施設は候補を妨げず、合法確定後に破壊する。
9. **印刷タグなし**: contact、jump、edge、invasionだけの札は余勢を使用できない。
10. **不合法最終局面**: 自殺手または盤面反復なら余勢、気、カード位置は不変。
11. **終着との重複**: terminalで合法な点は印刷タグによる通常modeを使い、余勢を消費しない。
12. **同ターン連鎖**: 余勢を使って領地成立、余勢を再生成し、別のeligible cardへ再使用できる。
13. **前線距離非更新**: 置いた石自身が王石へ近づいても、別の黒石がすでに近ければドローしない。
14. **閾値同時到達**: 配置後収入が3から4へ増えた場合、その解決でドロー可能。
15. **戦闘終了手**: 白王石を捕獲した場合、前線前進ドローを行わず戦闘終了する。

## Acceptance criteria

- 余勢が全流儀共通の一つのbattle resourceである。
- 全流儀が黒領地成立から生成でき、地合い流だけが施設建設を追加sourceに持つ。
- `Rules Canon`、`Styles.md`、`styles.json`が同じ範囲を記述する。
- eligible card、exact orthogonal distance 2、empty midpoint、normal placement precedenceが一義的である。
- 不合法・キャンセルでは余勢と気を消費しない。
- 余勢はターンをまたいで保持し、戦闘開始／終了で0へ戻る。
- 前線前進ドローの距離、収入評価時点、1ターン上限が一義的である。
- [[FEAT-002 Momentum Gate Fixtures]]のMOM-01〜MOM-19が仕様checkerを通る。
- M1では同fixtureを共有Rules Kernelのunit/golden testsへ移植する。

## Deferred decisions

- 余勢上限を変える遺物
- 斜めまたは2交点超の余勢配置
- 敵用の類似resource
- 余勢overflowを利用するビルド
- 余勢を明示的に捨てるカード
- 白王石以外を目標にする前進ドロー
