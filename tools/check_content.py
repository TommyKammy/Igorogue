#!/usr/bin/env python3
"""Minimal validation for Igorogue sample content data."""
from __future__ import annotations
from pathlib import Path
import json
import sys

ROOT=Path(__file__).resolve().parents[1]
CONTENT=ROOT/'game_data'/'content'
BALANCE=ROOT/'game_data'/'balance'


def load(path: Path):
    try:
        return json.loads(path.read_text(encoding='utf-8'))
    except Exception as e:
        raise RuntimeError(f'{path.relative_to(ROOT)}: {e}') from e


def main()->int:
    errors=[]
    ids={}
    for path in sorted(CONTENT.glob('*.json')):
        try: data=load(path)
        except Exception as e:
            errors.append(str(e)); continue
        if not isinstance(data,list):
            errors.append(f'{path.relative_to(ROOT)} must contain a list')
            continue
        for idx,item in enumerate(data):
            if not isinstance(item,dict):
                errors.append(f'{path.relative_to(ROOT)}[{idx}] must be object'); continue
            cid=item.get('id')
            if not cid:
                errors.append(f'{path.relative_to(ROOT)}[{idx}] missing id')
            elif cid in ids:
                errors.append(f'duplicate id {cid}: {ids[cid]} and {path.relative_to(ROOT)}')
            else:
                ids[cid]=str(path.relative_to(ROOT))
            if 'cost' in item and (not isinstance(item['cost'],int) or item['cost']<0):
                errors.append(f'{cid}: invalid cost')
            if path.name=='seals.json' and (not isinstance(item.get('komi'),int) or item['komi']<0):
                errors.append(f'{cid}: invalid komi')
    for path in sorted(BALANCE.glob('*.json')):
        try: load(path)
        except Exception as e: errors.append(str(e))
    if errors:
        print('Content checks failed:')
        for e in errors: print('-',e)
        return 1
    print(f'Content checks passed. {len(ids)} unique content ids.')
    return 0

if __name__=='__main__':
    raise SystemExit(main())
