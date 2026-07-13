---
type: system-design
status: accepted
project: Igorogue
updated: 2026-07-13
---
# Deck and Card System

## ゾーン

```text
山札 / 手札 / 解決中 / 捨て札 / 除外
```

- 戦闘開始時にシャッフル。
- 通常ドロー5。
- ターン終了時、手札と解決済みカードを捨て札へ。
- 山札不足時に捨て札を再シャッフル。
- 敵ターン中ドローは原則「次ターン予約ドロー」へ変換。

## カード分類

- 石札：石を置く。
- 手筋札：グループ、呼吸点、配置条件を操作。
- 領域札：領地と施設を操作。
- 触媒札：準備済み盤面を発火。
- 呪札：将来拡張。v0.2では最小限。

## デッキ健全性

- 初期12枚。
- M2 starterは6種類。exact card ID → count multisetは`game_data/content/starting_decks.json`を正本とし、`docs/30_Technical/Schemas/starting_deck.schema.json`で検証する。
- カードのcost、type、placement／target、effect definitionは`game_data/content/cards.json`を正本とし、starting recipeへ複製しない。
- 報酬取得は任意。
- 1ラン1〜2回の削除機会。
- 触媒だけを増やすと起動対象が不足する。
- 基本札は分岐進化し、完全な下位互換にしない。

## 報酬生成

重みの目安：現在ビルド50%、流儀25%、ワイルド25%。二回連続で関連候補がない場合だけ、次回1枠を救済する。
