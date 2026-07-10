---
type: handoff
status: active
project: Igorogue
updated: 2026-07-10
---
# Codex Mac Handoff

> [!important] 現在の実行ゲート
> ゲームプレイ実装へ進む前に、[[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]]を完了する。`TASK-0002`はそれまで開始しない。

## 現在地

- M-1の主要P0設計修復はAccepted済み。
- Godot 4.7 stable .NET／C# 12／pure .NET Rules Kernel方針はAccepted済み。
- Repository bootstrapは静的検査済みだが、.NET／Godotの実行証拠が未取得。
- 製品Rules Kernelは未実装。
- `tools/abstract_sim/`はE2代理証拠であり、製品バランスの根拠ではない。

詳細は[[Current Development State]]を参照する。

## Macで最初に行うこと

1. プロジェクトを同期フォルダ外へ展開する。
2. Git repositoryでなければ、変更前にbaseline commitを作る。
3. [[macOS Development Host Setup]]に従ってexact toolchainを確認する。
4. Codex Appでrepository rootを開く。
5. 最初はLocal環境を使用する。
6. `handoff/FIRST_PROMPT.txt`をCodexへ貼り付ける。
7. 読み取り専用監査が成功したら、[[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]]を開始する。

## Codexの必須読書順

1. ルート`AGENTS.md`
2. ルート`CODEX_MAC_HANDOFF.md`
3. [[Source of Truth Map]]
4. [[Current Development State]]
5. 対象TASK
6. TASKからリンクされたFeature SpecとADR
7. ルート`CODE_REVIEW.md`

## LocalとWorktree

### Localを使う作業

- 初回のtoolchain確認
- NuGet lock生成と人間レビュー
- Godotアプリbundleを使うsmoke
- Godotの見た目確認
- merge前の最終統合確認

### Worktreeを使う作業

- TASK-0002以降の独立したコードタスク
- 文書修正
- 独立レビュー
- 互いに同じ基盤を変更しない並列タスク

一つのCodex task／conversation／worktreeには一つのTASKだけを割り当てる。

## 人間承認が必要な事項

- toolchain・package・dependencyのinstall／upgrade
- version pinの変更
- Accepted ADRまたはプレイヤー可視ルールの変更
- scene／resource／export設定の編集
- lock file commit
- `main`へのmerge
- 履歴の書換え・削除

## 使用する指示書

- [[Codex App Operating Procedure]]
- [[Codex Review and Merge Procedure]]
- [[Codex Stop and Escalation Rules]]
- [[Codex Task Queue]]
- `codex-prompts/macos/`

## 成功条件

Codexが、現在のgateと停止条件を説明し、version pinを変えずにruntime evidenceを作り、証拠不足を`done`と偽らず、人間レビュー後に次のTASKへ移れること。
