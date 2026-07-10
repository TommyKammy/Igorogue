---
type: task
id: TASK-0019
status: done
milestone: M-1
priority: P0
project: Igorogue
updated: 2026-07-10
---
# TASK-0019 Accept Engine and Repository Architecture

## Outcome

Fable 5 Action Backlog B-3を実施し、Godot／Unity／MonoGameをIgorogue固有の評価軸で比較して、エンジン、言語、Rules Kernel境界、repository、CI方針をAcceptedへ固定する。

## Source

- Fable 5 Action Backlog B-3
- [[Architecture]]
- [[Determinism and Replay]]
- [[Simulation Architecture]]
- [[Performance and Platform]]

## In scope

- ADR-0001の候補比較とAccepted化
- Godot 4.7 stable .NET／C# 12／.NET 8選定
- GitHub-hosted single repository方針
- Domain／Application／Content／Godot／Sim.Cli境界
- version pin／upgrade／CI／Codex scene-edit policy
- machine-readable decisionとspecification checker
- M0 TASK-0001の受け入れ条件具体化

## Out of scope

- Godotや.NET SDKのインストール
- `Igorogue.sln`とGodot空projectの生成
- 実際のCI workflow
- Windows export実行
- 製品Rules Kernel
- UI scene実装

## Acceptance evidence

- Godot、Unity、MonoGame×8評価軸のweightが100で、採用候補と理由が記録される。
- Godotが最高weighted scoreであることをmachine checkerが再計算する。
- engine version、edition、language、TFM、renderer、platform、repository hostが一義的である。
- DomainからGodotを排除するdependency ruleが記載される。
- headless .NET tests、formal simulator、Godot smokeのCI段階が分離される。
- patch/minor/prereleaseのupgrade policyが記載される。
- `python tools/check_all.py`が成功する。

## Evidence

- [[ADR-0001 Engine and Repository]]
- [[Engine Toolchain and Repository Layout]]
- [[Official Engine Evaluation Sources]]
- `toolchain/engine_decision.json`
- `.godot-version`
- `tools/check_engine_decision.py`

## Execution log

- 2026-07-10: 公式sourceを再確認。
- 2026-07-10: 8軸100 weightの候補matrixを作成。
- 2026-07-10: Godot 4.7 stable .NETをAccepted。
- 2026-07-10: repository／CI／version／Codex境界を同期。
- 2026-07-10: checkerのpositive／negative testを実施。

## Known issues

- このタスクは選定と仕様固定であり、空projectの起動、dotnet test、Godot headless、exportの実証はTASK-0001で行う。
- Godot 4.7の最初のmaintenance patch採用は、stable公開後にtoolchain taskで判断する。
