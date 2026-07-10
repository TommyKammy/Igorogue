---
type: obsidian-governance
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Obsidian Governance

## ノートの役割

- Canon：現在の正式仕様。
- ADR：なぜ決めたか。
- TASK：今何をするか。
- Report：何を測ったか。
- Template：形式。

## Frontmatter

最低限`type`, `status`, `project`, `updated`を持つ。TASKは`id`, `milestone`, `dependencies`を追加。

## 状態

```text
draft / proposed / accepted / active / superseded / archived
```

## リンク

TASKは必ず正本、ADR、Feature Specへリンクする。チャット履歴だけを参照しない。

## プラグイン

必須コミュニティプラグインなし。Obsidian標準のProperties、Templates、Canvasを想定する。Basesは任意。
