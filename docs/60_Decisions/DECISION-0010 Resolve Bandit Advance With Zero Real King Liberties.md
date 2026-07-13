---
type: decision-needed
id: DECISION-0010
status: resolved
blocking: []
updated: 2026-07-13
---
# DECISION-0010 Resolve Bandit Advance With Zero Real King Liberties

## Why a decision is needed

[[FEAT-009 Enemy Action Planning and Placement]]は`advance_toward_black_king`を「白前線の合法点がある」場合に選ぶ一方、第1scoreを「配置点から黒王石グループの実呼吸点までの最小Manhattan距離」とする。

[[Rules Canon]]ではtimed仮呼吸点とcontinuous modifierを実呼吸点へ加えた値が有効呼吸点である。したがって、黒王石groupの実呼吸点が0でも有効呼吸点が1以上ならgroupは生存でき、別の場所に白前線合法点が残る到達可能stateがある。このstateではselection conditionを満たすが、空の実呼吸点集合に対する第1scoreが未定義である。

TASK-0037の暫定実装は実呼吸点0のときadvance候補を生成せずpassするが、これはAccepted conditionを実装都合で狭めるplayer-visible behaviorになる。

## Decision

Option 1を採用する。

- 黒王石groupの実呼吸点が1点以上なら、配置点からその実呼吸点集合までの最小Manhattan距離を第1scoreに使う。
- 黒王石groupの実呼吸点が0点で、timed／continuous込みの有効呼吸点により生存する場合は、配置点から黒王石groupに属する石までの最小Manhattan距離へfallbackする。
- 後続scoreの「配置後白group有効呼吸点」「盤中央までの距離」、最終Canonical point tie-breakは変更しない。
- このfallbackは、実呼吸点が1点以上ある通常stateの候補生成とrankingを変更しない。

## Options

1. **Recommended:** 実呼吸点が1点以上なら現行どおりその集合までの最小Manhattan距離を使い、0点なら黒王石groupの石までの最小Manhattan距離へfallbackする。後続scoreとCanonical point tie-breakは変更しない。
2. 実呼吸点0では`advance_toward_black_king`を不成立とし、他の意図もなければpassする。FEAT-009のselection conditionへ「黒王石groupの実呼吸点が1点以上」を追加する。
3. 実呼吸点0では第1scoreを全候補同率として扱い、配置後白group有効呼吸点、盤中央距離、Canonical point orderだけでrankingする。

## Selected option and rationale

Option 1は「捕獲機会がなければ黒王石へ最短で近づく」という山賊棋士のteaching resultを保ち、実呼吸点が存在する通常stateのrankingを一切変えない。Option 2は一時的な仮呼吸点だけで山賊をpassさせ、Option 3は黒王石への前進より盤中央や白group安全度を優先し得る。

## Implementation boundary

- 共通placement／effective-liberty／repetition／facility／territory evaluator。
- 実呼吸点が1点以上あるstateのBandit候補、ranking、planned target、retarget／fallback、normal／bonus lifecycle。
- DECISION-0009 Option 1のmulti-group primary target evidence。

## Consequences

- 実呼吸点0かつ有効呼吸点1以上の黒王石groupがあるstateでも、白前線合法点があれば`advance_toward_black_king`は候補を生成する。
- 実呼吸点0のstateful E3 testは、黒王石groupの石までの最小Manhattan距離、後続score、Canonical point orderが順に適用されることを確認する。
- TASK-0037の仕様blockは解除され、実装修正、full closeout suite、fixed-HEAD reviewへ進める。
- TASK-0038以降は各依存TASKとDECISION-0006の条件を引き続き満たす必要がある。

## Evidence available

- 実呼吸点0／有効呼吸点正の到達可能stateは、Rules Canonのtimed仮呼吸点とcontinuous modifierの定義から構成できる。
- Option 1実装前のTASK-0037統合baselineは`tools/dev/build` warning 0／error 0、`tools/dev/test` Domain 340、Application 156、Architecture 60の計556 tests pass。
- Option 1実装後のcloseoutは`tools/dev/test` Domain 347、Application 158、Architecture 60の計565 tests pass。real=0／effective=1のE3 planner regression、通常F09-01不変、入力反転時のplan／checksum一致を確認した。

## Owner decision

Project ownerは2026-07-13、「DECISION-0010もOption 1で進めて」と明示した。本Decisionは提示済みOption 1全体をresolvedな正本とする。
