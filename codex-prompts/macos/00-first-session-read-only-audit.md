# Codex Prompt — First macOS Read-Only Audit


You are taking over the Igorogue repository on macOS.

This is a READ-ONLY AUDIT. Do not edit, create, delete, install, commit, or generate files.

1. Confirm the repository root containing AGENTS.md and Igorogue.sln.
2. Read, in order:
   - AGENTS.md
   - CODEX_MAC_HANDOFF.md
   - docs/00_Home/Source of Truth Map.md
   - docs/00_Home/Current Development State.md
   - docs/40_Production/Tasks/TASK-0022 Bootstrap macOS Host and Close Runtime Evidence.md
   - CODE_REVIEW.md
3. Report every AGENTS.md instruction source currently active for this working directory.
4. Restate:
   - current development gate;
   - the only authorized next execution task;
   - why TASK-0002 is blocked;
   - human approval gates;
   - stop conditions.
5. Run only read-only commands:
   - pwd
   - git rev-parse --show-toplevel
   - git branch --show-current
   - git status --short
   - uname -m
   - sw_vers
   - git --version
   - python3 --version
   - dotnet --version (only if available)
   - dotnet --list-sdks (only if available)
   - find /Applications "$HOME/Applications" -type f -path '*/Contents/MacOS/Godot' -print 2>/dev/null
   - python3 tools/check_all.py
   - python3 tools/content_sync.py --check
   - python3 tools/verify_toolchain.py (allow this to report missing tools)
6. Do not fix failures.
7. Return one of:
   - READY FOR TASK-0022 STAGE 1
   - BLOCKED: HOST TOOLCHAIN
   - BLOCKED: REPOSITORY/GIT STATE
   - BLOCKED: INSTRUCTION OR SPEC CONFLICT

Include exact commands, exit codes, and the smallest human action required. Redact private absolute paths in the chat report.
