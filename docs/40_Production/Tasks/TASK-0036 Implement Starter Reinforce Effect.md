---
type: task
id: TASK-0036
status: review
project: Igorogue
milestone: M2
priority: high
dependencies: [TASK-0035]
updated: 2026-07-12
---
# TASK-0036 Implement Starter Reinforce Effect

## Source of truth

- [[Rules Canon]]
- [[Deck and Card System]]
- [[Combat Resolution Order]]
- [[FEAT-011 Temporary Liberty Lifecycle and Expiry Sweep]]
- [[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]
- [[DECISION-0008 Align Reinforce Content Order with FEAT-011]]
- typed Core Duel content catalog and `game_data/content/cards.json`

[[Initial Card Set]]は補助的なproposed設計意図であり、runtime値の正本として扱わない。

## Outcome

`card_reinforce`をatomic PlayCardへ接続し、friendly-group targeting、timed temporary liberty、conditional drawを共有kernelで解決する。

## Non-goals

- `card_development`のeffect／default deck採用、施設engine拡張、Momentum、enemy planner、Godot。

## Allowed areas

- reinforce technique operationの限定Domain／Application integration。
- Domain／Application tests、本TASK／status文書。

## Acceptance criteria

- 補強はtarget groupとstable stone anchorをcommand時stateへbindし、既存TLE lifecycleでenemy-turn-end expiryを設定する。
- target選択時の有効呼吸点が1なら仮呼吸点付与前にアタリ対象としてdrawし、その後に+1 effectを付与する。stale／foreign targetはexact no-opで拒否する。
- canonical state／facts／command logが同一入力で一致する。

## Validation

- repository wrappers、target lifecycle、TLE expiry、atari conditional draw、stale／foreign target negative tests。
- independent fixed-HEAD review、CI全job。

## Known issues

PlayCardからactual enemy turn／expiry sweep／replayまでのfull compositionはTASK-0039が所有する。`card_development`とdefault starter recipeはTASK-0038まで変更しない。

## Execution log

2026-07-13 — PR #25 human mergeとpost-merge main CI全3 job successによりdependency TASK-0035が`done`。Project ownerの継続指示を本TASK選択として記録し、fixed main `e025b8c326c52c9e76241e756a0e1e54171ef7fb`から専用worktree／branchを作成して`in_progress`へ遷移した。

2026-07-13 — Rules Canon、Deck and Card System、Combat Resolution Order、FEAT-011、ADR-0014、DECISION-0008、typed contentを監査。補強は付与前の有効呼吸点1判定とdrawを先に解決し、同じcommand-time groupのcanonical stable stone instanceへ+1 timed effectを付与する。Headless／replay／actual enemy boundary integration、Development、default recipe、Godotは後続TASKへ維持する。

2026-07-13 — Starter／Technique／FriendlyGroupと`draw_if_target_atari`→`temporary_liberty`のexact operation shapeから作る`StarterReinforceCardPlayDefinition`／pure evaluatorを実装した。content ID分岐を置かず、deck／qi／stone runtime／timed／continuous snapshot、target group、canonical runtime anchor、付与前effective libertyをexact bindする。

2026-07-13 — standalone `PlayCardCommand v1`へgroup-target用のmodeなしoverloadを追加し、既存stone payloadは維持した。Reinforce＋stone mode／stone＋noneをexact no-op拒否し、accepted時だけcard／costをcommit、条件付きdrawを完全解決してから既存`TemporaryLibertyGrantResolver`でstable effectを付与する。state encodingをv3へ上げ、Reinforce typed definition、effect identity／sequence／expiry、共有RNGをcanonical checksum／accepted-only logへ含めた。

2026-07-13 — noncanonical targetからのcanonical anchor、real／timed／continuous effective liberty、draw→grant fact順、空点／白group／mode／stale rejection、stack、turn 7 expiry、既存expiry sweep bridge、group merge追従、列挙反転をDomain／Application／Architecture testsで固定した。precommit独立レビュー3系統は仕様、決定論、architectureのいずれもactionable findingなし、`APPROVE`。

2026-07-13 — repository wrapperを最終差分で再実行。build、529 tests、governance／content、formal simulator smoke、`git diff --check`がすべて成功し、warning／error 0。fixed-HEAD independent review前のためstatusは`in_progress`を維持する。

2026-07-13 — independent fixed-HEAD reviewがimplementation commit `58846ea9f9f6707dad386a52605acabe23f927eb`をbase `e025b8c326c52c9e76241e756a0e1e54171ef7fb`と比較。clean tree、16-file cumulative diff、全Acceptance、accepted specs、command／state／fact／log determinism、exact no-op、non-goalを再検証し、actionable findingなし、`APPROVE`。secondary fixed-HEAD reviewも`APPROVE`したため本TASKを`review`へ遷移した。

## Evidence

- PR #25 human merge — merged head `2c8c1a0d20e5b1be856476ca0cd7f6a0bc20b79c`、merge commit `e025b8c326c52c9e76241e756a0e1e54171ef7fb`、post-merge main CI run `29225344562`全3 job success。
- typed Reinforce projection — runtime catalogのcost、FriendlyGroup target、DrawIfTargetAtari、TemporaryLiberty amount／duration／timing／stackingをexact shapeから投影し、逆順／異種shapeをfail closed。
- target／lifecycle evidence — command-time canonical runtime anchor、timed／continuous込みpre-grant effective liberty、draw→grant順、2 effect stack、turn 7 expiry、既存sweep handoff、merge追従、rejected exact no-op、列挙反転を直接検証。
- `tools/dev/build` — exit 0、exact .NET SDK `8.0.422`、warning 0／error 0。
- `tools/dev/test` — exit 0。Domain 324、Application 147、Architecture 58、計529 tests、failure 0／skip 0。
- `tools/dev/check` — exit 0。47 content IDs、content snapshot `sha256:cd53980e2edd69ad14b3815c800a3c5aab119f21d95d724d083afa2920c15ad6`。
- `tools/dev/sim-smoke` — exit 0、`checksum=5f943a3cbc6847a14e841612c57d2d2cf4aef78d8b7441c0ff4d8b279113625c`。bootstrap determinism evidenceとしてのみ使用。
- `git diff --check` — exit 0。
- independent precommit reviews — 仕様／決定論／architectureの3系統すべてactionable findingなし、`APPROVE`。
- implementation commit `58846ea9f9f6707dad386a52605acabe23f927eb` — typed Reinforce projection、exact target binding、atomic draw／grant、TLE lifecycle／determinism evidence。
- independent fixed-HEAD reviews — base `e025b8c326c52c9e76241e756a0e1e54171ef7fb`、head `58846ea9f9f6707dad386a52605acabe23f927eb`、primary／secondaryともactionable findingなし、`APPROVE`。
