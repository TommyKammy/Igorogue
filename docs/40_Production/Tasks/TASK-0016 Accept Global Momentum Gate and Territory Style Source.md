---
type: task
id: TASK-0016
status: done
project: Igorogue
milestone: M-1
priority: critical
dependencies: [TASK-0015]
updated: 2026-07-12
---
# TASK-0016 Accept Global Momentum Gate and Territory Style Source

## Outcome

余勢が全流儀共通の戦闘資源であること、普遍生成源、地合い流の追加施設source、使用可能札、距離2の配置gate、前線前進ドローを一義化し、Rules Canon、Styles、runtime data、fixtureが同じ契約を参照する状態にする。

## Source of truth

- [[Rules Canon]]
- [[FEAT-002 Momentum]]
- [[Styles]]
- `game_data/balance/system.json`
- `game_data/content/styles.json`
- [[FEAT-002 Momentum Gate Fixtures]]

## Non-goals

- Rules Kernelの製品コード実装
- 反攻数値の再設計
- A-6で扱う攻め碁流等の全rule同期
- 余勢上限を変える遺物
- 敵用余勢
- 斜め・曲線・2交点超の余勢配置
- M3での楽しさ検証

## Acceptance criteria

- 余勢は全流儀共通の一つのbattle resourceである。
- 初期0、最大2、ターンをまたいで保持、戦闘境界で0へ戻る。
- 全流儀が黒領地成立で+1を得る。一つのatomic resolutionで最大1。
- 地合い流だけが合法な黒施設建設で+1を得る。
- `Rules Canon`、`Styles.md`、`styles.json`が同じgate範囲を記述する。
- eligible cardは`stone`＋印刷`frontline`＋黒石1個配置である。
- `momentum_reach`は上下左右distance 2、中間Stone layer空、通常配置優先である。
- 不合法・キャンセルでは余勢、気、カードzoneを変更しない。
- 前線前進ドローの距離定義、解決後収入4、各ターン1回が一義的である。
- MOM-01〜MOM-19が専用checkerを通る。

## Allowed areas

- `docs/20_Design/Rules Canon.md`
- `docs/20_Design/Momentum and Acceleration.md`
- `docs/20_Design/Styles.md`
- `docs/20_Design/Feature Specs/FEAT-001*`
- `docs/20_Design/Feature Specs/FEAT-002*`
- 関連UI、technical、validation文書
- `game_data/balance/system.json`
- `game_data/content/styles.json`
- `game_data/fixtures/momentum_gate_fixtures.json`
- `tools/check_momentum.py`
- `tools/check_all.py`
- 進捗、索引、manifest、release note

## Validation

```bash
python tools/check_momentum.py
python tools/check_all.py
```

## Execution log

### 2026-07-10

- FEAT-002を定型文からAccepted完全仕様へ置換。
- 余勢を全流儀共通global battle resourceとして固定。
- 普遍sourceを黒領地成立、地合い流追加sourceを合法な施設建設へ分離。
- eligible cardと`momentum_reach`のdistance 2・中間空点・通常配置優先を固定。
- 前線前進ドローを最短王石距離、解決後予測収入4、各ターン1回で固定。
- system data、Styles、FEAT-001、Domain、Event、UI、Telemetryを同期。
- MOM-01〜MOM-19と専用仕様checkerを追加。

## Evidence

- `python tools/check_momentum.py`: PASS。
- `python tools/check_all.py`: PASS。既存座標、敵、盤面反復、施設、抽象代理testに回帰なし。
- MOM-13の期待消費量を意図的に変更し、専用checkerがFAILする負のテスト後、元へ戻して再PASS。

## Known issues

- checkerは仕様モデルであり、製品Rules Kernelではない。M1で同fixtureを共有Kernelへ移植する。
- 2026-07-12 disposition: 上記M1記述は完了時の計画を保存する歴史的記録。[[DECISION-0005 Resolve M1 Golden Migration and Gate 2 Entry]] Option 1により、現行のMOM-01〜19 production unit／golden migrationはM3に配置する。
- 地合い流の施設sourceが強すぎるかはM3で検証する。
- 余勢前進ドローが定石化を促す可能性があるため、発火率と初手分布を計測する。
- A-6の全流儀rule同期とA-7反攻再設計は別タスク。
