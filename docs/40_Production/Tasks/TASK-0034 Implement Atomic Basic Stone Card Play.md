---
type: task
id: TASK-0034
status: blocked
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0033]
updated: 2026-07-12
---
# TASK-0034 Implement Atomic Basic Stone Card Play

## Outcome

hand内の`card_basic_stone` instanceを指定して気コスト、target／mode、既存placement legality、zone移動を一つのApplication commandで原子的に解決し、PlayCard vertical proofを作る。

## Source of truth

- [[Rules Canon]]
- [[Combat Resolution Order]]
- [[Deck and Card System]]
- [[Command Event Model]]
- typed Core Duel content catalog

## Non-goals

- 他starter効果、default deck recipe、Momentum、enemy planner、preview、replay schema更新、Godot。

## Allowed areas

- pure Domain card-play seam。
- `src/Igorogue.Application/Battle/` command／state integration。
- Domain／Application／Architecture tests、本TASK／status文書。

## Acceptance criteria

- commandはcard instance ID、Canonical target、明示modeをbindし、hand外／stale／insufficient qi／target不正をexact no-opで拒否する。
- legality確定前にqi、card zone、RNG、board、historyを変更しない。
- accepted時だけqiを消費し、cardをhand → resolving → discardへ移し、既存authorized placement／capture／facility／territory／terminal pipelineを一度だけ通す。
- content ID別数値switchを置かず、typed operationを受ける。
- ordered facts、accepted-only command log、canonical checksumがsame inputで一致する。

## Validation

- `tools/dev/check`、`tools/dev/test`、`tools/dev/build`。
- accepted／rejected／stale／terminal／repetition／facility-point cases。
- independent fixed-HEAD review、CI全job。

## Known issues

TASK-0033 mergeまで`blocked`。starter candidateの残りはTASK-0035／0036。
