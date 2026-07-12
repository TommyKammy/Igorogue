---
type: decision-needed
id: DECISION-0008
status: resolved
blocking: []
updated: 2026-07-13
---
# DECISION-0008 Align Reinforce Content Order with FEAT-011

## Why a decision was needed

PR #22 reviewで、Accepted [[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]と`game_data/content/cards.json`の`card_reinforce.effects`順が矛盾していることが判明した。

- FEAT-011は、対象がアタリかを仮呼吸点付与前に判定し、該当時は1ドローしてから+1 effectを付与する。
- runtime contentは`temporary_liberty`、`draw_if_target_atari`の順であり、配列順を実行順とするとdrawを抑止し得る。
- loaderで黙示的に並べ替えるとContent層がsource dataの意味を変更し、逆順dataをfail-closedで拒否できない。

## Decision

- Accepted FEAT-011の`draw_if_target_atari`、`temporary_liberty`順をauthoritative semantic orderとする。
- TASK-0032へ、この矛盾を解消するためだけの`game_data/content/cards.json`修正とgenerated snapshot更新を許可する。
- typed loaderは`card_reinforce`の逆順または異なるoperation列を拒否し、黙示canonicalizeしない。
- content hash変更は新しいruntime snapshotとして記録する。過去のruntime／golden evidenceに記録された旧hashは履歴証拠として書き換えない。

## Consequences

- 後続TASK-0036はtyped operation順をそのまま実行しても、アタリ判定とdrawを仮呼吸点付与前に行える。
- starter ID集合とrouting fieldもPR #22 reviewでfail-closed validationを補強する。
- starting recipe、card数値、effect値、Accepted player ruleは変更しない。

## Owner decision

Project ownerの2026-07-13指示「PR #22についた指摘が妥当であれば修正して」に基づき、指摘の妥当性確認後にFEAT-011準拠の限定修正として解決した。
