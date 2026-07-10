# Igorogue Code Review Standard

This file defines the mandatory independent review checklist for Codex and human reviewers.

## Review stance

Do not trust the implementation summary. Compare the diff directly with the TASK, Rules Canon, Feature Specs, accepted ADRs, runtime data, and tests.

## Severity

- **BLOCKER**: invalid rules, data loss, nondeterminism, security risk, broken build, or acceptance criteria not met.
- **HIGH**: likely gameplay/replay defect, rule duplication, missing critical boundary test, or unsafe migration.
- **MEDIUM**: maintainability or diagnostics problem that should be fixed before the dependent task.
- **LOW**: non-blocking clarity, naming, or cleanup issue.

## Required checks

### Scope and governance

- The TASK was `ready` when work began.
- Outcome and every acceptance criterion are addressed.
- Non-goals remain untouched.
- No accepted ADR or player-visible rule was changed without explicit scope.
- TASK status, Execution Log, Evidence, and Known Issues are truthful.

### Architecture

- Domain and Application reference no Godot types.
- Godot, bots, replay, and simulator do not mutate state outside Application commands.
- No duplicate rules implementation exists outside the shared Rules Kernel.
- Runtime values come from `game_data/`, not hard-coded copies.
- New dependencies were explicitly approved.

### Determinism

- All ordering is explicit; unordered collection traversal cannot decide outcomes.
- RNG has a named stream, seed, and stable consumption order.
- Same version + content hash + seed + commands yields identical results.
- Rejected commands do not consume RNG or mutate state unless the accepted spec says otherwise.
- Replay/checksum compatibility is considered.

### Testing

- Tests fail before the fix or clearly protect the new behavior.
- Boundary, negative, and simultaneous-resolution cases are covered.
- Golden fixtures are used for rule-order behavior where appropriate.
- Tests were not weakened to make the change pass.
- Relevant governance, build, test, and smoke commands were run.

### Godot/UI

- Presentation code does not decide gameplay outcomes.
- Scene/resource edits were explicitly authorized.
- Godot headless parse/build passed.
- Human visual review is recorded for visible changes.
- Input focus, pixel scaling, and accessibility implications are noted.

### Evidence

- Commands, exit codes, checksums, and artifact paths are recorded.
- Machine-specific secrets, usernames, and private absolute paths are redacted.
- Proxy simulation results are not presented as formal Rules Kernel evidence.
- Remaining uncertainty is stated, not hidden.

## Required review output

For every finding provide:

```text
Severity:
File/line:
Rule or acceptance criterion:
Observed behavior:
Why it matters:
Reproduction or proof:
Recommended correction:
```

End with one decision:

- `APPROVE`
- `APPROVE WITH FOLLOW-UP`
- `CHANGES REQUIRED`
- `BLOCKED BY SPECIFICATION`
