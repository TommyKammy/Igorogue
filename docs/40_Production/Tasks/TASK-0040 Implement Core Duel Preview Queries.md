---
type: task
id: TASK-0040
status: in_progress
project: Igorogue
milestone: M2
priority: high
dependencies: [TASK-0039]
updated: 2026-07-13
---
# TASK-0040 Implement Core Duel Preview Queries

## Outcome

選択cardの合法target／mode、capture、結果呼吸点／アタリ、領地差、王石risk、Bandit intentをauthoritative stateから導出するread-only Application query／presentation projectionとして公開する。

## Non-goals

- state mutation、Godot rendering、Momentum／Brilliant／full counterattack preview、未確定ルールの推測。

## Allowed areas

- pure Domain analysisの再利用に必要な限定query seam。
- Application query／DTO（Godot型禁止）。
- Domain／Application／Architecture tests、本TASK／status文書。

## Acceptance criteria

- previewは実commandと同じlegality／capture／territory／enemy plan kernelを利用し、第二のrule implementationを作らない。
- queryはRNG、command log、state、first-use flagを変更しない。
- CanonicalPoint、stable reason IDs、primary／alternate intentをpresentation-neutralに返す。
- preview結果と同stateからのaccepted command resultが一致し、stale checksumを検出する。

## Validation

- repository wrappers、preview／commit parity、read-only／stale／enumeration-order tests。
- independent fixed-HEAD review、CI全job。

## Known issues

Full counterattack／将来retarget実行予測はNon-goalsのままTASK範囲外。保存済みnormal／bonus planと現在player windowのmandatory overrideだけを投影する。

## Execution log

2026-07-13 — PR #29のhuman mergeを確認。TASK-0039 source HEAD `afe3bc1ce64f4d4ebd240147053552ac1f848cae`、main merge commit `60d8cc5958e38768f4077ee2f4d686526d5b25fe`、post-merge main CI run `29252298693`全3 job successによりdependency blockを解除し、TASKを`ready`へ遷移した。

2026-07-13 — `ready`状態からAccepted sourceと既存aggregateの監査を開始してTASKを`in_progress`へ遷移し、各CanonicalPoint／modeを同一immutable `CoreDuelBattleSession`から既存`CoreDuelBattleStateMachine.Execute`へ投機実行して結果sessionを破棄するquery seamを採用した。合法性、捕獲、領地、施設、card効果、RNG結果はcommit pathを再利用し、Bandit表示は保存済みplan、王石riskは既存mandatory override previewからのみ導出する。

2026-07-13 — phase-independent battle projectionへ49交点の石／領地／施設、全groupとeffective liberty／atari、ordered hand、qi、非公開pile件数、保存済みnormal／bonus intentを公開した。card projectionはaccepted resultの全group、新規敵atari、capture、territory／facility delta、exact-bound commit commandと結果checksumを返し、将来retarget可否を予測せずstable target anchorが現在groupへ解決するかだけを示す。

2026-07-13 — stale state／log fail-closed、全starter card shape、canonical candidate order、preview／commit parity、read-only reference／RNG／log／first-use、new／existing atari、target group merge、facility build／destroy、terminal capture、normal／bonus intent、mandatory overrideを自動テストで固定した。repository wrappers、625 tests、sim／Godot smoke、Windows debug exportは全成功。

## Evidence

- PR #29 merge／post-merge main CI evidenceによりTASK-0039 dependency完了。
- Accepted source audit — 第二Rules Kernel、Domain変更、Godot型、未確定rule決定は不要と確認。
- `tools/dev/verify-tools` — .NET SDK `8.0.422`、Godot `4.7.stable.mono.official.5b4e0cb0f`、exit 0。
- `tools/dev/check`／`restore`／`build`／`test` — exit 0、build 0 warning／0 error、Domain 355＋Application 190＋Architecture 80＝625 tests success。
- `tools/dev/sim-smoke`／`godot-smoke` — exit 0、checksum `36ca153c20b82b2220c82b787c229d22f255fee7c42fed9c5ce7753ae0ff7bf1`。
- `tools/dev/export-windows` — exit 0、Windows executable SHA-256 `a0f1ed0472962c4932595dd9ede41c6b3f3f8ad25b2709571bbd4ab770a6b704`。
