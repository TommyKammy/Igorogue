---
type: test-strategy
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Testing Strategy

## Unit

- CanonicalPoint／InternalPointの往復変換
- canonical index／pointの往復変換
- 点対称の自己逆性と標準初期配置
- 盤外座標の拒否
- 隣接
- グループ
- 実呼吸点
- 仮配置
- timed仮呼吸点のanchor、stack、merge追従、carrier removal
- enemy-turn-end全effect同時失効
- 同時捕獲
- 自殺手
- StoneTopologyKey正規化
- 盤面反復禁止
- 領地
- 施設容量
- 施設点の空点意味論
- 施設停止・再稼働・直接踏破
- 余勢の普遍領地sourceと地合い流施設source
- 余勢到達の距離2・中間空点・通常配置優先
- 不合法余勢配置の全状態不変
- 前線前進ドローの距離、収入閾値、1ターン上限
- コミ0〜9の反攻開始値・敵ターン終了増加・自然到達ターン
- 熱のx3初回crossingと同ターンcap
- 過伸展cap、犠牲batch remainder、Pending、overflow、戦闘reset
- terminal king gateとcapture benefit suppression
- closed-window draw / qi / choice予約
- mandatory expiry topology revisit
- トリガー順

## Property

- 任意の盤内点`p`で`to_canonical(to_internal(p)) == p`。
- 任意の盤内点`p`で`reflect(reflect(p)) == p`。
- 標準初期配置は色・役割交換付き点対称で、各王石グループは3石・実呼吸点7。
- diagram row orderとCanonical point orderの変換は可逆。
- 捕獲後に盤面上の全グループは実呼吸点1以上、仮呼吸点例外を除く。
- 領地空点は同時に黒領地と白領地にならない。
- 同一入力のリプレイchecksum一致。
- 候補配置で確定する`StoneTopologyKey`はSeen集合と重複しない。mandatory expiry removalの観測列は重複を許す。
- 特殊石種または非石状態だけを変えても反復判定結果は変わらない。
- 施設の追加・除去だけでは実呼吸点、空点領域、領地サイズ、StoneTopologyKeyが変わらない。
- 合法な確定状態では石と施設が同一点に共存しない。
- 不合法な施設点配置では施設instanceとイベント列が不変。
- 領地喪失と奪還を往復しても施設instance ID、owner、build sequenceが保たれる。
- 余勢amountは常に0〜capで、ターン境界で保持され、戦闘境界で0へ戻る。
- 印刷済み配置だけで合法な点では、余勢候補集合に含まれず消費も起きない。
- 一つのatomic resolutionによる普遍余勢生成は、新黒領地region数にかかわらず最大1。
- 反攻追加行動は一敵ターン最大1で、Pending中のgainは失われずoverflowへ残る。
- コミ0〜9の全帯で、他sourceなしの最初の反攻追加行動が敵ターン20以内に発生する。

## Golden Replay

特定seedと入力列を保存し、イベント列と最終hashを比較する。標準初期盤面のcanonical配列・点対称、単純コウ、長周期の中間トポロジー再現、施設点踏破・停止・再稼働、余勢生成・消費・前進ドロー、仮呼吸点同時失効・王石gate・closed-window予約、反攻曲線・熱・Pending・overflowを必須fixtureに含める。

## Integration

カード、遺物、敵意図を含む短い戦闘fixture。

## UX

自動テストで「理解しやすさ」を完了扱いしない。人間プレイテストが必要。
