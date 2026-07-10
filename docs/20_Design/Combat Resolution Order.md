---
type: feature-summary
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Combat Resolution Order

## カード1枚の原子的解決

```text
対象プレビュー
→ 配置タグ・占有検査
→ 石配置と必須捕獲の仮解決
→ 自殺手検査
→ StoneTopologyKey反復検査
→ 気コストと必要なら余勢1を予約
→ コマンド確定
→ MomentumChanged(-1, spent_for_momentum_reach)（該当時）
→ カード主要効果
→ 石配置／同時捕獲を確定
→ FacilityDestroyed? at placement point
→ StoneTopologyKeyを履歴登録
→ 王石勝敗
→ 捕獲・施設破壊トリガー
→ 領地再計算と領地事実をanchor順で確定
→ 施設停止・再稼働を座標順で確定
→ TerritoryEstablishedと施設状態を確定
→ 普遍領地source／地合い流施設sourceの余勢生成
→ 余勢前進ドロー判定
→ 妙手倍率
→ ドロー・気・魂
→ カード移動
→ 状態チェックサム
```

反復検査の正本は[[ADR-0011 Battle-Local Stone Topology Repetition Ban]]。
`stone_topology_repetition`で不合法になった場合、コスト予約以降へ進まず、配置・捕獲・カード移動・トリガー・履歴登録を行わない。

## 施設点の原子的処理

- 施設点は仮配置、自殺手、同時捕獲、盤面反復の検査中は空点として扱う。
- 不合法手では施設状態を一切変更しない。
- 合法手の配置点に施設がある場合、石配置と同時捕獲の確定後、`StoneTopologyRegistered`より前に`FacilityDestroyed(reason=stone_occupied)`を確定する。
- 施設破壊自体は`StoneTopologyKey`へ含めない。
- 領地再計算後、複数施設の停止・再稼働イベントはCanonical point order、instance ID順で発行する。
- 施設建設だけのカードは石配置経路を通らず、`FacilityBuilt`、`FacilityActivated`、後続トリガーの順で解決する。

厳密な意味論は[[ADR-0012 Facility Sites Are Empty Intersections]]と[[FEAT-001 Territory and Facilities]]を参照する。

## 同時捕獲

一手で呼吸点0になった複数の相手グループは同時に除去し、すべての捕獲イベントを安定した座標順で発行する。

## トリガー順

通常captureと失効captureは[[FEAT-005 Sacrifice Triggers]]の共通`CaptureBatch`へ正規化する。

1. ルール必須処理と王石結果gate
2. 非終局なら標準capture accounting
3. source action／armed capture effect
4. 捕獲された石自身（group anchor、stone point、effect array順）
5. 流儀・家元印
6. プレイヤー遺物（装備順）
7. 施設（座標順、instance ID）
8. 敵パッシブ（content ID順）
9. 犠牲反攻
10. スコア・テレメトリ

黒王石capture、または白王石captureによる終局batchでは2以降の利益を抑止する。両王石同時captureは敗北。

## 余勢modeの原子的処理

- 余勢modeはカードの印刷済み配置タグとは別の`momentum_reach`として事前選択する。
- 通常配置だけで合法な点では余勢modeを使わず、余勢を消費しない。
- 自殺手、盤面反復、対象不正で拒否された場合、気、余勢、カードzoneを変更しない。
- 余勢は合法command確定後、`StonePlaced`より前に1消費する。
- その配置で黒領地が成立した場合、領地再計算後に余勢を再生成できる。
- 前線前進ドローは、解決後収入と最短王石距離を用いて各プレイヤーターン1回判定する。
- 厳密仕様は[[FEAT-002 Momentum]]。


## 敵ターン終了と仮呼吸点失効

```text
通常敵行動を完全解決
→ 当該ターンに予定された反攻追加行動を完全解決
→ 現Pendingを消費しoverflowから次Pendingを再prime
→ due仮呼吸点effectを全て一括失効
→ 全groupを同一snapshotで再計算
→ 有効呼吸点0のgroupを同時capture
→ mandatory StoneTopologyKey観測
→ 黒王石loss / 白王石win gate
→ 非終局capture trigger
→ 領地再計算、施設停止・再稼働
→ 敵ターン終了基礎反攻増加
→ 次意図計画
```

- 仮呼吸点は通常行動と反攻追加行動の両方へ有効。
- effectを一つずつ失効して途中captureしない。
- 失効captureのplayer draw / qiは次turnへ予約し、choiceはDeferredPlayerChoiceとする。
- 失効captureで成立した領地は暗黙の余勢・妙手を生成しない。
- 厳密仕様は[[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]、[[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]、[[FEAT-005 Sacrifice Triggers]]。

## 反攻advanceと敵ターン

- 各反攻sourceはatomic resolutionの該当事実が確定した後に`CounterattackAdvanced`を発行する。
- 妙手倍率がx3へ初到達した場合、倍率event直後に熱をadvanceする。
- 閾値到達でPendingを作った時点で、次の反攻意図をFEAT-009に従って計画・表示する。
- 敵ターンは通常行動を完全解決してから、ターン開始時にPendingだった反攻追加行動を最大1回解決する。
- 反攻後のoverflow再prime、仮呼吸点失効掃引、敵ターン終了基礎増加、次意図計画の順で処理する。
- 戦闘終了後は新しい反攻advance、追加行動、基礎増加を行わない。
- 厳密順序は[[FEAT-003 Komi Counterattack and Heat]]。
