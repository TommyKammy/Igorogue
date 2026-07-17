---
type: task
id: TASK-0042
status: in_progress
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0041]
updated: 2026-07-18
---
# TASK-0042 Validate M2 Core Duel Graybox

## Outcome

M2 Core Duelをfresh startからBandit win／loss、restart、replayまで人間操作し、technical evidenceとUATを分離して`PASS`／`NOT PASSED`／`DECISION NEEDED`を記録する。

## Non-goals

- balance tuning、content追加、Gate 3実装、funを自動testだけで証明すること。

## Allowed areas

- M2 validation report、playtest report、TASK／status文書。
- UATで再現したbugのbounded follow-up TASK proposal。
- production修正は別TASKとする。

## Acceptance criteria

- fresh launchからcard play、target preview、End Turn、Bandit action、terminal、restartを完走する。
- accepted command replayが同一terminal resultへ到達する。
- coordinate orientation、アタリ／capture／territory／intent表示、mouse focusをhuman reviewする。
- [[Milestones and Exit Gates]]のM2各項目を、7×7 UI、DECISION-0006でresolvedしたstarter set／exact recipe、山賊棋士、intent／アタリ／capture／territory表示へ一行ずつ追跡するexit matrixを作る。
- resolved recipeへ含まれる各card content IDが実際にdraw／select可能であり、各効果を少なくとも一度human操作できることをseed／content hash付きで記録する。除外カードがある場合はDECISION-0006のscopeと一致させる。
- Banditに対するwin pathとloss pathをそれぞれ完走し、seed、content hash、terminal result、replay evidenceを記録する。
- technical passと「面白い」のE4 claimを分離し、未検証を明示する。
- blockerがあれば再現steps／seed／content hashと最小follow-upを記録する。

## Validation

- repository wrappers、Godot smoke／export、full-battle replay。
- conversational UAT、human visual review、independent evidence review、CI全job。

## Known issues

Technical validation／conversational UATは開始し、fresh Test 1のside／orientation／intent／selection clearはProject ownerが合格とした。一方、M2判定、fresh win／loss human path、replay parity、全starter card effect coverageは未確定。Godot UIから現runのaccepted command log／Replay V3／state・log checksumを取り出す入口がないことをruntime監査で確認し、受け入れ条件のblockerとして追跡する。Project ownerは「まだゲームを楽しむレベルではない」と評価しており、技術判定と分離する。

## Execution log

2026-07-16 — TASK-0041はPR #31 source HEAD `a653edf6e86acbea334fe30925e4c174abf62317`、main merge `4a2745ca30990689789d60ef79e4721579b82bbe`、post-merge CI run `29292101348`全3 job success、Project owner human visual approvalを満たして`done`へ遷移した。dependencyを解除し、本TASKを`ready`／not startedとした。

2026-07-17 — PR #32 source HEAD `0ad575d56e353312d24f32f007ac7c324eddad07`、main merge `ebec9dbdf249cb1db8e13910996022877abdb617`を確認した。PR CI run `29507033877`は全3 job success。main push runが自動登録されなかったため、同merge HEADへworkflow dispatchしたCI run `29537645016`で全3 job successを再確認し、本TASKを`in_progress`へ遷移した。

2026-07-17 — fixed baselineで`tools/dev/check`、`build`、`test`、`sim-smoke`、`godot-smoke`、`export-windows`を実行し、全てexit 0。Domain 368、Application 193、Architecture 92の計653 tests成功、Godot graybox smoke seed `39039` checksum `7692094b4154966821fe7251d4fde59c73fcd16c09c8527579885dade55b9cf6`、Windows Debug export SHA-256 `19776780eacd618c28450320c6b78f051c713ad4060870de6520866eb768792a`を記録した。Godot human-runをReplay V3へ採取する入口は未確認であり、runtime監査とfresh UATを継続する。

2026-07-18 — PR #33 source HEAD `d3256587cf8a835e7e009dd44bae4cf3a609f0d5`、main merge `1d6b7c2e2ede5671e7d4736548e6728908fb7bf9`を確認した。PR CI run `29539092195`とpost-merge main CI run `29613756684`は全3 job success。Project ownerはfresh Test 1のplayer／Bandit識別、`(1,1)`左下／`(7,7)`右上、intent読解、card選択／右click解除を合格とした。同時に「まだゲームを楽しむレベルではない」と回答したため、E4 interaction partial passとE4 fun `NOT PASSED`を分離して記録した。
