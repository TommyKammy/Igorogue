---
type: technical-runbook
status: active
project: Igorogue
updated: 2026-07-10
---
# macOS Development Host Setup

## Goal

Prepare a macOS host that can run the accepted repository bootstrap without changing any accepted version or dependency decision.

## Supported host shapes

Apple Silicon and Intel Macs are acceptable if both .NET and Godot use the matching architecture. Record `uname -m` in runtime evidence.

Do not run the active repository from an iCloud Drive, Dropbox, network-share, or case-insensitive path whose synchronization tool rewrites permissions during builds.

## Required tools

```text
Git
Python 3.12 recommended
.NET SDK 8.0.422 exactly
Godot 4.7-stable .NET editor exactly
Godot 4.7 matching export templates
```

Xcode Command Line Tools are recommended for Git and standard developer utilities.

## Preliminary inspection

```bash
sw_vers
uname -m
xcode-select -p
git --version
python3 --version
dotnet --list-sdks
```

Do not install or upgrade anything through Codex without explicit human approval.

## .NET

Install SDK `8.0.422`, matching the Mac architecture. `global.json` is authoritative.

Verification:

```bash
dotnet --version
dotnet --list-sdks | grep '^8\.0\.422 '
python3 tools/verify_toolchain.py --skip-godot
```

If another .NET 8 patch is installed but 8.0.422 is absent, do not edit `global.json`. Report the missing exact SDK.

## Godot

Install the **.NET/Mono** Godot `4.7-stable` editor, not the standard editor. Install the matching export templates.

Locate the executable rather than assuming the application bundle name:

```bash
find /Applications "$HOME/Applications" \
  -type f -path '*/Contents/MacOS/Godot' -print 2>/dev/null
```

Set the exact path for the current shell:

```bash
export GODOT_BIN="/actual/path/to/Godot.app/Contents/MacOS/Godot"
"$GODOT_BIN" --version
python3 tools/verify_toolchain.py
```

The verifier must identify the .NET/Mono build. A standard Godot editor is intentionally rejected.

When Codex is launched from Finder, shell environment variables may differ from Terminal. Prefer passing the absolute path explicitly in commands:

```bash
GODOT_BIN="/actual/path/to/Godot" tools/dev/godot-smoke
```

Do not store private absolute home paths in committed project files. Evidence should replace them with `<GODOT_BIN>` or `<REPO_ROOT>`.

## macOS security prompts

Open downloaded developer applications through Finder once if macOS asks for confirmation. Use System Settings → Privacy & Security to approve a trusted official build when necessary.

Do not disable Gatekeeper globally and do not run blanket quarantine-removal commands as part of this project runbook.

## Git baseline

Worktrees require a Git repository. Before Codex edits files:

```bash
git rev-parse --show-toplevel 2>/dev/null || true
git status --short 2>/dev/null || true
```

If this is an extracted archive without Git metadata:

```bash
git init -b main
git add .
git commit -m "chore(repo): import Igorogue v0.2.10 Codex handoff baseline"
```

Verify a clean tree before the first Codex task.

## Read-only repository audit

```bash
python3 tools/check_all.py
python3 tools/content_sync.py --check
python3 tools/verify_toolchain.py
```

`verify_toolchain.py` may fail if tools are absent; that is a setup blocker, not permission to change repository pins.

## Runtime-evidence sequence

After the read-only audit and human approval:

```bash
tools/dev/update-locks
git diff -- '**/packages.lock.json' Directory.Packages.props NuGet.Config
tools/dev/restore
tools/dev/test
tools/dev/sim-smoke
tools/dev/sim-smoke
GODOT_BIN="<ABSOLUTE_PATH>" tools/dev/godot-smoke
GODOT_BIN="<ABSOLUTE_PATH>" tools/dev/export-windows
```

The two simulator smoke outputs must contain the same checksum and content hash.

## Authentic lock files

`packages.lock.json` files must be produced by SDK 8.0.422. Never fabricate them.

Review before commit:

- every expected project has a lock file;
- no package version changed from central package management unexpectedly;
- only expected transitive packages are present;
- a second generation produces no diff.

## Clean-checkout proof

After lock files are reviewed and committed, create a disposable clean clone or worktree from the reviewed commit and rerun:

```bash
tools/dev/check
tools/dev/restore
tools/dev/test
tools/dev/sim-smoke
GODOT_BIN="<ABSOLUTE_PATH>" tools/dev/godot-smoke
GODOT_BIN="<ABSOLUTE_PATH>" tools/dev/export-windows
```

Do not use untracked local files as hidden build prerequisites.

## Troubleshooting rule

When a command fails:

1. capture command, exit code, stdout, and stderr;
2. classify as missing tool, host configuration, repository defect, or specification conflict;
3. do not patch unrelated files;
4. use [[Codex Stop and Escalation Rules]];
5. keep TASK status `blocked` or `review` until evidence exists.
