---
type: system-design
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Board and Placement

## 座標契約

- 外部座標はCanonicalPoint `(x,y)`、各軸1〜7。
- `x`は左→右、`y`は下→上。盤面図は上から`y=7`→`y=1`。
- 内部0-based座標との変換、Canonical point order、標準初期配置、点対称は[[Coordinate System and Initial Position]]を正本とする。
- Canonical point orderは`y`昇順、次に`x`昇順。
- 点集合を列挙する規則、敵タイブレーク、イベント順、`StoneTopologyKey`はこの順序を共有する。

## 配置タグ

| タグ | 条件 | 主用途 |
|---|---|---|
| frontline | 黒石に隣接 | 基本展開 |
| contact | 黒・白両方に隣接 | 攻撃 |
| jump | 味方から1点空けた先 | 厚み・模様 |
| edge | 辺・隅の指定空点 | 地合い |
| invasion | 白領地または指定敵近傍 | 侵入 |
| terminal | 即捕獲する最後の呼吸点 | トドメ |

## 前線制約の理由

盤面の任意点へ打てると、初期手札だけで白王石を暗殺でき、領地経済が不要になる。前線制約により、展開距離そのものがカード資源になる。

## 施設点

施設はStone layerを占有しないため、施設点も配置候補上は空点である。
合法な石を施設点へ確定すると施設は破壊されるが、プレビュー、自殺手、同時捕獲、盤面反復の検査中は施設を除去しない。
不合法手では施設を失わない。詳細は[[ADR-0012 Facility Sites Are Empty Intersections]]。

## 盤面反復フィルター

すべての石配置候補は、配置タグ、自殺手、終着条件に加え、[[ADR-0011 Battle-Local Stone Topology Repetition Ban]]の履歴検査を通る必要がある。

- 比較するのは石色と王石位置だけ。
- 特殊石へ置き換えても同じ石色配置なら反復扱い。
- 施設、気、手札、妙手倍率等の非石変化では解除されない。
- 敵候補も同じフィルターを利用する。
- UI上の診断理由は`stone_topology_repetition`。

## プレビュー

カード選択時、各合法点に以下を重ねる。

- 捕獲数
- 新領地サイズ
- 余勢生成／消費
- 自グループの結果呼吸点
- 妙手倍率変化
- 反攻変化
- 施設破壊、停止、再稼働
- 盤面反復による不合法
