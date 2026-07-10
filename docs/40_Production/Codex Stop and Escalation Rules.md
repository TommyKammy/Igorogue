---
type: process
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Codex Stop and Escalation Rules

## Principle

A precise stop is better than an invented specification or a convincing but unverified completion claim.

## Mandatory stop conditions

Codex must stop before editing when:

- TASK status is not `ready`;
- acceptance criteria conflict;
- TASK conflicts with Rules Canon or an Accepted ADR;
- the requested behavior is undefined and player-visible;
- implementation requires changing version pins or dependencies outside scope;
- a destructive Git operation appears necessary;
- required local tools are missing or wrong-version;
- a test is flaky or a defect cannot be reproduced;
- evidence would require inventing output or claiming an unrun test;
- a Godot scene/resource edit is needed without explicit authorization;
- sensitive information would be committed.

## Decision Needed output

Create or propose a Decision Needed note containing:

```text
Problem:
Conflicting or missing sources:
Smallest decisions required:
Options:
Tradeoffs:
Recommended option, if any:
Files and tasks blocked:
Evidence available:
```

Do not select the option on behalf of the designer unless the TASK explicitly delegates the decision.

## Host/tool blocker output

```text
BLOCKED: HOST TOOLCHAIN
Expected:
Observed:
Verification command:
Repository files intentionally unchanged:
Human action required:
Safe command to rerun afterward:
```

Do not solve a missing exact SDK by changing `global.json`.

## Implementation defect discovered outside scope

- record a BUG or TASK proposal;
- include reproduction and severity;
- continue only if the current TASK can remain correct without the extra fix;
- otherwise stop and mark the dependency.

## Nondeterminism

If identical inputs produce different outputs:

1. preserve both logs, seeds, content hashes, and checksums;
2. do not rerun until a passing sample hides the defect;
3. mark the TASK blocked;
4. minimize the reproduction;
5. do not merge.

## Test or evidence failure

Never weaken a test, expected fixture, or acceptance criterion merely to turn the build green. If the specification appears wrong, escalate to a design decision.

## Final blocked report

A blocked response must contain:

- what was attempted;
- what was not changed;
- exact blocker;
- exact command/output;
- smallest human decision/action needed;
- safe resume point.
