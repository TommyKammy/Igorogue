using Godot;
using Igorogue.Application.Bootstrap;
using Igorogue.Content;
using Igorogue.Godot.CoreDuel;

using System.Globalization;

namespace Igorogue.Godot.Smoke;

public partial class BootstrapSmoke : Node
{
    private const string DefaultGameVersion = "v0.2.10";
    private const long DefaultGrayboxSeed = 39039L;

    public override void _Ready()
    {
        try
        {
            var manifestPath = ProjectSettings.GlobalizePath(
                "res://generated_content/content_manifest.json");
            var snapshot = new ContentManifestLoader().Load(manifestPath);
            var service = new BootstrapApplicationService();
            var first = service.Run(snapshot.ContentHash);
            var second = service.Run(snapshot.ContentHash);

            if (!string.Equals(first.Checksum, second.Checksum, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Bootstrap checksum changed within one process.");
            }

            GD.Print(
                $"IGOROGUE_GODOT_SMOKE checksum={first.Checksum} content={first.ContentHash} files={snapshot.Files.Count}");

            var options = GrayboxLaunchOptions.Parse(OS.GetCmdlineUserArgs());
            if (StringComparer.Ordinal.Equals(DisplayServer.GetName(), "headless"))
            {
                var grayboxChecksum = CoreDuelGameHost.RunHeadlessSmoke(
                    manifestPath,
                    options.GameVersion,
                    options.Seed,
                    options.ReplayOutputPath,
                    options.ReplayScenario ?? "loss");
                if (grayboxChecksum.ReplayEvidence is { } evidence)
                {
                    GD.Print(evidence.ToConsoleLine());
                    if (!evidence.Verified)
                    {
                        throw new InvalidOperationException(
                            $"Replay evidence failed closed: {evidence.ReasonId}.");
                    }
                }

                GD.Print(
                    $"IGOROGUE_GRAYBOX_SMOKE checksum={grayboxChecksum.Checksum} seed={options.Seed.ToString(CultureInfo.InvariantCulture)}");
                GetTree().Quit(0);
                return;
            }

            if (options.ReplayScenario is not null)
            {
                throw new ArgumentException(
                    "The graybox replay scenario option is headless-smoke only.");
            }

            var host = CoreDuelGameHost.Create(
                manifestPath,
                options.GameVersion,
                options.Seed,
                options.ReplayOutputPath);
            var graybox = new CoreDuelGraybox
            {
                Name = "CoreDuelGraybox",
                CapturePath = options.CapturePath,
            };
            graybox.Initialize(host);
            if (options.CaptureSelected)
            {
                graybox.PrepareCaptureSelection();
            }

            AddChild(graybox);
        }
        catch (Exception exception)
        {
            GD.PushError($"IGOROGUE_GODOT_SMOKE_FAILED {exception}");
            GetTree().Quit(1);
        }
    }

    private sealed record GrayboxLaunchOptions(
        string GameVersion,
        long Seed,
        string? CapturePath,
        bool CaptureSelected,
        string? ReplayOutputPath,
        string? ReplayScenario)
    {
        public static GrayboxLaunchOptions Parse(IEnumerable<string> arguments)
        {
            var gameVersion = DefaultGameVersion;
            var seed = DefaultGrayboxSeed;
            string? capturePath = null;
            var captureSelected = false;
            string? replayOutputPath = null;
            var replayOutputSeen = false;
            string? replayScenario = null;
            var replayScenarioSeen = false;
            foreach (var argument in arguments)
            {
                if (argument.StartsWith("--graybox-version=", StringComparison.Ordinal))
                {
                    gameVersion = argument["--graybox-version=".Length..];
                }
                else if (argument.StartsWith("--graybox-seed=", StringComparison.Ordinal))
                {
                    var rawSeed = argument["--graybox-seed=".Length..];
                    if (!long.TryParse(
                            rawSeed,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out seed))
                    {
                        throw new ArgumentException(
                            $"Invalid graybox seed: {rawSeed}.",
                            nameof(arguments));
                    }
                }
                else if (argument.StartsWith("--capture-graybox=", StringComparison.Ordinal))
                {
                    capturePath = argument["--capture-graybox=".Length..];
                }
                else if (StringComparer.Ordinal.Equals(
                             argument,
                             "--graybox-capture-selected"))
                {
                    captureSelected = true;
                }
                else if (argument.StartsWith(
                             "--graybox-replay-out=",
                             StringComparison.Ordinal))
                {
                    if (replayOutputSeen)
                    {
                        throw new ArgumentException(
                            "The graybox replay output option may be supplied only once.",
                            nameof(arguments));
                    }

                    replayOutputSeen = true;
                    replayOutputPath = CoreDuelReplayEvidenceRecorder.ValidateOutputPath(
                        argument["--graybox-replay-out=".Length..]);
                }
                else if (argument.StartsWith(
                             "--graybox-replay-scenario=",
                             StringComparison.Ordinal))
                {
                    if (replayScenarioSeen)
                    {
                        throw new ArgumentException(
                            "The graybox replay scenario option may be supplied only once.",
                            nameof(arguments));
                    }

                    replayScenarioSeen = true;
                    replayScenario = argument["--graybox-replay-scenario=".Length..];
                    if (replayScenario is not "loss" and not "win" and
                        not "existing-target-race")
                    {
                        throw new ArgumentException(
                            "The graybox replay scenario must be 'loss', 'win', or 'existing-target-race'.",
                            nameof(arguments));
                    }
                }
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(gameVersion);
            if (capturePath is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(capturePath);
            }

            if (capturePath is not null && replayOutputPath is not null)
            {
                throw new ArgumentException(
                    "Screenshot capture and terminal Replay V3 capture cannot share one launch.",
                    nameof(arguments));
            }

            if (replayScenario is not null && replayOutputPath is null)
            {
                throw new ArgumentException(
                    "The graybox replay scenario requires an explicit replay output path.",
                    nameof(arguments));
            }

            return new GrayboxLaunchOptions(
                gameVersion,
                seed,
                capturePath,
                captureSelected,
                replayOutputPath,
                replayScenario);
        }
    }
}
