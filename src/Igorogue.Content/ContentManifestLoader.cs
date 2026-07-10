using System.Security.Cryptography;
using System.Text.Json;

namespace Igorogue.Content;

public sealed class ContentManifestLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
    };

    public ContentSnapshot Load(string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        var fullPath = Path.GetFullPath(manifestPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Generated content manifest was not found.", fullPath);
        }

        using var stream = File.OpenRead(fullPath);
        var manifest = JsonSerializer.Deserialize<ContentManifest>(stream, JsonOptions)
            ?? throw new InvalidDataException("Generated content manifest is empty.");

        ValidateManifest(manifest);
        ValidateFiles(fullPath, manifest);
        return new ContentSnapshot(fullPath, manifest.ContentHash, manifest.Files);
    }

    private static void ValidateManifest(ContentManifest manifest)
    {
        if (manifest.SchemaVersion != 1)
        {
            throw new InvalidDataException(
                $"Unsupported content manifest schema: {manifest.SchemaVersion}.");
        }

        ValidateHash(manifest.ContentHash, "content_hash");

        var previousPath = string.Empty;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in manifest.Files)
        {
            var segments = file.Path.Split('/', StringSplitOptions.None);
            if (string.IsNullOrWhiteSpace(file.Path) ||
                file.Path.Contains('\\') ||
                Path.IsPathRooted(file.Path) ||
                segments.Any(segment => segment is "" or "." or ".."))
            {
                throw new InvalidDataException($"Invalid manifest path: '{file.Path}'.");
            }

            if (!seen.Add(file.Path))
            {
                throw new InvalidDataException($"Duplicate manifest path: '{file.Path}'.");
            }

            if (string.CompareOrdinal(previousPath, file.Path) > 0)
            {
                throw new InvalidDataException("Manifest files must be sorted by ordinal path.");
            }

            ValidateHash(file.Sha256, $"files[{file.Path}].sha256");
            if (file.Bytes < 0)
            {
                throw new InvalidDataException($"Negative byte length for '{file.Path}'.");
            }

            previousPath = file.Path;
        }
    }

    private static void ValidateFiles(string manifestPath, ContentManifest manifest)
    {
        var manifestDirectory = Path.GetDirectoryName(manifestPath)
            ?? throw new InvalidDataException("Manifest path has no parent directory.");
        var filesRoot = Path.GetFullPath(Path.Combine(manifestDirectory, "files"));

        foreach (var file in manifest.Files)
        {
            var relativeParts = file.Path.Split('/');
            var contentPath = Path.GetFullPath(
                Path.Combine(new[] { filesRoot }.Concat(relativeParts).ToArray()));
            var relative = Path.GetRelativePath(filesRoot, contentPath);
            if (Path.IsPathRooted(relative) ||
                relative.Equals("..", StringComparison.Ordinal) ||
                relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Manifest path escapes content root: '{file.Path}'.");
            }

            if (!File.Exists(contentPath))
            {
                throw new FileNotFoundException(
                    $"Generated content file was not found: '{file.Path}'.", contentPath);
            }

            var info = new FileInfo(contentPath);
            if (info.Length != file.Bytes)
            {
                throw new InvalidDataException(
                    $"Generated content length mismatch for '{file.Path}': expected {file.Bytes}, got {info.Length}.");
            }

            using var stream = File.OpenRead(contentPath);
            var actualHash = $"sha256:{Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant()}";
            if (!string.Equals(actualHash, file.Sha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Generated content hash mismatch for '{file.Path}'.");
            }
        }
    }

    private static void ValidateHash(string value, string fieldName)
    {
        const string prefix = "sha256:";
        if (string.IsNullOrWhiteSpace(value) ||
            !value.StartsWith(prefix, StringComparison.Ordinal) ||
            value.Length != prefix.Length + 64 ||
            !value[prefix.Length..].All(Uri.IsHexDigit))
        {
            throw new InvalidDataException(
                $"{fieldName} must use sha256:<64 hex digits>.");
        }
    }
}
