---
type: task
id: TASK-0043
status: review
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
- PlayCard、End Turn、各Bandit actionの`CoreDuelBattleCommandResult`を`SessionAfter`へ適用後に破棄する。特に一度のEnd Turn操作が生成するEndPlayerTurnと1回以上のBandit resultを後から復元できない。
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

- artifact範囲をfresh launchのexact initial `CoreDuelBattleSession`から、最初のterminal-producing `CoreDuelBattleCommandResult`までの両端を含む区間とする。この区間でApplication state machineへ実際に到達した全resultを順序付きで各1回保持する。accepted／rejected flagとreasonを保持し、hover／selection／right-click／queryだけのUI操作はcommand resultとして記録しない。
- 最初のterminal transitionでrestartより前に`BattleReplayDocumentV3.Capture(initial, results)`を行う。artifact terminalは画面のoutcome／end reasonと一致し、1 launchにつき最初の1 artifactだけを確定してrestart後に上書きしない。
- `RestartBattleCommand`とその他のpost-terminal resultはsealed terminal artifactへ含めない。restart後もsealed canonical bytes／document checksum／artifact SHA-256を不変に保ち、同じlaunchではrecorderをdisabled／sealed状態に維持してsecond artifactを生成しない。
- 明示`--graybox-replay-out=<path>`指定時だけReplay V3 serializerのcanonical bytesを安全に保存し、raw artifact SHA-256を算出する。未指定の通常grayboxはrepoやuser directoryへartifactを暗黙生成しない。
- 保存fileそのものを再openし、`BattleReplaySerializerV3`でloadする。in-memory documentだけの検証で済ませない。
- document metadataと同じcatalog／game version／seedからfresh sessionを開始し、`BattleReplayRunnerV3`でreplayする。terminal outcome／reason、final state checksum、final log checksumがhuman run／artifact／replayで完全一致する。
- content mismatch、tamper、save／readback failure、parity driftでは`verified=true`を出さずreason付きでfail closedする。
- stable console evidenceへpath、seed、content hash、attempt／accepted count、outcome／reason、initial／final state・log checksum、attempts／document checksum、artifact SHA-256、replayed state／log、`verified`を完全値で出力する。
- terminal overlayへ最小のReplay V3 save／parity結果を表示し、現在の`click to replay`を実処理どおり`click to restart`へ訂正する。既存のplay／restart操作を退行させない。
- seed `39039`のhuman-shaped win／loss host integration pathで、End Turn内の全Bandit resultを含むterminalまでのresult chain、canonical bytes、artifact SHA-256、file readback、fresh replay parity、restart後のsealed artifact不変／second artifact禁止を検証する。
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

本実装のmacOS headless win／loss／save-race evidence、Windows debug export、PR #35 CI／merge／post-merge CIは成功したが、macOS graphicalで人間が操作したloss／win terminal artifactとterminal overlayのhuman visual確認は未実施である。Windows上のruntime save／renameも未実行であり、Windows export successをruntime filesystem evidenceとして扱わない。本TASKは`review`に留め、graphical human UATとcloseout evidenceより前に`done`を主張しない。

## Execution log

2026-07-18 — TASK-0042 runtime監査でroot causeと最小scopeを特定した。TASK-0042をvalidation result `NOT PASSED`として閉じ、本TASKを`ready`化した。production codeは未変更。

2026-07-18 — PR #34のhuman mergeを確認。TASK-0042 closeout source HEAD `9e684d71925057e120d240058bfa23da05abb4f1`、main merge commit `6a9c7dee394399bc499438f74c55e041c26e4be5`をTASK-0043のfixed baselineとした。PR CI run `29614924069`とpost-merge main CI run `29620281041`はともにgovernance、pure .NET build／tests／sim smoke、Godot headless smoke／Windows exportの全3 job success。dependencyを解除し、本TASKを`in_progress`へ遷移した。

