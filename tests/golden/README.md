# Golden replay cases

Each case should contain:

- Name and purpose.
- Build/schema version.
- Canonical runtime content hash and pinned source-catalog SHA-256 values.
- Seed.
- Ordered commands.
- Expected turn-boundary checksums.
- Expected terminal result.
- Source fixture and evidence classification.
- Initial and every attempted-command state／log checksum.
- Ordered fact projection for each boundary.

Golden cases protect rules and determinism, not incidental presentation timing.

Rejected attempts belong to the ordered replay script but not the accepted-only `OrderedCommandLog`. Their boundary must prove state checksum、log checksum、RNG、log entry count are unchanged.


## Required ADR-0011 cases

`game_data/fixtures/board_repetition_fixtures.json`のKO-01〜KO-07を[[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]]の分類でgolden suiteへ移植する。
KO-01／02／05はtrue Application replay、KO-03／04はsource metadata正規化を明示したreachable replay、KO-06はtrue command sequenceであると同時に`stone_kind=blood`のgeneric command正規化を明示する。KO-07はsilent candidate-filter adapterと選択command replayである。exact payload／期待値は全件のDomain unit evidenceで維持する。

反復不合法commandを実行するcaseでは、診断用`CommandRejected`以外のbattle factがなく、state／accepted-only logが変わらないことを検査する。KO-07のsilent filterでは`CommandRejected`自体をbattle factへ発行しない。

## Required ADR-0012 cases

`game_data/fixtures/facility_intersection_fixtures.json`のFAC-01〜09を[[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]]の分類でgolden suiteへ移植する。

- FAC-01／02／06／07: Application initial-state evidence。
- FAC-03／04／08／09: canonical Application command replay。
- FAC-05: exact Domain transition evidenceと、同じinstanceを合法commandで停止→再稼働するlinked semantic replay。

direct Domain snapshot、任意history注入、任意board mutationをgolden replayと呼ばない。

## Temporary-liberty v2 catalog

`v2/temporary_liberty_cases.json`はTLE-01〜15を`headless-battle-state-v2`へ接続した
Application goldenである。source fixture、`system.json`、参照content fileのSHA-256、seed、
canonical initial snapshot、順序付きApplication command、各command後のstate／log checksum、
ordered fact、typed enemy-boundary stage trace、terminal resultを固定する。

通常testはcatalogをread-onlyでproduction executionと比較する。意図したschema更新時だけ
`IGOROGUE_UPDATE_TLE_GOLDEN=1`を設定して生成し、必ずJSON diffとsource SHAをreviewする。
TLE-11のseen historyはsource fixtureのexpected payloadではなく、初期入力からDomain provisional
expiryを解決して得たresult boardとsource boardの観測列から構築する。

schema 1 replayはlegacy `headless-battle-state-v1`だけ、schema 2 replayはauthoritative
`headless-battle-state-v2`だけを受理する。accepted／rejectedを問わずsubmitted attemptを保持し、
TASK-0029範囲外のfacility buildもv2では`unsupported_command` exact no-opとしてround tripする。
mandatory expiryで新しい黒領地が生じた場合、fact projectionはsource reasonと
`implicit_momentum_eligible=0`を明示し、facility reassociationより前へ固定する。

本catalogはMomentum event 0、Brilliant event 0を明示し、CTR-01〜25 coverageを主張しない。

## Core Duel replay v3

`v3/core_duel_turn_limit_loss.json`はgenerated `CoreDuelContentCatalog`、seed `39039`、
turn limit `1`をexact-bindし、starter card play → player turn end → Bandit action →
turn-limit lossを`headless-core-duel-state-v1`／replay schema 3で固定する。
`CoreDuelBattleGoldenTests`はproduction serializerの出力とfixture bytesを完全比較し、同じ
scriptを2回実行したreplay bytes、state、ordered facts、accepted-only log checksum、terminalを
比較する。同testはwhite kingをatariにしたauthoritative initial snapshotからstarter terminal
cardでvictoryへ到達し、そのreplayも同じApplication command経路で再生する。

意図したschema 3更新時はproduction `BattleReplaySerializerV3`でfixtureを再生成し、content
hash、全attempt boundary、attempt chain、document checksumを含むJSON全体のdiffをreviewする。
schema 1／2 fixtureはschema 3更新の生成対象に含めない。
