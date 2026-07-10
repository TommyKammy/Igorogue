---
type: task
id: TASK-0001
status: review
project: Igorogue
milestone: M0
priority: P0
dependencies: [TASK-0019]
updated: 2026-07-11
---
# TASK-0001 Decide Engine and Repository

## Outcome

[[ADR-0001 Engine and Repository]]でAcceptedされたGodot 4.7 stable .NET／C#／pure .NET Rules Kernel構成を、クリーンcheckoutから再現可能な空projectとCI-ready repositoryとしてbootstrapする。

## Source of truth

- [[ADR-0001 Engine and Repository]]
- [[Engine Toolchain and Repository Layout]]
- [[Architecture]]
- [[Determinism and Replay]]
- `toolchain/engine_decision.json`
- `toolchain/bootstrap_manifest.json`

## In scope

- `Igorogue.sln`
- Domain、Application、Content、Godot、test、Sim.Cliの最小project
- `global.json`、Central Package Management、lock files
- Godot 4.7 .NET空projectとCompatibility renderer
- 480×270／viewport／integer scaleのproject setting
- cross-platform dev wrapper
- content sync smoke
- GitHub Actionsのgovernance、.NET、Godot smoke job
- Windows debug export preset

## Non-goals

- ゲームルール実装
- カード、敵、盤面UI
- release signing
- macOS notarization
- engine plugin導入
- gameplay balance

## Acceptance criteria

1. クリーンcheckoutでtool verifierがGodot 4.7 stable .NETとpinned .NET 8 SDKを確認する。
2. `python3 tools/check_all.py`が成功する。
3. `dotnet restore --locked-mode`、build、xUnit testが成功する。
4. Godotを起動せず、Domain smoke testが成功する。
5. `Igorogue.Sim.Cli --smoke`がApplicationとContentを読み、決定論的な同一checksumを2回返す。
6. Godot headlessでC# solutionをbuildし、smoke sceneが起動して0で終了する。
7. コマンドラインからWindows debug exportが生成される。
8. architecture testがDomain→Godotの禁止referenceを検査する。
9. wrong Godot versionとstandard non-.NET editorをtool verifierが拒否する。
10. TASK Evidenceへ実行コマンド、tool versions、artifact hashを記録する。

## Execution log

2026-07-10:

- solutionと8 projectを追加
- pure .NET Domain/Application/Content境界を追加
- formal simulator CLIとGodot bootstrap smokeを追加
- SDK／engine verifier、content sync、cross-platform wrapperを追加
- governance／.NET／Godot／Windows export CIを追加
- static repository checkerとPython unit testsを追加
- generated content snapshotを作成

2026-07-11:

- pinned macOS hostで8個のauthentic lockを生成・review・commit
- task worktreeとdetached clean worktreeの双方でlocked restore、Release build、xUnit 11件、simulator、Godot smoke、Windows managed exportを確認
- Godot managed exportのbootstrap defectを最小修正し、native executableだけのfalse greenをCIとwrapperで拒否

## Evidence

### Packaging environment — passed

```text
python3 tools/content_sync.py --write
python3 -m unittest discover -s tools/tests -v
python3 tools/check_repository_bootstrap.py
python3 tools/check_all.py
python3 tools/content_sync.py --check
bash -n tools/dev/* tools/ci/install_godot.sh
```

The exact command log and package hash are recorded in the v0.2.9 patch report.

### Configured host and clean checkout — passed; CI pending

TASK-0022 supplied pinned-host and detached clean-worktree evidence for acceptance criteria 1–9. All eight lock files are committed. See [[TASK-0022 macOS Runtime Evidence]] for the redacted command log and artifact hashes.

The verified sequence was:

```text
tools/dev/update-locks
tools/dev/check
tools/dev/test
tools/dev/sim-smoke
GODOT_BIN=/absolute/path/to/godot-mono tools/dev/godot-smoke
GODOT_BIN=/absolute/path/to/godot-mono tools/dev/export-windows
```

Stage 4 CI on the reviewed branch remains pending, so acceptance criterion 10 and final closure are not yet claimed.

## Known issues

- Green CI is still required on the reviewed commit before moving this task from `review` to `done`.
- The Godot smoke scene is a bootstrap integration test, not gameplay.

## Exit decision

Keep task in `review`. Move to `done` only after a clean host or CI produces all runtime evidence and committed package locks.
