# Igorogue Package Manifest

## v0.2.10 Codex macOS handoff candidate

Baseline executable assets from v0.2.9 remain:

- `Igorogue.sln`
- `global.json`
- `Directory.Build.props`
- `Directory.Packages.props`
- `NuGet.Config`
- `.github/workflows/ci.yml`
- `src/Igorogue.Domain/`
- `src/Igorogue.Application/`
- `src/Igorogue.Content/`
- `game/Igorogue.Godot/`
- `tests/Igorogue.*.Tests/`
- `tools/Igorogue.Sim.Cli/`
- `tools/dev/`
- `tools/ci/`
- `toolchain/bootstrap_manifest.json`
- `build/generated_content/`

Handoff assets added in v0.2.10:

- `CODEX_MAC_HANDOFF.md`
- `CODE_REVIEW.md`
- `handoff/FIRST_PROMPT.txt`
- `handoff/HANDOFF_MANIFEST.json`
- `docs/00_Home/Codex Mac Handoff.md`
- `docs/00_Home/Current Development State.md`
- `docs/30_Technical/macOS Development Host Setup.md`
- `docs/40_Production/Codex App Operating Procedure.md`
- `docs/40_Production/Codex Review and Merge Procedure.md`
- `docs/40_Production/Codex Stop and Escalation Rules.md`
- `docs/40_Production/Codex Task Queue.md`
- `docs/40_Production/Tasks/TASK-0021 Prepare macOS Codex App Handoff.md`
- `docs/40_Production/Tasks/TASK-0022 Bootstrap macOS Host and Close Runtime Evidence.md`
- `codex-prompts/macos/`
- nested `AGENTS.md` files under `src/`, `game/Igorogue.Godot/`, `docs/`, `tools/`, and `tests/`

## Evidence boundary

Static repository and governance checks are included. Genuine NuGet locks and .NET/Godot runtime, clean-checkout, Windows export, and CI evidence remain TASK-0022 responsibilities on the configured Mac host.
