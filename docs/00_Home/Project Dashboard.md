---
type: dashboard
status: active
project: Igorogue
updated: 2026-07-10
---
# Project Dashboard

## Phase

`M0 Repository Bootstrap` is implemented as a review candidate. Static evidence passes; configured-host .NET/Godot runtime evidence and committed package locks remain.

## Sprint goal

Prove the Accepted Godot/.NET boundary from a clean checkout, then begin deterministic command-log work without moving game authority into Godot.

## Health

| Area | State | Note |
|---|---|---|
| Product vision | Green | Pillars documented |
| Rules | Yellow | P0 specs complete; FEAT-009 independent paper sign-off remains |
| Technical | Yellow | Repository artifacts exist; runtime evidence pending |
| Content | Yellow | v0.2 candidates unvalidated |
| UX | Yellow | Mockup stage |
| Validation | Yellow | Abstract proxy only; formal Kernel not implemented |

## Blockers

- Configured host or CI must generate and commit NuGet package locks.
- Godot/.NET headless build, tests, simulator smoke, and Windows export remain unproven in the packaging environment.
- No product Rules Kernel exists yet.

## Current evidence

- all M-1 deterministic design fixtures
- Accepted Godot 4.7 .NET architecture
- solution and 8-project repository bootstrap
- content synchronization and exact tool verifier
- architecture boundary tests
- GitHub Actions pipeline definition

## Next

1. [[TASK-0020 Review Repository Bootstrap Runtime Evidence]]
2. [[TASK-0002 Deterministic RNG and Command Log]] after TASK-0001 is done
3. FEAT-009 independent two-person sign-off
4. A-6 style data/document synchronization checker
