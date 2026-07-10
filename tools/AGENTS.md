# AGENTS.md — Tools and simulation

These instructions extend the repository root guidance for `tools/`.

- Formal simulation must use the same Application/Domain boundary as the live game.
- `tools/abstract_sim/` is an E2 proxy only; never present it as product Rules Kernel evidence.
- Tool output must be deterministic for identical inputs unless explicitly testing randomness distribution.
- Preserve exact commands, seeds, content hashes, and exit codes in evidence.
- Generators own generated files; fix the generator instead of hand-editing output.
- Negative tests must prove governance checkers can fail on malformed inputs.
