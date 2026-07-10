# Codex Prompt — Abstract Proxy Exploration

`tools/abstract_sim/`は非製品の設計代理モデルです。候補の粗い比較だけを行ってください。

- 盤面ルールの正しさを主張しない。
- 正式勝率として報告しない。
- 変更した係数と理由を記録する。
- 上位案だけを正式シミュレーション候補へ送る。
- モデルが扱わない盤面要素をLimitationsへ明記する。
