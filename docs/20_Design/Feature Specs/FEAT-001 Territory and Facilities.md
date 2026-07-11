---
type: feature-spec
id: FEAT-001
status: accepted
project: Igorogue
updated: 2026-07-10
version: 1.0.2
---
# FEAT-001 Territory and Facilities

## Player promise

領地は盤面上に見える経済基盤であり、施設を置くことで機能を持つ。
施設点は石を置ける空点のままなので、敵に領地を中立化されると停止し、施設点へ石を置かれると破壊される。

プレイヤーはカード確定前に、次を確認できる。

- 領地の所有色、サイズ、基本収入、建設容量
- 施設がactive、disabled、破壊予定のどれになるか
- 石配置が施設を破壊するか
- 領地変形後に停止・再稼働する施設
- 新規建設が容量または同名上限で拒否される理由

## Scope

本仕様は次を定義する。

- 空点領域と領地所有色
- 領地サイズ、基本収入、施設建設容量
- 施設点の空点意味論
- 施設建設の合法性
- active／disabled／destroyedの状態遷移
- 石配置による施設破壊
- 領地分割・結合・中立化時の再関連付け
- 施設イベント順、UI、テレメトリ

施設点の意味論は[[ADR-0012 Facility Sites Are Empty Intersections]]を正本とする。

## Non-goals

- 施設ごとの個別効果の全実装
- 施設売却、移設、所有権移転
- 1交点への複数施設
- 領地縮小時の自動load shedding
- 敵用施設建設
- 施設を石として捕獲する効果

## Terms

### Empty intersection

Stone layerに石がない交点。Facility layerに施設があってもempty intersectionである。

### Territory region

上下左右で連結したempty intersection集合。
隣接石色が黒だけなら黒領地、白だけなら白領地、両色または石なしなら中立。

### Facility instance

```text
instance_id
facility_content_id
owner_color
point
build_sequence
explicit_disable_sources[]
```

施設はpersistentな領地IDを保持しない。現在座標を含む領地へ毎回再関連付けする。

### Installed count

現在領地内に存在する、破壊されていない施設instance数。
activeか明示的disabledかを問わず建設枠を消費する。

### Construction capacity

`game_data/balance/system.json`の領地サイズ帯から得る基本枠に修正効果を加え、全体上限で丸めた、新規建設用の上限。
既存施設の稼働上限ではない。

## Territory calculation

```text
function calculate_territory_regions(board, facilities):
    unvisited = every point where StoneLayer == empty
    regions = []

    while unvisited is not empty:
        start = canonical_min(unvisited)
        points = flood_fill_orthogonal_empty_points(start)
        adjacent_colors = unique stone colors adjacent to points

        if adjacent_colors == {black}:
            owner = black
        else if adjacent_colors == {white}:
            owner = white
        else:
            owner = neutral

        regions.append(
            points = canonical_sorted(points),
            anchor = canonical_min(points),
            owner = owner,
            size = len(points)
        )
        unvisited -= points

    return canonical_sorted_by_anchor(regions)
```

施設はflood fill、隣接色、サイズへ影響しない。
施設点は通常のempty intersectionとして領地サイズへ含める。

## Territory income

領地の基本収入は次である。

```text
basic_income = ceil(region.size / territory_income_divisor)
```

プレイヤー収入へ加えるのは黒領地だけ。
施設修正は、領地再計算と施設稼働状態更新の後に、active施設だけから加える。

## Facility operating state

```text
function derive_facility_state(facility, board, territory_map):
    assert board.stone_at(facility.point) is empty

    if facility.explicit_disable_sources is not empty:
        return disabled(explicit_effect)

    region = territory_map.region_at(facility.point)
    if region.owner == facility.owner_color:
        return active

    return disabled(territory_control_lost)
```

状態変化時だけ次を発行する。

```text
FacilityActivated
FacilityDisabled
```

所有権は変化しない。相手領地になっても相手施設として利用されない。

## Build validation and resolution

