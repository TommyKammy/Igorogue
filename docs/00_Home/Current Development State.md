---
type: status
status: active
project: Igorogue
updated: 2026-07-14
---
# Current Development State

## Executive state

| Area | State | Evidence |
|---|---|---|
| Core concept | promising, not play-validated | design review only |
| Player-visible rules | major M-1 repairs accepted | specifications + deterministic fixtures |
| Enemy intent | 山賊棋士／侵入者 specified; human evidence unverified | FEAT-009 + fixtures; Gate 2 progression authorized by DECISION-0007 waiver |
| Engine architecture | accepted | ADR-0001 |
| Repository bootstrap | complete | TASK-0022 runtime evidence + CI |
| .NET build/test | proven on macOS and CI | locked restore + xUnit |
| Godot headless/export | proven on macOS and CI | smoke + managed Windows export |
| Playable Godot graybox | in progress | TASK-0041 implemented; 639 tests／smoke／export green, human visual review pending |
| Product Rules Kernel | M1 technical exit `PASS` | TASK-0027〜0030 done; PR #20 merged／CI green |
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

[[TASK-0040 Implement Core Duel Preview Queries]] was merged through PR #30: source HEAD `eaa62531615eef7a10cfe1d16fe92318d45143c8`、main merge commit `d8ccc08cf7fa3cc1a43046d128b2804b50b9d073`、post-merge main CI run `29285926156` all 3 jobs success. [[TASK-0041 Build Playable Godot Core Duel Graybox]] is the current `in_progress` task. Its start audit found no production factory for the standard authoritative initial snapshot; on 2026-07-14 the Project owner approved a narrow pure typed Domain／Content／Application startup seam and corresponding tests, without rule or runtime-value changes. Gate 2 entry remains owner-authorized through [[DECISION-0007 Authorize Gate 2 Without Retained TASK-0012 Human Evidence]], while TASK-0012 human evidence remains unverified.

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
17. TASK-0029 enemy boundary／golden replay integration — done
18. TASK-0030 M1 Headless Rules Kernel exit re-audit — done
19. TASK-0012 FEAT-009 two-human paper evidence — review／not retained; Gate 2 prerequisite waived by DECISION-0007
20. TASK-0031 Gate 2 Core Duel decomposition — done
21. TASK-0032 typed Core Duel content catalog — done
22. TASK-0033 deterministic battle deck／hand／qi kernel — done
23. TASK-0034 atomic basic stone card play — done
24. TASK-0035 starter stone card effects — done
25. TASK-0036 starter reinforce effect — done
26. TASK-0037 Bandit intent — done
27. TASK-0038 resolved starter recipe／Development scope — done
28. TASK-0039 headless Core Duel／replay integration — done
29. TASK-0040 Core Duel preview queries — done; PR #30 merged／post-merge CI green
30. TASK-0041 playable Godot Core Duel graybox — in_progress／current; bounded startup seam approved
31. TASK-0042 M2 Core Duel graybox validation — blocked by TASK-0041

## Human-only evidence waiver

[[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]] remains `review` because raw worksheets, signer identities, execution dates, and results are not stored. The Project owner's 2026-07-12 instruction authorizes proceeding on the assumption that sign-off occurred; [[DECISION-0007 Authorize Gate 2 Without Retained TASK-0012 Human Evidence]] waives only the Gate 2 prerequisite and does not convert that assumption into human evidence.

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
