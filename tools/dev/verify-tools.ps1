. "$PSScriptRoot/_Common.ps1"
$arguments = @("tools/verify_toolchain.py") + @($args)
Invoke-CheckedNative -FilePath $PythonBin -ArgumentList $arguments -FailureMessage "Toolchain verification failed"
