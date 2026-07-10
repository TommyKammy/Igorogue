# Codex Prompt — Formal Balance Simulation

指定されたSIMノートの仮説を、製品Rules Kernelを使うヘッドレスシミュレーターで評価してください。

## 必須

- build_id / content_hash
- rules_kernel_version
- Bot policy version
- explosion classifier version
- Regression / Calibration / Exploration seedの分離
- 平均だけでなく分布と外れ値seed
- 構成別の勝率、爆発率、早期爆発、爆発なし勝利・敗北
- 再現コマンド
- 元データへの相対パス

実装変更と採用判断を同一TASKで行わず、結果をSIM Reportへ保存してください。
