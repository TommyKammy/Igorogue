---
type: task
id: TASK-0002
status: done
project: Igorogue
milestone: M0
priority: high
dependencies: [TASK-0001]
updated: 2026-07-11
---
# TASK-0002 Deterministic RNG and Command Log

## Outcome

seed付きRNGと順序付きコマンドログの最小実装。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]

## Non-goals

タスク外のカード、UI、バランス、リファクタリング。

## Acceptance criteria

- 同一seedと入力で同一出力、checksum一致。
- 関連ユニットテスト。
- 決定論を壊さない。
- TASK Evidenceを更新。

## Execution log

2026-07-11 — TASK-0022、TASK-0001、TASK-0020のruntime gate closureを確認し、実装開始可能な`ready`へ遷移。

2026-07-11 — SHA-256 domain separation付き`splitmix64-v1`を実装し、`gameplay`、`reward`、`cosmetic`の用途別streamとimmutable stateを追加。authoritative stateには結果へ影響する`gameplay`と`reward`だけを含め、`cosmetic`を除外した。

2026-07-11 — game version、content hash、initial seed、RNG algorithm、command-log schema、checksum schemeを再生identityとするordered command logを実装。各entryへsequence、command type/schema、canonical payload、確定後state checksum、chain checksumを記録する。

2026-07-11 — 固定互換vector、stream隔離、bounded draw、immutable更新、同一入力再現、異なるseed／順序、invalid input、log checksumのunit testを追加。Accepted ADR、プレイヤー可視ルール、dependency/package、lock file、Godot scene/resourceは変更していない。

2026-07-11 — 独立Codex reviewで最終`APPROVE`、GitHub Actions run `29130984331`の3 job成功を確認。PR #3をmerge commit `d948b86d0514aa3ce88e76693c5a5fe50ec935aa`として人間が`main`へmergeし、`done`へ遷移。

## Evidence

- `tools/dev/check` — exit 0。documentation、wikilink、content、repository bootstrap、handoffを含む全check成功。content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`。
- `tools/dev/test` — exit 0。exact .NET SDK `8.0.422`、locked restore、Release buildを通過し、warning 0／error 0。Domain 14、Application 12、Architecture 5、合計31 testが成功。
- `tools/dev/sim-smoke`を2回実行 — 両方exit 0。同一の`checksum=3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`、同一のcontent hash、`files=7`を確認。
- RNG compatibility vector（seed 42） — gameplay initial state `2c8fe816dabe845d`、先頭3値`0d63df1a615c7127`／`b3d67c1dd9431d2f`／`67254f1d2ab2345e`。reward先頭値`3f04e68406f4a9f6`、cosmetic先頭値`16f8ab1934fa0b8e`。seed -1の符号付きtwo's-complement big-endian契約も固定testで保護。
- Command-log compatibility vector — seed 99のheader checksum `c0a290eb2ba7b0ae92c7140b073a3f667667e5b47aa72e46b4cf0cec75f6a62b`、先頭entry後`aaf2170b88e577d39f4345f1fce8676ee0830036426ec691e706c3c455ed67fd`。
- `tests/Igorogue.Application.Tests/OrderedCommandLogTests.cs` — 同一seed＋同一ordered commandsでoutput、canonical RNG state、state checksum、log checksumが一致。seed差とcommand順序差で期待する結果／log checksumが分岐し、malformed checksum、unknown command schema、null payloadの拒否でlogが不変。
- `tests/Igorogue.Domain.Tests/DeterministicRngTests.cs` — named stream隔離、cosmetic 100 drawがauthoritative state/checksumへ影響しないこと、invalid boundがRNG stateを消費しないことを確認。
- 独立Codex review — コードfindingなし。Project HubのTASK status driftのみLOW follow-upとして検出し、`review／current`へ同期した。
- GitHub PR [#3](https://github.com/TommyKammy/Igorogue/pull/3) — Actions run `29130984331`でgovernance、pure .NET build/test/simulator、Godot headless smoke/Windows exportの3 jobが成功。merge commit `d948b86d0514aa3ce88e76693c5a5fe50ec935aa`。

## Known issues

TASK-0002範囲の既知defectはなし。

本タスクはin-memory RNG／command-log contractまでで、永続化したreplay round tripとgolden replayはTASK-0011、実gameplay commandと合法性判定はTASK-0003〜TASK-0010の範囲。プレイヤー可視変更がないためGodot visual reviewとexportは実施していない。
