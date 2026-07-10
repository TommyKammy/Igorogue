#!/usr/bin/env python3
from __future__ import annotations
import json
from pathlib import Path
from model import Skill, STYLES, LOADOUTS, simulate_run, summarize

OUT = Path(__file__).resolve().parent / "out"
OUT.mkdir(exist_ok=True)
rows=[]
seed=200000
for skill in Skill:
    for style in STYLES:
        for loadout in LOADOUTS:
            results=[simulate_run(seed+i, skill, style, loadout) for i in range(250)]
            seed += 1000
            row={"skill":skill.value,"style":style,"loadout":loadout,**summarize(results)}
            rows.append(row)
(OUT/'matrix_summary.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(rows[:4],ensure_ascii=False,indent=2))
print(f'wrote {OUT / "matrix_summary.json"}')
