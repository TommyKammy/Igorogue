---
type: gate-audit
id: TASK-0042-M2-GRAYBOX-VALIDATION
status: in_progress
project: Igorogue
task: TASK-0042
milestone: M2
updated: 2026-07-18
fixed_main_head: ebec9dbdf249cb1db8e13910996022877abdb617
---
# TASK-0042 M2 Core Duel Graybox Validation

## Result

`M2 TECHNICAL EXIT: IN PROGRESS`

`E4 HUMAN UAT: IN PROGRESS`

`E4 FUN CLAIM: NOT PASSED`

`GATE 3 ENTRY: NOT EVALUATED`

本監査はfixed main HEAD `ebec9dbdf249cb1db8e13910996022877abdb617`を対象に、[[Milestones and Exit Gates]]のM2項目、[[DECISION-0006 Resolve M2 Starter Deck and Facility Scope]]のexact starter recipe、Bandit win／loss、restart、accepted command replay、各card effectのhuman operationを追跡する。technical evidenceと[[PT-0001 呼吸点と捕獲の理解]]のhuman evidenceは相互に代替しない。

最終判定は未確定である。TASK-0041のGodot captureとowner visual approvalは、fresh TASK-0042 UATまたはfun evidenceへ流用しない。

## Fixed baseline

- fixed main HEAD／PR #32 merge commit: `ebec9dbdf249cb1db8e13910996022877abdb617`
- PR #32 source HEAD: `0ad575d56e353312d24f32f007ac7c324eddad07`
- PR CI run: `29507033877`、全3 job success
- fixed main workflow-dispatch CI run: `29537645016`、全3 job success
- validation record／PR #33 source HEAD: `d3256587cf8a835e7e009dd44bae4cf3a609f0d5`
- PR #33 merge HEAD: `1d6b7c2e2ede5671e7d4736548e6728908fb7bf9`
- PR #33 CI run: `29539092195`、全3 job success
- post-merge main CI run: `29613756684`、全3 job success
- content snapshot: `sha256:aa26362f6c4b1cdc9c8dc9336654bd20fe5379f622eef3fa992257db62d86832`
- validation date: 2026-07-17

## Evidence contract

- `E3 automated`: repository wrapper、Rules Kernel test、Replay V3 round trip、Godot smoke、Windows export。
- `E4 human`: fresh Godot runを人間がmouse／keyboardで操作し、画面上の理解可能性と操作結果を回答する。
- `E4 fun claim`: 技術的完走やvisual承認だけでは成立しない。今回質問していない、または回答が不足する場合は`NOT EVALUATED`を維持する。
- M2 technical exitは、M2 exit matrix、starter card coverage、Bandit win／loss、restart、human-run replay parityの全必須行が閉じた場合だけ`PASS`とする。

## M2 exit matrix

| Accepted M2 requirement | Production artifact | Automated evidence | Fresh human evidence | Audit |
|---|---|---|---|---|
| 7×7 UI | `CoreDuelBattle` Godot scene／controller | Godot smoke／export green | PT-0001 Test 1: side／orientation understood | PASS |
| starter 6種類／12枚 exact recipe | `game_data/content/starting_decks.json`、runtime deck／hand | content snapshot confirms `5/2/1/2/1/1` and total 12; full checks pending | six-ID coverage pending | PENDING |
| `card_development`だけをfacility kernelへ接続するM2例外 | runtime card effect／facility command | pending | Development effect pending | PENDING |
| 山賊棋士 | FEAT-009 Bandit planner／Godot enemy turn | pending | win／loss paths pending | PENDING |
| 意図表示 | Godot intent panel | Bandit intent query／Godot smoke green | PT-0001 Test 1: next action readable | PASS |
| アタリ表示 | Godot board overlay | pending | comprehension pending | PENDING |
| 捕獲表示 | battle log／board transition | pending | comprehension pending | PENDING |
| 領地表示 | Godot territory overlay／legend | pending | comprehension pending | PENDING |

## Starter card effect coverage

Resolved recipe: `card_basic_stone` ×5、`card_contact` ×2、`card_development` ×1、`card_extend` ×2、`card_lure_stone` ×1、`card_reinforce` ×1。除外カードはDECISION-0006 Option 1のM2 scopeに従い、本表へ含めない。

| Card content ID | Draw seed | Drawn | Selected | Effect resolved by human | Content hash | Evidence |
|---|---:|---|---|---|---|---|
| `card_basic_stone` | 0 | PENDING | PENDING | PENDING | `sha256:aa26362f...` | seeded hand／query audit; human pending |
| `card_contact` | 0 | PENDING | PENDING | PENDING | `sha256:aa26362f...` | seeded hand／query audit; human pending |
| `card_development` | 6 | PENDING | PENDING | PENDING | `sha256:aa26362f...` | seeded hand／query audit; human pending |
| `card_extend` | 0 | PENDING | PENDING | PENDING | `sha256:aa26362f...` | seeded hand／query audit; human pending |
| `card_lure_stone` | 1 | PENDING | PENDING | PENDING | `sha256:aa26362f...` | seeded hand／query audit; human pending |
| `card_reinforce` | 0 | PENDING | PENDING | PENDING | `sha256:aa26362f...` | seeded hand／query audit; human pending |

