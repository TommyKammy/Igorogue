$ErrorActionPreference = "Stop"
$script:Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$script:PythonBin = if ($env:PYTHON_BIN) { $env:PYTHON_BIN } else { "python" }
$script:DotnetBin = if ($env:DOTNET_BIN) { $env:DOTNET_BIN } else { "dotnet" }
Set-Location $script:Root

function Invoke-CheckedNative {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList,
        [Parameter(Mandatory = $true)][string]$FailureMessage
    )

    & $FilePath @ArgumentList
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "$FailureMessage (exit code $exitCode)."
    }
}
