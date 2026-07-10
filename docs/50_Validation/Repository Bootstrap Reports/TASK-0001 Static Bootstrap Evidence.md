---
type: validation-report
id: VALIDATION-TASK-0001-STATIC
status: complete
project: Igorogue
task: TASK-0001
evidence_level: static-bootstrap
updated: 2026-07-10
---
# TASK-0001 Static Bootstrap Evidence

## Verdict

The repository bootstrap is structurally implemented and passes all checks available in the packaging environment. The task remains `review`, not `done`, because this environment does not contain the pinned .NET SDK or Godot .NET editor and therefore cannot produce runtime build, test, headless, export, or authentic NuGet lock evidence.

## Implemented surface

- 8 solution projects
- 10 POSIX/PowerShell wrapper pairs
- 3 GitHub Actions jobs
- exact SDK/engine/package manifest
- deterministic generated runtime content in two destinations
- pure .NET bootstrap checksum and formal simulator entry point
- Godot C# smoke scene
- architecture boundary tests
- exact tool verifier with negative tests

## Commands executed successfully

```text
python3 -m compileall -q tools
python3 -m unittest discover -s tools/tests -v
python3 tools/check_repository_bootstrap.py
python3 tools/content_sync.py --check
python3 tools/check_all.py
tools/dev/check
bash -n tools/dev/_common.sh tools/dev/check tools/dev/verify-tools \
  tools/dev/content-sync tools/dev/update-locks tools/dev/restore \
  tools/dev/build tools/dev/test tools/dev/sim-smoke \
  tools/dev/godot-smoke tools/dev/export-windows tools/ci/install_godot.sh
```

Results:

```text
Repository bootstrap checks passed — 8 projects, 10 wrapper pairs
Python tooling tests passed — 8 tests
Existing abstract proxy regression tests passed — 2 tests
All governance checks passed
CI YAML parsed — 3 jobs
JSON parsed — 44 files
XML/MSBuild parsed — 11 files
```

## Content reproducibility

Two successive writes produced the same canonical content hash:

```text
sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06
```

`build/generated_content` and `game/Igorogue.Godot/generated_content` were byte-identical.

## Negative verification

A fake executable reporting:

```text
4.7.stable.official.bootstrap
```

was rejected because it was the standard editor rather than the .NET/Mono editor. A fake executable reporting:

```text
4.7.stable.mono.official.bootstrap
```

was accepted together with the exact fake `.NET SDK 8.0.422` report.

## Expected failures in this environment

```text
python3 tools/verify_toolchain.py --json
→ No executable found for DOTNET_BIN

python3 tools/check_repository_bootstrap.py --strict-locks
→ fails because authentic packages.lock.json files have not been generated
```

These are unresolved acceptance criteria, not test regressions.

## Runtime evidence still required

1. exact tool verifier on the development host
2. authentic lock generation using SDK 8.0.422
3. clean `dotnet restore --locked-mode`
4. Release build
5. xUnit execution
6. formal simulator smoke and repeated checksum
7. Godot headless C# build
8. Godot smoke scene exit code 0
9. Windows debug export and SHA-256
10. green CI on the reviewed commit

See [[TASK-0020 Review Repository Bootstrap Runtime Evidence]].
