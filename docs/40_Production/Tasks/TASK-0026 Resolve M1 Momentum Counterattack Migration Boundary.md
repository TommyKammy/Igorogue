---
type: task
id: TASK-0026
status: done
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0025]
updated: 2026-07-12
---
# TASK-0026 Resolve M1 Momentum Counterattack Migration Boundary

## Outcome

Project ownerが選択した[[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]] Option 1を反映し、MOM-01〜19／CTR-01〜25のproduction Rules Kernel unit／golden migrationをM3へ同期する。TLE-01〜15はM1 requirementとして維持し、未実装依存を隠さない直列production workstreamを定義する。

## Source of truth

- [[Rules Canon]]
- [[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]]
- [[TASK-0025 Gate 1 Deterministic Foundation Audit]]
- [[Milestones and Exit Gates]]
- [[Golden Replay Index]]
- [[FEAT-002 Momentum]]
- [[FEAT-002 Momentum Gate Fixtures]]
- [[FEAT-003 Komi Counterattack and Heat]]
- [[FEAT-003 Counterattack Curve Fixtures]]
- [[ADR-0013 Baseline Pace and Burst-Driven Counterattack]]
- [[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]
- [[FEAT-011 Temporary Liberty Expiry Fixtures]]
- [[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]

## Non-goals

- MOM／CTR／TLEのproduction code、tests、golden replay実装。
- Rules Canonのplayer-visible behavior、`game_data/`の数値／fixture expected valueの変更。
- Momentum／counterattackのM3内の実装順、バランス値、人間playtest基準の変更。
- TLEのAccepted lifecycle／event order／capture benefitを部分仕様へ弱めること。
- [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]の二人human sign-off代替。
- M1 exitを`PASS`、Gate 2 taskを`ready`とすること。

## Allowed areas

- 本TASKと新規TLE production TASKノート。
- DECISION-0005のresolution、TASK-0025のstatus／post-audit disposition。
- MOM／CTRのmilestone migrationを記述するAccepted Feature Spec／fixture／ADR／Balance Report。
- Milestones、Golden Replay Index、Current Development State、Dashboard／Hub、Sprint、Backlog、Codex Task Queue。
- production code、tests、`game_data/`、toolchain、package／project、Godot filesは変更しない。

## Acceptance criteria

- DECISION-0005を`resolved`、`blocking: []`とし、2026-07-12のowner Option 1選択を明記する。
- FEAT-002／MOM fixture、CTR fixture、ADR-0013、Golden Replay Index、Milestones、queueが、MOM-01〜19／CTR-01〜25のproduction unit／golden migrationをM3に置く。player-visible ruleとfixture valueは変えない。
- TLE-01〜15のproduction Rules Kernel unit／golden migrationをM1に維持し、MOM／CTRと一緒にM3へ動かさない。
- TLEをstone identity／expiry core、closed-window capture benefit、enemy-boundary／replay／golden integrationの直列TASKへ分割する。TLE-09／10／14／15の未実装依存と、full MOM／CTRをM1で実装しない境界を各TASKに明記する。
- fixed-baseline [[TASK-0025 Gate 1 Deterministic Foundation Audit]]の`DECISION NEEDED`結果／matrixを歴史的evidenceとして保持し、post-audit dispositionでOption 1と現在の`NOT PASSED`要因を追記する。
- [[TASK-0025 Audit Gate 1 Deterministic Foundation Completion]]を`done`とするが、M1 exitはTLE E3 evidence、Gate 2 entryはさらにTASK-0012 human sign-offまでopenとする。
- PR #15 merge commit／post-merge main CIとfixed `origin/main` HEADを記録する。
- independent fixed-HEAD reviewがsource sync、historical evidence、TLE task boundary、docs-only scopeを`APPROVE`する。

## Validation

- `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を実行する。
- targeted `rg`でMOM／CTRの現行M1 migration記述が残っていないこと、TLE M1記述が維持されることを確認する。
- `git diff --check`、docs-only diff、全wikilink／TASK／Decision statusを確認する。
- root `CODE_REVIEW.md`に従い独立fixed-HEAD reviewを行う。

## Execution log

2026-07-12 — PR #15 merge commit `6c34a4fffe00b0fbec9dc5dd3033d84c6229a56d`とpost-merge main CI run `29173263652`の全3 job成功を確認。

2026-07-12 — Project ownerが「DECISION-0005はOption 1で進めてください」と明示選択。fixed `origin/main` `6c34a4fffe00b0fbec9dc5dd3033d84c6229a56d`から本TASKを`in_progress`で開始した。

2026-07-12 — FEAT-002／003、MOM／CTR fixtures、ADR-0013、BAL-0001、Golden Replay Index、Milestones／queueを同期し、MOM／CTR production migrationをM3、TLE-01〜15をM1へ一義化した。player-visible rule、fixture payload、`game_data/`は変更していない。

2026-07-12 — TLE production gapをTASK-0027 Domain lifecycle → TASK-0028 closed-window benefit／minimal counterattack boundary → TASK-0029 Application boundary／versioned golden replayへ分割した。3 TASKはhuman mergeごとの直列依存とし、full MOM／CTR、enemy planner、UI／GodotをNon-goalに固定した。

2026-07-12 — repository wrappers、targeted source scan、docs-only diffを検証し、本TASKを`review`へ遷移した。

2026-07-12 — independent fixed-HEAD reviewが`03fa1e6698a3d4cae053c464a02cc6d7a240e961`をparent／`origin/main` `6c34a4fffe00b0fbec9dc5dd3033d84c6229a56d`と比較。Option 1 source sync、fixed audit保存、status／dependency、TLE境界、docs-only scopeにfindingなし、`APPROVE`。

2026-07-12 — PR #16を人間merge。merged head `eae7b616768ee7934795f31eec133a3940607390`、merge commit `90dda9dd41b96864a24e19a7969285f56c4593b4`、post-merge main CI run `29180540418`全3 job成功を確認し、本TASKを`done`へ遷移した。

## Evidence

- PR #15 merged head `1e629504284e9a198d794fb9dada9417cf46e2e3`／merge commit `6c34a4fffe00b0fbec9dc5dd3033d84c6229a56d`。
- post-merge main CI run `29173263652` — Governance `86597785406`、Pure .NET `86597803448`、Godot／Windows export `86597846465`すべてsuccess。
- [[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]] owner Option 1選択。
- Accepted／active source scan — MOM-01〜19／CTR-01〜25 production migrationはM3、TLE-01〜15 production unit／golden migrationはM1。fixed-baseline audit／Decision rationaleに残る旧M1記述は歴史的contextとして分離。
- `tools/dev/check` — exit 0。documentation／wikilink／content／全fixture／governance check、14+2 Python tests成功。47 content IDs、content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`。
- `tools/dev/test` — exit 0。.NET SDK 8.0.422、Domain 190、Application 54、Architecture 15、計259 tests、warning 0／error 0。
- `tools/dev/sim-smoke` — exit 0。`checksum=3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`。bootstrap determinism evidenceとしてのみ使用。
- `git diff --check` — exit 0。変更は22 Markdown filesだけで、production code、tests、`game_data/`、Godot assetにdiffなし。
- independent fixed-HEAD review — `03fa1e6698a3d4cae053c464a02cc6d7a240e961`、findingなし、`APPROVE`。reviewer側もcheck、259 tests、sim-smoke、base diff checkすべてexit 0。
- PR #16 merged head `eae7b616768ee7934795f31eec133a3940607390`／merge commit `90dda9dd41b96864a24e19a7969285f56c4593b4`。
- post-merge main CI run `29180540418` — Governance `86617175981`、Pure .NET `86617194216`、Godot／Windows export `86617238452`すべてsuccess。

## Known issues

TLE-01〜15のproduction／E3 evidenceとTASK-0012の二人human sign-offは未完了である。本TASKはそれらを実装済みまたはGate 2 entry済みと扱わない。TASK-0027をcurrentとし、TASK-0028／0029は各直前TASKのhuman mergeまで`blocked`を維持する。
