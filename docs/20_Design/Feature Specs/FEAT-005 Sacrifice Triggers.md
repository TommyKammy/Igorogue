---
type: feature-spec
id: FEAT-005
status: accepted
project: Igorogue
updated: 2026-07-10
version: 1.0.0
---
# FEAT-005 Sacrifice and Capture Triggers

## Player promise

取られた石の種類と構成に応じて、次ターンの手札、魂、反攻圧へ明確に変換する。王石を失ったときだけは利益で敗北を打ち消せない。

## Capture batch

通常配置captureと[[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep|失効capture]]は、共通の`CaptureBatch`へ正規化する。

- groupはanchorのCanonical point order。
- group内stoneはpoint order。
- 一手・一掃引で同じstone instanceを二度triggerしない。
- captured stoneの`on_captured`はstone instanceごとに個別発火する。

## Terminal gate

`GroupCaptured`とトポロジー確定後、benefit triggerより前に王石を確認する。

```text
black king captured => BattleLost
else white king captured => BattleWon
else non-terminal trigger processing
```

両王石同時captureは敗北。終局batchではcapture reward、囮石、血石、流儀、印、遺物、施設、犠牲反攻を処理しない。

## Non-terminal trigger order

1. 標準capture accounting
2. source action / armed capture effect
3. captured stone self trigger
4. style and family seal
5. player relic（装備slot順）
6. facility（point order、instance ID）
7. enemy passive（content ID順）
8. sacrifice counterattack pressure
9. score and telemetry

同一段階ではstable IDまたは本文effect array orderを使用する。

## Starter sacrifice content

### Lure stone

- 配置時の最低保証`reserve_draw +1`はカード主要効果として処理済み。
- captureされた場合、追加で`reserve_draw +2`。

### Blood stone

captureされた場合:

```text
reserve_draw +1
soul +1
```

### Sacrifice style

- 各戦闘、最初に王石以外の黒石が実際にcaptureされたbatchで`reserve_draw +2`。
- 敵capture、失効captureのどちらでもよい。
- 王石を含む終局batchでは発火しない。
- 王石以外の黒石3個ごとに[[FEAT-003 Komi Counterattack and Heat]]の犠牲反攻を進める。

### Sacrifice family seal

装備時、各戦闘最初の王石以外のfriendly capture batchで`reserve_draw +2`。流儀効果と別instanceであり、両方装備時は両方発動する。

## Closed player window

敵ターン中と失効掃引中:

- drawは`TurnReservedDraw`
- qiは`TurnReservedQi`
- choiceは`DeferredPlayerChoice`
- soulは即時

player action window内では、効果本文が`reserve_draw`を指定しない限り通常の即時処理を行う。

## Capture attribution

- 石配置capture: 配置色がcapturing color。
- temporary-liberty-expiry capture: captured colorの反対色がcapturing color。
- white groupをblackがcaptureした場合、通常capture rewardの対象。
- black groupがwhiteにcaptureされた場合、friendly sacrifice triggerの対象。

## Board-mutating triggers

v0.2 M3ではcapture triggerから石の追加・除去を行わない。`種還り`等を導入する場合はnested atomic resolution、反復判定、trigger recursion capを別Featureで定義する。

## Events

最低限:

```text
CaptureBatchStarted
GroupCaptured
CaptureBenefitSuppressed
TurnReservedDrawChanged
TurnReservedQiChanged
DeferredPlayerChoiceCreated
SoulChanged
SacrificeFirstCaptureTriggered
SacrificeBatchAdvanced
CaptureBatchResolved
```

## Telemetry

- batch reason、capturing window、capturing color
- group / stone stable order
- stone kind別trigger
- terminal suppression count
- reserved draw / qi、soul、deferred choice
- sacrifice first-capture use、batch、remainder、counterattack delta

## Edge cases

1. 同一group内の囮石2個は各+2予約draw。
2. 囮石と血石はpoint orderで個別発火する。
3. 敵ターンcaptureのdrawは手札へ直接入れない。
4. 流儀と家元印のfirst-capture bonusは別々に発火できる。
5. 王石と血石が同一terminal batchに含まれても血石利益を抑止する。
6. capture reward上限へ達していてもcaptured-stone self triggerは発火する。
7. 犠牲stone remainderは戦闘内保持し、戦闘境界で0へ戻る。
8. 失効captureは通常captureと同じtrigger pipelineを使う。

## Acceptance criteria

- 王石captureは利益発動前に勝敗を決める。
- 同一group内の複数特殊石を個別発火する。
- 敵ターンdraw / qi / choiceを予約または延期する。
- trigger順がUI装備並べ替え以外の非決定要因に依存しない。
- B-2の失効fixtureでstone trigger、terminal suppression、sacrifice pressureが検証される。
