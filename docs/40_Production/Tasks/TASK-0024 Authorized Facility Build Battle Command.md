---
type: task
id: TASK-0024
status: review
project: Igorogue
milestone: M1
priority: high
dependencies: [TASK-0001, TASK-0023, TASK-0010]
updated: 2026-07-11
---
# TASK-0024 Authorized Facility Build Battle Command

## Outcome

既存Domain facility build evaluator／commitをauthorityとして、上流でcostとcard legalityを承認済みの黒施設建設をimmutableなheadless battle sessionへ適用する、versioned canonical Application commandを追加する。FAC-08／09をdirect Domain commitなしでtrue replayできる最小のseamに限定する。

## Source of truth

- [[Rules Canon]]
- [[Architecture]]
- [[Determinism and Replay]]
- [[Command Event Model]]
- [[FEAT-001 Territory and Facilities]]
- [[ADR-0012 Facility Intersection Fixtures]]
- `game_data/fixtures/facility_intersection_fixtures.json`
- [[TASK-0023 Implement Facility Runtime Semantics]]
- [[TASK-0010 Headless Battle State Machine]]
- [[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]]

## Non-goals

- card／cost reserve、on-build effect、Momentum、facility固有効果、UI候補生成。
- live `PlayCard`／UIのatomic commit境界。このheadless fixture seamをそのまま製品commandとして公開せず、将来のorchestrationはFEAT-001のcost→build→on-build→Momentum→checksum順を維持する。
- enemy facility build、enemy AI、turn／card loopの拡張。
- Domainの建設合法性、容量、同名上限、fact順の変更。
- board／territoryの再計算、stone topology／repetition historyの登録。
- golden fixture schema／file生成、replay deserialize／round trip。これらはTASK-0009／0011へ残す。
- Content、`game_data/`、package／project reference、Godot asset変更。

## Allowed areas

- `src/Igorogue.Application/Battle/`のcanonical authorized facility build commandとstate-machine dispatch。
- `tests/Igorogue.Application.Tests/`、`tests/Igorogue.Architecture.Tests/`のcommand、determinism、boundary tests。
- 本TASK、関連Decision、production state文書のstatus／Evidence同期。
- Domain、Content、`game_data/`、package／project reference、Godot assetは変更しない。

## Acceptance criteria

- `AuthorizedFacilityBuild`はexpected state checksum、expected prior log checksum、canonical point、facility content ID、instance IDを持つversion 1 commandで、payloadは黒playerによる承認済み建設を一義に表す。
- ongoing `player_action`だけがcommandを受け付ける。wrong phase、stale state／log、terminal battleは既存stable reasonで拒否する。
- 建設合法性とcommitは`FacilityBuildEvaluator.Evaluate`／`Commit`だけをauthorityとし、Applicationへcapacity、ownership、同名上限を複製しない。既存instance IDはDomain evaluatorが例外とするcommand identity衝突なので、Application boundaryでstable `facility_instance_exists`として拒否する。
- illegal target stone、occupied facility、非所有領地、capacity full、type limit、duplicate instanceは`CommandRejected`だけを返し、board、facility、runtime analysis、territory、repetition history、phase、turn、outcome、RNG、state checksum、command logをexact no-opとする。
- legal buildは`FacilityBuilt -> FacilityActivated(reason=built_in_controlled_territory)`をDomain順のまま返す。board、territory、repetition history、phase、turn、outcome、RNGを変えず、facility state／runtime analysisとstate checksumを更新する。
- 成功commandだけを`OrderedCommandLog`へ結果state checksum付きでexactly one件appendする。accepted command payloadのpoint／content ID／instance IDまたはaccepted入力順が異なれば、canonical command／log checksumは決定論的に分岐する。rejected attemptだけの差はaccepted-only logを変えない。
- 同じinitial state、metadata、seed、command列を2回実行すると、facts、各boundary state checksum、log checksumが一致する。
- public APIはGodot／UI型、filesystem、clock、process、ambient RNGへ依存せず、新package／project referenceを追加しない。

## Validation

