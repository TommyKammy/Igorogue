---
type: technical-spec
status: proposed
project: Igorogue
updated: 2026-07-10
---
# Command Event Model

## Point payload contract

`ChooseIntersection`およびpointを持つ全Eventは[[Coordinate System and Initial Position]]のCanonicalPointを使用する。JSON表現は`[x,y]`、各軸1〜7。内部0-based座標はDomain境界で変換する。

## Commands

```text
StartRun
ChooseStyle
EquipSeal
StartBattle
PlayCard
ChooseIntersection
ChooseMode
EndTurn
ChooseReward
BuyItem
RemoveCard
```

## Events

```text
StonePlaced
StoneTopologyRegistered
TemporaryLibertyGranted
TemporaryLibertyRemoved
TemporaryLibertyExpirySweepStarted
TemporaryLibertyExpired
TemporaryLibertyExpirySweepResolved
CaptureBatchStarted
GroupCaptured
KingCaptured
TerritoryCreated
TerritoryNeutralized
FacilityBuilt
FacilityActivated
FacilityDisabled
FacilityDestroyed
QiChanged
TurnReservedQiChanged
CardDrawn
TurnReservedDrawChanged
DeferredPlayerChoiceCreated
CaptureBenefitSuppressed
MomentumChanged
MomentumApproachDrawTriggered
BrilliantMultiplierChanged
CounterattackAdvanced
HeatGenerated
CounterattackPrimed
CounterattackBonusStarted
CounterattackBonusResolved
CounterattackPendingCarried
CounterattackReset
CounterattackTriggered
EnemyIntentPlanned
EnemyIntentRetargeted
EnemyActionStarted
EnemyActionResolved
EnemyPassed
EnemyInvasionAnchorSet
EnemyInvasionAnchorCleared
EnemyRemoteInvasionCooldownChanged
AutomaticStoneCreationSuppressed
CommandRejected
BattleWon
BattleLost
```

イベントはUI演出ではなくDomainの事実。演出側がイベントを購読する。



## 仮呼吸点・capture batch関連

```text
TemporaryLibertyGranted(effect_id, amount, anchor_stone_instance_id, source_id, expires_after_enemy_turn_index, created_sequence)
TemporaryLibertyRemoved(effect_id, reason, anchor_stone_instance_id)
TemporaryLibertyExpirySweepStarted(enemy_turn_index, expired_effect_ids)
TemporaryLibertyExpired(effect_id, amount, anchor_stone_instance_id, source_id, enemy_turn_index)
CaptureBatchStarted(batch_id, reason, capturing_window, enemy_turn_index?)
GroupCaptured(batch_id, color, group_anchor, stone_instances, contains_king, capturing_color, reason)
CaptureBenefitSuppressed(batch_id, reason=battle_terminal)
TurnReservedDrawChanged(old_amount, new_amount, delta, reason, source_id)
TurnReservedQiChanged(old_amount, new_amount, delta, reason, source_id)
DeferredPlayerChoiceCreated(choice_instance_id, source_id, options, resolve_at=next_player_turn_start)
TemporaryLibertyExpirySweepResolved(enemy_turn_index, captured_group_anchors, battle_result)
```

- due effect eventはCreatedSequence、effect ID順。
- `GroupCaptured`はgroup anchor順、payloadのstoneはpoint順。
- `StoneTopologyRegistered`へ`first_seen`と`source_reason`を追加する。mandatory expiry captureは`first_seen=false`でも実行する。
- 黒王石captureがあれば、白王石同時captureを含め`BattleLost`とし、`CaptureBenefitSuppressed`後に利益eventを発行しない。
- 閉じたplayer windowのdraw／qi／choiceは予約・延期eventへ変換する。
- 正本は[[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]、[[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]、[[FEAT-005 Sacrifice Triggers]]。

## 盤面反復関連

- プレイヤーが反復不合法点を実行要求した場合、任意の診断イベントとして`CommandRejected(reason=stone_topology_repetition)`を返せる。
- 敵候補生成の内部除外では`CommandRejected`を発行しない。
- 確定した石変化後、`StoneTopologyRegistered(key_hash, mutation_index, first_seen, source_reason)`を発行する。mandatory removalは既出keyでも発行する。
- 自動石生成が反復で抑止された場合、`AutomaticStoneCreationSuppressed(reason=stone_topology_repetition, source_id)`を発行する。
- 不合法手では`StonePlaced`、`GroupCaptured`、資源・カード・妙手イベントを発行しない。


## 施設関連

```text
FacilityBuilt(instance_id, facility_id, owner, point, build_sequence)
FacilityActivated(instance_id, reason, territory_anchor)
FacilityDisabled(instance_id, reason, territory_anchor?)
FacilityDestroyed(instance_id, reason, point, source_actor, source_command_id)
```

理由コードの最低集合:

```text
built_in_controlled_territory
territory_control_lost
territory_control_restored
explicit_effect
stone_occupied
```

- 合法な石配置点に施設がある場合、`StonePlaced`、`GroupCaptured[]`、`FacilityDestroyed`、`StoneTopologyRegistered`の順で発行する。
- 不合法配置では施設イベントを発行しない。
- 領地再計算では領地事実をregion anchor順で発行した後、施設状態変化をCanonical point order、instance ID順で発行する。
- 建設容量超過は建設時の`CommandRejected(reason=facility_capacity_full)`とテレメトリで表し、既存施設を自動停止しない。

## 領地成立関連

```text
TerritoryEstablished(
  source_actor,
  changed_points
)
```

- 一つのatomic resolutionにつき最大1件。
- `changed_points`は非黒領地から黒領地へ変化した全交点をCanonical point orderで保持し、空ならeventを発行しない。
- source actorが白でもownership deltaの診断factは発行できるが、普遍Momentum source候補になるのは黒actorだけ。
- 領地事実と`TerritoryEstablished`を先に確定し、その後に`FacilityDisabled / FacilityActivated`を座標順、最後に将来の普遍領地`MomentumChanged`を発行する。


## 余勢関連

`ChooseMode(mode=momentum_reach)`は、カードの印刷済み配置modeとは別の任意modeである。
通常合法点を選んだ場合はnormal modeへ正規化し、余勢を消費しない。

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

reason最低集合:

```text
battle_reset
black_territory_established
facility_built_by_territory_style
explicit_content_effect
spent_for_momentum_reach
```

不合法またはキャンセルされた余勢配置では`MomentumChanged`、`QiChanged`、カード移動Eventを発行しない。
詳細は[[FEAT-002 Momentum]]。


## 反攻関連

```text
CounterattackAdvanced(old_units, new_units, delta_units, reason, source_id, command_id?)
HeatGenerated(komi, brilliant_before, brilliant_after, delta_units)
CounterattackPrimed(residual_units, execute_on_enemy_turn_index)
CounterattackBonusStarted(enemy_turn_index, intent_id)
CounterattackBonusResolved(enemy_turn_index, action_result)
CounterattackPendingCarried(residual_units, execute_on_enemy_turn_index)
CounterattackReset(komi, start_units)
```

- `CounterattackTriggered`は旧ログ読込用のaliasで、新規Rules Kernelはより具体的なeventを発行する。
- unitは整数で保存し、表示点への変換はUI側で行う。
- 同一敵ターンの追加行動は最大1。
