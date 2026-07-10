# Igorogue（仮）

囲碁 × デッキ構築ローグライト × 拡大再生産。

## Repository map

- `docs/`: Obsidian design and production Vault
- `src/Igorogue.Domain/`: engine-independent game authority
- `src/Igorogue.Application/`: commands and use-case orchestration
- `src/Igorogue.Content/`: generated-content loading and validation
- `game/Igorogue.Godot/`: Godot presentation and platform integration
- `tools/Igorogue.Sim.Cli/`: formal headless runner using the same Application boundary
- `tests/`: xUnit and architecture tests
- `game_data/`: runtime content source of truth
- `tools/abstract_sim/`: non-product abstract design proxy
- `toolchain/`: pinned engine and SDK contracts

## Open the design Vault

Open `docs/` in Obsidian and start at `00_Home/Igorogue Project Hub.md`.

## Accepted stack

- Godot 4.7 stable .NET, Compatibility renderer
- C# 12 and exact .NET SDK 8.0.422 targeting `net8.0`
- pure .NET Domain/Application/Content
- xUnit v3
- deterministic generated content snapshot

## macOS Codex handoff

Read `CODEX_MAC_HANDOFF.md` first. The current gate is `TASK-0022`; gameplay implementation is blocked until authentic runtime evidence and CI are complete.

Use `handoff/FIRST_PROMPT.txt` for the first read-only Codex session.

## First host setup

Install the exact tools according to `docs/30_Technical/macOS Development Host Setup.md`, then:

```bash
python3 tools/verify_toolchain.py
tools/dev/update-locks
tools/dev/check
tools/dev/test
tools/dev/sim-smoke
GODOT_BIN=/absolute/path/to/godot-mono tools/dev/godot-smoke
GODOT_BIN=/absolute/path/to/godot-mono tools/dev/export-windows
```

Commit the generated `packages.lock.json` files before relying on `tools/dev/restore`.

The current bootstrap archive contains static evidence only when .NET and Godot are not available on the packaging host. See `docs/30_Technical/Repository Bootstrap Status.md`.
