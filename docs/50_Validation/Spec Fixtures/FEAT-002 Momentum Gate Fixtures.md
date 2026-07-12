---
type: spec-fixture
status: accepted
project: Igorogue
feature: FEAT-002
updated: 2026-07-12
---
# FEAT-002 Momentum Gate Fixtures

機械可読正本は`game_data/fixtures/momentum_gate_fixtures.json`。
本fixtureは仕様checker用であり、M3のMomentum production implementationで共有Rules Kernelのunit/golden replayへ移植する。

| ID | 検証内容 | 期待 |
|---|---|---|
| MOM-01 | 攻め碁流でも黒領地成立 | 余勢0→1 |
| MOM-02 | 一手で複数黒領地成立 | 普遍sourceは+1だけ |
| MOM-03 | 白行動による偶発的黒領地 | 生成なし |
| MOM-04 | 攻め碁流の施設建設 | 生成なし |
| MOM-05 | 地合い流の施設建設 | +1 |
| MOM-06 | 上限2でさらに生成 | applied 0、overflow 1 |
| MOM-07 | プレイヤーターン境界 | amount保持、前進draw flagだけreset |
| MOM-08 | 戦闘開始reset | amount 0 |
| MOM-09 | 上下左右distance 2、中間空 | 余勢候補 |
| MOM-10 | 中間点に石 | 候補外 |
| MOM-11 | `frontline`を持たない札 | 使用不可 |
| MOM-12 | 印刷タグで通常合法 | normal mode、消費0 |
| MOM-13 | 最終合法性が盤面反復NG | 気・余勢・カードzone不変 |
| MOM-14 | 合法な余勢配置 | 余勢1消費 |
| MOM-15 | 前線距離短縮＋解決後収入4 | 1ドロー |
| MOM-16 | 他の黒石が既に最前線 | ドローなし |
| MOM-17 | 解決後収入3 | ドローなし |
| MOM-18 | 同ターン2回目 | 配置可能、追加ドローなし |
| MOM-19 | 中間点または対象点に施設だけ存在 | Stone layer空として候補。対象施設は確定後破壊対象 |

## MOM-09 geometry

```text
7 .......
6 .....Q.
5 .......
4 .......
3 .......
2 .B.....
1 .......
  1234567
```

起点`S=(2,2)`、中間`M=(3,2)`、対象`T=(4,2)`。

## MOM-12 normal placement precedence

`T`が遠い起点からdistance 2でも、別の黒石へ隣接して`frontline`通常合法なら、normal modeで解決し余勢を使わない。

## MOM-15 approach draw

余勢配置前後で、全黒石から白王石までの最短Manhattan距離を比較する。
対象石自身だけではなく、盤面全体の最前線を更新した場合に限る。
