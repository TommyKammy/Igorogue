using System.Security.Cryptography;
using System.Text;
using Igorogue.Application.Bootstrap;
using Igorogue.Content;

namespace Igorogue.Application.Tests;

public sealed class BootstrapApplicationTests
{
    private const string ContentHash =
        "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void ApplicationSmokeIsDeterministic()
    {
        var service = new BootstrapApplicationService();

        var first = service.Run(ContentHash);
        var second = service.Run(ContentHash);

        Assert.Equal(first, second);
    }

    [Fact]
    public void InvalidContentHashIsRejected()
    {
        var service = new BootstrapApplicationService();
        Assert.Throws<ArgumentException>(() => service.Run("not-a-content-hash"));
    }

    [Fact]
    public void ManifestLoaderReadsAndAuthenticatesCanonicalContent()
    {
        using var fixture = ContentFixture.Create("{}\n");

        var snapshot = new ContentManifestLoader().Load(fixture.ManifestPath);

        Assert.Equal(fixture.ContentHash, snapshot.ContentHash);
        Assert.Single(snapshot.Files);
    }

    [Fact]
    public void ManifestLoaderRejectsTamperedContent()
    {
        using var fixture = ContentFixture.Create("{}\n");
        File.WriteAllText(fixture.ContentPath, "{\"tampered\":true}\n", new UTF8Encoding(false));

        Assert.Throws<InvalidDataException>(
            () => new ContentManifestLoader().Load(fixture.ManifestPath));
    }

    private sealed class ContentFixture : IDisposable
    {
        private ContentFixture(string root, string manifestPath, string contentPath, string contentHash)
        {
            Root = root;
            ManifestPath = manifestPath;
            ContentPath = contentPath;
            ContentHash = contentHash;
        }

        public string Root { get; }
        public string ManifestPath { get; }
        public string ContentPath { get; }
        public string ContentHash { get; }

        public static ContentFixture Create(string content)
        {
            var root = Path.Combine(Path.GetTempPath(), $"igorogue-{Guid.NewGuid():N}");
            var contentPath = Path.Combine(root, "files", "balance", "system.json");
            var manifestPath = Path.Combine(root, "content_manifest.json");
            Directory.CreateDirectory(Path.GetDirectoryName(contentPath)!);

            var bytes = new UTF8Encoding(false).GetBytes(content);
            File.WriteAllBytes(contentPath, bytes);
            var fileHash = $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";
            var aggregateHash = ContentHash;
            File.WriteAllText(
                manifestPath,
                $$"""
                {
                  "schema_version": 1,
                  "content_hash": "{{aggregateHash}}",
                  "files": [
                    {
                      "path": "balance/system.json",
                      "sha256": "{{fileHash}}",
                      "bytes": {{bytes.Length}}
                    }
                  ]
                }
                """,
                new UTF8Encoding(false));

            return new ContentFixture(root, manifestPath, contentPath, aggregateHash);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