- Application testsでlegal build、全Domain rejection reason、duplicate instance、wrong phase、stale／terminal rejection、accepted-only log、board／history／territory／RNG不変、fact順を固定する。
- 同一script 2回のfact projection、state checksum、log checksum一致と、content／instance／command順変更時の分岐を確認する。
- Architecture testsでApplication→Domain境界とhost／Godot非依存を確認する。
- `tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を実行し、closeout前に3 commandを連続でもう1回実行する。
- 実装担当とは別のCodexがroot `CODE_REVIEW.md`に従いfixed-HEAD reviewする。

## Execution log

2026-07-11 — PR #11の人間merge commit `d4ffd832f572fb46cbe2d29559032b30c68b2bb2`とpost-merge main GitHub Actions run `29151240059`の全3 job成功を確認。TASK-0010依存を完了した。

2026-07-11 — 到達性監査により、FAC-08／09に不足する正規Application build seamをTASK-0009から分離。[[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]]のfixture evidence分類はopenのまま、どのoptionでも必要なAccepted facility build ruleだけをDomain API変更なしの狭いtaskとして新規作成した。

2026-07-11 — Outcome、Non-goals、Allowed areas、Acceptance、ValidationをRules Canon、FEAT-001、ADR-0012、TASK-0010へ照合。open decisionを先取りせず、headless fixture用の黒施設build seamに限定して`ready`へ遷移した。

2026-07-11 — 変更予定をApplication Battle command／dispatch、Application tests、既存Architecture guards、production state文書に限定し、検証方法を再確認して`in_progress`へ遷移した。

2026-07-11 — `AuthorizedFacilityBuildCommand` v1とstate-machine dispatchを実装。canonical payloadはactor black、point、content ID、instance ID、expected state／log checksumを保持し、stable ID validationはDomain `FacilityBuildRequest`へ委譲した。

2026-07-11 — legal buildをDomain `FacilityBuildEvaluator.Evaluate`／`Commit`へ委譲し、`FacilityBuilt -> FacilityActivated`、accepted-only log、board／territory／history／phase／turn／outcome／RNG不変を実装。全Domain rejection reason、duplicate identity、phase／stale／terminal、determinism／divergence testsを追加した。

2026-07-11 — 一次validationで`tools/dev/check`と`tools/dev/test`を実行し成功。Release build warning 0／error 0、Domain 190、Application 33、Architecture 15の合計238 testがgreen。

2026-07-11 — fixed HEAD `a326328`を実装担当とは別のCodexがroot `CODE_REVIEW.md`に従って独立review。Rules Canon、FEAT-001、ADR-0012、Architecture、Determinism、FAC fixture、全Acceptanceへ照合し、finding 0、`APPROVE`。独立check／test／sim smokeもgreen。

2026-07-11 — closeout validationで`tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`を二連続実行し全成功。TASK-0024範囲の既知defectなしとして`review`へ遷移し、CIと人間merge判断を待つ。

## Evidence

- TASK-0010 merge commit `d4ffd832f572fb46cbe2d29559032b30c68b2bb2`。
- post-merge main GitHub Actions run `29151240059` — Governance `86540969301`、Pure .NET `86540988089`、Godot／export `86541037361`すべて成功。
- `tools/dev/check` — documentation、wikilink、content、design fixture、repository governanceすべて成功。content snapshot `sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06`。
- `tools/dev/test` — exact .NET SDK `8.0.422`、locked restore、Release build warning 0／error 0。Domain 190、Application 33、Architecture 15、合計238 test成功。
- `AuthorizedFacilityBuildCommandTests` — canonical payload、legal fact順／snapshot不変、6 rejection reason exact no-op、phase／stale／terminal、accepted-only log、2-run determinism、payload／order divergenceを確認。
- independent fixed-HEAD review — commit `a326328`、BLOCKER／HIGH／MEDIUM／LOW findingなし、独立`check`／238 tests／sim smoke成功、`APPROVE`。
- closeout validation 2 consecutive runs — 各runで`tools/dev/check`、`tools/dev/test`、`tools/dev/sim-smoke`がexit 0。各回238 tests、warning 0／error 0、同一sim checksum `3b59c2c2c2f20ec64af8a325a38ea48e7647935fa4a90c06ce2251e49879bcdd`。

## Known issues

TASK-0024範囲の既知defectはなし。TASK-0009は本taskのCI／人間mergeとopen [[DECISION-0004 Separate Exact Fixtures from Reachable Battle Replays]]解決まで`blocked`を維持する。card cost、on-build effect、Momentum、enemy buildは本commandの上流または後続taskで扱う。
