---
type: task
id: TASK-0017
status: done
project: Igorogue
milestone: M-1
priority: critical
dependencies: [TASK-0016]
updated: 2026-07-10
---
# TASK-0017 Redesign Counterattack Curve

## Outcome

コミ0〜9の全帯で20ターン内に反攻へ到達可能とし、妙手x3、攻めの過伸展、捨て石の大量犠牲を同一の可視ゲージへ接続する。閾値到達、overflow、Pending、敵追加行動、戦闘resetを一義化する。

## Source of truth

- [[Rules Canon]]
- [[FEAT-003 Komi Counterattack and Heat]]
- [[ADR-0013 Baseline Pace and Burst-Driven Counterattack]]
- [[BAL-0001 Counterattack Curve v0.2.6]]
- `game_data/balance/system.json`
- [[FEAT-003 Counterattack Curve Fixtures]]

## Non-goals

- 白王石厚みトークンの数量式
- 正式Rules Kernelの製品コード
- A-6の捕獲報酬等を含む全Styles同期
- 反攻遭遇率の確定
- 敵反攻意図内容の再設計
- UI最終アート

## Acceptance criteria

- 2 units = 1表示点の固定小数点表現を採用する。
- コミ0〜9の開始値と敵ターン終了増加が線形式である。
- 他sourceなしでもコミ0の追加行動が敵ターン16以内、コミ9が敵ターン8以内である。
- 熱は全コミ帯で妙手x3初回crossingに一度だけ発生する。
- 攻め碁流の過伸展と捨て石流の犠牲batchが一義的である。
- 一敵ターンの反攻追加行動は最大1で、overflowを保存する。
- 戦闘開始・終了でゲージ、Pending、流儀counterをresetする。
- `Rules Canon`、FEAT、ADR、BAL、system dataが一致する。
- CTR-01〜CTR-25が専用checkerを通る。

## Allowed areas

- 反攻・熱・コミに関するdesign、technical、UI、validation文書
- `game_data/balance/system.json`
- `game_data/content/styles.json`の反攻rule ID
- `game_data/fixtures/counterattack_curve_fixtures.json`
- `tools/check_counterattack.py`
- `tools/check_all.py`
- 進捗、索引、manifest、release note

## Validation

```bash
python tools/check_counterattack.py
python tools/check_all.py
```

## Execution log

### 2026-07-10

- 旧`start=4K / gain=K`を廃止し、baseline paceを追加。
- 2 units = 1点の固定小数点表現を採用。
- コミ0〜9の自然到達表を作成。
- 熱をコミ7以上限定から全コミx3初回crossingへ変更。
- 過伸展、犠牲batch、Pending、overflow、resetをFEAT-003で仕様化。
- ADR-0013をAcceptedし、ADR-0008をsupersededへ変更。
- BAL-0001へ前後比較とM3計測目標を保存。
- CTR-01〜CTR-25と専用checkerを追加。

## Evidence

- `python tools/check_counterattack.py`: PASS。
- `python tools/check_all.py`: PASS。
- CTR-01の期待追加行動ターンを意図的に変更すると専用checkerがFAILし、復元後に再PASSすることを確認。

## Known issues

- 数値証拠はE1机上計算であり、正式盤面と人間の面白さを証明しない。
- 熱24〜42点が強すぎる可能性はM3で検証する。
- 高コミ速攻が反攻前に取り切る支配戦略は正式Kernelで追試が必要。
- 白王石厚みの数量式は別P0/P1タスクで確定する。