```text
function validate_build_facility(state, actor, point, facility_id):
    if stone_at(point) is not empty:
        return illegal(facility_target_has_stone)

    if facility_at(point) exists:
        return illegal(facility_target_occupied)

    region = territory_region_at(point)
    if region.owner != actor.color:
        return illegal(facility_target_not_owned_territory)

    capacity = effective_construction_capacity(region)
    installed = installed_facilities_in(region)
    if len(installed) >= capacity:
        return illegal(facility_capacity_full)

    type_limit = facility_type_limit(facility_id)
    if count(installed where facility_content_id == facility_id) >= type_limit:
        return illegal(facility_type_limit_reached)

    return legal
```

合法時の確定順は次とする。

```text
reserve cost
→ create FacilityInstance with next build_sequence
→ FacilityBuilt
→ FacilityActivated(reason=built_in_controlled_territory)
→ facility/card on-build effects
→ 地合い流ならMomentumChanged(reason=facility_built_by_territory_style)
→ checksum
```

施設建設では領地再計算、`StoneTopologyRegistered`、盤面反復履歴追加を行わない。

## Stone placement on a facility site

施設点は配置前にはempty intersectionとして扱う。

```text
function resolve_stone_placement_on_possible_facility(state, command):
    preview placement, captures, suicide and repetition without removing facility

    if illegal:
        reject command
        leave facility unchanged
        return

    commit StonePlaced
    commit GroupCaptured events in stable order

    if facility_at(command.point) exists:
        remove facility instance
        emit FacilityDestroyed(reason=stone_occupied)

    register StoneTopologyKey
    continue king result, triggers and territory recalculation
```

黒石、白石、自動生成石に同じ規則を適用する。
施設破壊は配置点だけに起き、捕獲で空いた点に施設を自動生成・復元しない。

## Territory change resolution

石トポロジー変化後の領地・施設処理は次とする。

```text
old_states = current facility operating states
new_regions = calculate_territory_regions(board, facilities)
emit territory split/merge/create/neutralize facts in region-anchor order
reassociate every facility by its point
new_states = derive every facility state

for facility in canonical point order, then instance_id:
    if old active and new disabled:
        emit FacilityDisabled
    if old disabled and new active:
        emit FacilityActivated

run active facility triggers in canonical order
```

直接石を置かれた施設は、この再計算より前に破壊済みである。

## Capacity after split or merge

- 容量は建設時だけ検査する。
- 分割または縮小でinstalled countがcapacityを超えても、既存施設は自動停止しない。
- over-capacity領地では新規建設を拒否する。
- 結合時は結合後領地のinstalled countを用いる。
- disabled施設が所有領地へ戻った場合もinstalled countへ戻る。
- `facility_region_over_capacity`をテレメトリへ記録する。

## Per-type limits

現在値は`game_data/balance/system.json`を正本とする。

```text
development: 2 per region
furnace: 2 per region
default: 1 per region
```

明示的disabled施設も同名上限へ数える。

## Momentum integration

- 施設建設は全流儀共通の余勢生成源ではない。
- 地合い流だけが、合法な`FacilityBuilt` commandから余勢1を得る。
- 施設再稼働、停止解除、領地奪還は`FacilityBuilt`ではなく、地合い流の追加sourceを発火しない。
- 石変化により一つ以上の点が黒領地へ変わった場合の普遍sourceは[[FEAT-002 Momentum]]で定義し、施設建設経路とは別に処理する。
- 一つのatomic resolutionで複数regionが成立しても、普遍sourceは最大1。

## Event order

石配置を伴う原子的解決では、施設関連のDomain factは次に置く。

```text
StonePlaced
→ GroupCaptured[]
→ FacilityDestroyed? at placement point
→ StoneTopologyRegistered
→ KingCaptured / battle result
→ capture and destruction triggers
→ territory recalculation and territory facts
→ TerritoryEstablished ownership-delta facts
→ FacilityDisabled / FacilityActivated[]
→ universal territory momentum trigger
→ facility and remaining territory triggers
```

