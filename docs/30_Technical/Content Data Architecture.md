---
type: technical-spec
status: proposed
project: Igorogue
updated: 2026-07-10
---
# Content Data Architecture

## 正本

`game_data/content/*.json` と `game_data/balance/*.json`。

## 分離

- Content：ID、名称キー、タグ、効果構成。
- Balance：コスト、倍率、上限、出現重み。
- Localization：将来の文字列テーブル。

## 点の表現

- pointを持つcontent、balance、fixtureは[[Coordinate System and Initial Position]]のCanonicalPointを使う。
- JSON表現は`[x,y]`、各軸1〜7。
- 内部0-based座標を`game_data/`へ保存しない。
- 盤面図のrow indexをpointの`y`として直接扱わない。

## バリデーション

- ID一意。
- 参照先存在。
- コスト非負。
- 配置タグが既知。
- 効果の順序が明示。
- v0.2スコープ外IDをビルドへ含めない。

スキーマ例は`docs/30_Technical/Schemas/`を参照。


## 余勢と流儀rule

- 余勢の共通数値・幾何条件は`game_data/balance/system.json`の`momentum`を正本とする。
- 地合い流の追加生成rule IDは`facility_build_grants_momentum`。
- `styles.json`でこのruleを持つのは`style_territory`だけである。
- Rules Kernelは表示名やMarkdown文言ではなくrule IDとsystem dataを参照する。


## 反攻の固定小数点

- 反攻のruntime値は`game_data/balance/system.json`の`counterattack`を正本とする。
- `gauge_unit_scale=2`で、2 unitsを表示1点として扱う。
- Rules Kernelはbinary floatでゲージを保存せず、unit整数を使う。
- `styles.json`は`overextension_counterattack`と`sacrifice_counterattack`のrule IDだけを持ち、数値を重複しない。
