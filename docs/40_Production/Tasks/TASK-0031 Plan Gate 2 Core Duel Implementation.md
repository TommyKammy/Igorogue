---
type: task
id: TASK-0031
status: review
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0030]
updated: 2026-07-12
---
# TASK-0031 Plan Gate 2 Core Duel Implementation

## Outcome

M1 technical exitのcloseout evidenceと、M-1 human gateを証拠なしで先行するProject owner waiverを分離して固定し、M2 Core Duelをcontent projection、deck／hand／qi、card play、山賊棋士、resolved starter scope、headless integration／replay、preview、Godot graybox、playable UATの依存順にbounded TASKへ分割する。最初のproduction TASKだけが次に`ready`へ遷移できるqueueを作る。

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
- [[DECISION-0007 Authorize Gate 2 Without Retained TASK-0012 Human Evidence]]

## Non-goals

- production code、tests、`game_data/`、schema、toolchain、package／project reference、Godot assetの変更。
- Rules Canon、Accepted ADR／Feature Spec／Milestones、card／enemy runtime値の変更。
- starter deck composition、M2 facility inclusion、カード効果の未決事項を実装都合で決めること。
- Gate 3のMomentum／full counterattack／content expansion、formal simulator、meta progression。
- playable／fun claim、TASK-0032以降の実装先取り。

## Allowed areas

- 本TASKと新規TASK-0032〜0042ノート。
- 新規Decision Neededノート。
- TASK-0012 evidence statusとTASK-0030 post-gate evidence。
- `docs/50_Validation/Spec Fixtures/FEAT-009 Enemy Decision Fixtures.md`のhuman sign-off status同期。
- Current Development State、Project Dashboard／Hub、Current Sprint、Backlog、Codex Task Queueのstatus／sequence同期。

## Acceptance criteria

- PR #20 merged head／merge commit、post-merge main CIを記録し、TASK-0030をhuman merge evidenceで`done`へ遷移する。
- Project ownerの指示をexact quote付きowner-authorized assumption／gate waiverとして記録し、TASK-0012の実施／一致evidenceとは表現せず`review`を維持する。
- M1 technical `PASS`とDECISION-0007のowner waiverにより、TASK-0012 evidence未確認のままGate 2 entryだけが明示的に許可されたことを記録する。
- M2の最小vertical pathをTASK-0032〜0042へ分割し、各TASKにOutcome、Non-goals、Allowed areas、Acceptance、Validation、dependency／blockerを記載する。
- task順はtyped Content／system policy projection → deterministic deck／hand／qi → atomic card play → stone／reinforce effects → 山賊棋士 planner → resolved starter scope／conditional Development → headless integration／replay → Application preview → Godot graybox → playable UATとする。consumerをfoundationより先に`ready`にしない。
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
- TASK-0012の実施／一致をProject ownerのassumption以上に捏造する必要がある。
- production／Godot変更を混ぜないと文書検証を通せない。

## Execution log

2026-07-12 — PR #20を人間merge。merged head `5f194a9987ba314a2eefe9f30b020d31901fc79e`、merge commit／fixed `origin/main` `d1f69e10672ed7289c056cee32c4875964494fe4`、post-merge main CI run `29193892563`のGovernance `86653293786`、Pure .NET `86653311052`、Godot／Windows export `86653371594`がすべてsuccessであることを確認した。

2026-07-12 — Project ownerが「TASK-0012の二人human sign-offは行った前提で先に進めてください」と明示。実施／一致evidenceへ変換せず、owner-authorized assumption／gate waiverとして[[DECISION-0007 Authorize Gate 2 Without Retained TASK-0012 Human Evidence]]へ記録した。M1 technical `PASS`とこのwaiverによりGate 2 entryだけが許可されたため、本TASKを`ready`にした。

2026-07-12 — Project ownerの継続指示を本TASKの選択として記録。production実装を始めず、Gate 2を一TASKへ過積載しないdocs-only decompositionとして`in_progress`へ遷移した。

2026-07-12 — Accepted [[Deck and Card System]]の初期12枚、Accepted M2 milestoneの「初期6カード」、proposed Initial Card Setの6種／12枚recipe、proposed grayboxの施設除外、starter `card_development`の関係が一義的でなく、machine-readable starting recipeも存在しないことを確認した。[[DECISION-0006 Resolve M2 Starter Deck and Facility Scope]]へ隔離した。

