# Codex Prompt — Independent TASK Review

Review the implementation of:

```text
<TASK ID / TASK FILE>
Base branch: <BASE>
```

This is an independent review. Do not trust the implementer's summary and do not make fixes unless the human asks after findings are reported.

1. Read all active AGENTS instructions, the TASK, source-of-truth files, and root `CODE_REVIEW.md`.
2. Inspect the complete diff against the base branch.
3. Run relevant checks and reproduce evidence.
4. Check scope, architecture, determinism, tests, generated content, replay/save impact, and TASK evidence.
5. Report findings by severity with file/line, reproduction, and correction.
6. End with exactly one decision:
   - APPROVE
   - APPROVE WITH FOLLOW-UP
   - CHANGES REQUIRED
   - BLOCKED BY SPECIFICATION

Do not merge. Do not change TASK status to done.
