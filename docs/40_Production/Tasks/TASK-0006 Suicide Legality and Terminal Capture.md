---
type: task
id: TASK-0006
status: in_progress
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001]
updated: 2026-07-11
---
# TASK-0006 Suicide Legality and Terminal Capture

## Outcome

自殺手禁止とカード指定終着例外。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]
- [[ADR-0011 Battle-Local Stone Topology Repetition Ban]]
- [[ADR-0011 Board Repetition Fixtures]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- 捕獲で呼吸点が生まれる手は合法。
- 履歴済み`StoneTopologyKey`を再現する配置は不合法。
- 反復不合法手でコスト、盤面、カード、トリガー、履歴が変わらない。
- KO-01〜KO-07を共有Rules Kernelへ移植する。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

2026-07-11 — TASK-0005の独立review、green CI、PR #6の人間mergeを確認し、直列Gate 1の次タスクとして`ready`へ遷移。

2026-07-11 — Rules Canon、Combat Resolution Order、ADR-0011、KO-01〜KO-07、FEAT-009、後続TASK-0007との境界を照合。`terminal`は明示許可された即時相手group捕獲への代替配置modeとして最小表現し、自殺手・反復をbypassさせない。王石結果、trigger、Applicationのcost/card mutation、敵AI計画、全配置タグengineを先取りせず、共有Rules Kernelの合法性と戦闘内履歴へ着手。

2026-07-11 — Canonical point orderの49セルを`. / B / W / K / Q`で完全一致比較するversioned `StoneTopologyKey`を実装。石色と王石だけを読み、stone instance、特殊石kind、施設、資源、一時状態等を入力境界から除外した。

2026-07-11 — 初期盤面をindex 0へ登録し、順序付き観測列と非列挙Seen cacheを保持するimmutable `BattleRepetitionHistory`を実装。合法配置の未出現key登録は新しいhistoryを返し、sourceを変更しない。restore経路では将来のmandatory mutationに必要な重複観測を保持する。

2026-07-11 — occupied gateとTASK-0005の仮配置・同時capture後に、明示的`Normal`／`TerminalCapture` access、capture後exact effective-liberty snapshotによる自殺手、全戦闘履歴による反復の順で判定する`PlacementLegalityEvaluator`を実装。terminal grantだけでは合法化せず、即時相手group captureをDomainで確認し、自殺手・反復・王石capture例外を作らない。previewと履歴登録を分離し、illegal評価はcommit可能board、facts、履歴を返さない。

2026-07-11 — `game_data/fixtures/board_repetition_fixtures.json`をtest-only loaderで共有Rules Kernelへ移植。KO-01〜KO-06の仮結果とcommit seam、KO-03／06のstone kind除外、KO-04の非石state除外、KO-05の未出現topology、KO-06の全履歴照合、KO-07の同一history上でのsilent候補除外を検証した。

2026-07-11 — precommit API reviewでKO-01〜KO-07のfixture-driven移植不足をHIGH findingとして検出。canonical JSONからの10 fixture testを追加して解消し、API再reviewとterminal／scope reviewはいずれも残存findingなしで`APPROVE`。

2026-07-11 — package、project reference、lock、Application、Content、game_data、Accepted仕様、Godot assetは変更していない。

## Evidence

- `tools/dev/check` — exit 0。documentation、wikilink、content、KO-01〜KO-07を含む既存fixture、governance checkが成功。content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`。
- `tools/dev/test` — exit 0。exact .NET SDK `8.0.422`、locked restore、Release build、warning 0／error 0。Domain 108、Application 12、Architecture 5、合計125 testが成功。
- `tools/dev/sim-smoke`を2回実行 — 両方exit 0。同一の`checksum=3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`、同一content hash、`files=7`を確認。
- `tests/Igorogue.Domain.Tests/BoardRepetitionFixtureTests.cs` — canonical JSONのKO-01〜KO-07 inventory、仮capture結果、特殊石／非石state除外、全履歴反復、silent候補filter、合法commitとillegal非変更を10 testで確認。
- `tests/Igorogue.Domain.Tests/PlacementLegalityEvaluatorTests.cs` — captureで生じる呼吸点、real 0／effective 1、real正／effective 0、terminal grant条件、terminal後suicide、王石capture反復、suicide優先順、occupied／foreign snapshot／history／mode境界を確認。
- 読み取り専用API reviewとterminal／scope review — 初回HIGH findingを修正後、双方とも残るP1／P2なしで`APPROVE`。review側でもgovernance、125/125 test、warning 0／error 0を確認。

## Known issues

TASK-0006範囲の既知defectはなし。

`PlacementAccessMode.Normal`／`TerminalCapture`は、将来の共有Domain tag／intent policyが既に許可したmodeとして渡す契約である。Application、UI、Botがaccessを独自承認してはならない。frontline、contact、jump、invasion等の完全な配置tag engine、cost／card-zone mutation、正式なDomain Event publish、敵の計画・順位付けは対応する後続taskへ延期する。

timed／continuous stateからexact `EffectiveLibertySnapshot`を生成するcalculatorは、そのruntime effect導入taskへ延期する。mandatory失効captureの重複topology観測、王石勝敗、golden replayはそれぞれ後続の仕様task／TASK-0007／TASK-0009で統合する。
