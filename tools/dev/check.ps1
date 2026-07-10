. "$PSScriptRoot/_Common.ps1"
& $PythonBin tools/check_all.py
& $PythonBin tools/content_sync.py --check
