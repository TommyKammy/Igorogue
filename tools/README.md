# Tools

## Repository wrappers

| POSIX | PowerShell | Purpose |
|---|---|---|
| `tools/dev/check` | `check.ps1` | governance plus generated content check |
| `tools/dev/verify-tools` | `verify-tools.ps1` | exact .NET and Godot verification |
| `tools/dev/content-sync` | `content-sync.ps1` | regenerate runtime content snapshot |
| `tools/dev/update-locks` | `update-locks.ps1` | authentic first NuGet lock generation |
| `tools/dev/restore` | `restore.ps1` | locked restore |
| `tools/dev/build` | `build.ps1` | Release build |
| `tools/dev/test` | `test.ps1` | xUnit suite |
| `tools/dev/sim-smoke` | `sim-smoke.ps1` | formal runner bootstrap smoke |
| `tools/dev/godot-smoke` | `godot-smoke.ps1` | Godot C# headless build and scene smoke |
| `tools/dev/export-windows` | `export-windows.ps1` | Windows debug export plus SHA-256 |

## Governance

`python3 tools/check_all.py` runs document, design fixture, engine, repository, Python unit, and abstract-proxy regression checks.

## Generated content

`tools/content_sync.py` canonicalizes `game_data/balance/*.json` and `game_data/content/*.json` into both build and Godot snapshots. These snapshots are deterministic and validated byte-for-byte.
