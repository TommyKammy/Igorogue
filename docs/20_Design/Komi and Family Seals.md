---
type: system-design
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Komi and Family Seals

## 家元印

永続解放した持込能力。0〜3枠。各印にコミがある。

## 滑らかな補償

段階的な閾値で敵強化を追加すると「コミ4まで絶対得、5は取らない」という定石になる。したがって、補償は大きなtier buffではなく連続量として扱う。

## 敵への変換

- 白王石の厚み蓄積
- 反攻の戦闘開始値
- 反攻の敵ターン終了増加
- 妙手x3の熱増加量

印の個別効果を直接無効化しない。

反攻曲線の正本は[[FEAT-003 Komi Counterattack and Heat]]と[[ADR-0013 Baseline Pace and Burst-Driven Counterattack]]。現在のunit値は`game_data/balance/system.json`を参照する。

## 目標

- 最適装備数の平均が1〜2枠付近。
- 3枠が常時正解にならない。
- 最適コミが敵・流儀で変化する。
- 高コミは棋譜片効率ではなく棋力スコア向け。
- 低コミでも長期戦または爆発戦なら反攻を経験する。
