# FABLE5 Review — 実行コマンドと結果ログ

実施日: 2026-07-10 ／ 実行場所: リポジトリルート（明記のあるものは tools/abstract_sim/）

## 1. 文書検査

```
$ python3 tools/check_docs.py
Documentation checks passed.   (exit 0)
```

## 2. 抽象シミュレーターのユニットテスト

```
$ python3 -m unittest discover -s tools/abstract_sim/tests -v
test_deterministic (test_model.ModelTests) ... ok
test_summary_bounds (test_model.ModelTests) ... ok
Ran 2 tests in 0.013s — OK   (exit 0)
```

## 3. マトリクス再生成と一致確認

```
$ cd tools/abstract_sim && python3 run_matrix.py
```
- 再生成後の `out/matrix_summary.json` は実行前バックアップと**完全一致**（diff -q）。
- `docs/50_Validation/Simulation Reports/data/SIM-0001_matrix_summary.json` とも一致（json正規化後比較）。
- 条件: run_matrix.py 既定（seed=200000起点、セルごとに+1000、3方策×4流儀×7構成×250ラン=21,000ラン）。

## 4. 追加試行（24,000ラン）

- 実行方法: `model.simulate_run` を直接呼ぶスクリプト（run_batch.pyと同一経路）。
- 構成・seed起点・ラン数・全指標: `reviews/fable5/simulation/fable5_extra_runs.json`（12構成×2,000ラン、seed起点500000〜900000をJSON内に記録）。
- 主要結果:
  - expert/attack/none: 勝率 **100.0%**（支配戦略の存在）
  - expert/thickness/thickness_3: 爆発率 81.7%（seed 500000）／ 83.2%（seed 900000）— 固定構成80%超が seed 帯に依存しないことを確認
  - 構成別早期爆発率: 4.0〜44.5%（目標帯15〜25%を大幅逸脱する構成が多数）

## 5. コード検査で確認した事実

- `tools/abstract_sim/model.py:321` — `setup_explosion` 判定にスキル別分岐（EXPERT: engine>=2.8, INTERMEDIATE: >=3.5）。Metrics Dictionary の定義と不一致。
- `tools/check_content.py` — 検査対象はID一意・コスト非負・seals.jsonのコミ非負のみ（「参照先存在」等は未実装）。
- `game_data/balance/system.json` — counterattack: threshold=100, start_per_komi=4, gain_per_enemy_turn_per_komi=1 → コミ4以下は20ターン内に閾値未到達。

## 6. 画像確認

docs/25_UIUX/assets/ の7画像すべてを開いて確認（mockup_01〜05、contact_sheet、concept_board）。所見はFULL_REVIEW §7 および CONTRADICTIONS C-14/C-20/C-21。
