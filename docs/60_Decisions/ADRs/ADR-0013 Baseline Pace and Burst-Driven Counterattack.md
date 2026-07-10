---
type: adr
id: ADR-0013
status: accepted
project: Igorogue
updated: 2026-07-10
supersedes: ADR-0008
---
# ADR-0013 Baseline Pace and Burst-Driven Counterattack

## Context

Igorogueの第2設計柱は「仕込み → 発火 → 反攻」である。しかし旧データは次だった。

```text
threshold = 100
start = 4 * komi
enemy-turn gain = komi
```

この式ではコミ0は永遠に増えず、コミ4でも20敵ターン終了時に96である。低コミプレイヤーは反攻をほぼ体験せず、爆発後に取り切るか守るかという第三幕が存在しない。

また、熱がコミ7以上だけに限定されていたため、低コミで大きな妙手連鎖を起こしても敵の反応速度が変わらなかった。

## Decision

反攻は次の三層で進める。

1. **Baseline pace**: コミ0でも長期戦なら必ず反攻へ至る。
2. **Smooth komi acceleration**: コミ1ごとに開始値と毎敵ターン増加を少しずつ上げる。
3. **Burst-driven pressure**: 全コミ帯で妙手x3、攻めの過伸展、捨て石の大量犠牲が反攻を早める。

浮動小数点を避けるため、2 internal unitsを表示1点とする。

```text
threshold units = 200
start units = 20 + 4K
enemy-turn-end gain units = 12 + K
heat units = 48 + 4K at first x3 crossing each player turn
```

攻め碁流は2枚目以降の攻撃札ごとに16 units、一ターン最大48 units。捨て石流は王石以外の自石3個捕獲ごとに30 unitsを加える。

閾値を超えたら200 unitsを一度だけ引き、次の敵ターンへ1回の追加行動を予約する。Pending中のoverflowは保持し、一つの敵ターンでは追加行動を最大1回に制限する。

厳密な処理は[[FEAT-003 Komi Counterattack and Heat]]を正本とする。

## Why this curve

- コミ0でも自然経過だけで敵ターン16に初反攻し、20ターン制限内に第三幕が存在する。
- コミ9では自然経過だけで敵ターン8に初反攻し、高コミの始動力へ明確な時間圧を返す。
- コミ1ごとにunitが線形増加し、「コミ4までは得、5から突然損」の大きな閾値を作らない。
- 熱を全コミへ広げることで、反攻が単なる持込税ではなく、プレイヤーの急加速に対する盤面上の反応になる。
- 爆発そのものを止めず、爆発後の敵行動を増やすため、爽快感と緊張を時間的に分離できる。

## Rejected alternatives

### Keep the old komi-only curve

拒否。低コミでゲージが進まず、中心体験の第三幕を欠く。

### Give every explosion an immediate bonus enemy action

拒否。ゲージの予告価値とコミ差を消し、爆発を直接罰する感覚が強い。

### Add discrete counterattack tiers at komi 3/6/9

拒否。装備選択がtier直前へ収束し、滑らかなコミ補償に反する。

### Increase only the base enemy-turn gain

拒否。時間切れ対策にはなるが、爆発と反攻の因果が生まれない。

### Allow multiple bonus actions in one enemy turn

拒否。王石即死と情報過多を招く。overflowは次の敵ターンへ送る。

## Consequences

### Positive

- 低コミでも長期戦で反攻を体験する。
- 高コミ、過伸展、犠牲、大爆発が同一の可視ゲージへ集約される。
- プレイヤーが原因をUIで説明できる。
- unit整数により決定論的リプレイとchecksumを維持できる。

### Negative

- 反攻状態に固定小数点unit、Pending、流儀counterが増える。
- 低コミの防御型戦闘が旧案より難しくなる可能性がある。
- 妙手x3が頻発する場合、熱が実質的な毎ターン税になる。
- 高コミ速攻が反攻前に取り切るだけの定石になる可能性は残る。

## Validation

- [[BAL-0001 Counterattack Curve v0.2.6]]の机上表でコミ0〜9を比較する。
- [[FEAT-003 Counterattack Curve Fixtures]]の`CTR-01`〜`CTR-25`を仕様checkerで検証する。
- M3では、爆発ラン中の反攻遭遇60%以上、反攻予告後の攻撃継続／守備使用の両方が観測されることを確認する。
- 正式Rules Kernel実装後に同fixtureをunit／golden replayへ移植する。

## Superseded decision

[[ADR-0008 Setup Ignition Counterattack Curve]]の方向性を具体化し、本ADRで置き換える。