2026-07-18 — `CoreDuelGameHost`の全Application command実行を単一seamへ集約し、exact initial sessionからaccepted／rejected resultを順序どおり保持するlaunch-scoped recorderを追加した。最初のnon-terminal→terminal resultを含めて即時sealし、Restart／post-terminal resultを除外する。selection、hover、right-click、queryはstate machine resultでないため収録しない。

2026-07-18 — 明示`--graybox-replay-out=<absolute path>`だけを有効化し、canonical bytesを同一directoryのowned temporary fileへ`CreateNew`／`Flush(true)`後にno-overwrite moveする。保存fileを再openしてraw SHA-256、canonical再serialization、document metadata、fresh startup Replay V3のterminal／state／log parityを検証し、全工程後だけ`verified:true`を出す。terminal overlayを`REPLAY V3 VERIFIED／FAILED／OFF`へ接続し、誤記`click to replay`を`click to restart`へ修正した。

2026-07-18 — macOS Godot 4.7 .NET headlessでseed `39039`のloss／human-shaped winを実host経由で各2回実行した。各artifactは31 attempts／30 acceptedで、同一経路のcanonical bytesはbyte-for-byte一致。restart後にsecond terminalまで進めてもevidence object／bytesは不変で、save-time target raceはsentinel不変、temporary file残留なし、`verified:false`／`artifact_io_failure`となった。CI Godot jobもdirect launchから同wrapperへ接続した。

2026-07-18 — code-bearing fixed HEAD `eaca6e5a7f97a8d0b4db168abd6ffa131a1032a3`をbase `6a9c7dee394399bc499438f74c55e041c26e4be5`へ独立reviewした。実装findingなしで`APPROVE WITH FOLLOW-UP`。repository checks、653 tests、sim smoke、Godot win／loss／repeat／race smoke、Windows debug export、diff checkは全成功。残るgraphical human win／loss UAT、terminal overlay visual確認、PR CI／human reviewを明示し、TASKを`review`へ遷移した。

2026-07-18 — Draft PR #35 initial CI run `29622330781`はGodot起動前に`DOTNET_BIN does not point to a file: dotnet`でGodot jobのみ失敗した。POSIX `_common.sh`が未指定のPATH command名を明示path overrideとして子Pythonへexportする既存契約不整合が、CIをrepository wrapperへ接続したことで顕在化した。unset／emptyはexportしないlocal default、explicit non-emptyは子processへ保持する修正と3境界testをcommit `a693505d0f82eeafde40182e1e4a1d13bcf64828`へ追加した。独立reviewの空値findingも修正後にfindingなし`APPROVE`。`DOTNET_BIN` unset／emptyの両方でcheck／build／test／sim-smoke／godot-smoke／export-windows、653 tests、Godot Replay V3 smoke、Windows exportを再検証し、PR CI rerun `29622768204`は全3 job success。

2026-07-18 — PR #35 source HEAD `507956a89165fb08280f128b61c62bd01b8d2560`をmain merge commit `adf894dafe7096b977343fd6bdd2737e41a74809`へhuman mergeした。post-merge main CI run `29625979222`は全3 job success。merged HEADでcheck、build、653 tests、sim smoke、Godot Replay V3 loss／win／repeat／save-race smoke、Windows debug exportを再実行し、全て成功した。graphical human artifactは未生成のためstatusは`review`を維持し、後続[[TASK-0044 Revalidate M2 Core Duel Graybox on Merged Replay Head]]を`blocked`で準備した。

## Evidence

