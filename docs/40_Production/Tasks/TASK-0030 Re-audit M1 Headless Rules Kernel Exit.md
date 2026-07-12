---
type: task
id: TASK-0030
status: in_progress
project: Igorogue
milestone: M1
priority: critical
dependencies: [TASK-0025, TASK-0026, TASK-0027, TASK-0028, TASK-0029]
updated: 2026-07-12
---
# TASK-0030 Re-audit M1 Headless Rules Kernel Exit

## Outcome

PR #19 merge後のfixed main HEADでM1 Headless Rules KernelのAccepted exit statementsを共有Rules Kernel、Application、golden replay、CI evidenceへ再追跡し、technical resultを`PASS`／`NOT PASSED`／`DECISION NEEDED`のいずれかで確定する。Gate 2 entryはM1 technical resultと[[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]の二人human sign-offを分離して判定する。

## Source of truth

- [[Rules Canon]]
- [[Milestones and Exit Gates]]
- [[Development Plan]]
- [[Architecture]]
- [[Determinism and Replay]]
- [[Golden Replay Index]]
- [[Validation Strategy]]
- [[TASK-0025 Gate 1 Deterministic Foundation Audit]]
- [[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]]
- [[TASK-0027 Implement Temporary Liberty Domain Kernel]]
- [[TASK-0028 Implement Closed-Window Capture Benefits and TLE Boundary Pressure]]
- [[TASK-0029 Integrate Temporary Liberty Enemy Boundary and Golden Replay]]

## Non-goals

- gameplay／Gate 2 implementation、production code、tests、`game_data/`、toolchain、package／project reference、Godot assetの変更。
- Rules Canon、Accepted ADR／Feature Spec／Milestonesの意味変更。
- formal board simulator、card／deck／hand／qi loop、enemy planner、graybox UIの実装済み扱い。
- `tools/dev/sim-smoke`のformal board evidence化、E1 fixtureのE3昇格、playable／fun claim。
- TASK-0012の二人human sign-off代替、human evidenceなしのGate 2 task `ready`化。
- fixed HEAD `6398ec1e4f1e4ecf0c8eeaf71e33bb6ddeff6875`に対する歴史的TASK-0025 auditの上書き／再open。

## Allowed areas

- 本TASKノート。
- `docs/50_Validation/Gate Audits/`の新規M1 re-audit report。
- TASK-0029 post-merge evidence。
- Current Development State、Project Dashboard／Hub、Current Sprint、Backlog、Codex Task Queueのstatus／evidence同期。
- gap時のDecision Neededノートとbounded follow-up TASK proposal。

## Acceptance criteria

- PR #19 merged head／merge commit、post-merge main CI、fixed audited main HEADを記録し、TASK-0029をhuman merge evidenceにより`done`へ遷移する。
- Milestones、Development Plan、Architecture、Determinism、Golden Replay Index、Rules CanonのM1 statementsをmatrix化し、各行をimplemented artifact、test／golden、merged TASK、CI、evidence classへ追跡する。
- board、groups／liberties、capture／suicide／repetition、king／turn-limit terminal、territory／facility、seed／RNG、accepted-only command log、headless battle、versioned replayを現fixed HEADで再確認する。
- TASK-0025の歴史的`DECISION NEEDED`とDECISION-0005 Option 1のpost-audit dispositionを保持し、MOM-01〜19／CTR-01〜25をM3、TLE-01〜15をM1とする現在のsource境界を再確認する。
- TLE-01〜15についてproduction Domain lifecycle、closed-window benefits、enemy boundary、versioned Application golden／replayをE3 evidenceへ追跡し、仕様checkerをproduction evidenceとして使わない。
- 「UIなし一戦処理」とscripted command／enemy boundaryの関係、formal simulator smoke、card loop、actual enemy planner、graybox UIの非実装境界を明示し、playable／M2完成と表現しない。
- M1 technical resultを`PASS`／`NOT PASSED`／`DECISION NEEDED`の一つで記録する。全Accepted M1 statementsが矛盾なくE3 evidenceへ追跡できる場合だけ`PASS`とする。
- TASK-0012 human-only M-1 gateをtechnical resultから分離し、未完了ならGate 2 entryを`BLOCKED`、全Gate 2 taskをnot-readyに維持する。
- fixed-HEAD independent reviewでmatrix、evidence、結論、scopeが`APPROVE`される。

## Validation

- fixed `origin/main` derived worktreeで`tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を各2回実行し、同一content snapshot、test count、checksumを記録する。
- `tools/dev/build`、`git diff --check`、docs-only diff、全wikilink／TASK statusを確認する。
- root `CODE_REVIEW.md`に従うindependent fixed-HEAD docs／evidence reviewを記録する。
- GitHub CI全jobを確認する。

## Stop conditions

- Accepted M1 statement同士、DECISION-0005、または現artifact evidenceが矛盾する。
- M1 technical resultを出すためにAccepted sourceの意味変更またはproduction実装が必要。
- TASK-0012 human evidenceをCodex判断で完了扱いする必要がある。
- audit scope外の破壊的変更が必要。

## Execution log

2026-07-12 — PR #19 human mergeにより全dependencyが`done`となり、Current SprintがTLE E3 migration後のM1 exit再監査を次工程として明示しているため、production変更を含まないbounded evidence taskとして本TASKを`ready`にした。

2026-07-12 — Project ownerのmerge報告と継続指示を本TASKの選択として記録。新規gameplay実装を選択せず、fixed `origin/main` `35139bedb927f4c15b4e62a02c423947d5bdb1da`から`in_progress`で開始した。

2026-07-12 — PR #19 merged head `141f431c70cf905c2bb0af8b05e72ee382be8c6e`、merge commit `35139bedb927f4c15b4e62a02c423947d5bdb1da`、post-merge main CI run `29190754762`のGovernance `86644893062`、Pure .NET `86644908211`、Godot／Windows export `86644948844`がすべてsuccessであることを確認した。

2026-07-12 — 歴史的TASK-0025 matrixの非TLE rowsを現HEADのartifact／testsへ再追跡し、全行が維持されていることを確認した。DECISION-0005 Option 1によりactive Feature Spec、fixture、Golden Replay Index、Milestones、queueはMOM／CTR production migrationをM3へ一貫して配置しており、現M1 source conflictはない。

2026-07-12 — TASK-0027〜0029のDomain lifecycle、closed-window benefits、authoritative enemy boundary、replay schema 2、TLE-01〜15 goldenをE3 evidenceへ追跡した。accepted M1 statementsに未解決gapはなく、substantive audit resultを`M1 TECHNICAL EXIT: PASS`とした。actual enemy planner、card loop、Godot graybox、formal simulatorは後工程境界であり、playable claimを行わない。

2026-07-12 — TASK-0012の二人human sign-offはpendingのため、technical resultと分離して`GATE 2 ENTRY: BLOCKED`、Gate 2 implementation TASKはnot-readyを維持した。

## Evidence

- fixed main HEAD `35139bedb927f4c15b4e62a02c423947d5bdb1da`。
- PR #19 human merge／post-merge main CI run `29190754762`全3 job success。
- [[TASK-0030 M1 Headless Rules Kernel Exit Re-audit]] — fixed-HEAD exit matrix、TLE E3 migration、UI-less／formal-simulator／human-gate境界、technical `PASS`／Gate 2 `BLOCKED`判定。

## Known issues

[[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]の独立した人間2名によるpaper sign-offはpendingであり、Codex reviewでは代替しない。本auditのtechnical resultにかかわらず、human evidenceが揃うまでGate 2 entryはblockedである。
