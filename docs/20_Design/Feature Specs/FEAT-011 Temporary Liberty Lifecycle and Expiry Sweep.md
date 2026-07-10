---
type: feature-spec
id: FEAT-011
status: accepted
project: Igorogue
updated: 2026-07-10
version: 1.0.0
---
# FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep

## Player promise

補強や厚みで得た仮呼吸点は、表示された敵ターン終了まで確実にグループを守る。期限が切れたときは、盤面全体を同じ瞬間として判定し、何が死んだか、どの報酬が発動したかを予測可能に表示する。

## Scope

本Featureは次を定義する。

- timed仮呼吸点の付与、追従、stack、失効
- continuous呼吸点modifierとの区別
- 敵ターン終了時の同時capture sweep
- 王石結果gate
- capture triggerと予約資源
- mandatory topology mutation
- 領地・施設・余勢・妙手・反攻との境界順

## Domain state

```text
TemporaryLibertyState
├─ EffectsById: Map<EffectInstanceId, TemporaryLibertyEffect>
├─ NextCreatedSequence: integer
└─ ExpirySweepStartedForEnemyTurn?: integer

TemporaryLibertyEffect
├─ EffectInstanceId
├─ Amount: integer > 0
├─ OwnerColor
├─ AnchorStoneInstanceId
├─ SourceId
├─ CreatedSequence
├─ ExpiresAfterEnemyTurnIndex
└─ DurationKind = timed_enemy_turn_end
```

continuous modifierは保存可能な盤面条件instanceから毎回導出し、`EffectsById`へ入れない。

## Grant

### Target anchor

対象グループ内の石をCanonical point orderで並べ、最初の石instanceをanchorとする。effectはanchorを含む現在グループに一度だけ加算する。

### Expiry index

```text
if expiry sweep for current enemy turn has not started:
    expires_after = first enemy turn end at or after grant
else:
    expires_after = current enemy turn index + 1
```

プレイヤーターン中の補強は、直後の敵ターン終了で失効する。

### Reinforce card

`card_reinforce`は対象選択時の有効呼吸点が1なら、仮呼吸点付与前に「アタリ対象」と判定して1ドローする。その後、+1 effectを付与する。

## Effective liberty calculation

```text
effective_liberties(group) =
    unique real liberty count
  + sum(active timed effects whose anchor is in group)
  + sum(active continuous modifiers applying to group)
```

同一effectのanchorにグループ内複数石があっても一度だけ加算する。異なるeffectは加算する。

## Carrier removal

anchor stoneが通常capture等で除去された場合、そのeffectを`carrier_removed`として同じcapture resolution内で除去する。後の失効掃引では再度`Expired`を発行しない。

## Enemy-turn expiry phase

