---
type: spec-fixture
status: accepted
project: Igorogue
updated: 2026-07-10
id: ADR-0011-FIXTURES
---
# ADR-0011 Board Repetition Fixtures

[[ADR-0011 Battle-Local Stone Topology Repetition Ban]]の紙上・機械検査用fixture。
JSON正本は`game_data/fixtures/board_repetition_fixtures.json`に置く。

座標契約は[[Coordinate System and Initial Position]]を参照する。

## 表記

```text
. 空点
B 黒石
W 白石
K 黒王石
Q 白王石
```

盤面行は上から`y=7`、下が`y=1`。座標は`(x,y)`。
履歴比較は石種を無視し、`. / B / W / K / Q`だけを使う。

## 基本コウ形

### P0 — 黒が捕獲する前

```text
7 .......
6 .......
5 .......
4 ..B....
3 .BWB...
2 .W.W...
1 ..W....
  1234567
```

黒が`(3,2)`へ置くと、白`(3,3)`を捕獲してP1になる。

### P1 — 黒の捕獲後

```text
7 .......
6 .......
5 .......
4 ..B....
3 .B.B...
2 .WBW...
1 ..W....
  1234567
```

白が`(3,3)`へ置くと、仮結果はP0へ戻る。

## Fixture一覧

| ID | 検査 | 期待 |
|---|---|---|
| KO-01 | P0から黒が初回捕獲 | 合法。P1を履歴追加可能 |
| KO-02 | P1から白が即取り返し | 不合法。P0は履歴済み |
| KO-03 | 白が特殊石種で即取り返し | 不合法。石種はキーに含めない |
| KO-04 | 施設状態、気、手札等だけ変えた後に即取り返し | 不合法。非石状態はキーに含めない |
| KO-05 | 白が別地点へ石を置き、新しいトポロジーから取り返し | 合法。結果が未出現なら許可 |
| KO-06 | KO-05後、黒が再度戻して中間トポロジーを再現 | 不合法。戦闘内の全履歴を参照 |
| KO-07 | 敵第1候補が反復手、第2候補が合法 | 第1候補を除外し第2候補を選択 |

## KO-05/KO-06の意図

盤面反復禁止は「取り返しを永久に禁止」する規則ではない。
別地点へ石を置いて未出現の石配置を作れば、取り返しが合法になる場合がある。
ただし、その後に過去の中間配置へ戻る手は履歴一致により禁止される。

この性質により、盤面を前進させる余地を残しつつ、同じ2局面の往復と捕獲報酬反復を防ぐ。

## Rules Kernel移植時の期待イベント

反復不合法の手を実行要求した場合、状態変更イベントを発行しない。
入力層が不合法コマンドもログへ保存する設計なら、診断用に次のみを許可する。

```text
CommandRejected(reason=stone_topology_repetition)
```

少なくとも次のイベントは発行してはならない。

```text
StonePlaced
GroupCaptured
KingCaptured
QiChanged
CardMoved
BrilliantMultiplierChanged
StoneTopologyRegistered
```

敵候補生成では`CommandRejected`を発行せず、候補フィルターとして静かに除外する。
