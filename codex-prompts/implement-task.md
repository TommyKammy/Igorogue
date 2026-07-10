# Codex Prompt — Implement TASK

`docs/40_Production/Tasks/<TASK FILE>` を実装してください。

## 作業前

1. `AGENTS.md` を読む。
2. `docs/00_Home/Source of Truth Map.md` を読む。
3. TASKからリンクされたRules Canon、Feature Spec、ADRを読む。
4. Outcome、Non-goals、Acceptance criteriaを短く再掲する。
5. 変更予定ファイルと検証コマンドを提示する。
6. 仕様が曖昧なら実装せずDecision Neededを作る。

## 実装

- 受け入れ条件を満たす最小変更にする。
- ランタイム値をコードへ直書きしない。
- 決定論とリプレイ互換性を守る。
- UI、敵、シミュレーターで別のルール実装を作らない。

## 完了時

1. テストを追加・更新する。
2. ビルド、関連テスト、`python tools/check_docs.py`を実行する。
3. TASKのExecution log、Evidence、Known issuesを更新する。
4. 変更内容、テスト結果、設計仮定変更の有無、残るリスクを報告する。
