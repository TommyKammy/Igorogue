. "$PSScriptRoot/_Common.ps1"
Invoke-CheckedNative -FilePath $PythonBin -ArgumentList @("tools/verify_toolchain.py") -FailureMessage "Toolchain verification failed"
Invoke-CheckedNative -FilePath $PythonBin -ArgumentList @("tools/content_sync.py", "--check") -FailureMessage "Content verification failed"
if (-not $env:GODOT_BIN) { throw "Set GODOT_BIN to the Godot 4.7 stable .NET executable." }
Invoke-CheckedNative -FilePath $env:GODOT_BIN -ArgumentList @("--headless", "--path", "game/Igorogue.Godot", "--editor", "--build-solutions", "--quit") -FailureMessage "Godot C# build failed"
Invoke-CheckedNative -FilePath $env:GODOT_BIN -ArgumentList @("--headless", "--path", "game/Igorogue.Godot", "--quit-after", "120") -FailureMessage "Godot smoke scene failed"

$replayTempDir = Join-Path ([IO.Path]::GetTempPath()) ("igorogue-replay-smoke-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $replayTempDir | Out-Null
try {
    $lossPath = Join-Path $replayTempDir "loss.json"
    $winPath = Join-Path $replayTempDir "win.json"
    $lossRepeatPath = Join-Path $replayTempDir "loss-repeat.json"
    $winRepeatPath = Join-Path $replayTempDir "win-repeat.json"
    Invoke-CheckedNative -FilePath $env:GODOT_BIN -ArgumentList @(
        "--headless", "--path", "game/Igorogue.Godot", "--quit-after", "120", "--",
        "--graybox-replay-out=$lossPath", "--graybox-replay-scenario=loss"
    ) -FailureMessage "Godot loss replay smoke failed"
    Invoke-CheckedNative -FilePath $env:GODOT_BIN -ArgumentList @(
        "--headless", "--path", "game/Igorogue.Godot", "--quit-after", "120", "--",
        "--graybox-replay-out=$winPath", "--graybox-replay-scenario=win"
    ) -FailureMessage "Godot win replay smoke failed"
    Invoke-CheckedNative -FilePath $env:GODOT_BIN -ArgumentList @(
        "--headless", "--path", "game/Igorogue.Godot", "--quit-after", "120", "--",
        "--graybox-replay-out=$lossRepeatPath", "--graybox-replay-scenario=loss"
    ) -FailureMessage "Godot repeated loss replay smoke failed"
    Invoke-CheckedNative -FilePath $env:GODOT_BIN -ArgumentList @(
        "--headless", "--path", "game/Igorogue.Godot", "--quit-after", "120", "--",
        "--graybox-replay-out=$winRepeatPath", "--graybox-replay-scenario=win"
    ) -FailureMessage "Godot repeated win replay smoke failed"
    if ((Get-Item $lossPath).Length -le 0 -or (Get-Item $winPath).Length -le 0) {
        throw "Godot replay smoke did not write both non-empty Replay V3 artifacts."
    }
    $lossHash = (Get-FileHash -Algorithm SHA256 $lossPath).Hash
    $lossRepeatHash = (Get-FileHash -Algorithm SHA256 $lossRepeatPath).Hash
    $winHash = (Get-FileHash -Algorithm SHA256 $winPath).Hash
    $winRepeatHash = (Get-FileHash -Algorithm SHA256 $winRepeatPath).Hash
    if ($lossHash -ne $lossRepeatHash -or $winHash -ne $winRepeatHash) {
        throw "Repeated Godot replay smoke artifacts were not byte-identical."
    }

    $existingPath = Join-Path $replayTempDir "existing-race.json"
    $existingExpectedPath = Join-Path $replayTempDir "existing.expected"
    [IO.File]::WriteAllText($existingExpectedPath, "do-not-overwrite`n")
    $existingHash = (Get-FileHash -Algorithm SHA256 $existingExpectedPath).Hash
    $failureLog = Join-Path $replayTempDir "existing-race.log"
    $existingRejected = $false
    try {
        Invoke-CheckedNative -FilePath $env:GODOT_BIN -ArgumentList @(
            "--headless", "--path", "game/Igorogue.Godot", "--quit-after", "120", "--",
            "--graybox-replay-out=$existingPath", "--graybox-replay-scenario=existing-target-race"
        ) -FailureMessage "Expected existing replay output rejection" *>&1 |
            Tee-Object -FilePath $failureLog
    }
    catch {
        $existingRejected = $true
    }
    $failureText = Get-Content -Raw $failureLog
    if (-not $existingRejected -or
        -not $failureText.Contains('"verified":false') -or
        -not $failureText.Contains('"reason":"artifact_io_failure"') -or
        $existingHash -ne (Get-FileHash -Algorithm SHA256 $existingPath).Hash -or
        (Get-ChildItem -Path $replayTempDir -Filter ".igorogue-replay-*.tmp").Count -ne 0) {
        throw "Godot replay smoke overwrote or accepted an existing artifact."
    }
}
finally {
    Remove-Item -Recurse -Force $replayTempDir -ErrorAction SilentlyContinue
}
