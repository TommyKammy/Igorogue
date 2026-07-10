. "$PSScriptRoot/_Common.ps1"
& "$PSScriptRoot/build.ps1"
Invoke-CheckedNative -FilePath $DotnetBin -ArgumentList @("test", "Igorogue.sln", "-c", "Release", "--no-build", "--no-restore") -FailureMessage "Test execution failed"
