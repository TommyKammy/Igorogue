using Igorogue.Application.Bootstrap;
using Igorogue.Content;

namespace Igorogue.Sim.Cli;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"IGOROGUE_SIM_ERROR {exception.GetType().Name}: {exception.Message}");
            return 1;
        }
    }

    private static int Run(IReadOnlyList<string> args)
    {
        if (!args.Contains("--smoke", StringComparer.Ordinal))
        {
            Console.WriteLine("Usage: Igorogue.Sim.Cli --smoke [--content-manifest <path>]");
            return 2;
        }

        var manifestPath = ReadOption(args, "--content-manifest")
            ?? Environment.GetEnvironmentVariable("IGOROGUE_CONTENT_MANIFEST")
            ?? Path.Combine("build", "generated_content", "content_manifest.json");

        var snapshot = new ContentManifestLoader().Load(manifestPath);
        var service = new BootstrapApplicationService();
        var first = service.Run(snapshot.ContentHash);
        var second = service.Run(snapshot.ContentHash);

        if (!string.Equals(first.Checksum, second.Checksum, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("IGOROGUE_SIM_NON_DETERMINISTIC");
            return 3;
        }

        Console.WriteLine(
            $"IGOROGUE_SIM_SMOKE checksum={first.Checksum} content={first.ContentHash} files={snapshot.Files.Count}");
        return 0;
    }

    private static string? ReadOption(IReadOnlyList<string> args, string option)
    {
        for (var index = 0; index < args.Count; index++)
        {
            if (!string.Equals(args[index], option, StringComparison.Ordinal))
            {
                continue;
            }

            if (index + 1 >= args.Count)
            {
                throw new ArgumentException($"Missing value for {option}.");
            }

            return args[index + 1];
        }

        return null;
    }
}
