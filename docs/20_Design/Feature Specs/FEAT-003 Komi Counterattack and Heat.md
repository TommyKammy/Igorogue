---
type: feature-spec
id: FEAT-003
status: accepted
project: Igorogue
version: 1.0.1
updated: 2026-07-10
source_decision: ADR-0013
---
# FEAT-003 Komi Counterattack and Heat

## Player promise

持ち込んだ家元印が強いほど、白は早く反攻する。ただし反攻は印を直接無効化せず、予告された追加行動として盤面へ返ってくる。

領地・捕獲・妙手を一ターンへ集約した爆発は止めない。その代わり、爆発による`heat`が反攻を近づけ、プレイヤーへ「今取り切る／一度守る」の判断を作る。

## Scope

このFeatureは次を正本化する。

- 反攻ゲージの固定小数点表現
- コミ0〜9の開始値と自然増加
- 妙手倍率による熱
- 攻め碁流の過伸展
- 捨て石流の大量犠牲
- 閾値到達、overflow、pending、追加行動の順序
- 戦闘境界でのreset

白王石の厚みトークン量は本Featureの非対象である。コミを厚みへ変換する仕様は別Featureで確定する。

## Source of truth

- 数値: `game_data/balance/system.json`の`counterattack`
- 流儀rule ID: `game_data/content/styles.json`
- 設計判断: [[ADR-0013 Baseline Pace and Burst-Driven Counterattack]]
- 机上比較: [[BAL-0001 Counterattack Curve v0.2.6]]
- fixture: [[FEAT-003 Counterattack Curve Fixtures]]

## Fixed-point representation

反攻ゲージは浮動小数点を使わず、内部unitで保持する。

```text
2 units = 表示上の反攻1点
threshold = 200 units = 100点
```

UIはbarをunit精度で描画し、数値ラベルでは`.5`を表示してよい。リプレイ、セーブ、イベント、checksumはunit整数を正本とする。

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

`Pending`は「次の敵ターンで通常行動後に追加行動1回を行う」ことを表す。

## Battle start

合計コミを`K`、`0 <= K <= 9`とする。

```text
GaugeUnits = 20 + 4K
表示点 = 10 + 2K
Pending = false
HeatUsedThisPlayerTurn = false
SuccessfulAttackCardsThisPlayerTurn = 0
OverextensionUnitsThisPlayerTurn = 0
SacrificeStoneRemainder = 0
```

反攻状態は戦闘ごとに再初期化し、戦闘間へ持ち越さない。

## Baseline pace

戦闘が継続している敵ターンの終了時、通常行動と反攻追加行動をすべて解決した後で次を加える。

```text
EnemyTurnEndGainUnits = 12 + K
表示点 = 6 + 0.5K
```

基礎増加は敵ターン1回につき一度だけであり、反攻追加行動があっても二重に得ない。

他の増加が一切ない場合の机上到達は次のとおり。

| コミ | 開始点 | 敵ターン終了増加 | 閾値到達後 | 最初の追加行動 |
|---:|---:|---:|---:|---:|
| 0 | 10 | 6 | 敵ターン15終了 | 敵ターン16 |
| 1 | 12 | 6.5 | 敵ターン14終了 | 敵ターン15 |
| 2 | 14 | 7 | 敵ターン13終了 | 敵ターン14 |
| 3 | 16 | 7.5 | 敵ターン12終了 | 敵ターン13 |
| 4 | 18 | 8 | 敵ターン11終了 | 敵ターン12 |
| 5 | 20 | 8.5 | 敵ターン10終了 | 敵ターン11 |
| 6 | 22 | 9 | 敵ターン9終了 | 敵ターン10 |
| 7 | 24 | 9.5 | 敵ターン8終了 | 敵ターン9 |
| 8 | 26 | 10 | 敵ターン8終了 | 敵ターン9 |
| 9 | 28 | 10.5 | 敵ターン7終了 | 敵ターン8 |

これは無活動時の上限目安であり、熱、過伸展、犠牲で早まる。

## Heat

熱は高コミ専用ではなく、全コミ帯で「爆発が白の反応を早める」ための共通ruleである。

プレイヤーターン中、妙手倍率が初めて`x3.0`未満から`x3.0`以上へ到達した瞬間、戦闘が継続中なら一度だけ次を加える。

```text
HeatUnits = 48 + 4K
表示点 = 24 + 2K
```

- 既に`HeatUsedThisPlayerTurn == true`なら、そのターン中に倍率がさらに上がっても追加しない。
- 熱はターン終了時ではなく、最初のcrossingで即時発生する。プレイヤーは反攻予告を見たうえで残りの手を選べる。
- プレイヤーターン開始時に`HeatUsedThisPlayerTurn`をfalseへ戻す。
- 戦闘終了を起こした解決では熱を発生させない。

## Attack-style overextension

`style_attack`だけが`overextension_counterattack`を持つ。

- プレイヤーターン中に正常解決した`attack`タグ札を数える。
- 1枚目は増加なし。
- 2枚目以降、1枚につき`16 units = 8点`。
- 一つのプレイヤーターンで最大`48 units = 24点`。
- 拒否、キャンセル、対象不正のカードは数えない。
- 追加行動へ至らず戦闘が終了した場合、以後の増加を抑止する。

これは攻撃を禁止せず、「このターンに取り切れない場合の白の反応」を早める。

## Sacrifice-style pressure

`style_sacrifice`だけが`sacrifice_counterattack`を持つ。

敵の捕獲で黒王石以外の黒石が確定除去されるたび、石数を戦闘内remainderへ加える。

```text
3石ごとに 30 units = 15点
```

