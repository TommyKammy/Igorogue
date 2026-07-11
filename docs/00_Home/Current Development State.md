---
type: status
status: active
project: Igorogue
updated: 2026-07-11
---
# Current Development State

## Executive state

| Area | State | Evidence |
|---|---|---|
| Core concept | promising, not play-validated | design review only |
| Player-visible rules | major M-1 repairs accepted | specifications + deterministic fixtures |
| Enemy intent | 山賊棋士／侵入者 specified | FEAT-009 + fixtures; human two-person sign-off pending |
| Engine architecture | accepted | ADR-0001 |
| Repository bootstrap | complete | TASK-0022 runtime evidence + CI |
| .NET build/test | proven on macOS and CI | locked restore + xUnit |
| Godot headless/export | proven on macOS and CI | smoke + managed Windows export |
| Product Rules Kernel | facility runtime merged; headless battle state machine ready | TASK-0002 through TASK-0023 merged; TASK-0010 DoR |
| Formal board simulation | not implemented | M1 onward |
| Abstract proxy | reproducible but not valid product evidence | E2 only |
| Human fun validation | not started | M3 required |

## Accepted design repairs

- FEAT-009 enemy planning and deterministic placement
- battle-local stone-topology repetition ban
- facility sites as empty-intersection markers
- canonical 1–7 coordinate contract and point-symmetric start
- global Momentum gate with territory-style extra source
- baseline and burst-driven counterattack curve
- temporary-liberty expiry and simultaneous capture sweep
- Godot/.NET repository boundary

## Current gate

[[TASK-0023 Implement Facility Runtime Semantics]] was merged through PR #10 and its post-merge main CI is green. The project owner selected the FEAT-002 event order in [[DECISION-0002 Resolve Territory and Facility Event Order]], and [[DECISION-0003 Sequence Golden Replay After Battle State Machine]] selected TASK-0010→TASK-0009→TASK-0011. [[TASK-0010 Headless Battle State Machine]] is the current ready task.

## Next development sequence

1. TASK-0002 deterministic RNG and command log — done
2. TASK-0003 coordinates and neighbours — done
3. TASK-0004 groups and liberties — done
4. TASK-0005 placement and capture — done
5. TASK-0006 legality and terminal capture — done
6. TASK-0007 king result — done
7. TASK-0008 territory — done
8. TASK-0023 facility runtime — done
9. TASK-0010 state machine — ready／current
10. TASK-0009 golden fixtures — blocked／TASK-0010
11. TASK-0011 replay round trip — blocked／TASK-0010 and TASK-0009

## Open human-only item

[[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]] remains in review until two independent humans solve the decision fixtures without reading expected outputs and agree on the same intents and placements.

Codex review cannot be represented as two-human sign-off.

## Evidence classes

| Level | Meaning |
|---|---|
| E0 | design hypothesis |
| E1 | paper calculation or deterministic spec fixture |
| E2 | abstract proxy model |
| E3 | shared Rules Kernel simulation/test |
| E4 | internal human playtest |
| E5 | external playtest |

Balance acceptance requires E3 or above. Fun claims require E4 or above. Product continuation gates should use E5 where feasible.

## Prohibited shortcuts

- Do not expand cards/relics to hide an unproven core duel.
- Do not tune from proxy win rates as if they were real board outcomes.
- Do not start M3 meta progression before Core Duel and Engine Spark gates.
- Do not modify toolchain pins to make local setup convenient.
