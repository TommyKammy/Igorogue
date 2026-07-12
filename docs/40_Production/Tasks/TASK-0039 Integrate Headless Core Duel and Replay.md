---
type: task
id: TASK-0039
status: blocked
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0038]
updated: 2026-07-12
---
# TASK-0039 Integrate Headless Core Duel and Replay

## Outcome

resolved M2 starter recipe、deck／hand／qi、starter card play、Bandit plannerを一つのauthoritative headless battleへ接続し、StartBattleからwin／loss／restartまでversioned replay／goldenで再現する。

## Non-goals

- Godot UI、preview、Momentum／Brilliant／full counterattack、Invader、reward／shop／meta、fun claim。

## Allowed areas

- Application battle／replay。Application → Content project referenceは追加しない。
- Application／Domain／Contentを参照できるheadless host composition rootと限定integration adapter。
- integrationに必要な限定Domain canonical state。
- Application／Content／Architecture tests、新規golden、本TASK／status文書。
- TASK-0038が生成したresolved recipe／Domain definitions。

## Acceptance criteria

- DECISION-0006がresolvedであり、TASK-0038のmachine-readable starting recipe／starter scopeをcontent hashへbindする。
- StartBattleがseed、content snapshot、initial position、deck recipe、Bandit stateをexact-bindする。
- player turn start／PlayCard列／EndTurn／Bandit action／terminal／restartをApplication commandだけで完走する。
- battle startは初回player turn前に通常intentを生成し、enemy turn終了後に次回intentを生成する。player turn中のdisplayed `intent_id`固定とturn-end mandatory override previewをE3 integration testで確認する。
- replayの新state projection／schemaを旧v1／v2と並設し、cross-version／tamper／resource-limitをfail-closedで扱う。
- fixed scripted Core Duelが同一run 2回でbytes、state、facts、log checksum、terminalまで一致する。
- formal simulator／playable claimを行わない。

## Validation

- repository wrappers各2回、headless full-battle golden／replay round trip、v1／v2 regression。
- independent fixed-HEAD review、CI全job。

## Known issues

TASK-0038 mergeまで`blocked`。
