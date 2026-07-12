---
type: task
id: TASK-0031
status: in_progress
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0012, TASK-0030]
updated: 2026-07-12
---
# TASK-0031 Plan Gate 2 Core Duel Implementation

## Outcome

M1 technical exitとM-1 human gateのcloseout evidenceを固定し、M2 Core Duelをcontent projection、deck／hand／qi、card play、山賊棋士、headless integration／replay、preview、Godot graybox、playable UATの依存順にbounded TASKへ分割する。最初のproduction TASKだけが次に`ready`へ遷移できるqueueを作る。

## Source of truth

- [[Rules Canon]]
- [[Milestones and Exit Gates]]
- [[Development Plan]]
- [[Architecture]]
- [[Deck and Card System]]
- [[Initial Card Set]]
- [[Combat Resolution Order]]
- [[FEAT-009 Enemy Action Planning and Placement]]
- [[UI UX Overview]]
- [[Battle Screen Specification]]
- [[Interaction and Input]]
- [[v0.1.1 Graybox Scope]]
- [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]
- [[TASK-0030 Re-audit M1 Headless Rules Kernel Exit]]

## Non-goals

- production code、tests、`game_data/`、schema、toolchain、package／project reference、Godot assetの変更。
- Rules Canon、Accepted ADR／Feature Spec／Milestones、card／enemy runtime値の変更。
- starter deck composition、M2 facility inclusion、カード効果の未決事項を実装都合で決めること。
- Gate 3のMomentum／full counterattack／content expansion、formal simulator、meta progression。
- playable／fun claim、TASK-0032以降の実装先取り。

## Allowed areas

- 本TASKと新規TASK-0032〜0041ノート。
- 新規Decision Neededノート。
- TASK-0012／0030のpost-gate evidence。
- `docs/50_Validation/Spec Fixtures/FEAT-009 Enemy Decision Fixtures.md`のhuman sign-off status同期。
- Current Development State、Project Dashboard／Hub、Current Sprint、Backlog、Codex Task Queueのstatus／sequence同期。

## Acceptance criteria

- PR #20 merged head／merge commit、post-merge main CIを記録し、TASK-0030をhuman merge evidenceで`done`へ遷移する。
- Project ownerの明示attestationを、TASK-0012の独立二人human sign-off完了根拠として記録し、raw worksheet／identity未保存を隠さず`done`へ遷移する。
- M1 technical `PASS`とTASK-0012完了によりGate 2 entry条件が閉じたことを記録する。
- M2の最小vertical pathをTASK-0032〜0041へ分割し、各TASKにOutcome、Non-goals、Allowed areas、Acceptance、Validation、dependency／blockerを記載する。
- task順はtyped Content projection → deterministic deck／hand／qi → atomic card play → remaining starter effects → 山賊棋士 planner → headless integration／replay → Application preview → Godot graybox → playable UATとする。consumerをfoundationより先に`ready`にしない。
- starter 6 definitions、12-card multiset、`card_development`、施設なしgrayboxのsource ambiguityをDecision Neededへ分離し、解決前にstarting recipeを選ばない。
- Decision未解決でも安全なTASK-0032だけを本TASK merge後の次候補とし、他TASKはdependency順に`blocked`を維持する。
- Godot asset編集TASKは`.tscn`／`project.godot`等の明示scope、headless parse/build、human visual reviewを要求する。
- fixed-HEAD independent reviewがgate closeout、Decision境界、task decomposition、status同期を`APPROVE`する。

## Validation

- `tools/dev/check`を2回実行する。
- `git diff --check`、docs-only diff、wikilink、TASK／Decision status、dependency順を確認する。
- root `CODE_REVIEW.md`に従うindependent fixed-HEAD reviewを記録する。
- GitHub CI全jobを確認する。

## Stop conditions

- Accepted source間の矛盾をDecisionなしに選ぶ必要がある。
- Gate 2 task分解にplayer-visible rule／runtime値の変更が必要。
- TASK-0012完了をowner attestation以上に捏造する必要がある。
- production／Godot変更を混ぜないと文書検証を通せない。

## Execution log

2026-07-12 — PR #20を人間merge。merged head `5f194a9987ba314a2eefe9f30b020d31901fc79e`、merge commit／fixed `origin/main` `d1f69e10672ed7289c056cee32c4875964494fe4`、post-merge main CI run `29193892563`のGovernance `86653293786`、Pure .NET `86653311052`、Godot／Windows export `86653371594`がすべてsuccessであることを確認した。

2026-07-12 — Project ownerが「TASK-0012の二人human sign-offは行った前提で先に進めてください」と明示。二人が同一結果へ到達したsign-off完了のowner attestationとして記録し、M1 technical `PASS`と合わせてGate 2 entry条件が閉じたため、本TASKを`ready`にした。

2026-07-12 — Project ownerの継続指示を本TASKの選択として記録。production実装を始めず、Gate 2を一TASKへ過積載しないdocs-only decompositionとして`in_progress`へ遷移した。

2026-07-12 — Accepted [[Deck and Card System]]の初期12枚、Accepted M2 milestoneの「初期6カード」、proposed Initial Card Setの6種／12枚recipe、proposed grayboxの施設除外、starter `card_development`の関係が一義的でなく、machine-readable starting recipeも存在しないことを確認した。[[DECISION-0006 Resolve M2 Starter Deck and Facility Scope]]へ隔離した。

2026-07-12 — TASK-0012本体だけでなく[[FEAT-009 Enemy Decision Fixtures]]のHuman sign-off表も、owner attestation完了とraw worksheet／identity未保存を明記して同期した。

2026-07-12 — `tools/dev/check`を2回実行し、両runで全documentation／wikilink／content／fixture／repository検査が成功。47 unique content IDsとcontent snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`が一致し、`git diff --check`もexit 0だった。

## Evidence

- PR #20 human merge commit `d1f69e10672ed7289c056cee32c4875964494fe4`／post-merge main CI run `29193892563`全3 job success。
- TASK-0012 two-human sign-off completion — Project owner attestation、2026-07-12。
- `tools/dev/check` ×2 — exit 0、47 unique content IDs、同一content snapshot。
- `git diff --check` — exit 0。

## Known issues

DECISION-0006はstarting deck recipeを選ぶintegration TASKまでにowner resolutionが必要である。typed candidate contentとinjected-recipe deck kernelはdecision結果を先取りせず実装可能。
