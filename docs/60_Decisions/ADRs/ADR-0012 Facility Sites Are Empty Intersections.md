---
type: adr
id: ADR-0012
status: accepted
project: Igorogue
updated: 2026-07-10
decision_scope: v0.2
---
# ADR-0012 Facility Sites Are Empty Intersections

## Context

Igorogueの施設は領地内の交点へ設置するが、施設が石と同じように交点を占有するのか、呼吸点を塞ぐのか、領地の空点数へ含まれるのかが未確定だった。
この曖昧さを残すと、Rules Kernel、敵の施設踏破、領地収入、捕獲、プレビューが別々の解釈を持つ。

特に次を一義的に決める必要がある。

- 施設点が実呼吸点として数えられるか
- 施設点を空点領域の探索対象に含めるか
- 施設点へ黒石・白石・自動生成石を置けるか
- 不合法な配置試行で施設を失うか
- 領地が中立化または相手領地化した場合の状態
- 領地分割後に施設数が現在容量を超えた場合の扱い
- 施設変化が盤面反復履歴へ影響するか

## Decision

v0.2では、施設を**石レイヤーとは独立した、空点上の設置マーカー**として扱う。
プレイヤー向けには「施設点も空点」と説明し、内部では`facility_site_is_empty_intersection`を不変条件名とする。

### 1. 二層構造

交点は次の二層を持つ。

```text
Stone layer: empty / black / white / black_king / white_king
Facility layer: none / FacilityInstance
```

- 1交点に設置済み施設は最大1個。
- 施設を設置できるのはStone layerが`empty`の点だけ。
- 合法な確定状態では、石と施設は同じ交点に共存しない。
- 施設instanceは安定ID、施設content ID、所有色、座標、戦闘内建設順、明示的停止状態を持つ。

### 2. 空点としての扱い

施設が存在しても、その交点は次の計算で空点として扱う。

- 石配置の占有判定
- 実呼吸点集合
- 空点領域のflood fill
- 領地サイズ
- 領地基本収入
- `frontline`、`contact`、`terminal`等の配置タグ
- [[ADR-0011 Battle-Local Stone Topology Repetition Ban]]の`StoneTopologyKey`

したがって施設は、石グループを連結・切断せず、呼吸点を増減させず、領地を分割しない。
施設点は領地サイズの1空点として通常通り数える。

### 3. 建設合法性

施設建設は、コスト支払い前に次をすべて満たす必要がある。

1. 対象点のStone layerが空。
2. 対象点に設置済み施設がない。
3. 対象点が建設者と同色の現在領地に含まれる。
4. 対象領地内の設置済み施設数が現在の建設容量未満。
5. 対象領地内の同名施設数が施設種別上限未満。
6. カードまたは効果固有の対象条件を満たす。

合法な建設では、同一コマンド内で次を行う。

```text
FacilityBuilt
→ FacilityActivated(reason=built_in_controlled_territory)
→ on-build効果
→ 余勢等の後続トリガー
```

施設建設だけでは石トポロジー、領地形状、盤面反復履歴を変更しない。

### 4. 稼働状態

施設の設置状態と稼働状態を分ける。

```text
installed + active
installed + disabled
removed / destroyed
```

設置済み施設は、領地再計算後に次を満たす場合だけactiveとなる。

- 施設点が施設所有色の領地に含まれる
- 明示的な停止効果を受けていない

施設点が中立または相手領地になった場合、施設は同じinstanceのまま残り、
`FacilityDisabled(reason=territory_control_lost)`となる。
施設所有色の領地へ戻れば、自動的に
`FacilityActivated(reason=territory_control_restored)`となる。

- 所有権は相手へ移転しない。
- 中立化や相手領地化だけでは破壊しない。
- disabled施設の常時効果とトリガーは発生しない。
- 明示的停止施設も建設枠と同名上限を消費する。

### 5. 石配置による破壊

施設点への石配置は、施設がない空点への配置と同じ方法で合法性を判定する。
施設は仮配置、自殺手、捕獲、盤面反復の結果へ影響しない。

