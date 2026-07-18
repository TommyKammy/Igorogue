#!/usr/bin/env sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
PYTHON_BIN=${PYTHON_BIN:-python3}
if [ -z "${DOTNET_BIN:-}" ]; then
    unset DOTNET_BIN
    DOTNET_BIN=dotnet
fi
export ROOT PYTHON_BIN
cd "$ROOT"
