# Igorogue Abstract Design Proxy

このツールは、会話中の設計検証で用いた「抽象モデル」という考え方を、再現可能な形で作り直した参考実装です。

## 重要

- 製品用の盤面ルールではありません。
- 7×7交点、呼吸点、切断、死活を厳密には扱いません。
- 相対比較、仮説の粗い選別、テレメトリ設計の確認だけに使用します。
- 正式なバランス判断は、共通Rules Kernelを使うヘッドレスシミュレーターと人間プレイテストで行います。

## 実行

```bash
cd tools/abstract_sim
python run_batch.py --runs 1000 --skill expert --style territory --loadout territory_4
python run_matrix.py
python -m unittest discover -s tests
```

## 出力指標

- 完走率
- 中規模成長率
- 真の爆発率
- 早期爆発率
- 爆発の事前仕込み率
- 爆発なし勝利・敗北
- 最大ターン出力

モデル定義と限界は `docs/30_Technical/Abstract Proxy Model.md` を参照してください。
