---
type: spec-fixture
id: ADR-0012-FIXTURES
status: accepted
project: Igorogue
updated: 2026-07-11
source_decision: ADR-0012
---
# ADR-0012 Facility Intersection Fixtures

座標契約は[[Coordinate System and Initial Position]]を参照する。

[[ADR-0012 Facility Sites Are Empty Intersections]]と[[FEAT-001 Territory and Facilities]]の意味論を、製品Rules Kernel実装前に固定する仕様fixtureである。
M1ではcanonical payloadと期待値を共有Rules Kernelのunit testへexactに移植し、Application golden evidenceは下記の到達可能性分類に従う。

## Legend

盤面行は上から`y=7`、下が`y=1`。列は左から`x=1..7`。

```text
. empty stone layer
B black stone
W white stone
K black king
Q white king
```

施設は盤面文字へ埋め込まず、別の`facilities`配列で座標を指定する。
施設がある`.`も石ルールでは空点である。

## FAC-01 — 施設点は実呼吸点かつ黒領地

```text
7 .......
6 .......
5 .......
4 .......
3 .B.....
2 B.FB...
1 .B.....
  1234567
```

`F=(2,2)`は黒施設だがStone layerは`.`。

期待:

- `(1,2)`の黒単石の実呼吸点に`(2,2)`を含む。
- `(2,2)`の空点領域は黒領地、サイズ1。
- 施設はactive。

## FAC-02 — 施設は空点領域を分割しない

左下の黒領地は3空点で、`F=(1,1)`を含む。

```text
3 B......
2 .B.....
1 F.B....
  1234567
```

期待:

- 領地サイズ3。
- 基本収入1。
- 建設容量1。
- 施設を除外した別領域へ分割しない。

## FAC-03 — 合法な味方石配置で施設を破壊

FAC-01と同じ一目領地へ黒石を置く。

期待:

- 配置は合法。
- `StonePlaced`後に`FacilityDestroyed(reason=stone_occupied)`。
- 施設instanceは除去される。
- 破壊された施設は、石が将来捕獲されても自動復元しない。

## FAC-04 — 不合法な踏破では施設を失わない

FAC-01と同じ点へ白石を置こうとする。
周囲の黒石を捕獲できず、白石に呼吸点がない。

期待:

- `suicide`で不合法。
- 施設はactiveのまま。
- 施設破壊、石配置、履歴追加イベントなし。

## FAC-05 — 中立化停止と奪還再稼働

1. FAC-01の黒領地で施設active。
2. `(3,2)`側が白になり、施設点の空点領域が黒白両方へ接して中立化。
3. 黒囲いへ戻る。

期待:

```text
FacilityDisabled(reason=territory_control_lost)
FacilityActivated(reason=territory_control_restored)
```

instance ID、所有色、建設順は変化しない。

## FAC-06 — 相手領地でも所有権は移らない

```text
3 .W.....
2 W.FW...
1 .W.....
  1234567
```

`F`は黒所有施設。

期待:

- 空点領域の所有色は白。
- 施設は`disabled(territory_control_lost)`。
- 施設所有色は黒のまま。

## FAC-07 — 分割後over-capacityでも既存施設は停止しない

建設時には12目の黒領地内に2施設があった。
黒石の壁で分割後、2施設はサイズ3・建設容量1の右側領地へ入る。

```text
4 BBBBB..
3 ..BFB..
2 ..BFB..
1 ..B.B..
  1234567
```

期待:

- 右側黒領地サイズ3、容量1、installed count 2。
- `is_over_capacity=true`。
- 2施設はactiveのまま。

## FAC-08 — over-capacity領地では新規建設を拒否

FAC-07の右側領地の空き点`(4,3)`へ3個目を建設しようとする。

期待:

- `facility_capacity_full`で不合法。
- コスト、施設、余勢、履歴は変化しない。

## FAC-09 — 合法建設は石トポロジーを変えない

FAC-02の3目黒領地で、施設のない`(1,1)`へ開拓施設を建設する。

期待:

```text
FacilityBuilt
FacilityActivated(reason=built_in_controlled_territory)
```

- 建設前後の`StoneTopologyKey`は同一。
- 領地サイズ3、実呼吸点、基本収入は変化しない。
- 施設はactive。

## Machine-readable source

- `game_data/fixtures/facility_intersection_fixtures.json`
- `tools/check_facility_semantics.py`

## Golden evidence mapping

[[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]]に従い、FAC-01〜09のcanonical JSON payload／期待値は全件をDomain unit evidenceでexactに維持する。Application golden suiteは次に分ける。

- FAC-01／02／06／07はcanonical initial board／facilityを`HeadlessBattleStateMachine.Start`へ渡し、initial state checksumとterminal tripleを固定するinitial-state evidence。state transition replayとは呼ばない。
- FAC-03／04はcanonical stone placement attemptを正規Application commandでexact replayする。FAC-04のrejected commandはstate／log exact no-opとする。
- FAC-08／09は[[TASK-0024 Authorized Facility Build Battle Command]]でcanonical build attemptをexact replayする。
- FAC-05のcanonical `next_boards` sequenceはDomain unit evidenceに残す。Applicationでは同じ`facility_05` instanceが合法石commandにより`FacilityDisabled`後に`FacilityActivated`へ戻るlinked semantic true replayを追加する。

FAC-05のために任意board mutation commandを導入せず、direct Domain transitionをgolden replayと呼ばない。
