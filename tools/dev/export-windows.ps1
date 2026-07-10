. "$PSScriptRoot/_Common.ps1"
& "$PSScriptRoot/godot-smoke.ps1"
if (-not $env:GODOT_BIN) { throw "Set GODOT_BIN to the Godot 4.7 stable .NET executable." }
$ExportRoot = Join-Path $Root "build/exports/windows"
$ManagedRoot = Join-Path $ExportRoot "data_Igorogue.Godot_windows_x86_64"
if (Test-Path $ExportRoot) { Remove-Item -Recurse -Force $ExportRoot }
New-Item -ItemType Directory -Force $ExportRoot | Out-Null
$ExportPath = Join-Path $ExportRoot "Igorogue.exe"
Invoke-CheckedNative -FilePath $env:GODOT_BIN -ArgumentList @("--headless", "--path", "game/Igorogue.Godot", "--export-debug", "Windows Debug", $ExportPath) -FailureMessage "Godot Windows export failed"
Invoke-CheckedNative -FilePath "git" -ArgumentList @("diff", "--quiet", "--", ":(glob)**/packages.lock.json") -FailureMessage "Godot export modified committed NuGet lock files"
if (-not (Test-Path (Join-Path $ExportRoot "Igorogue.exe") -PathType Leaf)) { throw "Windows export executable is missing." }
if (-not (Test-Path $ManagedRoot -PathType Container)) { throw "Windows .NET export data directory is missing." }
if (-not (Test-Path (Join-Path $ManagedRoot "Igorogue.Godot.dll") -PathType Leaf)) { throw "Windows .NET export assembly is missing." }
Invoke-CheckedNative -FilePath $PythonBin -ArgumentList @("tools/hash_artifact.py", $ExportPath) -FailureMessage "Windows export hashing failed"
