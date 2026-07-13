---
type: task
id: TASK-0041
status: in_progress
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0040]
updated: 2026-07-14
---
# TASK-0041 Build Playable Godot Core Duel Graybox

## Outcome

Godot 4.7 .NETで7×7盤、手札、気、Bandit intent、turn／result、card target、End Turn、battle restartを操作できる最小grayboxを作り、Application command／queryだけへ接続する。

## Source of truth

- [[UI UX Overview]]
- [[Battle Screen Specification]]
- [[Interaction and Input]]
- [[Coordinate System and Initial Position]]
- [[v0.1.1 Graybox Scope]]

Battle Screen、Interaction、graybox scopeは`proposed`のvisual／interaction referenceとしてのみ使う。本TASKはそれらをAccepted ruleへ昇格させず、player-visible rule conflictがあればDecision Neededで停止する。

## Non-goals

- final art／audio、run map／shop／meta、Momentum／Brilliant／full counterattack UI、Invader、broad accessibility polish。

## Allowed areas

- `game/Igorogue.Godot/`のC# presentation code。
- 本TASKが明示的に必要とする`.tscn`、`.tres`、`project.godot`入力設定。
- 標準Core Duelのauthoritative initial snapshotを生成するために必要な、pure typed Domain startup contract、`game_data/` backed Content projection、Application startup factoryの限定seam。Godot型は禁止し、rule／runtime valueは変更しない。
- Godot／Domain／Content／Application／Architecture integration tests、本TASK／status文書。

## Acceptance criteria

- board orientationは左下(1,1)／右上(7,7)、石／領地／アタリ／capture可能点をquery projectionから描画する。
- hand／qi／turn／intent／primary・alternate points／battle resultを表示する。
- mouseでcard選択 → target hover → confirm、End Turn、restartを操作できる。Godotはstateを直接変更しない。
- graybox startupはproduction factoryから標準初期stateを取得し、初期配置、policy、runtime valueをGodot presentationへ複製／直書きしない。
- scene／resource編集はGodot headless parse/build、bootstrap smoke、Windows exportを通す。
- human visual reviewでlayout、focus、pixel scaling、危険表示、coordinate orientationを確認する。

## Validation

- repository wrappers、typed startup seam tests、Godot headless smoke、Windows debug export、scene parse。
- human visual review、independent fixed-HEAD review、CI全job。

## Known issues

visible scopeはgrayboxでありfinal presentationではない。Codex visual QAでinitial／selected-hoverの480×270 captureを確認したが、Project ownerによるhuman visual reviewは未完了。並列検証中、複数のGodot editor-build processが存在する時点で一度のGUI起動がネイティブ`EXC_BAD_ACCESS`で終了した。添付crash reportは原因を特定しておらず、並列build競合との因果関係は未確定。全process停止後のserial build→GUI capture、headless smoke、Windows exportでは再現していない。

## Execution log

2026-07-14 — PR #30のhuman mergeを確認。TASK-0040 source HEAD `eaa62531615eef7a10cfe1d16fe92318d45143c8`、main merge commit `d8ccc08cf7fa3cc1a43046d128b2804b50b9d073`、post-merge main CI run `29285926156`全3 job successによりdependencyを解除し、TASKを`in_progress`へ遷移した。

2026-07-14 — mandatory source／Application startup監査で、標準盤面とpolicyからauthoritative initial snapshotを生成するproduction factoryがなく、test fixtureだけが組み立てていることを確認した。Godotへのrule／value複製を避けるため、Project ownerがpure typed Domain／Content／Application startup seamと対応testだけを本TASKへ追加する限定scopeを承認した。実装によるrule／runtime value変更は行わない。

2026-07-14 — `CoreDuelBattleSetupDefinition`と`CoreDuelBattleStartup.Start`を追加し、`balance/system.json`から初期配置、turn limit、facility policy、counterattack policy／start gaugeをContent境界で型付き化した。生成snapshotは従来のstandard test fixtureとcanonical text、state checksum、command-log checksumが完全一致する。

