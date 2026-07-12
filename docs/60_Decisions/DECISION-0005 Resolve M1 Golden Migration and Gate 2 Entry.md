---
type: decision-needed
id: DECISION-0005
status: resolved
blocking: []
updated: 2026-07-12
---
# DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry

## Why work was blocked

[[TASK-0025 Audit Gate 1 Deterministic Foundation Completion]]で、Gate 1 ordered implementation sequenceはfixed main HEAD上で完了している一方、MOM／CTRのM1 fixture migration要求と後続milestone所属が矛盾していることを確認した。

TASK-0025のfixed baselineでは、当時activeだった[[Golden Replay Index]]とAccepted fixture／Feature Spec sourcesがMOM-01〜19、CTR-01〜25をM1 shared Rules Kernel／golden replayへ移植すると定める一方、[[Codex Task Queue]]とAcceptedな[[Milestones and Exit Gates]]はMomentum／counterattackをGate 3／M3へ置いていた。production code／goldenにMOM／CTR 44 fixturesのruntime implementationもなかった。

TLE-01〜15はこのconflictと別である。Acceptedな[[ADR-0014 End-of-Enemy-Turn Temporary Liberty Expiry Sweep]]はM1 Rules Kernel／golden移植を要求し、後工程へ置くAccepted sourceはない。したがって現状はTLEのM1 implementation gapであり、現Accepted scopeのままならbounded production／E3 migration taskが必要になる。

加えて、[[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]の二人human paper sign-offがpendingであり、Accepted M-1 exit conditionをCodexだけでは閉じられない。

## MOM／CTR conflicting sources at the fixed baseline

- activeな[[Golden Replay Index]] — MOM／CTRをM1で共有Rules Kernel event列とturn-boundary checksumへ移植。
- [[FEAT-002 Momentum Gate Fixtures]] — M1 unit／golden replay移植を要求。
- [[FEAT-003 Counterattack Curve Fixtures]] — M1 shared kernel／golden移植を要求。
- activeな[[Codex Task Queue]] — Momentum／counterattackをGate 3へ配置。
- Acceptedな[[Milestones and Exit Gates]] — M3へ施設、余勢、触媒、反攻、妙手を配置。

[[TASK-0010 Headless Battle State Machine]]のNon-goalはそのTASKの範囲限定であり、MOM／CTR／TLEのmilestone所属を変えるsourceではない。

## Options

1. **Recommended:** MOM／CTRのM1 migration記述をGate 3／M3へ合わせ、Accepted fixture／Feature Spec、Milestones、active index／queueを同じresolutionで同期する。TLEは現Accepted scopeどおりM1に維持し、TLE-09／10／14／15の未実装依存を含むbounded production／E3 migration taskをGate 2前へ追加する。
2. MOM／CTRもM1に維持し、MOM 19／CTR 25／TLE 15の全59 fixturesに必要なproduction Rules Kernel実装、golden replay、formal task evidenceをGate 2前へ追加する。Gate 1／M1をopenに保つ。

どちらのoptionでも、TLEを後工程へ動かす場合はconflict解決ではなく、ADR-0014を含むAccepted scopeの明示変更になる。TASK-0012 human sign-offは別prerequisiteとして維持する。

## Smallest safe default before the owner decision

owner decisionまでMOM／CTR境界を変更せず、TASK-0025を`blocked`、M2 taskをnot-readyに保つ。Gate 1のordered implementation sequenceが技術的に完了した事実は記録するが、MOM／CTRを実装済みまたはM1外と勝手に扱わない。TLEは現Accepted scopeでM1 gapとして扱い、ownerがADR-0014等を明示変更しない限りbounded follow-upとE3 evidenceを必須とする。

`tools/dev/sim-smoke`はbootstrap determinism evidenceだけとし、正式board simulator evidenceへ昇格しない。TASK-0012のhuman sign-offもCodex reviewで代替しない。

## Owner decision

2026-07-12 — Project ownerが「DECISION-0005はOption 1で進めてください」と明示選択した。

## Resolution

- MOM-01〜19／CTR-01〜25のproduction Rules Kernel unit／golden migrationをM3 Acceleration Labへ配置する。仕様checkerはそれまでE1 evidenceのままとする。
- TLE-01〜15はM1 requirementとして維持し、[[TASK-0027 Implement Temporary Liberty Domain Kernel]] → [[TASK-0028 Implement Closed-Window Capture Benefits and TLE Boundary Pressure]] → [[TASK-0029 Integrate Temporary Liberty Enemy Boundary and Golden Replay]]の直列workstreamでE3移植する。
- [[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]]の二人human sign-offはGate 2 entryの別prerequisiteとして維持する。
- [[TASK-0026 Resolve M1 Momentum Counterattack Migration Boundary]]がAccepted／active sourceとstatusを同期する。このDecision解決だけでM1 exitを`PASS`にしない。
