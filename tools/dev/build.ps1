. "$PSScriptRoot/_Common.ps1"
& "$PSScriptRoot/restore.ps1"
Invoke-CheckedNative -FilePath $DotnetBin -ArgumentList @("build", "Igorogue.sln", "-c", "Release", "--no-restore") -FailureMessage "Release build failed"
