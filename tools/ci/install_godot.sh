#!/usr/bin/env bash
set -euo pipefail
ROOT=$(cd "$(dirname "$0")/../.." && pwd)
CACHE_ROOT=${RUNNER_TEMP:-$ROOT/.cache}/igorogue-godot
mkdir -p "$CACHE_ROOT"

readarray -t VALUES < <(python3 - <<'PY_GODOT_MANIFEST'
import json
from pathlib import Path
manifest = json.loads(Path('toolchain/bootstrap_manifest.json').read_text(encoding='utf-8'))
print(manifest['godot']['linux_dotnet_url'])
print(manifest['godot']['dotnet_templates_url'])
print(manifest['godot']['template_directory'])
PY_GODOT_MANIFEST
)
GODOT_URL=${VALUES[0]}
TEMPLATES_URL=${VALUES[1]}
TEMPLATE_DIRECTORY=${VALUES[2]}
GODOT_ZIP="$CACHE_ROOT/godot-dotnet.zip"
TEMPLATES_ZIP="$CACHE_ROOT/godot-templates.tpz"
GODOT_EXTRACT="$CACHE_ROOT/editor"
TEMPLATE_ROOT="$HOME/.local/share/godot/export_templates/$TEMPLATE_DIRECTORY"

curl --fail --location --retry 3 "$GODOT_URL" --output "$GODOT_ZIP"
rm -rf "$GODOT_EXTRACT"
mkdir -p "$GODOT_EXTRACT"
python3 - "$GODOT_ZIP" "$GODOT_EXTRACT" <<'PY_UNZIP_EDITOR'
import sys, zipfile
with zipfile.ZipFile(sys.argv[1]) as archive:
    archive.extractall(sys.argv[2])
PY_UNZIP_EDITOR

GODOT_BIN=$(find "$GODOT_EXTRACT" -type f \( -name 'Godot*_mono_linux*.x86_64' -o -name 'Godot*_mono_linux*.64' \) -print -quit)
if [[ -z "$GODOT_BIN" ]]; then
  echo 'Could not locate the Godot .NET executable after extraction.' >&2
  exit 1
fi
chmod +x "$GODOT_BIN"

curl --fail --location --retry 3 "$TEMPLATES_URL" --output "$TEMPLATES_ZIP"
rm -rf "$TEMPLATE_ROOT"
mkdir -p "$TEMPLATE_ROOT"
python3 - "$TEMPLATES_ZIP" "$TEMPLATE_ROOT" <<'PY_UNZIP_TEMPLATES'
import sys, zipfile
from pathlib import Path
archive_path, destination = Path(sys.argv[1]), Path(sys.argv[2])
with zipfile.ZipFile(archive_path) as archive:
    names = archive.namelist()
    for name in names:
        if name.endswith('/'):
            continue
        relative = Path(name)
        parts = relative.parts
        if parts and parts[0] == 'templates':
            relative = Path(*parts[1:])
        if not relative.parts:
            continue
        target = destination / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(archive.read(name))
PY_UNZIP_TEMPLATES

printf 'GODOT_BIN=%s\n' "$GODOT_BIN" >> "${GITHUB_ENV:?GITHUB_ENV is required in CI}"
printf 'Installed Godot: %s\n' "$GODOT_BIN"
