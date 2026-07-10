# AGENTS.md — Tests and golden evidence

These instructions extend the repository root guidance for `tests/`.

- Do not weaken expected results to make an implementation pass.
- Rule-order behavior should use fixture/golden cases with explicit commands and checksums.
- Include negative, boundary, simultaneous-resolution, and rejected-command cases.
- Tests must not depend on unordered iteration, wall-clock time, machine paths, or network access.
- A rejected command must prove no unintended state/RNG mutation when required by specification.
- Preserve reproduction seeds and content hashes for regressions.
