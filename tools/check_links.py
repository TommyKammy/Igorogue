#!/usr/bin/env python3
"""Best-effort Obsidian wikilink checker."""
from __future__ import annotations
from pathlib import Path
import re

ROOT=Path(__file__).resolve().parents[1]
DOCS=ROOT/'docs'


def main()->int:
    files=[p for p in ROOT.rglob('*') if p.is_file()]
    names={p.name for p in files}
    stems={p.name.rsplit('.',1)[0] for p in files}
    missing=[]
    pattern=re.compile(r'!?\[\[([^\]]+)\]\]')
    for path in DOCS.rglob('*.md'):
        text=path.read_text(encoding='utf-8')
        for raw in pattern.findall(text):
            target=raw.split('|',1)[0].split('#',1)[0].strip()
            if not target: continue
            base=Path(target).name
            if base in names or base in stems or f'{base}.md' in names:
                continue
            missing.append((path.relative_to(ROOT),target))
    if missing:
        print('Wikilink checks failed:')
        for path,target in missing:
            print(f'- {path}: [[{target}]]')
        return 1
    print('Wikilink checks passed.')
    return 0

if __name__=='__main__':
    raise SystemExit(main())
