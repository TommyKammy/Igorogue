---
type: task
id: TASK-0037
status: review
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0036]
updated: 2026-07-13
---
# TASK-0037 Implement Bandit Intent Planning and Execution

## Outcome

FEAT-009の`enemy_bandit`について、候補生成、強制処刑／王石防衛、辞書式ranking、planned target、retarget／fallback／pass、通常／bonus action実行を共有Rules KernelとApplicationへ接続する。

## Source of truth

- [[FEAT-009 Enemy Action Planning and Placement]]
- [[Enemy Design and Intent]]
- [[FEAT-009 Enemy Decision Fixtures]]
- [[DECISION-0009 Resolve Bandit Multi-Group Capture Target Reference]]
- [[DECISION-0010 Resolve Bandit Advance With Zero Real King Liberties]]
- `game_data/content/enemies.json`
- existing authoritative enemy boundary

## Non-goals

- 侵入者、hidden randomness、囲碁探索AI、counterattack full runtime、UI、card loop integration／replay update。

## Allowed areas

- pure Domain enemy candidate／ranking policy。
- Application planning／execution integration。
- Domain／Application／Architecture tests、本TASK／status文書。

## Acceptance criteria

- all candidate legalityは既存placement／effective-liberty／repetition／territory／facility kernelを呼び、敵専用ルール複製を作らない。
- mandatory lethal、defense threshold、capture non-king、pressure、advance、fallback、passをAccepted lexicographic orderとCanonical point tie-breakで実装する。
- planned intentがtarget ref、primary、最大2 alternates、retargetable、planned checksumをcanonicalに保持する。
- battle startの初回player turn前と各enemy turn終了後に次の通常actionを計画する。bonus actionは既存boundaryが次enemy turnでの発生を確定した場合だけ別枠で計画し、M2でcounterattack generationを新設しない。
- player turn中はdisplayed `intent_id`を固定し、target invalidationは実行時retargetへ回す。mandatory lethal／defense overrideはturn-end preview queryへ明示できるtyped stateを返す。
- 実行時にoverride、same-intent retarget、fallbackを再評価し、terminal後は残りactionを抑止する。
- FEAT-009のF09-01〜03とBanditに適用するF09-08 branchをE3へ移植し、same state／content hashから同一plan／placementになる。Invader固有のF09-04〜07は本TASKへ含めない。

## Validation

- repository wrappers、fixture migration、battle-start／post-enemy-turn planning lifecycle、player-turn stability、override preview、input reversal、retarget／pass／terminal negatives。
- independent fixed-HEAD review、CI全job。

## Known issues

[[DECISION-0009 Resolve Bandit Multi-Group Capture Target Reference]]はowner-selected Option 1でresolved。複数非王石group同時捕獲では最大groupをprimary targetとし、同数なら黒王石groupまでのstone-to-stone最小Manhattan距離、さらに同率ならgroup anchorのCanonical point orderで決める。primary消滅時はsame-intent retarget、UI outlineはprimaryだけとする。

[[DECISION-0010 Resolve Bandit Advance With Zero Real King Liberties]]もowner-selected Option 1でresolved。黒王石groupの実呼吸点が0で有効呼吸点により生存するstateでは、advance第1scoreを黒王石groupに属する石までの最小Manhattan距離へfallbackする。実呼吸点が1点以上のrankingと後続scoreは変更しない。

Invaderは本TASK対象外。replay compositionとFEAT-009のfull per-action telemetryはTASK-0039のcompositionへdeferする。本TASKのfactsはintent lifecycleをtypedに保持するが、`enemy_id`／`enemy_behavior_version`、`preview_points`、`candidate_count`、`fallback_depth`、`board_checksum_before`／`board_checksum_after`等を1つのauthoritative action recordへcomposeすることは本TASKで実装済みと扱わない。

## Execution log

2026-07-13 — PR #26 human mergeを確認。merged head `3b0cec3c4327803f05b7dda5447912cab6e1ed95`、merge commit／main `45ca3f8cb3cb4cc4c8ade45273c38c76e08f8f73`、post-merge main CI run `29228982431`全3 job successによりdependency TASK-0036は`done`。

2026-07-13 — Project ownerの継続指示を本TASK選択としてread-only source auditを実施。既存runtime placement pipelineはeffective-liberty、repetition、facility、territoryを再利用でき、Domain共通evaluatorへ抽出可能と確認した。production code変更前に、複数非王石group同時捕獲時のsingular target／distance／retarget semanticsがAccepted sourceで未定義と判明した。

2026-07-13 — [[DECISION-0009 Resolve Bandit Multi-Group Capture Target Reference]]を作成。F09-02距離key `1`／`0`もliteral group間Manhattan距離として到達不能であり、pre-ranked E1 expected winnerとE3 board-derived scoreを区別した。player-visible behaviorを実装都合で決めず、本TASKを`blocked`のままowner decision待ちとした。

2026-07-13 — Project ownerが「最大グループを主対象にする（推奨）。同数なら王石への距離、次にcanonical anchorで決定。」を選択。DECISION-0009 Option 1として、primary group、distance score、primary消滅時retarget、単一UI outlineを解決し、本TASKを`in_progress`へ戻した。

