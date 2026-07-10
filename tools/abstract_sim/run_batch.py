#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import json
from pathlib import Path

from model import Skill, STYLES, LOADOUTS, simulate_run, summarize


def main() -> int:
    p = argparse.ArgumentParser(description="Run Igorogue's non-authoritative abstract design proxy.")
    p.add_argument("--runs", type=int, default=1000)
    p.add_argument("--seed-start", type=int, default=10000)
    p.add_argument("--skill", choices=[s.value for s in Skill], default=Skill.EXPERT.value)
    p.add_argument("--style", choices=sorted(STYLES), default="territory")
    p.add_argument("--loadout", choices=sorted(LOADOUTS), default="territory_4")
    p.add_argument("--json", type=Path)
    p.add_argument("--csv", type=Path)
    args = p.parse_args()

    skill = Skill(args.skill)
    results = [simulate_run(args.seed_start + i, skill, args.style, args.loadout) for i in range(args.runs)]
    summary = summarize(results)
    print(json.dumps(summary, ensure_ascii=False, indent=2))

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps({"summary": summary, "runs": [r.to_dict() for r in results]}, ensure_ascii=False, indent=2), encoding="utf-8")
    if args.csv:
        args.csv.parent.mkdir(parents=True, exist_ok=True)
        with args.csv.open("w", newline="", encoding="utf-8") as f:
            writer = csv.DictWriter(f, fieldnames=list(results[0].to_dict()))
            writer.writeheader()
            writer.writerows(r.to_dict() for r in results)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
