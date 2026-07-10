---
type: status
status: active
project: Igorogue
updated: 2026-07-10
---
# Current Development State

## Executive state

| Area | State | Evidence |
|---|---|---|
| Core concept | promising, not play-validated | design review only |
| Player-visible rules | major M-1 repairs accepted | specifications + deterministic fixtures |
| Enemy intent | 山賊棋士／侵入者 specified | FEAT-009 + fixtures; human two-person sign-off pending |
| Engine architecture | accepted | ADR-0001 |
| Repository bootstrap | static implementation complete | Python/static checks |
| .NET build/test | unproven on packaged host | TASK-0022 required |
| Godot headless/export | unproven on packaged host | TASK-0022 required |
| Product Rules Kernel | not implemented | TASK-0002 onward |
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

## Immediate gate

[[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]] is the first authorized execution task on the Mac host.

It must either:

- produce genuine runtime evidence and close TASK-0001/TASK-0020; or
- stop with a precise blocker report without changing accepted version pins.

## Next development sequence

After runtime evidence:

1. TASK-0002 deterministic RNG and command log
2. TASK-0003 coordinates and neighbours
3. TASK-0004 groups and liberties
4. TASK-0005 placement and capture
5. TASK-0006 legality and terminal capture
6. TASK-0007 king result
7. TASK-0008 territory
8. TASK-0009 golden fixtures
9. TASK-0010 state machine
10. TASK-0011 replay round trip

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
