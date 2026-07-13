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

visible scopeはgrayboxでありfinal presentationではない。Codex visual QAでinitial／selected-hoverの480×270 captureを確認したが、Project ownerによるhuman visual reviewは未完了。並列検証中に複数Godot editor-buildが同一`.godot`出力を競合し、一度のGUI起動で`.NET: Assemblies not found`後のネイティブcrashが発生した。残存processを停止し、正規Godot executableのserial build→GUI captureと全wrapperで再発なしを確認済み。

## Execution log

2026-07-14 — PR #30のhuman mergeを確認。TASK-0040 source HEAD `eaa62531615eef7a10cfe1d16fe92318d45143c8`、main merge commit `d8ccc08cf7fa3cc1a43046d128b2804b50b9d073`、post-merge main CI run `29285926156`全3 job successによりdependencyを解除し、TASKを`in_progress`へ遷移した。

2026-07-14 — mandatory source／Application startup監査で、標準盤面とpolicyからauthoritative initial snapshotを生成するproduction factoryがなく、test fixtureだけが組み立てていることを確認した。Godotへのrule／value複製を避けるため、Project ownerがpure typed Domain／Content／Application startup seamと対応testだけを本TASKへ追加する限定scopeを承認した。実装によるrule／runtime value変更は行わない。

2026-07-14 — `CoreDuelBattleSetupDefinition`と`CoreDuelBattleStartup.Start`を追加し、`balance/system.json`から初期配置、turn limit、facility policy、counterattack policy／start gaugeをContent境界で型付き化した。生成snapshotは従来のstandard test fixtureとcanonical text、state checksum、command-log checksumが完全一致する。

2026-07-14 — 既存`BootstrapSmoke.tscn`／`project.godot`を変更せず、graphical起動だけが`CoreDuelGraybox`を生成する構成にした。7×7盤、全hand、qi、turn／result、Bandit intent、primary／alternate、territory／facility、atari／capture／king riskをApplication queryから描画し、card select→hover→exact commit、End Turn→enemy command、terminal restartをApplication commandへ限定した。

2026-07-14 — headless graybox smokeを同一content／seedの決定性、query、legal card commit、enemy resolution、terminal、restart count 1まで拡張した。repository tests 639件、sim smoke、Godot headless parse／build／bootstrap smoke、Windows debug exportはすべて成功した。

2026-07-14 — 並列検証の競合で発生したGodotネイティブcrashを調査。orphaned editor-buildを終了し、`/Users/tomoakikawada/Applications/Godot_mono.app/Contents/MacOS/Godot`に固定したserial editor build→selected-hover GUI captureをexit 0で再実行し、Godot process残留なしを確認した。

## Evidence

- PR #30 merge／post-merge main CI evidenceによりTASK-0040 dependency完了。
- 2026-07-14 Project owner instruction — production標準初期snapshot factory欠落に対する限定typed startup seamを承認。player-visible rule／runtime value変更は対象外。
- `tools/dev/test` — Domain 360／Application 193／Architecture 86＝639 tests success、build 0 warning／0 error。
- `tools/dev/sim-smoke` — exit 0、bootstrap checksum `36ca153c20b82b2220c82b787c229d22f255fee7c42fed9c5ce7753ae0ff7bf1`。
- `tools/dev/godot-smoke` — Godot 4.7 .NET headless parse／build／scene run exit 0、full terminal／restart graybox checksum `7692094b4154966821fe7251d4fde59c73fcd16c09c8527579885dade55b9cf6`。
- Graphical capture — Compatibility renderer／Apple M4でinitialとselected-hoverを480×270 PNGへ保存。左下`(1,1)`、右上`(7,7)`方向、black王石左下／white王石右上、intent primary／alternate、selected focus、legal target／hover previewをCodex visual QAで確認。Project owner human reviewはpending。
- `tools/dev/export-windows` — exit 0、Windows debug executable SHA-256 `311d17928384c219430f96a9959a2eebcd1bb8a649163fe8bd9cc5ae8b33977d`。
- Godot crash follow-up — concurrent editor-build競合を解消後、serial editor build、GUI capture、headless smoke／exportがすべてexit 0、残存Godot process 0。
