. "$PSScriptRoot/_Common.ps1"
& $PythonBin tools/verify_toolchain.py
& $PythonBin tools/content_sync.py --check
if (-not $env:GODOT_BIN) { throw "Set GODOT_BIN to the Godot 4.7 stable .NET executable." }
& $env:GODOT_BIN --headless --path game/Igorogue.Godot --editor --build-solutions --quit
& $env:GODOT_BIN --headless --path game/Igorogue.Godot --quit-after 120
