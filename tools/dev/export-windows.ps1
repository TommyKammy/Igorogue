. "$PSScriptRoot/_Common.ps1"
& "$PSScriptRoot/godot-smoke.ps1"
if (-not $env:GODOT_BIN) { throw "Set GODOT_BIN to the Godot 4.7 stable .NET executable." }
New-Item -ItemType Directory -Force build/exports/windows | Out-Null
& $env:GODOT_BIN --headless --path game/Igorogue.Godot --export-debug "Windows Debug" "$Root/build/exports/windows/Igorogue.exe"
& $PythonBin tools/hash_artifact.py build/exports/windows/Igorogue.exe
