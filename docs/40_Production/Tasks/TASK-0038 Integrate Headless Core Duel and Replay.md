---
type: task
id: TASK-0038
status: blocked
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0037]
updated: 2026-07-12
---
# TASK-0038 Integrate Headless Core Duel and Replay

## Outcome

resolved M2 starter recipe、deck／hand／qi、starter card play、Bandit plannerを一つのauthoritative headless battleへ接続し、StartBattleからwin／loss／restartまでversioned replay／goldenで再現する。

## Non-goals

- Godot UI、preview、Momentum／Brilliant／full counterattack、Invader、reward／shop／meta、fun claim。

## Allowed areas

- Application battle／replay／content composition root。
- integrationに必要な限定Domain canonical state。
- Application／Architecture tests、新規golden、本TASK／status文書。
- DECISION-0006 resolutionに明示されたdocs／`game_data` recipe。

## Acceptance criteria

- DECISION-0006がresolvedであり、machine-readable starting recipeをcontent hashへbindする。
- StartBattleがseed、content snapshot、initial position、deck recipe、Bandit stateをexact-bindする。
- player turn start／PlayCard列／EndTurn／Bandit action／terminal／restartをApplication commandだけで完走する。
- replayの新state projection／schemaを旧v1／v2と並設し、cross-version／tamper／resource-limitをfail-closedで扱う。
- fixed scripted Core Duelが同一run 2回でbytes、state、facts、log checksum、terminalまで一致する。
- formal simulator／playable claimを行わない。

## Validation

- repository wrappers各2回、headless full-battle golden／replay round trip、v1／v2 regression。
- independent fixed-HEAD review、CI全job。

## Known issues

TASK-0037 mergeとDECISION-0006 resolutionまで`blocked`。
