---
type: start-here
status: active
project: Igorogue
updated: 2026-07-10
---
# Igorogue（仮）— START HERE

Igorogueは、**囲碁の呼吸点・捕獲・領地**を、デッキ構築ローグライトと拡大再生産へ接続するPCゲームです。

このパッケージは、Obsidianで開ける設計Vault、Codex向けの開発統制、UI/UXモックアップ、データ設計例、再構築した抽象シミュレーターをまとめたものです。

## 最初の5ステップ

1. ルート`CODEX_MAC_HANDOFF.md`を読む。
2. `docs/` をObsidian Vaultとして開き、[[Igorogue Project Hub]]をホームに設定する。
3. [[Codex Mac Handoff]]、[[Current Development State]]、[[Source of Truth Map]]を読む。
4. [[macOS Development Host Setup]]に従ってexact toolchainを確認する。
5. Codex AppのLocal taskで`handoff/FIRST_PROMPT.txt`を実行し、成功後に[[TASK-0022 Bootstrap macOS Host and Close Runtime Evidence]]へ進む。

## 重要な注意

- `tools/abstract_sim/` は設計比較用の代理モデルであり、製品ロジックではありません。
- 過去の会話内シミュレーション値は、元コードとseedが残っていないため「設計上の履歴」として保存しています。
- 製品版では、ライブゲームとヘッドレスシミュレーターが同じRules Kernelを使用します。
- Balatroは反復性とルール改変の参考ですが、画面構成、用語、アート、演出はIgorogue独自にします。

## 主要入口

- [[Igorogue Project Hub]]
- [[Game Design Overview]]
- [[Integrated v0.2 Scope]]
- [[UI UX Overview]]
- [[Development Plan]]
- [[Codex Mac Handoff]]
- [[Codex App Operating Procedure]]
- [[Codex Operating Model]]
- [[Abstract Proxy Model]]
- [[Validation Strategy]]
