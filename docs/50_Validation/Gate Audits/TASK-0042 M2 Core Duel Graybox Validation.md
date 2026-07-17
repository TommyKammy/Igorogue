---
type: gate-audit
id: TASK-0042-M2-GRAYBOX-VALIDATION
status: not_passed
project: Igorogue
task: TASK-0042
milestone: M2
updated: 2026-07-18
fixed_main_head: ebec9dbdf249cb1db8e13910996022877abdb617
---
# TASK-0042 M2 Core Duel Graybox Validation

## Result

`M2 TECHNICAL EXIT: NOT PASSED`

`E4 HUMAN UAT: NOT PASSED`

`E4 FUN CLAIM: NOT PASSED`

`GATE 3 ENTRY: BLOCKED`

本監査はfixed main HEAD `ebec9dbdf249cb1db8e13910996022877abdb617`を対象に、[[Milestones and Exit Gates]]のM2項目、[[DECISION-0006 Resolve M2 Starter Deck and Facility Scope]]のexact starter recipe、Bandit win／loss、restart、accepted command replay、各card effectのhuman operationを追跡する。technical evidenceと[[PT-0001 呼吸点と捕獲の理解]]のhuman evidenceは相互に代替しない。

Test 1の4項目はfresh E4 evidenceとしてPASSしたが、starter six-ID effects、アタリ／capture／territory、Bandit win／loss／restartは未実施で、human-operated runのReplay V3採取はproduction導線欠如によりBLOCKEDした。したがってM2 technical exitとE4 human UATは`NOT PASSED`、Gate 3 entryは`BLOCKED`とする。TASK-0041のGodot captureとowner visual approvalは、fresh TASK-0042 UATまたはfun evidenceへ流用しない。

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
| starter 6種類／12枚 exact recipe | `game_data/content/starting_decks.json`、runtime deck／hand | content snapshot confirms `5/2/1/2/1/1` and total 12 | six-ID effect coverage 0／6 | NOT PASSED |
| `card_development`だけをfacility kernelへ接続するM2例外 | runtime card effect／facility command | production tests green | Development effect NOT RUN | NOT PASSED |
| 山賊棋士 | FEAT-009 Bandit planner／Godot enemy turn | automated win／loss paths and replay parity pass | human win／loss NOT RUN | NOT PASSED |
| 意図表示 | Godot intent panel | Bandit intent query／Godot smoke green | PT-0001 Test 1: next action readable | PASS |
| アタリ表示 | Godot board overlay | implementation／smoke green | NOT RUN | NOT PASSED |
| 捕獲表示 | battle log／board transition | implementation／smoke green | NOT RUN | NOT PASSED |
| 領地表示 | Godot territory overlay／legend | implementation／smoke green | NOT RUN | NOT PASSED |

## Starter card effect coverage

Resolved recipe: `card_basic_stone` ×5、`card_contact` ×2、`card_development` ×1、`card_extend` ×2、`card_lure_stone` ×1、`card_reinforce` ×1。除外カードはDECISION-0006 Option 1のM2 scopeに従い、本表へ含めない。

| Card content ID | Draw seed | Drawn | Selected | Effect resolved by human | Content hash | Evidence |
|---|---:|---|---|---|---|---|
| `card_basic_stone` | 0 | NOT RUN | NOT RUN | NOT RUN | `sha256:aa26362f...` | seeded hand／query audit only |
| `card_contact` | 0 | NOT RUN | NOT RUN | NOT RUN | `sha256:aa26362f...` | seeded hand／query audit only |
| `card_development` | 6 | NOT RUN | NOT RUN | NOT RUN | `sha256:aa26362f...` | seeded hand／query audit only |
| `card_extend` | 0 | NOT RUN | NOT RUN | NOT RUN | `sha256:aa26362f...` | seeded hand／query audit only |
| `card_lure_stone` | 1 | NOT RUN | NOT RUN | NOT RUN | `sha256:aa26362f...` | seeded hand／query audit only |
| `card_reinforce` | 0 | NOT RUN | NOT RUN | NOT RUN | `sha256:aa26362f...` | seeded hand／query audit only |

