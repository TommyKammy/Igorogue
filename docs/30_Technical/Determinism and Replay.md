---
type: technical-spec
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Determinism and Replay

## 再現キー

```text
game_version
content_hash
initial_seed
ordered_commands
```

## RNGストリーム

- `gameplay`: 敵行動候補、カード効果。
- `reward`: 報酬、ショップ。
- `cosmetic`: 演出専用。ゲーム結果へ影響しない。

ストリームを分けても、用途を途中で変更するとリプレイ互換性が壊れるため、イベント仕様として管理する。

## 座標の正規化

- command、replay、fixture、telemetryの点は[[Coordinate System and Initial Position]]のCanonicalPointを使用する。
- JSONでは`[x,y]`、各軸1〜7。内部0-based座標を保存形式へ露出させない。
- 盤面配列と`StoneTopologyKey`はcanonical index、すなわち`y`昇順、次に`x`昇順で正規化する。
- 盤面図を読み書きする境界だけ、上段`y=7`の表示行順へ変換する。

## コマンドログ

カードID、CanonicalPointの対象座標、選択肢、ターン終了、報酬選択を保存する。

## チェックサム

各確定コマンド後に、正規化したDomain Stateのhashを記録する。リプレイ再生時に差異を即検出する。


## 反復履歴

- `StoneTopologyKey`観測列とSeen集合は合法性へ影響するDomain Stateであり、状態checksumへ含める。mandatory removalに限り観測列は重複keyを含み得る。
- 中断セーブでは順序付き履歴を保存するか、同一バージョンの初期状態とコマンドログから完全再構築する。
- 派生hash setの反復順序をゲーム結果へ利用せず、キー生成と履歴追加順はCanonical point orderと確定イベント順に従う。
- 不合法候補の仮実行は履歴とRNGストリームを消費しない。
- 詳細は[[ADR-0011 Battle-Local Stone Topology Repetition Ban]]。


## 施設状態

- 設置済み施設instance、座標、所有色、建設順、明示的停止源をDomain checksumへ含める。
- active／`territory_control_lost`状態は盤面と施設instanceから決定論的に再導出できる。
- 複数施設の停止・再稼働イベントはCanonical point order、instance ID順。
- 施設建設・停止・再稼働・直接破壊だけでは`StoneTopologyKey`履歴を追加しない。
- 不合法な施設点配置の仮実行は施設状態とRNGを変更しない。
- 詳細は[[ADR-0012 Facility Sites Are Empty Intersections]]。


## 反攻状態

- 反攻ゲージはbinary floatではなく整数unitで保存し、`2 units = 表示1点`とする。
- `GaugeUnits`、Pending、planned counterattack intent、熱使用flag、過伸展counter、犠牲remainderをDomain checksumへ含める。
- 閾値到達時の200 units減算、Pending作成、反攻意図計画は同じ確定event sequenceで行う。
- Pending中のoverflowを保存し、同一敵ターンで追加行動を複数回実行しない。
- 戦闘開始時のresetは合計コミから決定論的に再計算する。
- 詳細は[[FEAT-003 Komi Counterattack and Heat]]。

## 仮呼吸点と失効掃引

- timed effect instance、anchor stone instance、amount、created sequence、expiry enemy-turn indexをchecksumへ含める。
- due effectはCreatedSequence / ID順、doomed groupはgroup anchor順で処理する。
- capture snapshotは全due effect除去後、stone removal前に一度だけ作る。
- `TurnReservedDraw`、`TurnReservedQi`、DeferredPlayerChoicesもchecksumと中断saveへ含める。
- mandatory expiry captureが既出topologyへ戻っても結果を拒否せず、`first_seen=false`で観測する。
- 正本は[[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]。

