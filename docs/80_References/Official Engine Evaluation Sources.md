---
type: reference
status: active
project: Igorogue
updated: 2026-07-10
---
# Official Engine Evaluation Sources

Reviewed on 2026-07-10. Engine releases, pricing, and platform support can change; any future engine upgrade task must re-check the relevant official page.

## Godot

- Official release archive: https://godotengine.org/download/archive/
  - At review time, Godot 4.7 was the current stable line; 4.7.1 was not yet a stable release.
- Command-line and headless operation: https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html
  - Documents `--headless`, command-line project paths, C# solution builds, and command-line export for CI.
- C#/.NET support: https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html
  - Requires the .NET edition of the editor; supports Windows, Linux, and macOS desktop targets; web export is not currently supported for C# projects.
- Multiple resolutions and pixel art: https://docs.godotengine.org/en/stable/tutorials/rendering/multiple_resolutions.html
  - Documents viewport stretch and integer scale mode for consistent pixel art.
- License: https://godotengine.org/license/
  - Godot Engine is distributed under the MIT license.

## Unity

- Unity 6 command-line interface: https://docs.unity3d.com/6000.0/Documentation/Manual/CommandLineArguments.html
- Command-line Player builds: https://docs.unity3d.com/6000.0/Documentation/Manual/build-command-line.html
  - Documents batch mode and CI-oriented builds.
- Release support: https://unity.com/releases/unity-6/support
- 2026 plan and pricing changes: https://unity.com/products/pricing-updates
  - Unity Personal availability depends on the published revenue/funding threshold; Pro has a per-seat subscription price; the former runtime fee was cancelled.

## MonoGame

- MonoGame Foundation and release history: https://monogame.net/about/
  - Describes MonoGame as a free/open-source framework and lists 3.8.4 as the latest stable release at review time.
- Documentation: https://docs.monogame.net/

## Interpretation rule

The facts above establish capabilities and operational constraints. The weighted scores in ADR-0001 remain an Igorogue-specific judgment, not claims made by the engine vendors.
