$ErrorActionPreference = "Stop"
$script:Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$script:PythonBin = if ($env:PYTHON_BIN) { $env:PYTHON_BIN } else { "python" }
$script:DotnetBin = if ($env:DOTNET_BIN) { $env:DOTNET_BIN } else { "dotnet" }
Set-Location $script:Root