- 2石以下の余りは同じ戦闘中に保持する。
- 一度に7石取られた場合は2 batch、余り1。
- 黒王石を含む捕獲で戦闘が終了した後は増加・pending作成を行わない。
- 戦闘境界で余りを0へ戻す。

## Advance and scheduling

すべての増加sourceは次の純粋な処理を使う。

```text
advance_counterattack(delta_units, reason):
    if BattleEnded:
        return no_change

    GaugeUnits += delta_units
    emit CounterattackAdvanced

    if Pending == false and GaugeUnits >= 200:
        GaugeUnits -= 200
        Pending = true
        plan next enemy-turn counterattack intent
        emit CounterattackPrimed
```

一度のadvanceで閾値を複数回超えても、`Pending`は最大1。Pending中は追加gainをそのまま`GaugeUnits`へ保持し、閾値を即時に複数消費しない。

## Enemy turn order

```text
1. 敵ターン開始時のPendingを実行予定として固定
2. 通常行動1回を完全解決
3. 戦闘終了なら反攻を中止
4. 実行予定があれば、FEAT-009に従う反攻追加行動1回
5. 実行済みPendingをfalseへする
6. overflowでGaugeUnits >= 200なら、一度だけ200を引き、次敵ターンのPendingを作る
7. [[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]の失効掃引を完全解決
8. 戦闘継続中なら敵ターン終了基礎増加を一度適用
9. 基礎増加で閾値を超え、Pendingがなければ次敵ターンをprime
10. 次の通常意図と反攻意図をUIへ公開
```

一つの敵ターンで反攻追加行動は最大1。overflowにより連続する敵ターンで反攻することはある。

## Timing examples

### Player-turn crossing

```text
Gauge 95点
→ 攻め碁流2枚目の攻撃札 +8
→ 103点
→ 100点を消費し、残り3点、Pending=true
→ 同じプレイヤーターン中にUIへ次の反攻を表示
→ 直後の敵ターンで通常行動後に追加行動
```

### Enemy-turn-end crossing

```text
Gauge 95点, K=0
→ 敵ターンの全行動を解決
→ 基礎+6
→ 101点
→ 残り1点、Pending=true
→ 追加行動は同じ敵ターンではなく次の敵ターン
```

### Overflow

```text
Pending=true, Gauge 110点
→ 今回の敵ターンで追加行動1回
→ Pendingを消費
→ Gaugeから100点を引き残り10点
→ 次の敵ターンもPending=true
```

## UI

常時表示:

- 現在ゲージ／100
- 次の自然増加量
- 今選択中の手による差分とreason
- `Pending`時の「次の敵ターン +1手」badge
- 予告済み反攻意図、対象、第1候補、代替候補

熱発生点、過伸展札、犠牲batch成立点には事前予測を表示する。

## Events

```text
CounterattackAdvanced(
  old_units,
  new_units,
  delta_units,
  reason,
  source_id,
  command_id?
)

HeatGenerated(komi, brilliant_before, brilliant_after, delta_units)
CounterattackPrimed(residual_units, execute_on_enemy_turn_index)
CounterattackBonusStarted(enemy_turn_index, intent_id)
CounterattackBonusResolved(enemy_turn_index, action_result)
CounterattackPendingCarried(residual_units, execute_on_enemy_turn_index)
CounterattackReset(komi, start_units)
```

`CounterattackTriggered`という旧総称は互換aliasとしてのみ残し、新実装は上記の事実eventを使う。

## Telemetry

最低限記録する。

- battle startのコミ、開始unit
- 各advanceのreason、delta、before／after
- 初めてPendingになったプレイヤー／敵ターン
- 反攻追加行動を実行した敵ターン
- 熱発生直前・直後の妙手倍率
- 過伸展札数とcap超過
- 犠牲石数、batch数、remainder
- 爆発分類の前後何ターンで反攻したか
- 反攻予告後の守備札使用、攻撃継続、ターン終了

## Edge cases

1. 通常行動で黒王石を取った場合、反攻追加行動と基礎増加を行わない。
2. プレイヤーが白王石を取った解決では、熱・過伸展・犠牲による新規pendingを作らない。
3. Pending中の増加は失わず、同一敵ターンに2回行動へ変換しない。
4. 反攻候補が全て不合法なら敵は追加行動をpassするが、Pendingは消費済みとする。
5. 基礎増加は追加行動と仮呼吸点失効掃引の完全解決後、戦闘継続中に1回だけ。
6. 妙手倍率が`2.9 -> 3.0 -> 4.2`となっても熱は一度だけ。
7. 一度`3.0`へ達した後に効果で倍率が下がり、再度上がっても同ターンは熱を得ない。
8. 攻撃札が拒否された場合、過伸展枚数へ数えない。
9. 同時捕獲で犠牲石が6個なら2 batchを一度に処理する。
10. 戦闘ロード時は保存されたunit、Pending、remainderを復元し、派生UIだけ再構築する。

## Acceptance criteria

- コミ0〜9すべてで、他sourceなしでも閾値到達が敵ターン15終了以内、追加行動が敵ターン16以内である。
- コミ1増加ごとに開始unitが4、自然増加が1unit増え、大きな段階buffを追加しない。
- 熱は全コミ帯でx3初回crossingに発生し、同ターン一度だけである。
- 反攻追加行動は一敵ターン最大1で、overflowを保存する。
- 戦闘間でゲージ、Pending、流儀counterを持ち越さない。
- `CTR-01`〜`CTR-25`が仕様checkerを通る。
- M3では爆発ラン中の反攻遭遇60%以上と、予告後の攻守分岐を人間テストで検証する。
