---
type: decision-needed
id: DECISION-0007
status: resolved
blocking: []
updated: 2026-07-12
---
# DECISION-0007 Authorize Gate 2 Without Retained TASK-0012 Human Evidence

## Context

[[TASK-0012 Implement FEAT-009 Enemy Behavior Specification]] requires two independent humans to solve F09-01〜08 and reach the same results. The repository contains neither worksheets, execution dates, signer identities, nor recorded results.

On 2026-07-12, the Project owner instructed: 「TASK-0012の二人human sign-offは行った前提で先に進めてください」.

That wording authorizes forward progress on an assumption. It does not by itself prove that two reviews occurred or agreed, so the missing evidence must not be reconstructed or described as verified.

## Decision

- Treat the Project owner instruction as an explicit waiver of the TASK-0012 evidence prerequisite for Gate 2 entry.
- Keep TASK-0012 in `review` because its human-evidence acceptance criterion is not verifiable from retained artifacts.
- Permit TASK-0031 and its dependency-safe successors to proceed despite that status.
- Do not cite this decision as E1／E3 gameplay evidence or as proof of reviewer identity, independence, execution, or agreement.

## Consequences

- Gate 2 is owner-authorized open, not evidence-complete.
- FEAT-009 machine fixtures remain E1 until production migration; TASK-0037 owns the Bandit subset E3 migration.
- If worksheets or an explicit factual attestation become available later, update TASK-0012 and the fixture table in a separate bounded documentation task.

## Owner decision

Resolved by the Project owner's 2026-07-12 instruction quoted above.
