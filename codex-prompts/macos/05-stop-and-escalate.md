# Codex Prompt — Stop and Escalate

Stop all edits. Read `docs/40_Production/Codex Stop and Escalation Rules.md`.

Produce a blocker report with:

- TASK and current stage;
- what was attempted;
- files changed so far;
- exact command and exit code;
- expected versus observed behavior;
- whether repository state remains safe;
- source-of-truth ambiguity or host requirement;
- smallest human decision/action needed;
- exact safe resume command;
- recommended TASK status.

Do not invent a specification, weaken a test, change a version pin, install software, or claim success.
