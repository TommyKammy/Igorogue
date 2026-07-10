#!/usr/bin/env python3
from __future__ import annotations
import subprocess, sys
commands=[
 [sys.executable,'tools/check_docs.py'],
 [sys.executable,'tools/check_links.py'],
 [sys.executable,'tools/check_content.py'],
 [sys.executable,'tools/check_coordinate_system.py'],
 [sys.executable,'tools/check_enemy_behaviors.py'],
 [sys.executable,'tools/check_board_repetition.py'],
 [sys.executable,'tools/check_facility_semantics.py'],
 [sys.executable,'tools/check_momentum.py'],
 [sys.executable,'tools/check_counterattack.py'],
 [sys.executable,'tools/check_temporary_liberty.py'],
 [sys.executable,'tools/check_engine_decision.py'],
 [sys.executable,'tools/check_repository_bootstrap.py'],
 [sys.executable,'tools/check_codex_handoff.py'],
 [sys.executable,'-m','unittest','discover','-s','tools/tests','-v'],
 [sys.executable,'-m','unittest','discover','-s','tools/abstract_sim/tests'],
]
for cmd in commands:
    print('+',' '.join(cmd))
    result=subprocess.run(cmd)
    if result.returncode:
        raise SystemExit(result.returncode)
print('All checks passed.')
