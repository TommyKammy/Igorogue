---
type: technical-spec
status: proposed
project: Igorogue
updated: 2026-07-10
---
# Save Schema

## Meta Save

- schema_version
- unlocked_content_ids
- kifu_fragments
- style_dan_records
- discoveries
- settings

## Run Save

- point_encoding: `canonical_1_based_xy`（[[Coordinate System and Initial Position]]）
- game_version
- content_hash
- seed
- run state
- current battle state
- installed facility instances（ID、content ID、owner、point、build sequence、明示的停止源）
- next facility build sequence
- ordered stone topology observation history（mandatory mutationでは重複keyを含み得る）
- temporary liberty effects（ID、amount、anchor stone instance、source、created sequence、expiry enemy-turn index）
- TurnReservedDraw、TurnReservedQi、DeferredPlayerChoices
- counterattack state（gauge units、Pending、planned intent、heat/overextension turn flags、sacrifice remainder）
- command log offset

## 移行

セーブ移行はバージョン単位の純粋変換とし、失敗時は元セーブを保持する。進行中ランの互換性がない場合、明示して補償する。

施設のactive／territory-control停止状態はロード後に現在盤面から再導出する。保存値がある場合も検証用cacheであり正本にしない。

反攻状態は進行中戦闘の再開に必要なためRun Saveへ保存するが、戦闘終了時に破棄し次戦闘へ持ち越さない。