- `tools/dev/check` — exit 0、documentation／wikilink／content／governance／wrapper checks success。
- `tools/dev/build`／`tools/dev/test` — exit 0、warning 0／error 0、Domain 368＋Application 193＋Architecture 92＝653 tests success。Replay V1／V2／V3 schema、tamper、foreign metadata、attempt／size limit regressionを維持。
- `tools/dev/sim-smoke` — exit 0、checksum `36ca153c20b82b2220c82b787c229d22f255fee7c42fed9c5ce7753ae0ff7bf1`。
- common Replay V3 boundary — initial state `f1f718f000f63cf284c38284295b0d69c5a8ff3b512e3c0a50569ef1e9ab3be8`、initial log `9cbd4fb1c03a5ffa33f24c9f7aea52d62e9c46e3c222667b4d0c0aabb52f1efa`、31 attempts／30 accepted。
- loss artifact (`<TEMP_DIR>/igorogue-task0043-loss.json`) — outcome `loss`／reason `black_king_captured`、final state `008f3d0865cc83ebad869706ba0885d7231cf422d1dc95b940b1ece3d93f4711`、final log `f77e02965374c6266b9f92e96184a58ff88956806b56eb6cff00c0310bd34339`、attempts checksum `10dda0caff92aed9879578635f9fae253788b1c31a10dc3ae5ea9dcd2f684c21`、document checksum `3955c87d149baf1ed388eec247ddca127cea93c5607f94a69895d371496ab0ca`、raw SHA-256 `2f81068272beaa4b6f4f6c2e7b77884f16f24606d35e9c6961da22fe27c304b7`、28,814 bytes、fresh replay state／log一致、`verified:true`。
- win artifact (`<TEMP_DIR>/igorogue-task0043-win.json`) — outcome `win`／reason `white_king_captured`、final state `1fc97bb91f9be10b71d5370053580f051a499fef459741b674c427b85a743706`、final log `17a22ad2716a70f105d3bc0d4be4d4f5ef5e780aa7c009d98eff071cf7a18622`、attempts checksum `91d7c0c375d5e757ea5a27078b647d301fcdce8afc97efc3ae0b0b7c81a786b3`、document checksum `253f4dd78436f5d6f67e2d16b74a86c521b14aec4224093d24f6361049d65028`、raw SHA-256 `6ff9aae41202c49ff136e7962af9b4243889d3e9a036b0b8a74fb4d1a8b6c6be`、30,140 bytes、fresh replay state／log一致、`verified:true`。
- independent raw digest inspection — `shasum -a 256`がstable console evidenceのloss／win artifact SHA-256と完全一致。
- `tools/dev/godot-smoke` — Godot `4.7.stable.mono.official.5b4e0cb0f`、default no-artifact smoke、loss／win／repeat byte identity、restart sealed immutability、second artifact禁止、save-time race fail-closedがexit 0。
- `tools/dev/export-windows` — exit 0、Windows debug executable SHA-256 `ebfeefeb705ec612d000efed236532d52c1863a1628bdc1f5ffda6e297faf821`。
- independent fixed-HEAD review — `eaca6e5a7f97a8d0b4db168abd6ffa131a1032a3`、implementation findingなし、`APPROVE WITH FOLLOW-UP`。follow-upはmacOS graphical human loss／win UATとterminal overlay visual確認。
- PR #35 CI correction — initial run `29622330781`のPOSIX `DOTNET_BIN` export defectを`a693505d0f82eeafde40182e1e4a1d13bcf64828`で修正。unset／empty／explicit pathの20 governance tests、独立review、required validation wrappersが成功し、rerun `29622768204`はgovernance、pure .NET build／653 tests／sim smoke、Godot Replay V3 smoke／Windows exportの全3 job success。
- PR #35 merge／post-merge — source `507956a89165fb08280f128b61c62bd01b8d2560`、main `adf894dafe7096b977343fd6bdd2737e41a74809`、post-merge CI run `29625979222`全3 job success。
- merged main automated revalidation — check、build warning 0／error 0、653 tests、sim checksum `36ca153c20b82b2220c82b787c229d22f255fee7c42fed9c5ce7753ae0ff7bf1`、Godot graybox checksum `7692094b4154966821fe7251d4fde59c73fcd16c09c8527579885dade55b9cf6`、loss／win Replay V3 SHA-256 `2f81068272beaa4b6f4f6c2e7b77884f16f24606d35e9c6961da22fe27c304b7`／`6ff9aae41202c49ff136e7962af9b4243889d3e9a036b0b8a74fb4d1a8b6c6be`、Windows executable SHA-256 `4f195894a1cdd5bae7ff3cd0da861194a5104b93ab2e2b76d8d82d67217355db`。
