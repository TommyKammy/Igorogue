---
type: decision-needed
id: DECISION-0001
status: resolved
blocking: []
updated: 2026-07-11
---
# DECISION-0001 Insert Facility Runtime Task Before Golden Fixtures

## Why work is blocked

[[TASK-0009 Golden Board Fixtures]]はFAC-01〜09のunit test／golden replay完全移植を要求するが、`FacilityInstance`、建設、踏破破壊、稼働状態、容量、facility factを実装するproduction taskが直列queueに存在しなかった。

## Conflicting sources

Rules CanonまたはAccepted ADRの競合ではない。[[FEAT-001 Territory and Facilities]]と[[ADR-0012 Facility Sites Are Empty Intersections]]が要求するproduction依存と、TASK queueの間に欠落があった。

## Options

1. TASK-0008とTASK-0009の間へ専用facility runtime taskを挿入し、TASK-0009のacceptanceを維持する。
2. TASK-0009をstone-layer projectionと後続facility runtime goldenへ分割・再配置する。

## Smallest safe default

TASK-0009を`blocked`に保ち、golden fixture taskへ未計画のruntime実装を混ぜない。

## Resolution

2026-07-11 — Project ownerの継続実装権限の下で、Codexがsmallest safe operational defaultとしてOption 1を採用した。[[TASK-0023 Implement Facility Runtime Semantics]]をTASK-0008とTASK-0009の間へ挿入し、TASK-0009のacceptanceは変更せず、TASK-0023の独立review、green CI、人間mergeまで`blocked`を維持する。これはownerによるplayer-visible design choiceの代行ではなくproduction sequencingだけを閉じる運用判断であり、Accepted rule／ADRを変更しない。
