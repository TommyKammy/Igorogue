. "$PSScriptRoot/_Common.ps1"
& "$PSScriptRoot/restore.ps1"
& $DotnetBin build Igorogue.sln -c Release --no-restore
