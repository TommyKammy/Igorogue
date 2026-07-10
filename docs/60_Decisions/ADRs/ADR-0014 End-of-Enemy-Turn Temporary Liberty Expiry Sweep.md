---
type: adr
id: ADR-0014
status: accepted
project: Igorogue
updated: 2026-07-10
decision_scope: v0.2
---
# ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep

## Context

仮呼吸点は、実呼吸点が0になったグループを敵ターン終了まで盤上へ残せる。したがって失効境界では、通常の石配置とは異なる次の問題を一義的に解決する必要がある。

- 複数の仮呼吸点effectを個別に失効させた途中状態で、捕獲結果が変化してはならない。
- 黒・白の複数グループ、または両王石が同時に有効呼吸点0になり得る。
- 捕獲された囮石・血石、流儀、家元印、遺物、施設のトリガー順が必要である。
- 敵ターン中に発生したドロー・気・選択を、閉じたプレイヤー操作windowで処理できない。
- 失効捕獲が過去の`StoneTopologyKey`へ戻る場合でも、mandatory effectを不発にできない。
- 反攻追加行動、失効捕獲、犠牲による反攻増加、敵ターン終了基礎増加の順が必要である。

## Decision

v0.2では、敵の通常行動と予告済み反攻追加行動をすべて解決した後、敵ターン終了基礎反攻増加より前に、**仮呼吸点失効掃引**を一度だけ行う。

### 1. Timed effect identity and target

`TemporaryLibertyEffect`は安定したeffect instance IDを持ち、付与時に対象グループ内のCanonical point order最小の石instanceを`AnchorStoneInstanceId`として保存する。

- effectはanchor stoneを含む現在の同色グループへ、一effect instanceにつき一度だけ加算する。
- グループが味方グループと結合してもeffectは追従する。
- 複数effectが同じ現在グループへ追従した場合は加算する。
- anchor stoneが捕獲等で盤面から消えた時点でeffectを`carrier_removed`として除去する。
- v0.2の通常ルールではグループの一部だけを直接除去しない。将来導入する場合はanchor移管規則を別途ADR化する。

`duration=enemy_turn_end`は、付与後最初に到来する敵ターン終了境界を意味する。失効掃引開始後に新しく付与されたeffectは、現在の掃引対象にせず次の敵ターン終了を指定する。

霊泉等の盤面条件による呼吸点は`ContinuousLibertyModifier`であり、timed effectではない。失効対象から除外し、捕獲snapshot作成時に再導出する。

### 2. Enemy-turn boundary order

```text
1. 敵ターン開始時に今回実行する反攻追加行動を固定
2. 通常敵行動を完全解決
3. 戦闘終了なら残りを中止
4. 固定済みなら反攻追加行動を完全解決
5. 今回実行したPendingを消費し、overflowから次敵ターンPendingを再prime
6. 仮呼吸点失効掃引
7. 失効捕獲トリガー、領地、施設を完全解決
8. 戦闘継続中なら敵ターン終了基礎反攻増加
9. 次の通常意図とPending反攻意図を計画・表示
```

仮呼吸点は通常行動と反攻追加行動の両方が終わるまで有効である。

### 3. Simultaneous expiry

現在の`EnemyTurnIndex`で失効する全effectを、`CreatedSequence`、次にeffect IDの昇順で列挙する。イベントは安定順に発行するが、Domain Stateからは**全effectを一括除去**する。

一effectを除去するたびに捕獲確認を行ってはならない。

### 4. Capture snapshot and simultaneous removal

全失効後、残存timed effectとcontinuous modifierを適用し、盤面上の全グループの有効呼吸点を再計算する。

```text
doomed_groups = every group whose effective liberty count == 0
```

- doomed group集合は、石を一つも除去していない同一snapshotから作る。
- doomed groupをgroup anchorのCanonical point orderで並べる。
- 集合内の全石を同時に除去する。
- 除去によって他のdoomed groupへ実呼吸点が生まれても、その掃引内では救済しない。
- 除去後に新たな有効呼吸点0グループは生じないため、再帰的な第二掃引を行わない。
- capture reasonは`temporary_liberty_expired`。
- 捕獲色は、除去されたグループと反対の色として記録する。白グループ失効は黒のcapture、黒グループ失効は白のcaptureとして通常のcapture accountingへ渡す。

