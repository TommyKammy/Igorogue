. "$PSScriptRoot/_Common.ps1"
& $PythonBin tools/verify_toolchain.py --skip-godot
& $PythonBin tools/content_sync.py --write
& $DotnetBin restore Igorogue.sln --use-lock-file --force-evaluate
& $PythonBin tools/check_repository_bootstrap.py --strict-locks
Write-Host "Lock files generated. Review and commit every packages.lock.json before using locked restore."
