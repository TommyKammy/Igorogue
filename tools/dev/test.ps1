. "$PSScriptRoot/_Common.ps1"
& "$PSScriptRoot/build.ps1"
& $DotnetBin test Igorogue.sln -c Release --no-build --no-restore
