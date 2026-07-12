---
type: task
id: TASK-0037
status: blocked
project: Igorogue
milestone: M2
priority: critical
dependencies: [TASK-0036]
updated: 2026-07-12
---
# TASK-0037 Implement Bandit Intent Planning and Execution

## Outcome

FEAT-009の`enemy_bandit`について、候補生成、強制処刑／王石防衛、辞書式ranking、planned target、retarget／fallback／pass、通常／bonus action実行を共有Rules KernelとApplicationへ接続する。

## Source of truth

- [[FEAT-009 Enemy Action Planning and Placement]]
- [[Enemy Design and Intent]]
- [[FEAT-009 Enemy Decision Fixtures]]
- `game_data/content/enemies.json`
- existing authoritative enemy boundary

## Non-goals

- 侵入者、hidden randomness、囲碁探索AI、counterattack full runtime、UI、card loop integration／replay update。

## Allowed areas

- pure Domain enemy candidate／ranking policy。
- Application planning／execution integration。
- Domain／Application／Architecture tests、本TASK／status文書。

## Acceptance criteria

- all candidate legalityは既存placement／effective-liberty／repetition／territory／facility kernelを呼び、敵専用ルール複製を作らない。
- mandatory lethal、defense threshold、capture non-king、pressure、advance、fallback、passをAccepted lexicographic orderとCanonical point tie-breakで実装する。
- planned intentがtarget ref、primary、最大2 alternates、retargetable、planned checksumをcanonicalに保持する。
- 実行時にoverride、same-intent retarget、fallbackを再評価し、terminal後は残りactionを抑止する。
- FEAT-009のF09-01〜03とBanditに適用するF09-08 branchをE3へ移植し、same state／content hashから同一plan／placementになる。Invader固有のF09-04〜07は本TASKへ含めない。

## Validation

- repository wrappers、fixture migration、input reversal、override／retarget／pass／terminal negatives。
- independent fixed-HEAD review、CI全job。

## Known issues

TASK-0036 mergeまで`blocked`。InvaderはM3以降。
