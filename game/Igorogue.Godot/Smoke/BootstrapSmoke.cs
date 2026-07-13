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
                    options.Seed);
                GD.Print(
                    $"IGOROGUE_GRAYBOX_SMOKE checksum={grayboxChecksum} seed={options.Seed.ToString(CultureInfo.InvariantCulture)}");
                GetTree().Quit(0);
                return;
            }

            var host = CoreDuelGameHost.Create(
                manifestPath,
                options.GameVersion,
                options.Seed);
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
        bool CaptureSelected)
    {
        public static GrayboxLaunchOptions Parse(IEnumerable<string> arguments)
        {
            var gameVersion = DefaultGameVersion;
            var seed = DefaultGrayboxSeed;
            string? capturePath = null;
            var captureSelected = false;
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
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(gameVersion);
            if (capturePath is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(capturePath);
            }

            return new GrayboxLaunchOptions(
                gameVersion,
                seed,
                capturePath,
                captureSelected);
        }
    }
}