[[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]の順序を実装する。

```text
expire_temporary_liberties(enemy_turn_index):
    due = all effects where ExpiresAfterEnemyTurnIndex == enemy_turn_index
    if due is empty:
        return no_events

    emit TemporaryLibertyExpirySweepStarted
    remove every due effect from state simultaneously
    emit TemporaryLibertyExpired for due in CreatedSequence / ID order

    groups = recompute all groups from unchanged stone board
    doomed = every group with effective_liberties(groups) == 0
    order doomed by group anchor
    remove all doomed stones simultaneously
    emit GroupCaptured for each ordered group

    if any stone was removed:
        append resulting StoneTopologyKey observation

    apply king-result gate
    if battle ended:
        suppress capture benefits
        emit TemporaryLibertyExpirySweepResolved
        return

    process non-terminal capture benefits
    recalculate territory and facilities
    do not grant implicit momentum or brilliant multiplier
    emit TemporaryLibertyExpirySweepResolved
```

## Capture batch data

```text
CaptureBatch
├─ BatchId
├─ Reason = temporary_liberty_expired
├─ BoundaryEnemyTurnIndex
├─ CapturingWindow = closed_player_window
└─ CapturedGroups[]

CapturedGroup
├─ Color
├─ GroupAnchor
├─ StoneInstances[] in Canonical point order
├─ ContainsKing
└─ CapturingColor = opposite(Color)
```

## Stable ordering

- due effect event: `CreatedSequence`, effect ID
- captured group: group anchor canonical index
- stone self trigger: group order、stone point order、effect array order
- equipped seal / relic: equipped slot order
- facility: point order、instance ID
- enemy passive: stable content ID

## King gate

```text
black king captured => loss
else white king captured => win
```

両王石同時captureはloss。終局batchではbenefit triggerを処理しない。

## Closed-window resource conversion

| Trigger output | Enemy action / expiry phase result |
|---|---|
| draw | `TurnReservedDraw` |
| qi | `TurnReservedQi` |
| player choice | `DeferredPlayerChoice` |
| soul | immediate |
| counterattack pressure | immediate, unless battle ended |

次プレイヤーターン開始時は、DeferredChoice、基礎気＋領地収入＋予約気、通常draw＋予約drawの順で確定する。

## Territory and engine consequences

- capture後の空点は通常どおり領地再計算へ含む。
- 施設は[[FEAT-001 Territory and Facilities]]に従い停止・再稼働する。
- expiry phaseだけで成立した黒領地は、暗黙の余勢を生成しない。
- expiry captureはplayer-turn妙手倍率を増やさない。
- 通常の白グループcapture reward、captured-stone trigger、流儀・印・遺物は非終局時に発動できる。

## Repetition interaction

expiry captureは拒否不能なmandatory mutation。過去キーを再現しても実行する。ordered observationへキーを追加し、`first_seen=false`とする。Seen集合は変化しない。

## UI

### Before enemy turn

- 仮呼吸点を持つgroupへ盾pipsと失効境界を表示する。
- 敵の通常・反攻予告を重ね、失効まで耐えるかをpreviewする。

### At expiry

1. 全expired pipを同時に暗転
2. doomed groupsを同時に点滅
3. 同時capture
4. 王石結果または予約報酬を表示
5. 領地・施設差分を表示

個々のeffectを順番に消して、一つずつ石を取る演出にしない。

## Telemetry

- effect source、amount、anchor、grant turn、expiry turn
- expiry時のreal / timed / continuous / effective liberty内訳
- doomed group count、stone count、king involvement
- simultaneous both-king result
- reserved draw / qi、deferred choice
- standard capture rewardsとsacrifice pressure
- result topology `first_seen`
- expiry capture後の領地差、施設状態差
- expiryまで保護中に受けた通常敵行動数・反攻行動数

## Edge cases

1. 一groupへ+1 effectが2個あり同時失効しても、一度だけcaptureする。
2. 一effectだけ失効し、別の未来effectが残るgroupは残存bonus込みで判定する。
3. 霊泉のcontinuous modifierは失効しない。
4. 通常行動と反攻行動の間に失効しない。
5. merge後もanchorを含むgroupへeffectが追従する。
6. anchorが先にcaptureされたeffectは`carrier_removed`で消え、expiry eventを再発行しない。
7. 複数色のdoomed groupは一snapshotから同時除去する。
8. 両王石同時captureはplayer loss。
9. 終局batchでは囮石・血石・capture reward・反攻圧を発動しない。
10. 非終局のenemy-window draw / qiは予約する。
11. expiry captureが過去topologyを再現しても不発にしない。
12. expiryによる黒領地成立は次turn収入へ反映するが、暗黙の余勢・妙手を与えない。
13. due effectが0件ならexpiry sweep eventを発行しない。
14. sacrifice pressureはexpiry capture trigger後、敵ターン終了基礎反攻増加より前に処理する。
15. expiry sweep開始後に付与されたeffectは次の敵ターン終了まで残る。

## Acceptance criteria

- 失効effectを一括除去し、一snapshotのdoomed groupを同時captureする。
- 黒王石loss、白王石win、両王石lossの優先順位が一義的である。
- 終局batchで利益triggerが抑止される。
- 非終局captureのtrigger順とclosed-window予約が一義的である。
- mandatory topology revisitが実行される。
- 失効phaseが反攻追加行動後、敵ターン終了基礎増加前にある。
- `TLE-01`〜`TLE-15`が仕様checkerを通る。
