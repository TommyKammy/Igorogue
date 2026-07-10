---
type: reference
status: active
project: Igorogue
updated: 2026-07-10
---
# Official Codex and Obsidian Links

参照日：2026-07-10。機能は更新され得るため、実運用前に公式ページを再確認する。

## OpenAI Codex

- Codex overview: https://developers.openai.com/codex
- Codex app: https://developers.openai.com/codex/app
- App worktrees: https://developers.openai.com/codex/app/worktrees
- AGENTS.md: https://developers.openai.com/codex/guides/agents-md
- Best practices: https://developers.openai.com/codex/learn/best-practices
- Codex CLI: https://developers.openai.com/codex/cli

## Obsidian

- Core plugins: https://obsidian.md/help/plugins
- Bases: https://obsidian.md/help/bases
- Tags: https://obsidian.md/help/tags
- CSS snippets: https://obsidian.md/help/snippets

このVaultは必須コミュニティプラグインなしで動く設計にしている。

## Igorogueで採用する公式運用要点

- Codex app worktrees require a Git repository and are suited to one isolated task at a time.
- `AGENTS.md` is loaded before work; nested instructions closer to the working directory extend/override broader guidance.
- Changes should be tested and independently reviewed before acceptance; use the diff panel or `/review` where available.
- Local is preferred for the first host/toolchain gate and human visual checks; worktrees are preferred for subsequent isolated implementation tasks.
