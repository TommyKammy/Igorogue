---
type: technical-design
status: proposed
project: Igorogue
updated: 2026-07-10
---
# Domain Model

## 主な状態

```text
GameState
├─ BattleState
│  ├─ BoardState[49]
│  ├─ TurnState
│  ├─ DeckState
│  ├─ ResourceState
│  ├─ FacilityState
│  ├─ EnemyState
│  ├─ RepetitionState
│  ├─ TemporaryLibertyState
│  ├─ KomiState
│  └─ CounterattackState
└─ RunState
   ├─ Style
   ├─ FamilySeals
   ├─ Relics
   ├─ Soul
   ├─ Map
   └─ ContentVersion
```



## ResourceState and MomentumState

```text
ResourceState
├─ Qi
├─ MomentumState
├─ TurnReservedDraw
├─ TurnReservedQi
└─ DeferredPlayerChoices[]

MomentumState
├─ Amount: 0..2
└─ ApproachDrawUsedThisPlayerTurn: bool
```

- `MomentumState`はプレイヤー側battle global stateで、領地・施設・流儀instanceへ紐付かない。
- `Amount`はターンをまたいで保持し、戦闘開始・終了時に0へ戻る。
- `ApproachDrawUsedThisPlayerTurn`だけをプレイヤーターン開始時にfalseへ戻す。
- 余勢到達候補は保存状態ではなく、盤面、カード印刷タグ、現在余勢から導出する。
- 厳密仕様は[[FEAT-002 Momentum]]。

## TemporaryLibertyState

```text
TemporaryLibertyState
├─ EffectsById
├─ NextCreatedSequence
└─ ExpirySweepStartedForEnemyTurn?

TemporaryLibertyEffect
├─ EffectInstanceId
├─ Amount
├─ OwnerColor
├─ AnchorStoneInstanceId
├─ SourceId
├─ CreatedSequence
├─ ExpiresAfterEnemyTurnIndex
└─ DurationKind = timed_enemy_turn_end
```

- anchorは付与時対象groupのCanonical point order最小stone instance。
- effectはanchorを含む現在groupへ一度だけ加算し、merge後も追従する。
- continuous modifierは盤面条件から再導出し、timed stateへ保存しない。
- enemy-turn expiry sweepは[[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]。

## FacilityState

```text
FacilityState
├─ InstalledByPoint: Map<Point, FacilityInstance>
├─ NextBuildSequence
└─ ExplicitDisableSourcesByInstance

FacilityInstance
├─ InstanceId
├─ FacilityContentId
├─ OwnerColor
├─ Point
└─ BuildSequence
```

- `BoardState`のStone layerと`FacilityState`は別レイヤー。
- 確定状態では同一点に石と施設を共存させない。
- active／disabledは現在領地と明示的停止源から導出し、persistentな領地IDを保存しない。
- 施設は座標で領地へ再関連付けする。
- 領地分割後のcapacity超過は派生診断であり、既存施設を自動停止しない。
- 詳細は[[ADR-0012 Facility Sites Are Empty Intersections]]と[[FEAT-001 Territory and Facilities]]。

## RepetitionState

```text
RepetitionState
├─ OrderedStoneTopologyKeys
├─ SeenStoneTopologyKeysCache
└─ LastRegisteredMutationIndex
```

- 永続・リプレイ正本は順序付き`OrderedStoneTopologyKeys`観測列。mandatory mutationに限り既出keyの重複を含み得る。
- `SeenStoneTopologyKeysCache`は候補配置の合法性照会用の一意派生キャッシュで、保存時は再構築可能。
- キーはCanonical point orderで49交点を空／黒／白／黒王石／白王石として符号化する。
- 特殊石種と非石Domain Stateはキーに含めない。
- 戦闘初期配置をindex 0として登録し、合法な原子的石変化だけを追加する。
- 詳細は[[ADR-0011 Battle-Local Stone Topology Repetition Ban]]。

## ID

カード、遺物、敵、イベントには安定した文字列IDを用いる。表示名をIDにしない。

## 座標

```text
CanonicalPoint
├─ x: 1..7（左→右）
└─ y: 1..7（下→上）

InternalPoint
├─ ix: 0..6
└─ iy: 0..6
```

- content data、fixture、command、replay、telemetryはCanonicalPointを使用する。
- Rules Kernelの配列添字だけInternalPointを使用してよい。
- 変換は`(x-1,y-1)`／`(ix+1,iy+1)`の純粋関数に限定する。
- canonical indexは`(y-1)*7+(x-1)`で、`BoardState[49]`はこの順序へ正規化する。
- 盤面図の上段`y=7`とcanonical index 0の`(1,1)`を混同しない。
- 点対称は`reflect(x,y)=(8-x,8-y)`。
- 厳密な契約は[[Coordinate System and Initial Position]]。

## EnemyState minimum fields for FEAT-009

```text
EnemyState
├─ EnemyId
├─ BehaviorVersion
├─ PlannedNormalIntent
├─ PlannedCounterattackIntent?
├─ EnemyTurnIndex
├─ ActionIndex
├─ ActiveInvasionAnchor?
├─ RemoteInvasionCooldown
├─ RemoteInvasionUsedThisTurn
└─ CooldownSetThisTurn
```

`PlannedIntent`は意図ID、対象アンカー、第1候補点、代替候補点、計画時checksumを持つ。詳細は[[FEAT-009 Enemy Action Planning and Placement]]。


## CounterattackState

```text
CounterattackState
├─ GaugeUnits: integer >= 0
├─ Pending: bool
├─ PlannedCounterattackIntent?
├─ HeatUsedThisPlayerTurn: bool
├─ SuccessfulAttackCardsThisPlayerTurn: integer >= 0
├─ OverextensionUnitsThisPlayerTurn: integer >= 0
└─ SacrificeStoneRemainder: 0..2
```

- 2 units = 表示1点、閾値200 units。
- `Pending`は次敵ターンの通常行動後に追加行動1回を予約する。
- Pending中の`GaugeUnits`はoverflowを保持し、同一敵ターンに複数追加行動へ変換しない。
- 戦闘開始時はコミ式からGaugeUnitsを作り、他フィールドをresetする。
- 厳密仕様は[[FEAT-003 Komi Counterattack and Heat]]。