2026-07-14 — 既存`BootstrapSmoke.tscn`／`project.godot`を変更せず、graphical起動だけが`CoreDuelGraybox`を生成する構成にした。7×7盤、全hand、qi、turn／result、Bandit intent、primary／alternate、territory／facility、atari／capture／king riskをApplication queryから描画し、card select→hover→exact commit、End Turn→enemy command、terminal restartをApplication commandへ限定した。

2026-07-14 — headless graybox smokeを同一content／seedの決定性、query、legal card commit、enemy resolution、terminal、restart count 1まで拡張した。repository tests 653件、sim smoke、Godot headless parse／build／bootstrap smoke、Windows debug exportはすべて成功した。

2026-07-14 — independent reviewで標準初期配置のtyped境界がking数と対称性だけではfail closedにならない点を修正した。`standard_v0_2` ID、中央空点、各色king 1／guard 2、3石連結king group、実呼吸点7をDomainで検証し、Content mutation負例と、座標を固定せず同じ構造を許容する正例を追加した。

2026-07-14 — 並列検証中のGodotネイティブcrashを調査。添付reportはAppKit起動中の`EXC_BAD_ACCESS`／`abort()`を示すが原因は特定できない。全editor-build processを終了し、`$GODOT_BIN`に固定したserial editor build→selected-hover GUI captureをexit 0で再実行し、headless smoke／exportも成功、Godot process残留なしを確認した。因果関係は未確定のため、serial起動で再発する場合は別defectとしてconsole／editor logを取得する。

2026-07-14 — code-bearing fixed HEAD `93866298b6c7d416169c2a02d0233504b7580212`を独立再レビューした。startup validation、Godot command／query境界、restart smoke、crash evidenceに未解決findingなし。status文書2件の旧test件数だけを653へ同期し、Project owner human visual reviewとPR CIを残るacceptance gateとして維持した。

2026-07-14 — Draft PR #31を作成し、HEAD `34e42d05d62b0fcdb588251a4e4518145fa5226d`に対するCI run `29289620374`でgovernance、pure .NET build／tests／sim smoke、Godot headless smoke／Windows exportの全3 job successを確認した。Project owner human visual reviewだけを残るacceptance gateとして維持する。

## Evidence

- PR #30 merge／post-merge main CI evidenceによりTASK-0040 dependency完了。
- 2026-07-14 Project owner instruction — production標準初期snapshot factory欠落に対する限定typed startup seamを承認。player-visible rule／runtime value変更は対象外。
- `tools/dev/test` — Domain 368／Application 193／Architecture 92＝653 tests success、build 0 warning／0 error。
- `tools/dev/sim-smoke` — exit 0、bootstrap checksum `36ca153c20b82b2220c82b787c229d22f255fee7c42fed9c5ce7753ae0ff7bf1`。
- `tools/dev/godot-smoke` — Godot 4.7 .NET headless parse／build／scene run exit 0、full terminal／restart graybox checksum `7692094b4154966821fe7251d4fde59c73fcd16c09c8527579885dade55b9cf6`。
- Graphical capture — Compatibility renderer／Apple M4でinitialとselected-hoverを480×270 PNGへ保存。左下`(1,1)`、右上`(7,7)`方向、black王石左下／white王石右上、intent primary／alternate、selected focus、legal target／hover previewをCodex visual QAで確認。Project owner human reviewはpending。
- `tools/dev/export-windows` — exit 0、Windows debug executable SHA-256 `311d17928384c219430f96a9959a2eebcd1bb8a649163fe8bd9cc5ae8b33977d`。
- Godot crash follow-up — 原因未確定。全editor-build process停止後のserial editor build、GUI capture、headless smoke／exportがすべてexit 0で再現なし、残存Godot process 0。
- Independent fixed-HEAD review — startup seam／Godot runtimeは`APPROVE`、governanceのcrash表現／private path findingsは解消済み。Project owner human visual reviewはpending。
- Draft PR #31 CI — run `29289620374`、HEAD `34e42d05d62b0fcdb588251a4e4518145fa5226d`、全3 job success。
