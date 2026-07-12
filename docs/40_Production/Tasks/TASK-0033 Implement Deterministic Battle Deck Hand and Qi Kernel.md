---
type: task
id: TASK-0033
status: blocked
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0032]
updated: 2026-07-12
---
# TASK-0033 Implement Deterministic Battle Deck Hand and Qi Kernel

## Outcome

外部注入されたcard instance multisetを使い、draw pile／hand／resolving／discard／exhaust、gameplay RNG shuffle／reshuffle、turn-start qi／draw、turn-end discardをpure deterministic Domain／Application stateとして実装する。

## Non-goals

- default starting recipe、card effect／target解決、PlayCard command、enemy planner、replay schema変更、Godot。
- DECISION-0006の選択、runtime値のコード直書き。

## Allowed areas

- `src/Igorogue.Domain/`のcard／turn state。
- integrationに必要な限定Application state。
- Domain／Application／Architecture tests、本TASK／status文書。

## Acceptance criteria

- card instance IDとcontent ID、全zone順、重複禁止、zone間排他をimmutable／canonicalに保持する。
- named `gameplay` RNGでbattle-start shuffleとdiscard reshuffleを行い、seed／消費順／no-op時非消費を固定する。
- draw不足時のdiscard reshuffle、空deck、resolving完了、exhaustをstable orderで処理する。使用済みcardは解決後もturn-endまでresolved stateとして保持し、turn endで残りhandとともにdiscardへ移す。
- 既存turn-start pipelineのturn-scoped flag reset → DeferredPlayerChoice → 領地再計算を保持し、その後にbase qi＋territory income＋reserved qi → base draw＋reserved drawの順で適用して予約値をexact resetする。
- base qi／drawはinjected policyを使い、`game_data`値をDomainへ直書きしない。
- state checksumがRNG、全zone、qi、reserved resourcesを含み、same seed／recipe／commandsで一致する。

## Validation

- `tools/dev/check`、`tools/dev/test`、`tools/dev/build`。
- shuffle／reshuffle golden sequence、input reversal、overflow／empty／duplicate negatives。
- independent fixed-HEAD review、CI全job。

## Known issues

TASK-0032 mergeまで`blocked`。starting recipeは注入値でありDECISION-0006を先取りしない。
