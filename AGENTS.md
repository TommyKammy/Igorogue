# AGENTS.md — Igorogue

## Project mission

囲碁の「呼吸点・捕獲・領地」を、デッキ構築と拡大再生産へ接続したPC向けローグライトを開発する。

中心体験は次の通り。

1. 序盤に領地・施設・種石・捨て石などを仕込む。
2. 中盤に複数の効果が接続し、盤面とデッキが急加速する。
3. 熟練者は発火条件を狙って作れるが、毎ランの爆発は保証されない。
4. 高コミや過伸展により、爆発後には敵の反攻が迫る。

## Mandatory read order

1. この `AGENTS.md`
2. ルート `CODEX_MAC_HANDOFF.md`
3. `docs/00_Home/Source of Truth Map.md`
4. `docs/00_Home/Current Development State.md`
5. `docs/20_Design/Rules Canon.md`
6. ユーザーが指定した `TASK-xxxx` ノート
7. タスクからリンクされたFeature Spec、ADR、Balance Report
8. closeout前にルート `CODE_REVIEW.md`

指定タスクがない場合、実装を始めず、Backlogから勝手に選ばない。

## Current handoff gate

v0.2.10時点で、Macホスト上の最初の実行タスクは`TASK-0022`だけである。`TASK-0002`以降のゲーム実装は、TASK-0022がruntime evidenceとCIを満たし、TASK-0001が完了するまで開始しない。

## Non-negotiable rules

- Accepted ADRまたはRules Canonを、タスクの明示なしに変更しない。
- ゲームデザイン上の未決事項を、実装都合だけで決定しない。
- ライブゲーム、リプレイ、正式シミュレーターは同じRules Kernelを使う。
- 実行時乱数はseed付きで、ストリーム用途を明示する。
- 同一バージョン、content hash、seed、入力列から同一結果を得る。
- バランス値をコードへ直書きしない。`game_data/`を正本にする。
- プレイヤーに見えるルール変更にはRules CanonまたはFeature Specの更新が必要。
- 数値調整には、変更前後の測定値または再現seedを添える。
- 受け入れ条件を満たす最小の変更を優先し、無関係なリファクタリングを混ぜない。
- `tools/abstract_sim/`の結果を製品ルールの正しさの根拠にしない。
- Domain/ApplicationへGodot型を導入しない。
- UI、Bot、Replay、Simulatorからゲーム状態を直接変更せず、Application commandを通す。

## Source-of-truth policy

- ルールと意図: Obsidian Markdown
- 現在のランタイム値: `game_data/`
- 実装: ソースコード
- 検証証拠: 自動テスト、golden replay、simulation report、playtest report
- 作業状態: TASKノート

矛盾を見つけた場合、最も新しいファイルを勝手に採用しない。Decision Neededノートを作り、タスクをBLOCKEDにする。

## Task protocol

1. 目的、非対象、受け入れ条件を再掲する。
2. 変更予定ファイルと検証方法を提示する。
3. 実装する。
4. テストを追加・更新する。
5. ビルド、テスト、ドキュメント検査を実行する。
6. TASKノートのExecution Log、Evidence、Known Issuesを更新する。
7. 最終報告で変更内容、検証結果、残るリスクを明記する。

## Stop conditions

- 受け入れ条件同士が矛盾する。
- Accepted ADRとタスクが矛盾する。
- 承認のないプレイヤー可視ルール変更が必要。
- 再現不能な不具合を「直った」と扱う必要がある。
- タスク範囲外の破壊的変更が必要。

## Accepted toolchain

- Godot 4.7 stable .NET, Compatibility renderer
- C# 12 / exact .NET SDK 8.0.422 / `net8.0`
- pure .NET Domain, Application, Content, and formal simulator
- xUnit v3 through `dotnet test`
- runtime content generated from `game_data/`

Codex must not introduce Godot types into Domain/Application. Do not hand-edit `.tscn`, `.tres`, `project.godot`, or export presets unless the TASK explicitly authorizes it and requires a Godot smoke check.

## Build and test commands

Use repository wrappers instead of host-specific commands.

```text
GOVERNANCE=tools/dev/check
VERIFY_TOOLS=tools/dev/verify-tools
GENERATE_LOCKS=tools/dev/update-locks
RESTORE_LOCKED=tools/dev/restore
BUILD=tools/dev/build
TEST=tools/dev/test
SIM_SMOKE=tools/dev/sim-smoke
GODOT_SMOKE=GODOT_BIN=/absolute/path/to/godot-mono tools/dev/godot-smoke
WINDOWS_EXPORT=GODOT_BIN=/absolute/path/to/godot-mono tools/dev/export-windows
```

Windows PowerShell uses the same names with `.ps1`.

Before the first locked restore on a configured host, run `tools/dev/update-locks`, review, and commit every `packages.lock.json`. Never fabricate lock files.

## Godot asset editing

Codex may edit C#, JSON, Markdown, schemas, tests, and build scripts by default. `.tscn`, `.tres`, `project.godot`, and `export_presets.cfg` require explicit task scope plus Godot headless parse/build and human visual review.

## Independent review

実装担当とは別のCodex task/conversationで、ルート`CODE_REVIEW.md`に従ってレビューする。Codexは自動mergeせず、`review → done`と`main`へのmergeは人間判断とする。

## Commit convention

```text
feat(combat): TASK-0012 implement liberty resolution
fix(replay): BUG-0041 preserve rng event ordering
bal(cards): BAL-0023 tune Momentum threshold
docs(adr): ADR-0007 accept smooth komi curve
```
