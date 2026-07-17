---
type: task
id: TASK-0043
status: ready
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0042]
updated: 2026-07-18
---
# TASK-0043 Capture and Verify Godot Human Run Replay V3

## Outcome

Godot grayboxで人間入力から実行されたCore Duel Application command結果を欠落なく保持し、最初のterminalをrestart前にReplay V3 canonical JSONへ保存する。保存fileを同じgame version／content hash／seedからfresh startupしたsessionへload／replayし、terminal result、state checksum、log checksumのparityを検証可能にする。

## Root cause

- `CoreDuelGameHost`は現在sessionだけを保持し、fresh initial sessionを保持しない。
- PlayCard、End Turn、各Bandit action、Restartの`CoreDuelBattleCommandResult`を`SessionAfter`へ適用後に破棄する。特に一度のEnd Turn操作が生成するEndPlayerTurnと1回以上のBandit resultを後から復元できない。
- Replay V3 capture／serializer／runnerは既に必要情報を扱えるが、Godot compositionからinitial session／ordered result chain／save pathへ接続されていない。
- terminal overlayは`click to replay`と表示するが、実際の処理はfresh restartでありReplay V3再生ではない。

## Non-goals

- gameplay rule、balance、card／enemy content、Replay schemaの変更。
- fun改善、card追加、Gate 3実装。
- interactive replay viewer、timeline、scrub、raw mouse／hover／selection event replay。
- general save system、複数terminal artifact管理、暗黙のrepository内artifact生成。

## Allowed areas

- `game/Igorogue.Godot/CoreDuel/`のC# host／graybox／replay evidence service。
- `game/Igorogue.Godot/Smoke/BootstrapSmoke.cs`の明示launch optionとstable evidence output。
- Godot host integration tests、既存Replay V3 regression tests、validation／TASK／status文書。
- `.tscn`、`.tres`、`project.godot`、export presetは変更しない。

## Acceptance criteria

- fresh launchのexact initial `CoreDuelBattleSession`と、Application state machineへ実際に到達した全`CoreDuelBattleCommandResult`を順序付きで各1回保持する。accepted／rejected flagとreasonを保持し、hover／selection／right-click／queryだけのUI操作はcommand resultとして記録しない。
- 最初のterminal transitionでrestartより前に`BattleReplayDocumentV3.Capture(initial, results)`を行う。artifact terminalは画面のoutcome／end reasonと一致し、1 launchにつき最初の1 artifactだけを確定してrestart後に上書きしない。
- 明示`--graybox-replay-out=<path>`指定時だけReplay V3 serializerのcanonical bytesを安全に保存し、raw artifact SHA-256を算出する。未指定の通常grayboxはrepoやuser directoryへartifactを暗黙生成しない。
- 保存fileそのものを再openし、`BattleReplaySerializerV3`でloadする。in-memory documentだけの検証で済ませない。
- document metadataと同じcatalog／game version／seedからfresh sessionを開始し、`BattleReplayRunnerV3`でreplayする。terminal outcome／reason、final state checksum、final log checksumがhuman run／artifact／replayで完全一致する。
- content mismatch、tamper、save／readback failure、parity driftでは`verified=true`を出さずreason付きでfail closedする。
- stable console evidenceへpath、seed、content hash、attempt／accepted count、outcome／reason、initial／final state・log checksum、attempts／document checksum、artifact SHA-256、replayed state／log、`verified`を完全値で出力する。
- terminal overlayへ最小のReplay V3 save／parity結果を表示し、現在の`click to replay`を実処理どおり`click to restart`へ訂正する。既存のplay／restart操作を退行させない。
- seed `39039`のhuman-shaped win／loss host integration pathで、End Turn内の全Bandit resultを含むresult chain、canonical bytes、artifact SHA-256、file readback、fresh replay parity、restart recorder resetを検証する。
- Replay V3のschema／tamper／foreign metadata／attempt・size limitsとv1／v2／v3 compatibility regressionを維持する。

## Validation

- `tools/dev/check`
- `tools/dev/build`
- `tools/dev/test`
- `tools/dev/sim-smoke`
- `GODOT_BIN=/absolute/path/to/godot-mono tools/dev/godot-smoke`
- `GODOT_BIN=/absolute/path/to/godot-mono tools/dev/export-windows`
- macOS graphical loss／win UAT with explicit `/tmp` Replay V3 output path
- saved JSON metadata／artifact SHA-256／fresh replay parity inspection
- `git diff --check`
- independent fixed-HEAD review
- CI全job

## Known issues

[[TASK-0042 M2 Core Duel Graybox Validation]]で、automated Replay V3 win／loss parityは成立する一方、Godot human-operated runからartifactを取得できずM2 technical exitが`NOT PASSED`となった。content snapshotは`sha256:aa26362f6c4b1cdc9c8dc9336654bd20fe5379f622eef3fa992257db62d86832`。

## Execution log

2026-07-18 — TASK-0042 runtime監査でroot causeと最小scopeを特定した。TASK-0042をvalidation result `NOT PASSED`として閉じ、本TASKを`ready`化した。production codeは未変更。
