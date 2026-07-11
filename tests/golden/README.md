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
KO-01／02／05／06はtrue Application replay、KO-03／04はsource metadata正規化を明示したreachable replay、KO-07はsilent candidate-filter adapterと選択command replayである。exact payload／期待値は全件のDomain unit evidenceで維持する。

反復不合法commandを実行するcaseでは、診断用`CommandRejected`以外のbattle factがなく、state／accepted-only logが変わらないことを検査する。KO-07のsilent filterでは`CommandRejected`自体をbattle factへ発行しない。

## Required ADR-0012 cases

`game_data/fixtures/facility_intersection_fixtures.json`のFAC-01〜09を[[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]]の分類でgolden suiteへ移植する。

- FAC-01／02／06／07: Application initial-state evidence。
- FAC-03／04／08／09: canonical Application command replay。
- FAC-05: exact Domain transition evidenceと、同じinstanceを合法commandで停止→再稼働するlinked semantic replay。

direct Domain snapshot、任意history注入、任意board mutationをgolden replayと呼ばない。
