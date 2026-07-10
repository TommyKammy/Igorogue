. "$PSScriptRoot/../dev/_Common.ps1"

$failureObserved = $false
try {
    Invoke-CheckedNative -FilePath $PythonBin -ArgumentList @("-c", "import sys; sys.exit(17)") -FailureMessage "Expected probe failure"
}
catch {
    if ($_.Exception.Message -notmatch "exit code 17") {
        throw
    }
    $failureObserved = $true
}

if (-not $failureObserved) {
    throw "Invoke-CheckedNative accepted a failing native command."
}

Invoke-CheckedNative -FilePath $PythonBin -ArgumentList @("-c", "import sys; sys.exit(0)") -FailureMessage "Expected probe success"
Write-Output "PowerShell native fail-fast checks passed."
