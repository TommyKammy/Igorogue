---
type: decision-needed
id: DECISION-0009
status: resolved
blocking: []
updated: 2026-07-13
---
# DECISION-0009 Resolve Bandit Multi-Group Capture Target Reference

## Problem

`capture_non_king`候補の1手が複数の非王石黒グループを同時捕獲する場合、[[FEAT-009 Enemy Action Planning and Placement]]の単一`target_ref`がどのグループを指すか定義されていない。

この選択は、表示する対象輪郭、候補score第2項、実行時のplanned-target成立判定、same-intent retarget／fallback、telemetryの`target_ref_before / after`を変える。実装都合でcanonical anchor最小のグループを選ぶことは、player-visible behaviorの決定になる。

## Conflicting or missing sources

- [[Combat Resolution Order]]は、1手で呼吸点0になった複数の相手グループを同時捕獲すると定める。
- FEAT-009は石グループtargetを「計画時のグループに属する最小座標の石」で参照するが、同時捕獲される複数グループからprimary targetを選ぶ規則を持たない。
- `capture_non_king`は第1scoreとして同時捕獲する黒石総数を使い、第2scoreを「捕獲対象と黒王石グループとの最小マンハッタン距離」とするが、「捕獲対象」が単一primary groupか全captured groupsかを定めない。
- 本Decision解決前の[[FEAT-009 Enemy Decision Fixtures]] F09-02と`game_data/fixtures/enemy_behavior_decision_fixtures.json`の距離key `1`／`0`は、別々の同色グループと黒王石グループのliteral stone-to-stone Manhattan距離として到達不能だった。距離1なら直交連結して同一グループ、距離0なら石が重なるためである。期待winnerは第1scoreの捕獲数だけで決まるが、当該表は完全な盤面をbindしないpre-ranked E1 comparator fixtureであり、実盤面由来E3値とは扱えない。

## Smallest decisions required

1. 複数group同時捕獲時のprimary target選択規則。
2. `capture_non_king` score第2項をprimary groupだけから測るか、全captured groupsから測るか。
3. primary groupだけが実行前に消滅し、別の当初captured groupが残る場合にplanned targetを維持するか、same-intent retargetとするか。
4. UIがprimary groupだけを囲むか、同時捕獲対象を複数表示するか。
5. F09-02の距離keyを正本規則へ整合させるか、pre-ranked E1 comparator値として明示的に置換するか。

## Decision

Option 1を採用する。

- 複数の非王石黒グループを同時捕獲する`capture_non_king`候補では、捕獲石数が最も多いグループをprimary targetとする。
- 同数なら、黒王石グループまでのstone-to-stone最小Manhattan距離が短いグループ、さらに同率ならgroup anchorのCanonical point orderでprimary targetを決める。
- `capture_non_king`候補scoreの第2項は、primary target groupと黒王石グループのstone-to-stone最小Manhattan距離とする。
- primary target anchorが実行前に対象条件を失った場合、当初同時捕獲される別グループが残っていてもplanned targetは消失したものとし、same-intent retargetへ進む。
- UIはtarget outlineとしてprimary groupだけを囲む。候補rankingの第1項は従来どおり同時捕獲する黒石総数を使う。
- F09-02の距離keyは`3`／`2`へ置換し、完全な盤面をbindしないpre-ranked E1 comparator inputと明記する。TASK-0037では、別の到達可能な盤面からproduction kernelがscoreを導くE3 testを追加する。

## Options

### Option 1 — Largest captured group is the primary target（Selected）

- 捕獲石数が多いgroupをprimary targetとし、同数なら黒王石groupまでの距離が短いgroup、さらに同率ならgroup anchorのCanonical point orderで決める。
- score第2項はprimary target groupと黒王石groupのstone-to-stone最小Manhattan距離を使う。
- primary anchor stoneが対象条件を失った場合、別の当初captured groupが残っていてもsame-intent retargetとする。
- UIはprimary groupをtarget outlineし、primary／alternate placement previewは同時捕獲総数を別情報として保持する。

最大の捕獲価値を生む主対象を表示でき、既存の単一`target_ref` schemaを維持する。primary選択用の明示的tie-breakが増える。

### Option 2 — Canonical-first captured group is the primary target

- 同時捕獲groupのうちanchorがCanonical point orderで最初のgroupをprimary targetとする。
- score第2項、target invalidation、UIはOption 1と同じくprimary groupだけを基準にする。

実装と再現は最小だが、表示対象が最大捕獲価値や黒王石への近さを表さない場合がある。

### Option 3 — Composite capture target

- `target_ref`をcanonicalに並べた複数group anchorの集合へ拡張し、score第2項は集合中の全石から黒王石groupまでの最小距離を使う。
- 実行時の有効性とUI outlineも複数targetを扱う。

同時捕獲の意味を最も忠実に表示できる一方、FEAT-009のsingular target contract、facts、telemetry、preview、後続replay schemaへ広い変更が必要になる。

## Selected option and rationale

Option 1は既存の単一target contractを保ちつつ、playerへ最も捕獲石数の多いgroupを主対象として示せる。Option 2より捕獲価値と表示対象の対応が明確で、Option 3よりTASK-0037／0039のstateとreplay変更を限定できる。

## Consequences

- [[TASK-0037 Implement Bandit Intent Planning and Execution]]の仕様blockは解除され、candidate projection、planned target、retarget／fallback、F09-02 E3 migrationを再開できる。
- TASK-0038〜0042は各々の依存TASK完了まで引き続きblockedである。
- FEAT-009、Enemy Design and Intent、fixture Markdown／JSONを本Decisionと同じ変更単位で整合させる。

## Evidence available

- PR #26 merged head `3b0cec3c4327803f05b7dda5447912cab6e1ed95`、merge commit `45ca3f8cb3cb4cc4c8ade45273c38c76e08f8f73`、post-merge main CI run `29228982431`全3 job success。
- TASK-0037 read-only source audit。既存runtime placement／effective-liberty／repetition／facility／territory kernelは再利用可能で、仕様決定後に共通Domain evaluatorへ抽出できる。
- baseline `tools/dev/test` exit 0。Domain 324、Application 147、Architecture 58、計529 tests pass。
- `tools/check_enemy_behaviors.py`はpre-ranked E1候補比較であり、board legality、target、execution、facts、checksumを検証しない。

## Owner decision

Project ownerは2026-07-13、「最大グループを主対象にする（推奨）。同数なら王石への距離、次にcanonical anchorで決定。を選択します」と明示した。本Decisionは提示済みOption 1全体をresolvedな正本とする。
