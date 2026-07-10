. "$PSScriptRoot/_Common.ps1"
Invoke-CheckedNative -FilePath $PythonBin -ArgumentList @("tools/check_all.py") -FailureMessage "Repository governance checks failed"
Invoke-CheckedNative -FilePath $PythonBin -ArgumentList @("tools/content_sync.py", "--check") -FailureMessage "Content verification failed"
