# Codex Prompt — Independent Review

対象TASKとdiffを独立レビューしてください。実装担当の説明を信頼せず、正本とコードを直接比較してください。

## 確認

- Acceptance criteriaの漏れ
- Rules Canon / Accepted ADRとの不一致
- 決定論、RNG、イベント順
- Rules Kernel以外に複製されたルール
- 不十分な境界値・property test
- バランス値の直書き
- 無関係な変更
- リプレイ・セーブ互換性
- TASK Evidenceの妥当性

指摘は重大度、再現方法、修正案を付けてください。
