---
type: task
id: TASK-0010
status: done
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

2026-07-11 — Domainへ共通`IBattleFact`、一atomic resolution最大1件の`TerritoryEstablishedFact`、placement commitへexact bindしたterritory delta、施設踏破後のsurvivor-only reassociationを実装。placement／facility factを一つのtyped ordered seamへ統合した。

2026-07-11 — Applicationへimmutable `BattleState`／`HeadlessBattleSession`、already-authorized placement／player turn end／enemy pass command、phase／turn-limit／terminal orchestration、versioned canonical state checksum、accepted-command-only `OrderedCommandLog`接続を実装した。

2026-07-11 — Domain／Application／Architecture testsでexact snapshot、pre-trigger順、選択済み`TerritoryEstablished -> facility transition`順、illegal exact no-op、king terminal suppression、20-turn boundary、state／log determinism、host isolationを固定した。

2026-07-11 — adversarial auditで、異なるcontent identity間のauthorized command流用、terminal outcome／reason整合のApplication重複、same-Geometry stale territory snapshot、per-boundary fact比較不足を検出。fix commit `0911c30d8b586cfae4be6ef93a0b345b499a8d26`でexpected prior log checksum、Domain-owned `BattleEndReasonRules`、`FacilityPlacementCommit` exact binding、全boundary fact projectionへ修正し、`terminal_capture_required`、正式turn 20、king precedence、terminal facility trample testも追加した。

2026-07-11 — fixed HEAD `0911c30d8b586cfae4be6ef93a0b345b499a8d26`を実装担当とは別のCodexがroot `CODE_REVIEW.md`に従って独立review。全Acceptance、scope、determinism、event order、facility survivor seam、testsを照合し、finding 0、`APPROVE`。独立check／test／sim smokeもgreen。

2026-07-11 — closeout validationで`tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を連続実行し全成功。実装範囲の既知defectなしとして`review`へ遷移し、CIと人間merge判断を待つ。

2026-07-11 — draft PR #11のGitHub Actions run `29150792085`でGovernance、Pure .NET build／232 tests／simulator smoke、Godot .NET headless smoke／Windows debug exportの全3 job成功を確認。`review`を維持し、人間merge判断だけを待つ。

2026-07-11 — PR #11の人間merge commit `d4ffd832f572fb46cbe2d29559032b30c68b2bb2`を確認。post-merge main GitHub Actions run `29151240059`も全3 job成功のため、`done`へ遷移した。

## Evidence

- TASK-0023 merge commit `f34c89f4c443ce03d25964513a1e9613cdc9dd63`。
- post-merge main GitHub Actions run `29149023851` — Governance job `86535370909`、Pure .NET job `86535387684`、Godot／export job `86535426893`すべて成功。
- implementation commits — `4c7ad02904758feedd385bf15ccce1f05eb7e7f1` Domain ordered facts／delta／survivor transition、`a2c1309` Application state machine、`9c4cac6` architecture guards、`77d4eaa` integration tests、`0911c30d8b586cfae4be6ef93a0b345b499a8d26` adversarial review fixes。
- `tools/dev/check`をpost-fixとcloseoutで実行 — 両方exit 0。documentation、wikilink、content、全design fixture／governance check成功。content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`。
- `tools/dev/test`をpost-fix独立reviewとcloseoutで実行 — 両方exit 0。exact .NET SDK `8.0.422`、locked restore、Release build warning 0／error 0。最終Domain 190、Application 27、Architecture 15、合計232 test成功。
- `tools/dev/sim-smoke`をpost-fix独立reviewとcloseoutで実行 — 両方exit 0。同一`checksum=3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`、同一content hash、`files=7`。
- `TerritoryDeltaResolverTests`／`FacilityOperatingTransitionTests` — exact placement source／result、same-Geometry stale rejection、white-source diagnostic delta、survivor-only transition、destroyed instance exclusionを確認。
- `HeadlessBattleStateMachineTests` — initial binding、facility trample、selected event order、occupied／suicide／repetition／terminal grant rejection、phase／actor／state／session stale、king terminal、20-turn loss、terminal precedence、各boundary fact／state／log determinismを確認。
- `ArchitectureBoundaryTests` — shared fact seam、non-forgeable delta／state result、accepted placement dependency、Application→Domain、Godot／filesystem／clock／process／ambient RNG非露出を確認。
- independent fixed-HEAD review — `0911c30d8b586cfae4be6ef93a0b345b499a8d26`、BLOCKER／HIGH／MEDIUM／LOW findingなし、独立validation green、`APPROVE`。
- GitHub Actions run `29150792085` — PR #11のGovernance `86539852257`、Pure .NET `86539868985`、Godot／export `86539916578`すべて成功。
- PR #11 human merge commit `d4ffd832f572fb46cbe2d29559032b30c68b2bb2`。
- post-merge main GitHub Actions run `29151240059` — Governance `86540969301`、Pure .NET `86540988089`、Godot／export `86541037361`すべて成功。
- Content、`game_data/`、package／project reference、Godot assetの変更なし。

## Known issues

TASK-0010範囲の既知defectはなし。

本タスクのauthorized placementはreal-liberty-onlyであり、仮呼吸点runtimeを実装しない。enemy AI、追加敵行動、カード資源、Momentum、正式golden replay／round tripは明示した後続taskの範囲である。
