---
type: system-design
id: SPEC-COORD-001
status: accepted
project: Igorogue
updated: 2026-07-10
version: 1.0.0
---
# Coordinate System and Initial Position

本仕様は、7×7盤の表示座標、内部座標、盤面図、決定論的な点順序、点対称変換、標準初期配置の正本である。
プレイヤー可視の要約は[[Rules Canon]]、現在値は`game_data/balance/system.json`を正本とする。

## 1. Canonical display point

外部へ保存・表示する座標を`CanonicalPoint = (x, y)`と呼ぶ。

```text
1 <= x <= 7
1 <= y <= 7
```

- `x`は左から右へ増える。
- `y`は下から上へ増える。
- 左下は`(1,1)`、右上は`(7,7)`。
- 中央は`(4,4)`。
- 辺は`x ∈ {1,7}`または`y ∈ {1,7}`。
- 隅は辺条件を二つ同時に満たす4点。

カード対象、敵意図、content data、fixture、コマンドログ、リプレイ、テレメトリはCanonicalPointを使用する。
UIが座標ラベルを非表示にしても、保存される座標契約は変えない。

## 2. Board diagrams

Markdown、ログ、デバッグUIの盤面図は次で統一する。

- 行は上から`y=7`、下へ進み最下段が`y=1`。
- 各行の文字は左から`x=1`、右端が`x=7`。
- 座標ラベルを添える場合、左に`y`、下に`1234567`を置く。

盤面図の行順は表示上の都合によるものであり、Canonical point orderとは逆方向の`y`順である。

## 3. Internal point

Rules Kernelの配列添字には`InternalPoint = (ix, iy)`を使用してよい。

```text
0 <= ix <= 6
0 <= iy <= 6
```

変換は一箇所に限定し、次の純粋関数とする。

```text
to_internal(x, y) = (x - 1, y - 1)
to_canonical(ix, iy) = (ix + 1, iy + 1)
```

InternalPointをcontent data、リプレイ、telemetryへ露出させない。

## 4. Canonical point order and index

複数交点を安定順に並べる場合、次を使用する。

```text
(y ascending, then x ascending)
```

すなわち`(1,1), (2,1), ... (7,1), (1,2), ... (7,7)`である。
Canonical linear indexは次とする。

```text
index(x, y) = (y - 1) * 7 + (x - 1)
0 <= index <= 48
```

この順序を次で共有する。

- `StoneTopologyKey`
- 安定したイベント順
- 敵候補の最終タイブレーク
- 領地anchor、グループanchor
- facility状態変化
- fixtureの期待集合

隣接は規則上は集合だが、列挙が必要な場合はCanonical point orderへソートする。

## 5. Orthogonal neighbours

`(x,y)`の上下左右候補は次である。

```text
(x-1,y), (x+1,y), (x,y-1), (x,y+1)
```

盤外点を除外する。

- 隅は2隣接。
- 隅以外の辺は3隣接。
- 内部は4隣接。
- 斜めは隣接しない。

## 6. Point reflection

7×7盤中央`(4,4)`を中心とする180度回転、すなわち点対称変換を`reflect`とする。

```text
reflect(x, y) = (8 - x, 8 - y)
```

次を満たす。

```text
reflect(reflect(p)) = p
reflect(4,4) = (4,4)
```

## 7. Standard v0.2 initial position

標準初期配置IDは`standard_v0_2`とする。

| 色 | 役割 | CanonicalPoint | 点対称の対応 |
|---|---|---:|---:|
| 黒 | 王石 | `(2,2)` | 白王石 `(6,6)` |
| 黒 | 護衛 | `(2,3)` | 白護衛 `(6,5)` |
| 黒 | 護衛 | `(3,2)` | 白護衛 `(5,6)` |
| 白 | 王石 | `(6,6)` | 黒王石 `(2,2)` |
| 白 | 護衛 | `(5,6)` | 黒護衛 `(3,2)` |
| 白 | 護衛 | `(6,5)` | 黒護衛 `(2,3)` |

```text
7 .......
6 ....WQ.
5 .....W.
4 .......
3 .B.....
2 .KB....
1 .......
  1234567
```

`K`は黒王石、`Q`は白王石、`B/W`は護衛の通常石である。護衛は戦闘開始後、特殊能力を持たない基本石として扱う。

## 8. Initial-position invariants

標準初期配置は次を満たさなければならない。

1. 6石の座標はすべて盤内かつ重複しない。
2. 各色に王石1、護衛2が存在する。
3. 黒の各石を`reflect`し、色を白へ交換すると同じ役割の白石になる。
4. 白から黒への逆変換も成立する。
5. 各王石と護衛2石は上下左右で連結した3石の王石グループを作る。
6. 黒・白の初期王石グループは、それぞれ実呼吸点7を持つ。
7. 中央`(4,4)`は空点である。
8. 初期StoneTopologyKeyは戦闘開始時の反復履歴index 0へ登録される。

本対称性は**幾何学的な初期条件**だけを保証する。プレイヤーが先に複数カードを使うこと、敵AI、流儀、カード効果まで対称であるとは主張しない。先手・行動経済の公平性はM3で別途検証する。

## 9. Serialization contract

- JSONの点は`[x, y]`の2整数配列で保存する。
- Domain APIでは名前付き`CanonicalPoint{x,y}`を推奨する。
- 不正な点を自動clampしない。境界で拒否する。
- 盤面配列をシリアライズする場合、Canonical linear index順とする。
- Markdown盤面図を配列へ読む場合だけ、上段`y=7`から内部へ変換する。

## 10. Validation

- [[Coordinate System and Initial Position Fixtures]]の`COORD-01`〜`COORD-12`を正本fixtureとする。
- JSON正本は`game_data/fixtures/coordinate_system_fixtures.json`。
- `tools/check_coordinate_system.py`でsystem data、文書、変換、隣接、初期対称性を検査する。
- M1では同fixtureを共有Rules Kernelのunit testとgolden replayへ移植する。

## Supersession policy

盤面サイズ、座標原点、軸方向、Canonical point order、標準初期配置、点対称要件を変更する場合は、本仕様をsupersededにし、全fixture、replay schema、tie-break、StoneTopologyKeyを同じ変更単位で更新する。
