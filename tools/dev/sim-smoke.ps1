. "$PSScriptRoot/_Common.ps1"
& "$PSScriptRoot/build.ps1"
Invoke-CheckedNative -FilePath $DotnetBin -ArgumentList @("run", "--project", "tools/Igorogue.Sim.Cli/Igorogue.Sim.Cli.csproj", "-c", "Release", "--no-build", "--no-restore", "--", "--smoke", "--content-manifest", "build/generated_content/content_manifest.json") -FailureMessage "Simulator smoke failed"
