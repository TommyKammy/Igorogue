using System.Text.Json.Serialization;

namespace Igorogue.Content;

public sealed record ContentManifest(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("content_hash")] string ContentHash,
    [property: JsonPropertyName("files")] IReadOnlyList<ContentManifestFile> Files);

public sealed record ContentManifestFile(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("sha256")] string Sha256,
    [property: JsonPropertyName("bytes")] long Bytes);

public sealed class ContentSnapshot
{
    private readonly Dictionary<string, byte[]> verifiedContent;

    internal ContentSnapshot(
        string manifestPath,
        string contentHash,
        IEnumerable<ContentManifestFile> files,
        Dictionary<string, byte[]> verifiedContent)
    {
        ManifestPath = manifestPath;
        ContentHash = contentHash;
        Files = Array.AsReadOnly(files.ToArray());
        this.verifiedContent = verifiedContent;
    }

    public string ManifestPath { get; }

    public string ContentHash { get; }

    public IReadOnlyList<ContentManifestFile> Files { get; }

    internal ReadOnlyMemory<byte> RequiredContent(string relativePath)
    {
        if (!verifiedContent.TryGetValue(relativePath, out var content))
        {
            throw new InvalidDataException(
                $"Required generated content file is missing from manifest: {relativePath}.");
        }

        return content;
    }
}
