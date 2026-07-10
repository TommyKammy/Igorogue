---
type: adr
id: ADR-0011
status: accepted
project: Igorogue
updated: 2026-07-10
decision_scope: v0.2
---
# ADR-0011 Battle-Local Stone Topology Repetition Ban

## Context

Igorogueでは、プレイヤーが1ターンに複数の石札を使い、敵も反攻時には同一敵ターンに最大2回配置する。
そのため、交互に1手ずつ打つ古典囲碁の単純コウだけでは、次の問題を一義的に処理できない。

- 同一ターン中の別配置を挟んだ取り返し
- 複数ターンにまたがる石配置の循環
- 捕獲、魂、ドロー、妙手倍率等を同一局面から反復獲得する行為
- 特殊石の種類や非石リソースの変化だけで反復禁止を回避する行為
- 敵の第1候補が反復手だった場合の決定論的な候補除外

v0.2ではコウ材、コウ権利、三コウ等をゲームシステムとして再現しない。一方、Rules Kernel、敵AI、golden fixtureが共有できる、短く予測可能な反復禁止規則はM1開始前に必要である。

## Decision

v0.2では、プレイヤー向け名称を**盤面反復禁止**、内部名称を`battle_local_stone_topology_superko`とする、戦闘単位の石配置反復禁止を採用する。

### 1. StoneTopologyKey

各安定盤面について、[[Coordinate System and Initial Position]]のCanonical point order `(y昇順, x昇順)` で49交点を並べ、各交点を次のいずれかとして符号化する。盤面図の上段`y=7`からの行順をそのままキーにしてはならない。

```text
empty
black
white
black_king
white_king
```

`StoneTopologyKey`には、次を**含めない**。

- 基本石、囮石、血石、種石、忍び石等の石種
- 石instance ID、出典カード
- 仮呼吸点、成長待ち時間、その他一時状態
- 施設、領地、余勢、気、魂、手札、山札、捨て札
- 妙手倍率、コミ、厚み、反攻、熱
- 敵意図、ターン番号、行動者

したがって、特殊石へ置き換えただけで石色と王石位置が同じなら、同一の`StoneTopologyKey`とみなす。

### 2. 履歴の登録

- 戦闘初期配置を最初の`StoneTopologyKey`として登録する。
- 石を追加する原子的効果は、配置とそれに伴う同時捕獲を仮解決した後のキーを算出する。
- 合法な候補配置が確定したとき、そのキーを順序付き観測列へ追加する。
- [[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]のmandatory capture等、拒否不能な石除去も確定後のキーを観測列へ追加する。mandatory mutationは既出キーへ戻っても実行し、観測列の重複を許す。
- `SeenStoneTopologyKeysCache`は一意集合であり、候補配置の合法性だけに使用する。
- プレビュー、仮実行、不合法手、非石効果、パス、石変化のないターン終了では履歴を追加しない。
- 履歴は戦闘開始時に初期化し、戦闘終了時に破棄する。

### 3. 合法性

配置と必須捕獲を仮解決した結果の`StoneTopologyKey`が、現在の戦闘履歴に1度でも存在する場合、その石変化は不合法である。

```text
candidate_key in repetition_history
    => illegal: stone_topology_repetition
```

この判定は、配置タグ、占有、自殺手の判定後、コスト支払いと実際の状態変更より前に行う。

- プレイヤー操作: 対象点を選べず、気、カード、トリガー、捕獲報酬は変化しない。
- 敵行動: 候補一覧から除外し、[[FEAT-009 Enemy Action Planning and Placement]]の次順位候補へ進む。
- 自動石生成: 当該生成だけを抑止する。再予約、別点選択、消費の扱いは生成元のFeature Specへ従う。
- 王石捕獲手も例外ではなく、反復手なら不合法である。

### 4. 原子的石変化

v0.2の即時石配置は1原子的効果につき1石とする。配置による複数相手グループの同時捕獲は、同じ原子的石変化に含める。

将来、1効果で複数石を同時生成または直接除去する場合は、途中状態を履歴へ登録するか、最終状態だけを登録するかを当該Feature Specと後継ADRで決定する。v0.2では例外を作らない。

### 5. UI

反復禁止となる交点は、合法点表示から除外したうえで、カード選択中は循環矢印に斜線を重ねたアイコンを表示できる。

ツールチップは次の意味を伝える。

> この手は、戦闘中に一度現れた石色配置へ戻るため打てない。

過去の何手目と一致したかはデバッグUIでは表示してよいが、通常UIの必須情報にはしない。

## Consequences

### Positive

- 同一石配置を使った捕獲、魂、ドロー、妙手倍率の無限・循環獲得を排除できる。
- プレイヤー、敵AI、自動生成、リプレイが同じ合法性関数を使える。
- 非石状態を変えるだけでは反復禁止を解除できない。
- 別地点への石配置で新しいトポロジーを作った後の取り返しは可能だが、その後に過去トポロジーへ戻る手は再び禁止される。
- RNGを使用せず、履歴と候補結果だけで決定できる。

### Negative

- 古典囲碁の単純コウより強い制約であり、コウ材の完全再現ではない。
- 特殊石の種類が異なっても石色配置が同じなら禁止されるため、カード効果上は別局面でも配置できない場合がある。
- 中断セーブとDomain checksumは反復履歴を保持またはコマンドログから再構築する必要がある。
- 将来コウ特化ビルドを導入する際は、本ADRをsupersededにするか、明示的な限定例外が必要になる。

## Alternatives considered

### No repetition rule

拒否。捕獲報酬を伴う循環と、敵・プレイヤー双方の無限取り返しを許す。

### Classical immediate ko lock only

拒否。複数カードのプレイヤーターンと反攻2行動により、無関係な1配置を挟むだけで同一トポロジー循環へ戻れる。長周期の反復も防げない。

### Compare only with the previous turn-start position

拒否。ターン中に複数回石が変化するため、ターン途中の局面を使った循環と捕獲報酬反復が残る。

### Full Domain State superko

拒否。手札、気、タイマー等が変わるたび別状態となり反復防止を回避できる一方、何が一致条件かをプレイヤーが理解しにくい。

## Validation

- [[ADR-0011 Board Repetition Fixtures]]を正本fixtureとする。
- `game_data/fixtures/board_repetition_fixtures.json`を`tools/check_board_repetition.py`で検査する。
- M1のRules Kernelは同じfixtureをユニットテストおよびgolden replayへ移植する。

## Supersession policy

コウ権利、コウ材、反復を利用するカード・遺物、複数石同時生成を導入する変更は、後継ADRを必要とする。
