---
type: spec-fixtures
status: accepted
project: Igorogue
updated: 2026-07-10
feature: FEAT-011
---
# FEAT-011 Temporary Liberty Expiry Fixtures

機械可読正本は`game_data/fixtures/temporary_liberty_expiry_fixtures.json`。盤面図は上段`y=7`、点とevent順はCanonical point orderを使用する。

| ID | 主検証 |
|---|---|
| TLE-01 | +1失効で実呼吸点0の黒単石をcapture |
| TLE-02 | 同一groupの複数effectを一括失効しcaptureは一度 |
| TLE-03 | 離れた複数groupを同一snapshotでanchor順capture |
| TLE-04 | future timed effectが残れば生存 |
| TLE-05 | 霊泉continuous modifierが残れば生存 |
| TLE-06 | effectがanchor stoneを含む結合groupへ追従 |
| TLE-07 | 黒王石失効captureで利益前に敗北 |
| TLE-08 | 両王石同時captureは敗北 |
| TLE-09 | 囮石・血石・流儀・印のclosed-window trigger順 |
| TLE-10 | 白group失効captureの黒capture rewardと予約／DeferredChoice |
| TLE-11 | mandatory captureが既出topologyへ戻っても実行 |
| TLE-12 | 新黒領地は次turn収入へ反映するが暗黙余勢・妙手なし |
| TLE-13 | due effectなしでは掃引eventなし |
| TLE-14 | 通常行動・反攻追加行動の後、基礎反攻増加の前に失効 |
| TLE-15 | 犠牲反攻を失効trigger後、敵ターン終了基礎増加前に適用 |

## 共通期待

- effect eventはCreatedSequence / ID順。
- group captureはgroup anchor順。
- capture候補は全effect失効後・石除去前の一snapshotで確定。
- terminal batchはbenefitを抑止。
- fixture checkerは仕様代理であり、M1 Rules Kernelの代替ではない。
