---
type: task
id: TASK-0040
status: blocked
project: Igorogue
milestone: M2
priority: high
dependencies: [TASK-0039]
updated: 2026-07-12
---
# TASK-0040 Implement Core Duel Preview Queries

## Outcome

選択cardの合法target／mode、capture、結果呼吸点／アタリ、領地差、王石risk、Bandit intentをauthoritative stateから導出するread-only Application query／presentation projectionとして公開する。

## Non-goals

- state mutation、Godot rendering、Momentum／Brilliant／full counterattack preview、未確定ルールの推測。

## Allowed areas

- pure Domain analysisの再利用に必要な限定query seam。
- Application query／DTO（Godot型禁止）。
- Domain／Application／Architecture tests、本TASK／status文書。

## Acceptance criteria

- previewは実commandと同じlegality／capture／territory／enemy plan kernelを利用し、第二のrule implementationを作らない。
- queryはRNG、command log、state、first-use flagを変更しない。
- CanonicalPoint、stable reason IDs、primary／alternate intentをpresentation-neutralに返す。
- preview結果と同stateからのaccepted command resultが一致し、stale checksumを検出する。

## Validation

- repository wrappers、preview／commit parity、read-only／stale／enumeration-order tests。
- independent fixed-HEAD review、CI全job。

## Known issues

TASK-0039 mergeまで`blocked`。