2026-07-13 — 共通runtime placement evaluator、Bandit候補生成／ranking／plan／execution／factsと、既存BattleState v2／replay schemaを変更しないApplication sidecarを実装。F09-01〜03、F09-08、mandatory override、same-intent retarget、fallback、normal／bonus、terminal抑止、stale exact no-op、DECISION-0009全tie-breakとprimary消滅branchをE3 testsへ移植した。

2026-07-13 — FEAT-009のfull per-action telemetryは既存BattleState／replayとのcompositionが必要なためTASK-0039へdefer。本TASKではintent factsを追加するが、enemy id／behavior version、preview points、candidate count、fallback depth、実行前後board checksum等の完全なaction recordを統合済みと主張しない。

2026-07-13 — 独立semantic auditで、黒王石groupの実呼吸点0／有効呼吸点正の到達可能stateにおけるadvance scoreがAccepted sourceで未定義と判明。暫定実装のpassを承認済み挙動としてcloseoutせず、[[DECISION-0010 Resolve Bandit Advance With Zero Real King Liberties]]を作成して本TASKを`blocked`へ遷移した。

2026-07-13 — Project ownerが「DECISION-0010もOption 1で進めて」と明示。実呼吸点集合が空の場合だけ黒王石groupの石までの最小Manhattan距離へfallbackするOption 1を正本とし、仕様blockを解除して本TASKを`in_progress`へ戻した。実装修正、E3 regression、full closeout suiteはこれから実行する。

2026-07-13 — DECISION-0010 Option 1をproduction candidate generator／rankerへ実装。timed +1により黒王石groupが実呼吸点0／有効呼吸点1で生存する到達可能stateから、黒王石groupの石までの距離、後続score、Canonical tie-breakをproduction plannerが導くE3 regressionを追加した。通常F09-01 rankingは不変。

2026-07-13 — 独立contract auditのP2を修正。Bandit contentのaction budget、mandatory順、通常／反攻priority、intentとscore profileの対応、tie-breakを候補評価前にfail-closed検証し、checksumだけ変わって実行policyが無視される状態を拒否する。normal後に変化した盤面でbonusを再評価するcase、normal terminal時のbonus抑止、generated production catalogからのF09-01実行も追加検証した。

2026-07-13 — full closeout suite成功。TASK-0037を`review`へ遷移し、fixed-HEAD independent reviewとCIを待つ。

2026-07-13 — fixed-HEAD Domain reviewで、F09-01の初回`(6,4)`だけでなく固定黒手後の`advance (6,3)`／`pressure (6,2)`までをE3で保護する不足が指摘された。F09-01紙上3行動の各exact Domain contextをproduction plannerへ通す回帰を追加し、全566 tests成功を確認した。Application／lifecycle reviewと修正HEAD Domain再レビューはいずれも指摘なしで承認済み。CIは未完了。

2026-07-13 — Draft PR #27を作成。review済みHEAD `9f074500cebf8abe401b13eaf20e2aca15d7260e` に対するCI run `29236172329`で、governance／generated content、Pure .NET build／566 tests／sim smoke、Godot 4.7 .NET headless smoke／Windows debug exportの全3 jobが成功した。TASKはhuman review／merge待ちの`review`を維持する。

## Evidence

- PR #26 human merge／post-merge main CI run `29228982431`全3 job success。
- mandatory source read、existing Domain／Application enemy boundary read-only audit、F09-01〜03／08 fixture gap audit。
- baseline `tools/dev/test` exit 0。Domain 324、Application 147、Architecture 58、計529 tests pass。
- integrated `tools/dev/build` exit 0、warning 0、error 0。
- closeout `tools/dev/test` exit 0。Domain 348、Application 158、Architecture 60、計566 tests pass。
- closeout `tools/dev/check` exit 0。content snapshot `sha256:cd53980e2edd69ad14b3815c800a3c5aab119f21d95d724d083afa2920c15ad6`。
- closeout `tools/dev/sim-smoke` exit 0。checksum `5f943a3cbc6847a14e841612c57d2d2cf4aef78d8b7441c0ff4d8b279113625c`。
- closeout `tools/dev/build` exit 0、warning 0、error 0。`git diff --check` exit 0。
- independent semantic auditはDECISION-0009、merge-anchor、primary消滅、normal／bonus、terminal、facts／checksum、BattleState／replay非変更を承認し、DECISION-0010だけをP1 specification blockとして報告した。
- Project ownerの2026-07-13の選択によりDECISION-0010 Option 1はresolved。real=0／effective=1のE3 regressionと通常case不変をproduction kernelで確認した。
- independent contract auditのP2 fail-closed content findingは修正済み。full per-action telemetry compositionはKnown IssuesどおりTASK-0039へdeferする。
- fixed-HEAD Application reviewは指摘なし。Domain reviewのF09-01紙上3行動E3 coverage findingは修正し、`advance (6,3)`から`pressure (6,2)`への遷移をproduction plannerで確認した。修正HEAD `ef93acc2ccede6b16e650a6bf5e3f10522efca94` の再レビューも指摘なしで承認済み。
- Draft PR #27、CI run `29236172329`、HEAD `9f074500cebf8abe401b13eaf20e2aca15d7260e`。全3 job success（governance／generated content、Pure .NET build／tests／sim smoke、Godot .NET headless／Windows debug export）。
