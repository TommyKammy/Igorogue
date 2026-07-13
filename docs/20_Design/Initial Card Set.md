---
type: content-catalog
status: proposed
project: Igorogue
updated: 2026-07-13
---
# Initial Card Set

カードdefinitionの現在値は`game_data/content/cards.json`を正本とする。M2のstarting deck multisetは`game_data/content/starting_decks.json`を正本とし、`docs/30_Technical/Schemas/starting_deck.schema.json`に従う。このノートの表は両machine-readable sourceを読むための人間向けcatalogであり、runtime値の重複正本ではない。

## M2 starter 6種類／12枚

| カード | 枚数 | 気 | 配置／対象 | 効果 |
|---|---:|---:|---|---|
| 打石 | 5 | 1 | frontline/terminal | 基本石を置く |
| ノビ | 2 | 1 | frontline | 結果実呼吸点3以上で1ドロー |
| ツケ | 2 | 1 | contact/terminal | 敵をアタリにすれば気+1 |
| 補強 | 1 | 1 | 黒グループ | 仮呼吸点+1。対象がアタリなら1ドロー |
| 開拓 | 1 | 2 | 黒領地 | 開拓施設を置く |
| 囮石 | 1 | 0 | contact | 次ターン1ドロー予約。取られればさらに2 |

DECISION-0006 Option 1により、上記6種類と`5 / 2 / 2 / 1 / 1 / 1`のmultisetをM2へ採用する。`card_development`（開拓）だけがM2のfacility例外であり、開拓以外のfacility contentはM3以降に留める。


## 余勢eligible

[[FEAT-002 Momentum]]により、`type=stone`かつ印刷タグに`frontline`を持ち、黒石を1個置く札だけが余勢を使用できる。
現在の機械可読候補では、打石、ノビ、血石、種石が該当する。
`terminal`等で通常合法な点では余勢を消費しない。

## v0.2候補

| カード | 気 | 役割 |
|---|---:|---|
| 一間トビ | 1 | jump。孤立展開 |
| 辺這い | 2 | edge。領地完成で気+1 |
| 忍び | 2 | invasion。生存で1ドロー |
| 締め | 1 | 敵呼吸点を一時封鎖 |
| 血石 | 1 | 取られた場合のみドロー・魂 |
| 種石 | 1 | 遅延増殖 |
| 炉 | 2 | 施設。収入強化 |
| 祭壇 | 2 | 施設。犠牲を魂へ |
| 見張り台 | 2 | 施設。侵入弱体 |
| 割り込み | 2 | 異なる白グループ間へ配置 |
| 市場 | 2 | 施設。領地完成ドロー |
| 連続捕獲 | 2 | 次の捕獲で気+1、2ドロー |
| 大模様 | 3 | 2ターン後に中央模様を変換 |
| 群生 | 1 | 種石の成長を前倒し |
| 墓標 | 1 | 自石捕獲地点を未来の収入へ |
| 反撃 | 1 | アタリ救出と敵封鎖 |
| 呼吸炉 | 1 | 実呼吸点を気へ変換 |
| 死線 | 1 | アタリ利用時のコスト／ドロー |
| 星打ち | 2 | 中央への孤立配置 |
| 種還り | 1 | 捕獲地点へ遅延苗石 |
| 道場 | 2 | 施設。石札割引 |
| 眼作り | 2 | 将来用の生存トークン。v0.2後半候補 |
| 虎口 | 1 | 形条件の罠 |
| しのぎ | 1 | アタリ救出で余勢 |
| 侵食 | 2 | 白領地を中立化する一時効果 |

## 施設カードの共通対象

施設カードの`黒領地`／`black_territory_empty`は、Stone layerが空、施設未設置、現在黒領地、建設容量・同名上限内の交点を意味する。
施設点の空点意味論と建設失敗理由は[[FEAT-001 Territory and Facilities]]を参照する。