Seeded query監査ではfresh盤のlegal target数がBasic 7、Extend 7、Reinforce 3、Contact 0、Lure 0、Development 0である。後3種は盤面準備後にhuman操作する。seed `0`でContact／Basic／Extend／Reinforce、seed `1`でLure、seed `6`でDevelopmentを初期handへ到達させられる。

## Bandit terminal and replay matrix

| Path | Seed | Human terminal | Restart | Accepted commands captured | Replay same terminal | State／log checksums | Audit |
|---|---:|---|---|---|---|---|---|
| loss | 39039 | NOT RUN | NOT RUN | BLOCKED | BLOCKED | automated state `008f3d...`／log `f77e02...` only | NOT PASSED |
| win | 39039 | NOT RUN | NOT RUN | BLOCKED | BLOCKED | automated state `7487e8...`／log `9bcb5d...` only | NOT PASSED |

Automated replay determinismとhuman-run replay evidenceは別の受け入れ条件として扱う。自動scriptが同じterminalへ到達しても、Godotで人がacceptedしたcommand列を採取し、その列をReplay V3で再生できなければhuman-run replay行は閉じない。

## Confirmed blocker investigation

2026-07-18のruntime監査で、Godot `CoreDuelGameHost`がbattle sessionをprivateに保持し、各command resultを遷移後に保持しないことを確認した。`BootstrapSmoke`が公開するlaunch optionはversion／seed／screenshotだけであり、UI／CLIに現在runのaccepted command log、Replay V3保存／読込、state checksum、log checksumを出力する入口はない。標準seedによる自動win／loss pathとReplay V3 round tripはRules Kernel経由で再現可能だが、Godotで人が操作した同じrunをartifact化できない。

Exact reproは、seed `39039`でGodotをfresh起動し、任意のcard play／End Turnをacceptedさせ、terminalへ到達してもReplay V3 artifact、accepted command transcript、seed／content hash、final state／log checksumを画面またはfileから取得できないことである。これはAccepted仕様間の矛盾ではなく実装導線の欠落なので、最終結果は`DECISION NEEDED`ではなく`NOT PASSED`候補として扱う。

別のseeded production auditではseed `39039`の6-turn Bandit win command pathと、同seedのturn-limitなし自動loss pathを再現できている。win最終state checksum候補は`1fc97bb91f9be10b71d5370053580f051a499fef459741b674c427b85a743706`である。これもhuman terminal／restart／replay evidenceへは数えず、PT-0001 runbookとしてだけ使う。

## Automated terminal replay evidence

- loss candidate、seed `39039`: cardを使わずEnd Turnを15回、各Bandit actionを自動解決。`loss／black_king_captured`、turn 15、state `008f3d0865cc83ebad869706ba0885d7231cf422d1dc95b940b1ece3d93f4711`、log `f77e02965374c6266b9f92e96184a58ff88956806b56eb6cff00c0310bd34339`。Replay V3は30 accepted commands、27,930 bytesで同一stateへround trip。
- win candidate、seed `39039`: production Application APIの9-turn pathで`win／white_king_captured`、state `7487e89e2a5f326ed2629c24ce89aa0a6885abaabfd3ba9a672927b8897f4079`、log `9bcb5d01594ea05d3c562068136fb3740cced49c87356e6f8c0c5bf96104cb01`。Replay V3は38 accepted commands、36,539 bytesで同一stateへround trip。

両行はE3 automated evidenceであり、human terminal／restart／replay evidenceの代替ではない。

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

1. [[TASK-0043 Capture and Verify Godot Human Run Replay V3]]でinitial sessionとexact command-result chainを保持し、terminal artifactをsave／load／fresh replay検証できるようにする。
2. TASK-0043 merge後の新fixed HEADでstarter six-ID effects、アタリ／capture／territory、win／loss／restart／replay parityをfresh再検証する。
3. meaningful per-turn decisionとfun未到達の原因は、再現可能なhuman runを得た後に別scopeで診断する。Gate 3を先行しない。