配置が不合法なら、施設、気、カード、履歴は一切変化しない。

配置が合法なら、石配置と必須捕獲を確定した後、配置点の施設を必ず破壊する。

```text
StonePlaced
→ GroupCaptured（0件以上、安定順）
→ FacilityDestroyed(reason=stone_occupied)
→ StoneTopologyRegistered
```

- 黒石が黒施設へ置かれた場合も破壊する。
- 白石、敵の遠隔侵入、自動生成石も同じ規則を使う。
- 破壊された施設は、その石が後で捕獲されても復元しない。
- 施設自身の破壊時トリガーは`FacilityDestroyed`後の通常トリガー順で処理する。

### 6. 領地変形と建設容量

v0.2の施設容量は**建設時の上限**とする。
領地の分割・縮小・結合によって、既存施設数が現在容量を超えても、それだけを理由に施設を停止・破壊しない。

- over-capacity領地では既存施設は、所有領地内である限り稼働を続ける。
- 施設数が現在容量未満になるまで、新規施設を建設できない。
- 領地結合時は、結合後領地内の設置済み施設を合算して新規建設を判定する。
- 領地分割時は、各施設を座標によって新しい領地へ再関連付けする。
- over-capacityの発生をテレメトリへ記録し、M3で意図的分割による悪用を検証する。

運用容量として新旧施設を自動停止する方式は、優先順位選択と連鎖停止を追加するためv0.2では採用しない。

### 7. 決定論的順序

一度の領地再計算で複数施設の状態が変わる場合、イベントは次で並べる。

1. Canonical point order `(y昇順, x昇順)`
2. facility instance ID昇順

施設状態は石配置合法性へ影響しないため、施設状態イベントの順序で石結果が変化してはならない。

## Consequences

### Positive

- 呼吸点、領地、配置タグが施設の有無で分岐せず、Rules Kernelを単純に保てる。
- 施設が盤面内部に見えながら、空点として侵入・踏破されるリスクが明確になる。
- 中立化では停止、直接踏破では破壊という二段階の防御リスクを作れる。
- 施設を変えて盤面反復禁止を回避できない。
- 不合法配置で施設が失われないため、プレビューと実行が一致する。

### Negative

- 施設がある見た目でも呼吸点として数えるため、UIで「置ける空点」であることを明示する必要がある。
- 大領地で建設後に意図的に領地を分割すると、建設容量を超えた施設密度を維持できる可能性がある。
- 敵施設を占領して利用する表現はできず、破壊または停止に限定される。
- 施設点が一目の眼にある場合、通常の自殺手規則により敵が直接踏破できないことがある。

## Alternatives considered

### Facility occupies the intersection like a stone

拒否。施設が呼吸点、連結、捕獲へ参加し、施設カードが実質的な石札になる。領地内装という役割を失い、囲碁ルールと施設効果の境界が複雑になる。

### Facility blocks placement but not liberties

拒否。同じ点を「呼吸点では空、配置では占有」と扱い、プレイヤー予測と合法性関数を分岐させる。

### Facility is destroyed whenever territory control is lost

拒否。侵入1手で蓄積したエンジンを恒久消失させ、奪還判断より即時防御を強制する。v0.2では停止と直接踏破を分ける。

### Dynamic operating capacity with automatic load shedding

延期。領地縮小時にどの施設を止めるかという優先順位UIが必要になり、M3の認知負荷を増やす。意図的分割が支配戦略になる証拠が得られた場合に後継ADRで検討する。

## Validation

- [[ADR-0012 Facility Intersection Fixtures]]を正本fixtureとする。
- `game_data/fixtures/facility_intersection_fixtures.json`を`tools/check_facility_semantics.py`で検査する。
- M1の共有Rules Kernelは同fixtureをユニットテストとgolden replayへ移植する。

## Supersession policy

施設占領、施設を石として扱う効果、運用容量による自動停止、1交点複数施設を導入する変更は後継ADRを必要とする。
