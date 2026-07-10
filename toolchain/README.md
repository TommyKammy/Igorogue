# Igorogue Toolchain Contract

Machine-readable contracts:

- `engine_decision.json`: Accepted engine comparison and architecture decision
- `bootstrap_manifest.json`: exact SDK, Godot, xUnit, renderer, and export pins

Human rationale:

- `docs/60_Decisions/ADRs/ADR-0001 Engine and Repository.md`
- `docs/30_Technical/Engine Toolchain and Repository Layout.md`
- `docs/30_Technical/Repository Bootstrap Status.md`

Validate static contracts:

```bash
python3 tools/check_engine_decision.py
python3 tools/check_repository_bootstrap.py
```

Validate installed tools:

```bash
python3 tools/verify_toolchain.py
```