2026-07-12 — TASK-0012本体と[[FEAT-009 Enemy Decision Fixtures]]のHuman sign-off表を、evidence未確認、owner-authorized assumption、raw worksheet／identity未保存へ同期した。架空のreviewer／resultは記録しない。

2026-07-12 — `tools/dev/check`を2回実行し、両runで全documentation／wikilink／content／fixture／repository検査が成功。47 unique content IDsとcontent snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`が一致し、`git diff --check`もexit 0だった。

2026-07-12 — initial fixed-HEAD reviewはbase `d1f69e10672ed7289c056cee32c4875964494fe4`、head `ed961440ed37e660ebf968065f2e3701c29ed0db`を照合し、Content system policy／resolved recipe projectionのowner gapを`MEDIUM`として`CHANGES REQUIRED`。secondary auditも、owner assumptionをhuman evidenceへ変換した記述、Decision前のDevelopment実装、Accepted turn-end discardとの不一致、FEAT-009 planning lifecycle不足、M2 exit matrix不足を指摘した。

2026-07-12 — owner instructionをevidenceと分離したresolved waiver DECISION-0007へ修正し、TASK-0012を`review`へ戻した。TASK-0032へbase qi／drawとContent → Domain conversionを追加し、Development／recipe適用をDECISION-0006でblockedな新TASK-0038へ移動。turn-end discard、Bandit planning lifecycle、M2 exact exit matrixを補い、後続をTASK-0042まで再採番した。

2026-07-12 — review修正後に`tools/dev/check`を2回再実行し、47 unique content IDsと同一content snapshotを維持して全成功。`git diff --check HEAD`もexit 0。

2026-07-12 — fixed head `5dc4b7a6d68c39db76024b0a6c585d09d70a4f48`の再レビューで、DECISION-0006 Option 3はDevelopmentを含むのにTASK-0038が除外していた矛盾を`HIGH`として検出。Option 3にもM2 facility例外を要求し、TASK-0038をoption番号ではなくresolved recipeのDevelopment有無で分岐するよう修正した。

2026-07-12 — Option 3修正後に`tools/dev/check`を2回実行し、全check、47 unique content IDs、content snapshotが一致して成功した。

2026-07-12 — fixed head `3b7952f7f34a4e4920d1242b974b3dd882911739`の再レビューで、TASK-0038 Allowed areasだけがDevelopment integrationをOption 1に限定したままと判明。resolved recipeがDevelopmentを含む任意optionを許可する文言へ同期した。

2026-07-12 — independent fixed-HEAD reviewがbase `d1f69e10672ed7289c056cee32c4875964494fe4`、head `604db6e171b788bc66409d26ba12cdf4c76a1e06`のcumulative diffを再照合。前回findingがすべて閉じ、owner waiver、Decision branches、Accepted／proposed境界、Content ownership、discard／Reinforce timing、FEAT-009 lifecycle、M2 exit matrix、ID／dependency／statusにactionable findingなし、`APPROVE`。secondary auditも`APPROVE`し、本TASKを`review`へ遷移した。

## Evidence

- PR #20 human merge commit `d1f69e10672ed7289c056cee32c4875964494fe4`／post-merge main CI run `29193892563`全3 job success。
- TASK-0012 evidence waiver — Project owner exact instruction、2026-07-12。実施／一致evidenceではない。
- `tools/dev/check` ×2 — exit 0、47 unique content IDs、同一content snapshot。
- `git diff --check` — exit 0。
- independent fixed-HEAD review — base `d1f69e10672ed7289c056cee32c4875964494fe4`、head `604db6e171b788bc66409d26ba12cdf4c76a1e06`、actionable findingなし、`APPROVE`。secondary auditも`APPROVE`。

## Known issues

TASK-0012の二人human sign-off evidenceはrepositoryで未確認であり、Gate 2先行根拠はDECISION-0007のProject owner waiverに限定される。DECISION-0006はstarting deck recipe／Development scopeを適用するTASKまでにowner resolutionが必要である。typed candidate contentとinjected-recipe deck kernelはdecision結果を先取りせず実装可能。
