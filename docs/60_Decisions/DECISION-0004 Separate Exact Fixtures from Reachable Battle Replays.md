---
type: decision-needed
id: DECISION-0004
status: resolved
blocking: []
updated: 2026-07-11
---
# DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays

## Why work is blocked

[[TASK-0009 Golden Board Fixtures]]はKO-01〜07とFAC-01〜09の完全移植、true Application replay、production変更禁止を同時に要求している。しかし[[TASK-0010 Headless Battle State Machine]]が受け付けるcommandは承認済み石配置、player turn終了、enemy passだけであり、canonical fixtureの全入力をstate transitionとして表せない。

## Conflicting sources

- `game_data/fixtures/board_repetition_fixtures.json`のKO-03は未実装の`stone_kind`、KO-04は未実装の非石state change、KO-07はenemy candidateのsilent filteringを含む。
- `game_data/fixtures/facility_intersection_fixtures.json`のFAC-05はboard snapshotの直接差し替え、FAC-08／09はfacility build operationを含む。
- TASK-0010はspecial stone、非石resource、enemy ranking、facility buildをNon-goalとする。
- TASK-0009のValidationはdirect Domain snapshotをgolden replayと呼ぶことを禁止する。

Accepted ruleの矛盾ではなく、exact spec fixtureと現Applicationで到達可能なcommand replayを同一物として扱ったvalidation計画の矛盾である。

## Options

1. exact fixture assertionとApplication replayを明示的に分ける。全KO／FAC payloadはDomain unit evidenceで維持し、到達可能な遷移はtrue replay、未実装metadataは明示adapter、FAC-05は同じ停止／再稼働意味論の合法command caseで補う。FAC-08／09のために狭い[[TASK-0024 Authorized Facility Build Battle Command]]を先行する。
2. TASK-0009へ任意board mutation、future resource state、enemy AI、facility buildを混在させ、fixtureを一つのproduction command列として強制再生する。
3. 到達不能caseを将来taskへ延期し、TASK-0009のKO-01〜07／FAC-01〜09完全coverageを縮小する。

## Smallest safe default

TASK-0009を`blocked`に保ち、test-only board mutationやdirect Domain commitをtrue replayと呼ばない。Accepted ruleを変えず、欠けている正規Application commandだけを独立taskへ分離する。

[[TASK-0024 Authorized Facility Build Battle Command]]はどのoptionでも必要なFAC-08／09の正規commandだけを先行できる。TASK-0009のacceptance、ADR-0012、`tests/golden/README.md`はowner decisionまで変更しない。

## Owner decision

2026-07-11 — Project ownerは推奨Option 1を次の継続条件として提示された後、PR #12をmergeして「作業を続けて」と指示した。このowner指示をOption 1の選択として記録する。

- KO-01〜07／FAC-01〜09のcanonical JSON payloadと期待値は、全caseのDomain unit evidenceでexactに維持する。
- 現Application commandで到達可能な遷移だけをtrue replayとする。direct Domain snapshot、任意history注入、任意board mutationをreplayと呼ばない。
- KO-03／04はsource metadataを保持してgeneric stone commandへの正規化を明示し、exact metadata／topology assertionをDomain evidenceで併用する。
- KO-07はtest adapterが共有Domain legalityで候補をsilent filterし、選択されたcommandだけをApplicationへsubmitする。除外候補の`CommandRejected`をbattle facts／logへ追加しない。
- FAC-01／02／06／07はexact Domain evidenceに加えてApplication start boundaryを固定し、state transition replayとは呼ばない。
- FAC-05のcanonical direct snapshot sequenceはDomain evidenceに残し、同一instanceの停止→再稼働を合法commandで起こすlinked semantic Application replayを追加する。
- FAC-03／04／08／09はcanonical inputをApplication commandでexact replayする。

[[ADR-0011 Board Repetition Fixtures]]、[[ADR-0012 Facility Intersection Fixtures]]、`tests/golden/README.md`、[[TASK-0009 Golden Board Fixtures]]を同じ変更で同期する。これはvalidation evidenceの分類であり、Rules Canon、fixture payload、player-visible ruleを変更しない。
