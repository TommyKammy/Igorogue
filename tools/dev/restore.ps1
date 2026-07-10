. "$PSScriptRoot/_Common.ps1"
Invoke-CheckedNative -FilePath $PythonBin -ArgumentList @("tools/verify_toolchain.py", "--skip-godot") -FailureMessage "Toolchain verification failed"
Invoke-CheckedNative -FilePath $PythonBin -ArgumentList @("tools/check_repository_bootstrap.py", "--strict-locks") -FailureMessage "Repository bootstrap verification failed"
Invoke-CheckedNative -FilePath $DotnetBin -ArgumentList @("restore", "Igorogue.sln", "--locked-mode") -FailureMessage "Locked restore failed"
