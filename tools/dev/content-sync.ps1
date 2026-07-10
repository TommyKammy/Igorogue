. "$PSScriptRoot/_Common.ps1"
$arguments = @("tools/content_sync.py", "--write") + @($args)
Invoke-CheckedNative -FilePath $PythonBin -ArgumentList $arguments -FailureMessage "Content synchronization failed"
