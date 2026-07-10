---
type: content-design
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Styles

流儀の機械可読正本は`game_data/content/styles.json`。
余勢の共通ルール正本は[[FEAT-002 Momentum]]。

## 全流儀共通の余勢

- 余勢は流儀専用ではなく、全流儀が所持・消費できるglobal battle resource。
- 黒側のatomic resolutionで非黒領地点が黒領地になると、全流儀が余勢+1を得る。
- eligibleな`frontline`石札は、全流儀で余勢を`momentum_reach`へ使用できる。
- 地合い流だけが追加rule `facility_build_grants_momentum`を持つ。他流儀は施設建設だけでは余勢を得ない。

## 地合い流

- 初期入替：囮石を1枚外し、開拓を1枚加える。
- 長所：安定した領地・施設。
- 追加rule `facility_build_grants_momentum`：合法な黒施設の新規建設ごとに余勢+1。一つの建設commandにつき最大1。
- 制約：敵の領地侵入行動の重みが上がる。
- 施設再稼働、奪還、停止解除は新規建設ではなく、この追加sourceを発火しない。

## 攻め碁流

- 初期入替：開拓を1枚外し、ツケを1枚加える。
- 初回ツケ0コスト。
- 初回捕獲で魂+1、1ドロー。
- 黒領地基本収入-1。
- `overextension_counterattack`：同一ターン2枚目以降の正常解決した攻撃札で反攻+8点。一ターン最大+24点。

## 厚み流

- 初期入替：囮石を1枚外し、一間トビを1枚加える。
- 中央最初の石札で1ドロー。
- 隅領地収入-1。
- 安定性が高いが、断ち切りに弱い。

## 捨て石流

- 初期入替：補強を1枚外し、血石を1枚加える。
- 各戦闘最初の実捕獲された自石で次ターン+2ドロー。
- `sacrifice_counterattack`：敵に捕獲された王石以外の自石3個ごとに反攻+15点。余りは戦闘中保持。
- 最低保証で回すのではなく、敵意図を読んで取らせる。

## 反攻ruleの正本

過伸展と犠牲batchの数値・cap・判定順は[[FEAT-003 Komi Counterattack and Heat]]と`game_data/balance/system.json`を正本とする。
