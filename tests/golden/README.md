# Golden replay cases

Each case should contain:

- Name and purpose.
- Build/schema version.
- Seed.
- Ordered commands.
- Expected turn-boundary checksums.
- Expected terminal result.

Golden cases protect rules and determinism, not incidental presentation timing.


## Required ADR-0011 cases

`game_data/fixtures/board_repetition_fixtures.json`のKO-01〜KO-07を、Rules Kernel実装後にgolden replayへ移植する。
反復不合法ケースでは、診断用`CommandRejected`以外のDomain State変更イベントがないことを検査する。
