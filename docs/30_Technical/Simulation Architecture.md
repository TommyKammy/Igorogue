---
type: simulation-architecture
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Simulation Architecture

## 二段階

### 抽象代理モデル

`tools/abstract_sim/`。高速な設計案比較。盤面の正しさを保証しない。

### 正式ヘッドレスシミュレーター

製品Rules Kernelを直接使用し、Botが合法コマンドを選ぶ。バランス判断の主要証拠。

## seed群

- Regression：固定回帰。
- Calibration：変更前後比較。
- Exploration：未知の外れ値探索。

同じseed群で調整と最終評価を完結させない。

## Bot

初心者、中級者、熟練者、探索型、最悪ケース型を別policy_versionとして管理する。
