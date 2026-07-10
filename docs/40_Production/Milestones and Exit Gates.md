---
type: milestone-plan
status: accepted
project: Igorogue
updated: 2026-07-10
---
# Milestones and Exit Gates

## M-1 Design Repair

- 敵行動、盤面反復、施設点、座標、余勢、反攻のP0仕様修復
- 人間2名が同一盤面を同一順序で紙上解決
- Rules Kernelが設計判断を推測しなくてよい状態

## M0 Project Bootstrap

- [[ADR-0001 Engine and Repository]]に従うGodot 4.7 .NET／C# solution
- pure .NET test、formal simulator smoke、Godot headless smoke、Windows debug export
- pinned tool verifier、content sync、CI
- Codex TASK一件完遂

## M1 Headless Rules Kernel

- 盤面、呼吸点、捕獲、領地
- seed、コマンドログ、リプレイ
- UIなし一戦処理

## M2 v0.1.1 Graybox

- 7×7UI
- 初期6カード
- 山賊棋士
- 意図、アタリ、捕獲、領地表示

## M3 Acceleration Lab

- 2流儀
- 12〜16カード
- 遺物4
- 施設、余勢、触媒、反攻、妙手
- 仕込み→発火→反攻が人間プレイで成立

## M4 v0.2 Vertical Slice

- [[Integrated v0.2 Scope]]
- 4戦Act
- 正式シミュレーションと外部テスト

## M5 Meta and Onboarding

- 棋譜片、横解放、詰碁、段位、図鑑

## M6 Demo Candidate

- アート、音、セーブ、配布、外部テスト
