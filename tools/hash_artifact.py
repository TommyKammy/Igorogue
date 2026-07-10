#!/usr/bin/env python3
from __future__ import annotations
import hashlib
import sys
from pathlib import Path

if len(sys.argv) != 2:
    raise SystemExit("Usage: hash_artifact.py <file>")
path = Path(sys.argv[1])
if not path.is_file():
    raise SystemExit(f"Artifact not found: {path}")
digest = hashlib.sha256(path.read_bytes()).hexdigest()
output = path.with_suffix(path.suffix + ".sha256")
output.write_text(f"{digest}  {path.name}\n", encoding="utf-8")
print(f"sha256:{digest}  {path}")
