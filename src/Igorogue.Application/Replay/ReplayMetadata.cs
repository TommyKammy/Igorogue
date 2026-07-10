using System.Globalization;
using Igorogue.Domain.Determinism;

namespace Igorogue.Application.Replay;

public sealed record ReplayMetadata
{
    public const int CurrentCommandLogSchemaVersion = 1;
    public const string CurrentChecksumScheme = "sha256-length-prefixed-v1";

    private ReplayMetadata(
        string gameVersion,
        string contentHash,
        long initialSeed,
        string rngAlgorithmVersion,
        int commandLogSchemaVersion,
        string checksumScheme)
    {
        GameVersion = gameVersion;
        ContentHash = contentHash;
        InitialSeed = initialSeed;
        RngAlgorithmVersion = rngAlgorithmVersion;
        CommandLogSchemaVersion = commandLogSchemaVersion;
        ChecksumScheme = checksumScheme;
    }

    public string GameVersion { get; }

    public string ContentHash { get; }

    public long InitialSeed { get; }

    public string RngAlgorithmVersion { get; }

    public int CommandLogSchemaVersion { get; }

    public string ChecksumScheme { get; }

    public static ReplayMetadata Create(
        string gameVersion,
        string contentHash,
        long initialSeed)
    {
        ValidateToken(gameVersion, nameof(gameVersion));
        var canonicalContentHash = ValidateContentHash(contentHash);
        return new ReplayMetadata(
            gameVersion,
            canonicalContentHash,
            initialSeed,
            DeterministicRngState.AlgorithmVersion,
            CurrentCommandLogSchemaVersion,
            CurrentChecksumScheme);
    }

    public string ToCanonicalText() =>
        $"game_version={GameVersion}\n" +
        $"content_hash={ContentHash}\n" +
        $"initial_seed={InitialSeed.ToString(CultureInfo.InvariantCulture)}\n" +
        $"rng_algorithm={RngAlgorithmVersion}\n" +
        $"command_log_schema={CommandLogSchemaVersion.ToString(CultureInfo.InvariantCulture)}\n" +
        $"checksum_scheme={ChecksumScheme}\n";

    private static void ValidateToken(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '_' and not '-'))
        {
            throw new ArgumentException(
                "Value must contain only ASCII letters, digits, '.', '_', or '-'.",
                parameterName);
        }
    }

    private static string ValidateContentHash(string contentHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        const string prefix = "sha256:";
        var digest = contentHash.StartsWith(prefix, StringComparison.Ordinal)
            ? contentHash[prefix.Length..]
            : string.Empty;

        if (digest.Length != 64 || digest.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "Content hash must use the form sha256:<64 hex digits>.",
                nameof(contentHash));
        }

        return prefix + digest.ToLowerInvariant();
    }
}
