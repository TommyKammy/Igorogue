---
type: task
id: TASK-0044
status: blocked
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0043]
updated: 2026-07-18
---
# TASK-0044 Revalidate M2 Core Duel Graybox on Merged Replay Head

## Outcome

PR #35 merged fixed HEADをimmutable baselineとして、TASK-0042で未完了だったM2 Core DuelのE3／E4をfresh再検証する。starter 6 effects、アタリ／capture／territory、Bandit win／loss／restart、human-run Replay V3 parity、play-experience diagnosisを新しいauditへ集約し、technical、human UAT、fun、Gate 3 dispositionを分離して判定する。

## Dependency gate

[[TASK-0043 Capture and Verify Godot Human Run Replay V3]]のmacOS graphical loss／win、terminal overlay、explicit Replay V3 artifact／readback／fresh parity、restart後のsealed artifact不変が完了し、TASK-0043が`done`になるまで本TASKは`blocked`とする。PR #35のmergeだけをhuman evidenceの代替にしない。

TASK-0043と同じmerged HEAD、seed、content hashで採取したgraphical loss／win evidenceは本TASKへ再利用し、同一UATを二重実行しない。

## Non-goals

- production code、Rules Canon、Accepted ADR、balance、content、seed recipe、UI、Replay schemaの変更。
- Gate 3 implementation、art／audio、meta progressionの開始。
- `.tscn`、`.tres`、`project.godot`、export presetの編集。
- Windows debug exportをWindows runtime filesystem evidenceとして扱うこと。
- human UATの未実施項目をautomated／headless evidenceで置換すること。

UATでbugを確認した場合はseed、content hash、再現手順、期待値／実値を記録し、別のbounded follow-up TASKを提案する。本TASK内でproduction fixやplayer-visible rule変更を開始しない。

## Allowed areas

- `docs/50_Validation/Gate Audits/`の新しいTASK-0044 revalidation audit。
- [[PT-0001 呼吸点と捕獲の理解]]のfresh Test 2〜8 evidence。
- TASK、Current Sprint、Queue、Backlog、Dashboard、Project Hub、Current Development Stateのstatus／evidence更新。
- read-only runtime execution、explicit `/tmp` Replay V3 artifact、log、screenshot、checksum evidence。

## Acceptance criteria

- baseline full SHA、clean tree、.NET SDK、Godot version、content hash、PR #35 source／merge SHA、post-merge CIを記録する。
- TASK-0043 graphical loss／winでterminal overlay `REPLAY V3 VERIFIED`、explicit artifact path、disk readback、artifact SHA-256、fresh replay terminal／state／log parity、restart後のsealed bytes不変を証拠化する。
- resolved starter six IDsをhuman操作でdraw／select／resolveする。seed `0`でBasic Stone／Contact／Extend／Reinforce、seed `1`でLure Stone、seed `6`でDevelopmentを確認し、6／6 matrixを埋める。
- humanがcanonical orientation、intent、legal／illegal preview、アタリ、capture、territory、terminal reason、restart後のfresh stateを確認する。
- seed `39039`のloss／winを各1本terminalまでhuman完走し、accepted-result chainから保存されたReplay V3が同じoutcome／reason／final state／final logへ到達する。旧automated evidenceをhuman evidenceの代替にしない。
- [[PT-0001 呼吸点と捕獲の理解]] Test 2〜8を更新し、technical完走とfun質問を分離する。「まだ楽しくない」は正当なfun結果であり、technical failureへ混ぜない。
- 新auditでM2 exit matrix、six-card matrix、terminal／replay matrixを埋め、`M2 TECHNICAL EXIT`、`E4 HUMAN UAT`、`E4 FUN CLAIM`、`GATE 3 ENTRY`を別行で判定する。
- 全必須technical／E4行が閉じるまでGate 3を`BLOCKED`に維持する。funがnegativeでも原因を記録するだけで、未承認のrules／balance変更を行わない。
- fixed-HEAD independent reviewとCI全jobを完了する。

## Validation

- `tools/dev/check`
- `tools/dev/verify-tools`
- `tools/dev/build`
- `tools/dev/test`
- `tools/dev/sim-smoke`
- `GODOT_BIN=/absolute/path/to/godot-mono tools/dev/godot-smoke`
- `GODOT_BIN=/absolute/path/to/godot-mono tools/dev/export-windows`
- macOS graphical loss／win／restart UAT with separate absolute `/tmp` Replay V3 paths
- saved JSON metadata／artifact SHA-256／fresh replay parity／sealed restart inspection
- `git diff --check`
- independent fixed-HEAD review
- PR CI／post-merge CI

## Blocker

TASK-0043 graphical human loss／win UATが未完了である。TASK-0043を`done`へ遷移できるevidenceが揃うまで、本TASKを`in_progress`へ変更しない。

## Execution log

2026-07-18 — PR #35 source HEAD `507956a89165fb08280f128b61c62bd01b8d2560`がmain merge commit `adf894dafe7096b977343fd6bdd2737e41a74809`へhuman mergeされた。post-merge main CI run `29625979222`は全3 job success。merged HEADでcheck、build、653 tests、sim smoke、Godot Replay V3 loss／win／repeat／save-race smoke、Windows debug exportを再実行し、全て成功した。

2026-07-18 — TASK-0043 graphical human evidenceが未完了のため、本TASKをdependency `[TASK-0043]`の`blocked`として準備した。Gate 3は引き続き`BLOCKED`。
