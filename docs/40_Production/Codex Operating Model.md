---
type: codex-governance
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Codex Operating Model

Mac引き継ぎの実行手順は[[Codex Mac Handoff]]、[[Codex App Operating Procedure]]、[[Codex Review and Merge Procedure]]を正本とする。このノートは要約である。

## 一件ずつ渡す

Codexへは`status: ready`のTASKだけを渡す。大きな「v0.2を実装」は禁止。

## スレッド

```text
[TASK-0012] Capture Resolution
```

ブランチ、コミット、TASK IDを一致させる。

## 開始プロンプト

1. `AGENTS.md`と正本を読む。
2. 目的、非対象、受け入れ条件を再掲。
3. 変更予定ファイルとテストを提示。
4. 曖昧ならDecision Neededを作り停止。

## 完了

- テスト
- diff確認
- TASK Evidence更新
- 設計仮定を変えたか明記

## 独立レビュー

別スレッドで仕様不一致、決定論、テスト不足、直書き値、無関係変更を確認する。

## Worktree

独立タスクだけを並列化する。基盤とその利用側を同時に変更しない。初回のTASK-0022はLocalで行い、runtime gate後は一TASK一worktreeを基本とする。

## Review and merge

別task/conversationでルート`CODE_REVIEW.md`に従って独立レビューする。Codexは自動mergeせず、人間が`review → done`と`main`へのmergeを判断する。

## Stop and escalation

仕様矛盾、exact tool不足、未承認dependency、非決定性、scene編集権限不足では[[Codex Stop and Escalation Rules]]に従って停止する。
