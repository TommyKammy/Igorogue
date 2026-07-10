. "$PSScriptRoot/_Common.ps1"
& $PythonBin tools/verify_toolchain.py --skip-godot
& $PythonBin tools/check_repository_bootstrap.py --strict-locks
& $DotnetBin restore Igorogue.sln --locked-mode
