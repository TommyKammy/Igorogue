---
type: spec-fixture
id: SPEC-COORD-001-FIXTURES
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Coordinate System and Initial Position Fixtures

[[Coordinate System and Initial Position]]の紙上・機械検査用fixture。
JSON正本は`game_data/fixtures/coordinate_system_fixtures.json`に置く。

## 座標表記

- CanonicalPointは`(x,y)`、各軸1〜7。
- 盤面図は上から`y=7`、下が`y=1`。
- Canonical point orderは`y昇順、次にx昇順`。

## 標準初期盤面 `standard_v0_2`

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

## Fixture一覧

| ID | 検査 | 期待 |
|---|---|---|
| COORD-01 | 左下の表示→内部変換 | `(1,1) -> (0,0)` |
| COORD-02 | 右上の表示→内部変換 | `(7,7) -> (6,6)` |
| COORD-03 | 中央の往復変換とindex | `(4,4) <-> (3,3)`, index 24 |
| COORD-04 | 表示座標の盤外拒否 | `(0,1)`, `(8,7)`, `(1,0)`, `(7,8)`は不正 |
| COORD-05 | Canonical linear index境界 | `(1,1)=0`, `(7,1)=6`, `(1,2)=7`, `(7,7)=48` |
| COORD-06 | 左下隅の隣接 | `(2,1)`, `(1,2)`の2点 |
| COORD-07 | 下辺中央の隣接 | `(3,1)`, `(5,1)`, `(4,2)`の3点 |
| COORD-08 | 中央の隣接 | `(4,3)`, `(3,4)`, `(5,4)`, `(4,5)`の4点 |
| COORD-09 | 点対称の自己逆変換 | `(2,3)->(6,5)`、再変換で`(2,3)` |
| COORD-10 | 初期石の役割付き点対称 | 黒王・護衛が白王・護衛へ1対1対応 |
| COORD-11 | 初期王石グループ | 各色3石連結、実呼吸点7 |
| COORD-12 | 盤面図とCanonical order | 図のrow 0は`y=7`、canonical index 0は`(1,1)` |

## 黒初期王石グループの実呼吸点

Canonical point orderで次の7点。

```text
(2,1), (3,1), (1,2), (4,2), (1,3), (3,3), (2,4)
```

白側は上記を`reflect(x,y)=(8-x,8-y)`した集合である。

## Rules Kernel移植時の期待

- `CanonicalPoint`と`InternalPoint`の変換を純粋関数でテストする。
- 盤外値をclampせず拒否する。
- 隣接集合の件数と内容をテストする。
- 初期盤面のDomain checksumと`StoneTopologyKey`をgolden化する。
- 色交換付き点対称をproperty test化する。
- 盤面図parserを実装する場合、diagram row orderとCanonical point orderを混同しない。
