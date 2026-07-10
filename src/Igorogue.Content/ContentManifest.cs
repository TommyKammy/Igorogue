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

public sealed record ContentSnapshot(
    string ManifestPath,
    string ContentHash,
    IReadOnlyList<ContentManifestFile> Files);
