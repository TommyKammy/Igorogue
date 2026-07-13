---
type: task
id: TASK-0032
status: done
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0031]
updated: 2026-07-13
---
# TASK-0032 Implement Typed Core Duel Content Catalog

## Outcome

`game_data/`のstarter card候補6種、`enemy_bandit`、Core Duel system policy（base qi／base draw）を、content hashへbindされたimmutable／typed projectionとしてload／validateしてpure Domain definitionsへ変換し、後続Applicationへcontent ID別switchなしで渡せる境界を作る。

## Source of truth

- [[Architecture]]
- [[Engine Toolchain and Repository Layout]]
- [[Deck and Card System]]
- [[FEAT-009 Enemy Action Planning and Placement]]
- [[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]
- [[DECISION-0008 Align Reinforce Content Order with FEAT-011]]
- `game_data/content/cards.json`
- `game_data/content/enemies.json`
- `game_data/balance/system.json`
- card／enemy schemas

## Non-goals

- starting deck recipeの選択、card effect解決、deck／hand state、enemy ranking、Application command、replay、Godot。
- [[DECISION-0008 Align Reinforce Content Order with FEAT-011]]で承認された`card_reinforce` operation順修正を除く、`game_data/`値、schema意味、Accepted ruleの変更。

## Allowed areas

- `src/Igorogue.Content/`とContent → Domainのapproved project reference。
- content definitionに必要な最小`src/Igorogue.Domain/` value types。
- Content／Architecture tests。
- `tests/Igorogue.Application.Tests/BootstrapApplicationTests.cs`の既存Content manifest integrity tests。
- `tools/check_repository_bootstrap.py`のapproved Content → Domain reference assertion。
- `tools/check_enemy_behaviors.py`と対応unit testのenemy schema-derived action budget assertion。
- `game_data/content/cards.json`のReinforce operation順と、対応するgenerated content snapshot。
- `tests/Igorogue.Application.Tests/TemporaryLibertyGoldenFixtureAdapter.cs`と`tests/golden/v2/temporary_liberty_cases.json`のsource-bound content hash更新。
- `docs/50_Validation/Golden Replay Index.md`のhistorical／active TLE catalog hash記録。
- [[DECISION-0008 Align Reinforce Content Order with FEAT-011]]。
- project reference変更で生成差分が生じる`packages.lock.json`。
- 本TASKとstatus文書。

## Acceptance criteria

- starter候補6種と山賊棋士のID、cost／type／target／placement tags／ordered operations、behavior parameters／priority／fallback、および`system.json`のbase qi／base drawをtyped immutable Domain definitionsへ欠落なく変換する。
- unknown／duplicate ID、unsupported operation shape、dangling reference、invalid stable orderをfail-closedで拒否する。
- canonical projectionとcontent hashが入力key順に依存せず、same snapshotから同一結果になる。
- ContentはAccepted architectureどおりpure Domain definitionsへ変換できるが、ルール結果を決めず、Application／Godot型へ依存しない。Domain definitionはfilesystem／JSON型へ依存しない。
- DECISION-0006のstarting recipe／facility採用を先取りしない。

## Validation

- `tools/dev/check`、`tools/dev/test`、`tools/dev/build`。
- malformed card／enemy／system policy negatives、input enumeration reversal、Content → Domain／Application非依存architecture boundary。
- independent fixed-HEAD review、CI全job。

## Known issues

DECISION-0006のstarting recipe／facility scopeは未解決だが、本TASKのcandidate definition projectionをblockしない。starting recipeとeffect executionは実装しない。

## Execution log

2026-07-13 — PR #21 human mergeとpost-merge main CI successによりdependency TASK-0031が`done`。本TASKを唯一の次production taskとして`ready`にした。

2026-07-13 — Project ownerの継続指示を本TASK選択として記録。fixed main `708852d900f84d0b4905706b99dd77415b6a0ae8`から専用worktree／branchを作成し、Outcome、Non-goals、Allowed areas、Acceptance、Validationを再確認して`in_progress`へ遷移した。

2026-07-13 — accepted architectureと現governanceを照合。Content → Domain project referenceはTASKで承認済みだが、`tools/check_repository_bootstrap.py`とArchitecture testが旧no-reference境界を固定しているため、Allowed areasへ限定assertion更新とaffected lockfileを明記した。

2026-07-13 — immutableなDomain content definitionsとtyped loaderを実装。starter候補6種のordered operations／capture trigger、山賊棋士のbehavior／priority／fallback、base qi／base drawをcontent hashへbindし、unknown／duplicate／dangling／cyclic／unsupported shapeをfail-closedにした。starting recipe、effect execution、deck／hand、enemy rankingは導入していない。

2026-07-13 — independent pre-commit auditの3 findingを反映。manifest検証済みbytesをsnapshotへ保持して同一bytesをparseし、mandatory overrideと通常／反攻priorityの重複、enemy schema外action budgetを負例付きで拒否した。

2026-07-13 — Content → Domain reference追加後に`tools/dev/update-locks`を実行し、影響する5つの`packages.lock.json`だけを再生成・確認した。最終差分に対してgovernance、Release build、全444 .NET test、simulator smokeを成功させた。

2026-07-13 — independent Codex taskがbase `708852d900f84d0b4905706b99dd77415b6a0ae8`からfixed HEAD `d78054b2e53c8957cf87ed756c74ffe6846b3a10`を`CODE_REVIEW.md`に従って再監査。actionable findingなし、decision `APPROVE`。本TASKを`review`へ遷移し、human merge待ちとした。TASK-0033は本TASKのhuman mergeまで`blocked`を維持する。

2026-07-13 — PR #22 Codex reviewの未解決3 threadをthread-awareに取得し、TASK、後続TASK、card schema、Rules Canon、FEAT-011、runtime dataと照合。starter exact ID集合、routing field欠落、Reinforce効果順の3件をすべて妥当と判定し、review remediation中は本TASKを`in_progress`へ戻した。

2026-07-13 — ReinforceのAccepted順とruntime dataの矛盾を[[DECISION-0008 Align Reinforce Content Order with FEAT-011]]で解決。Project ownerの「妥当であれば修正」指示を限定承認として、`draw_if_target_atari`→`temporary_liberty`へdataを整合し、loaderでは逆順をrejectして黙示canonicalizeしない方針とした。

2026-07-13 — starter exact ID集合をContent境界で検証し、Domainへcontent ID解釈を持ち込まない既存architectureを維持。starter stoneは非空`placement_tags`、non-stone starterは`target`を必須化し、未知ID、欠落／空routing、Reinforce逆順の負例を追加した。

2026-07-13 — content更新後の最初のtestで、Domainへ置いたexact ID guardが既存no-content-ID architecture testに抵触し、source-bound TLE goldenが旧hashを検出して計3 failure。ID guardをContentへ移し、documented opt-inでTLE catalogを再生成した。再生成diffはcontent／cards source hashとcontent hash由来log checksumだけが変わり、state checksum、ordered facts、resultは不変。通常モード再実行は全453 .NET tests pass。

2026-07-13 — independent remediation auditでactive Golden Replay Indexがpre-review TLE catalog SHAだけを指す文書不整合を検出。旧SHAを履歴として保持し、新active catalog／content／cards source SHAを追記した。

2026-07-13 — independent Codex taskがPR #22 remediation commit `0ac66d1c9caa6299b5be347fec1328d3e9bd7e20`をparent `ef4264173a940f86b0c479c6739010f3a5cb57cb`からfixed-HEAD review。actionable findingなし、decision `APPROVE`。本TASKを`review`へ戻し、TASK-0033はhuman mergeまで`blocked`を維持する。

2026-07-13 — PR #22を人間merge。merged head `6291f1463f7d5f2267fe84c547f344d165b30b02`、merge commit／post-merge main HEAD `0b4b8f5c1558e98051c758002269fca1994d5ca9`を確認した。main push CI run `29214123093`のGovernance `86706780633`、Pure .NET `86706800087`、Godot／Windows export `86706857486`はすべてsuccess。本TASKを`done`へ遷移した。

## Evidence

- PR #21 merge commit `708852d900f84d0b4905706b99dd77415b6a0ae8`／post-merge main CI run `29210667448`全3 job success。
- Initial implementation `tools/dev/check` exit 0。47 content IDs、pre-review generated content hash `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`（7 files）。
- `tools/dev/build` exit 0。exact .NET SDK `8.0.422`、0 warnings／0 errors。
- Initial implementation `tools/dev/test` exit 0。Domain 293、Application 106、Architecture 45、合計444 tests pass。
- governance unit tests exit 0。schema-derived action budget negativesを含む17 tests pass、abstract simulator 2 tests pass。
- Initial implementation `tools/dev/sim-smoke` exit 0。checksum `3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`、pre-review content hash、7 files。
- `tools/dev/update-locks` exit 0。Content project dependency追加に伴うContent／Application.Tests／Architecture.Tests／Sim.Cli／Godotの5 lockfileを更新。
- Independent fixed-HEAD review：`d78054b2e53c8957cf87ed756c74ffe6846b3a10`、actionable finding 0、`APPROVE`。
- PR #22 remediation `tools/dev/check` exit 0。47 content IDs、content hash `sha256:cd53980e2edd69ad14b3815c800a3c5aab119f21d95d724d083afa2920c15ad6`（7 files）、governance 17／abstract simulator 2 tests pass。
- PR #22 remediation `tools/dev/test` exit 0。Domain 293、Application 106、Architecture 54、合計453 tests pass。
- PR #22 remediation `tools/dev/build` exit 0。exact .NET SDK `8.0.422`、0 warnings／0 errors。
- PR #22 remediation `tools/dev/sim-smoke` exit 0。checksum `5f943a3cbc6847a14e841612c57d2d2cf4aef78d8b7441c0ff4d8b279113625c`、content hash `sha256:cd53980e2edd69ad14b3815c800a3c5aab119f21d95d724d083afa2920c15ad6`、7 files。
- TLE golden opt-in regeneration：15 cases、state checksum／ordered facts／result不変。cards source SHA-256、content hash、content hash由来log checksumだけを更新。
- PR #22 remediation independent fixed-HEAD review：`0ac66d1c9caa6299b5be347fec1328d3e9bd7e20`、actionable finding 0、`APPROVE`。
- PR #22 human merge — merged head `6291f1463f7d5f2267fe84c547f344d165b30b02`、merge commit `0b4b8f5c1558e98051c758002269fca1994d5ca9`、post-merge main CI run `29214123093`全3 job success。
