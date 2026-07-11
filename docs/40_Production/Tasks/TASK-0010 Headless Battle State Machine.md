---
type: task
id: TASK-0010
status: in_progress
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001, TASK-0023]
updated: 2026-07-11
---
# TASK-0010 Headless Battle State Machine

## Outcome

既存のpure Domain placement／repetition／king／territory／facility kernelをimmutableなApplication battle sessionへ束ね、UI、カード、敵AIを必要とせず、配置modeを上流で承認済みの黒／白scripted placementとpass／turn-boundary command列から、王石捕獲または20回目のplayer turn後まで決定論的に一戦を解決する最小headless state machineを実装する。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]
- [[Command Event Model]]
- [[Combat Resolution Order]]
- [[FEAT-001 Territory and Facilities]]
- [[DECISION-0002 Resolve Territory and Facility Event Order]]
- [[DECISION-0003 Sequence Golden Replay After Battle State Machine]]
- [[TASK-0023 Implement Facility Runtime Semantics]]

## Non-goals

- `PlayCard`、deck／hand／discard、気、card zone、placement tag候補生成、terminal grantの付与判断。
- `MomentumState`、生成／消費、`momentum_reach`、前線前進ドロー。選択済みevent order seamだけを固定し、`MomentumChanged`は発行しない。
- enemy intent、candidate ranking、retarget、AI。白actionはtest／harnessが既承認pointまたはpassとして注入する。
- 仮呼吸点、continuous modifier、expiry sweep、capture benefit、予約draw／qi／choice、facility effect trigger。
- 反攻、heat、追加敵行動、妙手、魂、income適用。
- facility build。TASK-0023 APIはplacement destructionとterritory reassociationだけ利用する。
- golden fixture生成、replay deserialize／round trip。これらはTASK-0009／TASK-0011へ残す。
- UI／Godot、Content／`game_data/`、balance、package／project reference変更。
- 完成版turn／card loop、正式enemy runtime、または「遊べる製品」の主張。

## Allowed areas

- `src/Igorogue.Domain/Board/`、`Combat/`、`Facilities/`のterritory delta、facility-aware placement後reassociation、typed ordered factに必要な最小追加。
- `src/Igorogue.Application/Battle/`のimmutable state／session、typed commands／results、phase／turn orchestration、canonical checksum、既存`OrderedCommandLog`接続。
- `tests/Igorogue.Domain.Tests/`、`tests/Igorogue.Application.Tests/`、`tests/Igorogue.Architecture.Tests/`の本タスク向けテスト。
- owner選択に基づくDECISION-0002解決と、Rules Canon／FEAT-001／FEAT-002／Combat Resolution Order／Command Event Modelのevent order表現同期。
- 本TASK、関連Decision、production state文書の実行状態とEvidence同期。
- Content、`game_data/`、package／project reference、Godot assetは変更しない。

## Acceptance criteria

