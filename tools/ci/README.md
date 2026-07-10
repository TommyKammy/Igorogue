# CI helper scripts

`install_godot.sh` installs the exact Godot .NET editor and matching export templates declared by `toolchain/bootstrap_manifest.json`. It is intended for ephemeral Linux GitHub Actions runners.

The install script never changes the Accepted version. Upgrade the manifest only through a successor ADR.
