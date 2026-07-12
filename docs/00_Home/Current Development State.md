---
type: status
status: active
project: Igorogue
updated: 2026-07-12
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
| Product Rules Kernel | M1 technical exit `NOT PASSED`; TLE evidence awaits human merge／re-audit | TASK-0027／0028 done; TASK-0029 in review |
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

[[TASK-0028 Implement Closed-Window Capture Benefits and TLE Boundary Pressure]] was merged through PR #18 at `ddccd57db12219847646d0b2de85c18b2c94b120`; post-merge main CI run `29187053532` is green. Project owner selected [[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]] Option 1: MOM／CTR production migration belongs to M3, while TLE-01〜15 remains in M1. [[TASK-0029 Integrate Temporary Liberty Enemy Boundary and Golden Replay]] implemented the remaining Application boundary／versioned golden replay evidence and is in review pending human merge. M1 exit remains open until merge and re-audit; Gate 2 entry also requires the TASK-0012 human sign-off.

## Next development sequence

1. TASK-0002 deterministic RNG and command log — done
2. TASK-0003 coordinates and neighbours — done
3. TASK-0004 groups and liberties — done
4. TASK-0005 placement and capture — done
5. TASK-0006 legality and terminal capture — done
6. TASK-0007 king result — done
7. TASK-0008 territory — done
8. TASK-0023 facility runtime — done
9. TASK-0010 state machine — done
10. TASK-0024 authorized facility build command — done
11. TASK-0009 golden fixtures — done
12. TASK-0011 replay round trip — done
13. TASK-0025 Gate 1／M1 exit audit — done
14. TASK-0026 M1 MOM／CTR migration boundary — done
15. TASK-0027 temporary-liberty Domain kernel — done
16. TASK-0028 closed-window capture benefits — done
17. TASK-0029 enemy boundary／golden replay integration — review／current

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
