---
type: technical-status
id: TECH-BOOTSTRAP-0001
status: complete
project: Igorogue
updated: 2026-07-11
---
# Repository Bootstrap Status

## Purpose

Record the completed [[TASK-0001 Decide Engine and Repository]] bootstrap and its configured-host evidence.

## Implemented repository artifacts

- `Igorogue.sln`
- exact `.NET SDK 8.0.422` pin in `global.json`
- Central Package Management for xUnit v3
- pure `net8.0` Domain, Application, and Content projects
- formal simulator CLI using Application and Content
- Godot 4.7 .NET presentation project and smoke scene
- architecture tests that prohibit Domain/Application references to Godot
- deterministic content snapshot generator
- exact tool verifier
- POSIX and PowerShell wrappers
- GitHub Actions governance, .NET, Godot smoke, and Windows export jobs

## Evidence

Evidence level: **configured-host and clean-checkout runtime evidence**.

The repository structure, dependency direction, wrapper parity, content generation, and tool-verifier behavior pass governance. [[TASK-0022 macOS Runtime Evidence]] records authentic locks, locked restore, Release build, xUnit, repeated simulator checksum, Godot .NET smoke, managed Windows export, PowerShell fail-fast, final CI, and merge evidence.

## Tool contract

```text
.NET SDK: 8.0.422 exactly
Godot: 4.7-stable .NET/Mono editor exactly
Godot renderer: Compatibility
C#: 12.0
Target framework: net8.0
Test runner package: xunit.v3.mtp-v1 3.2.2
```

A standard non-.NET Godot editor is rejected.

## macOS handoff execution

[[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]] closed the runtime and CI evidence gap. Gate 1 Rules Kernel work begins with [[TASK-0002 Deterministic RNG and Command Log]].