Seeded query監査ではfresh盤のlegal target数がBasic 7、Extend 7、Reinforce 3、Contact 0、Lure 0、Development 0である。後3種は盤面準備後にhuman操作する。seed `0`でContact／Basic／Extend／Reinforce、seed `1`でLure、seed `6`でDevelopmentを初期handへ到達させられる。

## Bandit terminal and replay matrix

| Path | Seed | Human terminal | Restart | Accepted commands captured | Replay same terminal | State／log checksums | Audit |
|---|---:|---|---|---|---|---|---|
| loss | pending | PENDING | PENDING | PENDING | PENDING | pending | PENDING |
| win | pending | PENDING | PENDING | PENDING | PENDING | pending | PENDING |

Automated replay determinismとhuman-run replay evidenceは別の受け入れ条件として扱う。自動scriptが同じterminalへ到達しても、Godotで人がacceptedしたcommand列を採取し、その列をReplay V3で再生できなければhuman-run replay行は閉じない。

## Confirmed blocker investigation

2026-07-18のruntime監査で、Godot `CoreDuelGameHost`がbattle sessionをprivateに保持し、各command resultを遷移後に保持しないことを確認した。`BootstrapSmoke`が公開するlaunch optionはversion／seed／screenshotだけであり、UI／CLIに現在runのaccepted command log、Replay V3保存／読込、state checksum、log checksumを出力する入口はない。標準seedによる自動win／loss pathとReplay V3 round tripはRules Kernel経由で再現可能だが、Godotで人が操作した同じrunをartifact化できない。

Exact reproは、seed `39039`でGodotをfresh起動し、任意のcard play／End Turnをacceptedさせ、terminalへ到達してもReplay V3 artifact、accepted command transcript、seed／content hash、final state／log checksumを画面またはfileから取得できないことである。これはAccepted仕様間の矛盾ではなく実装導線の欠落なので、最終結果は`DECISION NEEDED`ではなく`NOT PASSED`候補として扱う。

別のseeded production auditではseed `39039`の6-turn Bandit win command pathと、同seedのturn-limitなし自動loss pathを再現できている。win最終state checksum候補は`1fc97bb91f9be10b71d5370053580f051a499fef459741b674c427b85a743706`である。これもhuman terminal／restart／replay evidenceへは数えず、PT-0001 runbookとしてだけ使う。

## Scope verification

TASK-0042ではvalidation／playtest／TASK／status文書だけを変更する。production code、tests、`game_data/`、Godot asset、Accepted Rules Canon／ADR／Feature Spec／Milestonesは変更しない。再現したproduction defectは別のbounded TASKとして提案する。

## Validation evidence

- PR #32 source CI run `29507033877` — all 3 jobs success。
- fixed main workflow-dispatch CI run `29537645016` — all 3 jobs success。
- PR #33 CI run `29539092195` — all 3 jobs success。
- post-merge main CI run `29613756684` — all 3 jobs success。
- `tools/dev/check` — exit 0。documentation、wikilink、content、fixtures、governance checks success。content snapshot一致。
- `tools/dev/build` — exit 0。.NET SDK 8.0.422、warning 0／error 0。
- `tools/dev/test` — exit 0。Domain 368、Application 193、Architecture 92、計653 tests、failure 0／skip 0。
- `tools/dev/sim-smoke` — exit 0。checksum `36ca153c20b82b2220c82b787c229d22f255fee7c42fed9c5ce7753ae0ff7bf1`。bootstrap determinism evidenceとしてのみ使用する。
- `tools/dev/godot-smoke` — exit 0。Godot checksum `36ca153c20b82b2220c82b787c229d22f255fee7c42fed9c5ce7753ae0ff7bf1`、graybox checksum `7692094b4154966821fe7251d4fde59c73fcd16c09c8527579885dade55b9cf6`、seed `39039`。
- `tools/dev/export-windows` — exit 0。Windows Debug export SHA-256 `19776780eacd618c28450320c6b78f051c713ad4060870de6520866eb768792a`。
- Godot fresh human UAT Test 1 — PASS。Project ownerがplayer／Bandit識別、canonical orientation、intent読解、card選択／右click解除を合格とした。
- E4 fun prompt — NOT PASSED。Project owner回答「まだゲームを楽しむレベルではない」。
- independent fixed-HEAD evidence review — pending。

## Smallest safe next action

1. seed `0`のfresh Godot runで、PT-0001 Test 2のBasic／Extend／Reinforce previewと通常効果をhuman確認する。
2. 残るcard effect、アタリ／capture／territory、win／loss／restartを一項目ずつ確認する。
3. human-run replay evidence gapを最小follow-up TASKへ切り出し、TASK-0042を`NOT PASSED`として閉じる条件と再検証入口を固定する。
