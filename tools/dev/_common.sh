#!/usr/bin/env sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
PYTHON_BIN=${PYTHON_BIN:-python3}
DOTNET_BIN=${DOTNET_BIN:-dotnet}
export ROOT PYTHON_BIN DOTNET_BIN
cd "$ROOT"
