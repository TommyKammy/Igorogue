---
type: git-governance
status: proposed
project: Igorogue
updated: 2026-07-10
---
# Branching and Commit Convention

## Branch

```text
task/TASK-0012-capture-resolution
bug/BUG-0041-replay-order
bal/BAL-0023-momentum-threshold
```

## Commit

```text
feat(combat): TASK-0012 implement capture resolution
fix(replay): BUG-0041 preserve rng event ordering
bal(cards): BAL-0023 tune momentum threshold
```

## Merge Gate

- TASK受け入れ条件
- 自動テスト
- ドキュメント検査
- 独立レビュー
- 無関係diffなし
