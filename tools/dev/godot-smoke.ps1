. "$PSScriptRoot/_Common.ps1"
Invoke-CheckedNative -FilePath $PythonBin -ArgumentList @("tools/verify_toolchain.py") -FailureMessage "Toolchain verification failed"
Invoke-CheckedNative -FilePath $PythonBin -ArgumentList @("tools/content_sync.py", "--check") -FailureMessage "Content verification failed"
if (-not $env:GODOT_BIN) { throw "Set GODOT_BIN to the Godot 4.7 stable .NET executable." }
Invoke-CheckedNative -FilePath $env:GODOT_BIN -ArgumentList @("--headless", "--path", "game/Igorogue.Godot", "--editor", "--build-solutions", "--quit") -FailureMessage "Godot C# build failed"
Invoke-CheckedNative -FilePath $env:GODOT_BIN -ArgumentList @("--headless", "--path", "game/Igorogue.Godot", "--quit-after", "120") -FailureMessage "Godot smoke scene failed"