- start factoryはinitial `BoardState`と`FacilityState`をexact snapshotへbindし、repetition history、territory、facility runtime analysisを同じboardから作る。foreign facility snapshotを拒否し、初期状態は`player_action`、player turn 1、`ongoing`である。
- phaseは`player_action -> enemy_action -> player_action | ended`。黒のalready-authorized placementはplayer phaseで複数回、`EndPlayerTurn`でenemy phaseへ移る。enemy phaseは白のalready-authorized placement1回またはexplicit pass 1回で境界を完了する。wrong actor／phase、stale state checksum、異なるmetadata／履歴のstale log checksum、battle-ended commandはexact no-opとして拒否する。
- placementは既存`HypotheticalPlacementResolver`、real-only `EffectiveLibertySnapshot`、`PlacementLegalityEvaluator`、`BattleRepetitionHistory`、`FacilityPlacementIntegrator`だけをauthorityとする。occupied、suicide、repetition、terminal-capture-requiredはstable reasonで拒否し、board／facility／history／territory／phase／turn／RNG／state checksum／command logを変更しない。
- legal placementのpre-trigger fact順は`StonePlaced -> GroupCaptured[] -> FacilityDestroyed? -> StoneTopologyRegistered -> KingCaptureEvaluated`を維持し、topology observationはexactly one件とする。raw `LegalPlacementCommit`からfacility順を再構成しない。
- old／new `TerritoryAnalysis`のpointwise ownership deltaから、非黒領地から黒領地へ変化した全pointをCanonical orderで持つ`TerritoryEstablished`を一つのatomic resolutionにつき最大1件返す。DECISION-0002に従い、`TerritoryEstablished? -> FacilityDisabled / FacilityActivated[]`の順とし、facility factsはpoint、ordinal instance ID順、`MomentumChanged`は0件とする。
- placement pointのfacility破壊後もremaining facilityだけのbefore／after operating stateを比較するtyped resolver seamを持つ。破壊instanceをtransitionへ戻さず、final `FacilityState`、territory、runtime analysisをnew boardへexact bindする。
- king captureは黒king lossを優先し、白king captureはwin、両king captureはlossとする。terminal placementでもplacement／destruction／topology結果は確定し、territory／facility operating triggerを発行せず、terminal result後のcommandを拒否する。
- enemy action／pass後、player turnが上限未満ならturnを増やしてplayer phaseへ戻す。20回目のplayer turn後のenemy boundaryがongoingなら`PlayerDefeat(reason=turn_limit)`とする。king terminalをturn-limitで上書きしない。turn limitはApplication policy入力であり、コードへruntime値20を複製しない。
- battle stateはversioned canonical projectionとSHA-256 checksumを持ち、phase、player turn、outcome／reason、board topology、full ordered repetition history、facility canonical state、territory ownership projection、authoritative RNG state、turn policyを含む。入力とcommand列が同じならfacts、各boundary state checksum、log checksumが一致し、ambient RNGとunordered outcome traversalを使用しない。
- `AuthorizedStonePlacement`、`EndPlayerTurn`、enemy passはexpected state checksumとexpected prior log checksumを持つversioned canonical commandである。成功したstate-transition commandだけを既存`OrderedCommandLog`へ結果checksum付きでappendし、rejected commandはlogを変更しない。異なるgame version／content hashのsession間でauthorized commandを流用できない。
- public APIはGodot／UI型、filesystem、clock、processへ依存せず、ApplicationはDomain ruleを複製しない。新package／project referenceを追加しない。

## Validation

- Domain testsでterritory deltaのinput order、black establishment、white／neutral change、canonical changed points、facility destruction後のremaining transition、stale／cross-snapshot rejectionを固定する。
- Application testsでinitial state、複数black action、phase／actor／stale rejection、enemy placement／pass、occupied／suicide／repetition no-op、facility trample order、king win／loss、turn 19→20、turn 20 loss、terminal precedenceを固定する。
- 同じinitial state、seed、metadata、ordered commandsを2回実行し、各command boundaryの全fact projection、state checksum、log checksum一致を確認する。command順変更では分岐し、rejection前後はstate／log checksum不変、異なるcontent identityではstale session拒否とする。
- Architecture testsでApplication→Domain境界、Godot／filesystem／ambient RNG非依存、raw board mutationのApplication重複なしを確認する。
- `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を実行し、closeout前に3 commandを連続でもう1回実行する。
- 実装担当とは別のCodexがroot `CODE_REVIEW.md`に従いfixed-HEAD reviewする。

## Execution log

2026-07-11 — PR #10の人間merge commit `f34c89f4c443ce03d25964513a1e9613cdc9dd63`と、post-merge main GitHub Actions run `29149023851`のGovernance、Pure .NET、Godot／export全3 job成功を確認。TASK-0023依存を完了した。

2026-07-11 — Project ownerが[[DECISION-0002 Resolve Territory and Facility Event Order]]の推奨Option 2を選択。[[DECISION-0003 Sequence Golden Replay After Battle State Machine]]はruleを変更しないOption 1で閉じ、TASK-0010→TASK-0009→TASK-0011へ並べ替えた。

2026-07-11 — Outcome、Non-goals、Allowed areas、Acceptance、Validationを具体化。M1のscripted placement state machineへ限定し、Momentum、カード、enemy AI、仮呼吸点、反攻、golden replayを後続taskへ残して`ready`へ遷移。

2026-07-11 — Outcome、Non-goals、Allowed areas、Acceptance、Validationを再確認。Domainのtyped fact／territory delta／facility survivor transition、ApplicationのBattle state／session／command orchestration、Domain／Application／Architecture testsを変更対象として`in_progress`へ遷移。

## Evidence

- TASK-0023 merge commit `f34c89f4c443ce03d25964513a1e9613cdc9dd63`。
- post-merge main GitHub Actions run `29149023851` — Governance job `86535370909`、Pure .NET job `86535387684`、Godot／export job `86535426893`すべて成功。
- 実装Evidenceは未作成。

## Known issues

本タスクのauthorized placementはreal-liberty-onlyであり、仮呼吸点runtimeを実装しない。enemy AI、追加敵行動、カード資源、Momentum、正式golden replay／round tripは明示した後続taskの範囲である。