`TerritoryEstablished`は一つのatomic resolutionにつき最大1件で、source actorと、非黒領地から黒領地へ変化した交点をCanonical point orderで保持する。施設状態変化はこのfactの後、将来の普遍領地Momentum sourceは施設状態変化の後に解決する。

施設だけを建設・破壊する効果は石トポロジー履歴を変更しない。

## UI

- 施設は交点を完全に塗りつぶさず、中央に空点カーソルが見える下層glyphとして描画する。
- active施設は通常色、disabled施設は低彩度＋停止アイコン。
- 石カード選択時、施設点も合法候補として表示できる。
- 施設点への石配置プレビューは`施設を破壊`を明示する。
- 不合法配置では破壊予告を表示しない。
- over-capacity領地は建設カード選択中だけ容量警告を表示する。
- ツールチップに「この交点は空点として扱う。石が置かれると施設は破壊される」を表示する。

## Telemetry

最低限、次を記録する。

```text
facility_instance_id
facility_content_id
owner_color
point
build_sequence
old_operating_state
new_operating_state
state_change_reason
territory_anchor_before
territory_anchor_after
territory_size_before
territory_size_after
construction_capacity
installed_count
is_over_capacity
source_command_id
source_actor
```

M3では次を集計する。

- 施設点が実呼吸点として利用された回数
- 自施設を自石で破壊した回数
- 敵に直接踏破された施設数
- 中立化による停止数と奪還再稼働数
- over-capacity領地の発生率と継続ターン
- over-capacityを意図的な領地分割で作った割合

## Edge cases

1. **一目領地の施設**: 施設点は空点かつ実呼吸点である。敵がそこへ置く手が自殺なら、施設があっても不合法。
2. **味方石による踏破**: 黒施設へ黒石を合法配置した場合も施設は破壊される。所有色による例外はない。
3. **不合法踏破**: 自殺手、反復手、配置タグ不一致で拒否された場合、施設は残り、コストも支払わない。
4. **中立化と奪還**: 施設点が中立になれば同じinstanceのまま停止し、黒領地へ戻れば自動再稼働する。
5. **相手領地化**: 黒施設が白領地内になっても白へ転換しない。黒所有のdisabled施設として残る。
6. **領地分割**: 施設は座標で各新領地へ再関連付けする。persistentな旧領地IDを参照しない。
7. **分割後over-capacity**: 既存施設は停止しないが、新規建設は`facility_capacity_full`で拒否される。
8. **領地結合**: 結合領地内の施設数と同名数を合算し、新規建設だけを制限する。
9. **明示的停止中の容量**: 明示的disabled施設も設置済みとして枠と同名上限を消費する。
10. **石捕獲後の跡地**: 施設を踏破した石が後に捕獲されても、破壊された施設は復元しない。
11. **戦闘終了手**: 王石捕獲手が施設点へ置かれた場合、施設破壊は戦闘終了判定より前にDomain factとして確定する。
12. **複数施設状態変化**: 一度に複数施設が停止・再稼働する場合、座標順、instance ID順でイベントを発行する。

## Acceptance criteria

- [[ADR-0012 Facility Sites Are Empty Intersections]]がAcceptedである。
- 施設点が呼吸点、空点領域、領地サイズ、配置候補、`StoneTopologyKey`で空点として扱われる。
- 合法配置だけが施設を破壊し、不合法配置では全状態が不変である。
- 中立化停止、奪還再稼働、相手領地で非移転が一義的である。
- 建設容量が建設時上限であり、分割後over-capacityの挙動が定義される。
- [[ADR-0012 Facility Intersection Fixtures]]の全fixtureが仕様checkerを通る。
- M1で同fixtureを共有Rules Kernelのunit/golden testsへ移植する。

## Deferred decisions

- 敵施設と施設所有権奪取
- 施設売却・移設
- 運用容量と施設優先順位UI
- 施設を石として扱うカード
- 施設破壊時の汎用報酬システム