### 5. Mandatory topology mutation

失効捕獲はmandatory rule resolutionであり、盤面反復禁止によって拒否しない。

- 捕獲後のキーを`OrderedStoneTopologyKeys`へ観測順に追加する。
- 既出キーでもmandatory mutationは実行し、観測列には重複を許す。
- `SeenStoneTopologyKeysCache`は集合のまま保持する。
- `StoneTopologyRegistered`は`first_seen`と`source_reason`を含む。
- プレイヤー、敵、自動石生成の**候補配置**は引き続きSeen集合を参照して反復手を拒否する。

### 6. King-result gate

全doomed groupを除去し、`GroupCaptured`とトポロジーeventを発行した後、利益トリガーより前に王石を確認する。

```text
if black king captured:
    BattleLost
else if white king captured:
    BattleWon
else:
    process capture benefits and later boundary steps
```

両王石が同一掃引で捕獲された場合は敗北である。終局batchでは、囮石・血石・魂・ドロー・気・犠牲batch・妙手・余勢等の利益トリガーを処理しない。

### 7. Non-terminal trigger order

非終局batchだけ、次の順で処理する。

1. 標準capture accounting（白グループ捕獲の魂等）
2. source action／事前armed capture effect
3. 捕獲された石自身の`on_captured`（group anchor順、group内point順、効果配列順）
4. 流儀、家元印、プレイヤー遺物（装備順）
5. 施設（Canonical point order、instance ID順）
6. 敵passive（安定content ID順）
7. 反攻の犠牲batch
8. スコア・テレメトリ

v0.2ではcapture triggerから石を直接追加・除去する効果をM3へ含めない。導入時はnested atomic resolutionの後継仕様を必要とする。

### 8. Closed player window

敵行動中または失効掃引中はプレイヤーカード操作windowが閉じている。

- player drawは`TurnReservedDraw`へ加算し、次プレイヤーターンの通常drawへ加える。
- player qiは`TurnReservedQi`へ加算し、次プレイヤーターンの基礎気・領地収入と同時に適用する。
- player choiceを要求する効果は`DeferredPlayerChoice`を作り、次プレイヤーターン開始時、気とカードを確定する前に解決する。
- 魂等のrun resourceは即時に確定してよい。

### 9. Territory, facilities, momentum, and brilliant multiplier

非終局capture trigger後に領地を再計算し、施設停止・再稼働を確定する。

- 次プレイヤーターンの領地収入には新状態を使用する。
- 失効掃引はblack-owned atomic commandではないため、領地変化だけでは普遍余勢を生成しない。
- player-turn scope外なので妙手倍率を増加しない。
- explicit content effectが余勢またはスコアを与える場合だけ例外とする。

### 10. Event minimum set

```text
TemporaryLibertyGranted
TemporaryLibertyRemoved
TemporaryLibertyExpirySweepStarted
TemporaryLibertyExpired
GroupCaptured(reason=temporary_liberty_expired)
StoneTopologyRegistered(first_seen, source_reason)
KingCaptured
CaptureBenefitSuppressed
TurnReservedDrawChanged
TurnReservedQiChanged
DeferredPlayerChoiceCreated
TemporaryLibertyExpirySweepResolved
```

## Consequences

### Positive

- 仮呼吸点が反攻を含む敵ターン全体を確実に保護する。
- effectごとの処理順で生死が変わらず、複数群・両王石を一義的に扱える。
- 捨て石報酬と王石敗北の優先関係が明確になる。
- 敵ターン中のドロー・気・選択が失われない。
- mandatory captureと盤面反復禁止を両立できる。

### Negative

- 両王石同時捕獲を敗北とするため、相打ちを狙う構成には厳しい。
- mandatory mutationにより、順序付きトポロジー観測列は重複を含み得る。
- 次ターン予約資源とDeferredChoiceがDomain Stateへ追加される。
- continuous modifierとtimed effectをcontent dataで区別する必要がある。

## Validation

- [[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]を実装仕様の正本とする。
- [[FEAT-011 Temporary Liberty Expiry Fixtures]]の`TLE-01`〜`TLE-15`を使用する。
- `game_data/fixtures/temporary_liberty_expiry_fixtures.json`を`tools/check_temporary_liberty.py`で検査する。
- M1では同fixtureをRules Kernel unit testとgolden replayへ移植する。
