using Godot;
using Igorogue.Application.Bootstrap;
using Igorogue.Content;

namespace Igorogue.Godot.Smoke;

public partial class BootstrapSmoke : Node
{
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
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"IGOROGUE_GODOT_SMOKE_FAILED {exception}");
            GetTree().Quit(1);
        }
    }
}
