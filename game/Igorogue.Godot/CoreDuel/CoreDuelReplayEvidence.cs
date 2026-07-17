using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Content;

namespace Igorogue.Godot.CoreDuel;

/// <summary>
/// Immutable result of saving the first terminal Core Duel run and verifying
/// that the bytes on disk replay from a fresh authoritative startup.
/// </summary>
public sealed record CoreDuelReplayEvidence
{
    internal CoreDuelReplayEvidence(
        string path,
        string gameVersion,
        long seed,
        string contentHash,
        int attemptCount,
        int acceptedCount,
        string? outcomeId,
        string? endReasonId,
        string initialStateChecksum,
        string initialLogChecksum,
        string? finalStateChecksum,
        string? finalLogChecksum,
        string? attemptsChecksum,
        string? documentChecksum,
        string? artifactSha256,
        long? artifactBytes,
        string? replayedStateChecksum,
        string? replayedLogChecksum,
        bool verified,
        string reasonId)
    {
        Path = path;
        GameVersion = gameVersion;
        Seed = seed;
        ContentHash = contentHash;
        AttemptCount = attemptCount;
        AcceptedCount = acceptedCount;
        OutcomeId = outcomeId;
        EndReasonId = endReasonId;
        InitialStateChecksum = initialStateChecksum;
        InitialLogChecksum = initialLogChecksum;
        FinalStateChecksum = finalStateChecksum;
        FinalLogChecksum = finalLogChecksum;
        AttemptsChecksum = attemptsChecksum;
        DocumentChecksum = documentChecksum;
        ArtifactSha256 = artifactSha256;
        ArtifactBytes = artifactBytes;
        ReplayedStateChecksum = replayedStateChecksum;
        ReplayedLogChecksum = replayedLogChecksum;
        Verified = verified;
        ReasonId = reasonId;
    }

    public string Path { get; }

    public string GameVersion { get; }

    public long Seed { get; }

    public string ContentHash { get; }

    public int AttemptCount { get; }

    public int AcceptedCount { get; }

    public string? OutcomeId { get; }

    public string? EndReasonId { get; }

    public string InitialStateChecksum { get; }

    public string InitialLogChecksum { get; }

    public string? FinalStateChecksum { get; }

    public string? FinalLogChecksum { get; }

    public string? AttemptsChecksum { get; }

    public string? DocumentChecksum { get; }

    public string? ArtifactSha256 { get; }

    public long? ArtifactBytes { get; }

    public string? ReplayedStateChecksum { get; }

    public string? ReplayedLogChecksum { get; }

    public bool Verified { get; }

    public string ReasonId { get; }

    public string ToConsoleLine() =>
        "IGOROGUE_GRAYBOX_REPLAY_V3 " + JsonSerializer.Serialize(
            new ConsoleEvidence(
                Path,
                GameVersion,
                Seed,
                ContentHash,
                AttemptCount,
                AcceptedCount,
                OutcomeId,
                EndReasonId,
                InitialStateChecksum,
                InitialLogChecksum,
                FinalStateChecksum,
                FinalLogChecksum,
                AttemptsChecksum,
                DocumentChecksum,
                ArtifactSha256,
                ArtifactBytes,
                ReplayedStateChecksum,
                ReplayedLogChecksum,
                Verified,
                ReasonId));

    private sealed record ConsoleEvidence(
        [property: JsonPropertyName("path")] string Path,
        [property: JsonPropertyName("game_version")] string GameVersion,
        [property: JsonPropertyName("seed")] long Seed,
        [property: JsonPropertyName("content_hash")] string ContentHash,
        [property: JsonPropertyName("attempt_count")] int AttemptCount,
        [property: JsonPropertyName("accepted_count")] int AcceptedCount,
        [property: JsonPropertyName("outcome")] string? OutcomeId,
        [property: JsonPropertyName("end_reason")] string? EndReasonId,
        [property: JsonPropertyName("initial_state")] string InitialStateChecksum,
        [property: JsonPropertyName("initial_log")] string InitialLogChecksum,
        [property: JsonPropertyName("final_state")] string? FinalStateChecksum,
        [property: JsonPropertyName("final_log")] string? FinalLogChecksum,
        [property: JsonPropertyName("attempts_checksum")] string? AttemptsChecksum,
        [property: JsonPropertyName("document_checksum")] string? DocumentChecksum,
        [property: JsonPropertyName("artifact_sha256")] string? ArtifactSha256,
        [property: JsonPropertyName("artifact_bytes")] long? ArtifactBytes,
        [property: JsonPropertyName("replayed_state")] string? ReplayedStateChecksum,
        [property: JsonPropertyName("replayed_log")] string? ReplayedLogChecksum,
        [property: JsonPropertyName("verified")] bool Verified,
        [property: JsonPropertyName("reason")] string ReasonId);
}

/// <summary>
/// Launch-scoped recorder. It observes exact state-machine result boundaries,
/// seals on the first non-terminal to terminal transition, and never reopens.
/// </summary>
internal sealed class CoreDuelReplayEvidenceRecorder
{
    private readonly CoreDuelContentCatalog catalog;
    private readonly CoreDuelBattleSession initialSession;
    private readonly string? outputPath;
    private readonly List<CoreDuelBattleCommandResult> results = [];
    private CoreDuelBattleSession expectedSession;
    private bool sealedRecorder;

    internal CoreDuelReplayEvidenceRecorder(
        CoreDuelContentCatalog catalog,
        CoreDuelBattleSession initialSession,
        string? outputPath)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(initialSession);
        this.catalog = catalog;
        this.initialSession = initialSession;
        expectedSession = initialSession;
        this.outputPath = outputPath is null
            ? null
            : ValidateOutputPath(outputPath);
    }

    internal bool Enabled => outputPath is not null;

    internal CoreDuelReplayEvidence? Evidence { get; private set; }

    internal void Observe(CoreDuelBattleCommandResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!Enabled || sealedRecorder)
        {
            return;
        }

        if (!ReferenceEquals(result.SessionBefore, expectedSession))
        {
            sealedRecorder = true;
            Evidence = Failure(
                "capture_discontinuity",
                result.SessionAfter,
                document: null,
                artifactSha256: null,
                artifactBytes: null,
                replayedStateChecksum: null,
                replayedLogChecksum: null);
            return;
        }

        results.Add(result);
        expectedSession = result.SessionAfter;
        if (result.SessionBefore.State.IsTerminal ||
            !result.SessionAfter.State.IsTerminal)
        {
            return;
        }

        sealedRecorder = true;
        Evidence = SaveAndVerify(result.SessionAfter);
    }

    internal static string ValidateOutputPath(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (outputPath.Any(character => character is '\0' or '\r' or '\n'))
        {
            throw new ArgumentException(
                "Replay output path cannot contain NUL or line-break characters.",
                nameof(outputPath));
        }

        if (!Path.IsPathFullyQualified(outputPath))
        {
            throw new ArgumentException(
                "Replay output path must be fully qualified.",
                nameof(outputPath));
        }

        var fullPath = Path.GetFullPath(outputPath);
        var root = Path.GetPathRoot(fullPath);
        if (root is null ||
            StringComparer.Ordinal.Equals(
                fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)))
        {
            throw new ArgumentException(
                "Replay output path must name a file below an existing directory.",
                nameof(outputPath));
        }

        if (Directory.Exists(fullPath))
        {
            throw new ArgumentException(
                "Replay output path cannot name a directory.",
                nameof(outputPath));
        }

        var directory = Path.GetDirectoryName(fullPath);
        if (directory is null || !Directory.Exists(directory))
        {
            throw new ArgumentException(
                "Replay output parent directory must already exist.",
                nameof(outputPath));
        }

        if (File.Exists(fullPath))
        {
            throw new ArgumentException(
                "Replay output file already exists and will not be overwritten.",
                nameof(outputPath));
        }

        return fullPath;
    }

    private CoreDuelReplayEvidence SaveAndVerify(CoreDuelBattleSession terminalSession)
    {
        BattleReplayDocumentV3? document = null;
        string? artifactSha256 = null;
        long? artifactBytes = null;
        string? replayedStateChecksum = null;
        string? replayedLogChecksum = null;
        try
        {
            document = BattleReplayDocumentV3.Capture(initialSession, results);
            RequireTerminalParity(document, terminalSession);

            using var canonicalStream = new MemoryStream();
            BattleReplaySerializerV3.Save(document, canonicalStream);
            var canonicalBytes = canonicalStream.ToArray();
            var canonicalSha256 = Sha256(canonicalBytes);

            SaveAtomicNoOverwrite(outputPath!, canonicalBytes);

            using var source = new FileStream(
                outputPath!,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            using var diskStream = new MemoryStream();
            source.CopyTo(diskStream);
            var diskBytes = diskStream.ToArray();
            artifactBytes = diskBytes.LongLength;
            artifactSha256 = Sha256(diskBytes);
            if (!canonicalBytes.AsSpan().SequenceEqual(diskBytes) ||
                !StringComparer.Ordinal.Equals(canonicalSha256, artifactSha256))
            {
                return Failure(
                    "artifact_readback_mismatch",
                    terminalSession,
                    document,
                    artifactSha256,
                    artifactBytes,
                    replayedStateChecksum,
                    replayedLogChecksum);
            }

            source.Position = 0;
            var loaded = BattleReplaySerializerV3.Load(source);

            using var loadedCanonicalStream = new MemoryStream();
            BattleReplaySerializerV3.Save(loaded, loadedCanonicalStream);
            if (!loadedCanonicalStream.ToArray().AsSpan().SequenceEqual(diskBytes))
            {
                return Failure(
                    "artifact_not_canonical",
                    terminalSession,
                    loaded,
                    artifactSha256,
                    artifactBytes,
                    replayedStateChecksum,
                    replayedLogChecksum);
            }

            if (!StringComparer.Ordinal.Equals(
                    loaded.Metadata.ContentHash,
                    catalog.ContentHash))
            {
                return Failure(
                    "content_mismatch",
                    terminalSession,
                    loaded,
                    artifactSha256,
                    artifactBytes,
                    replayedStateChecksum,
                    replayedLogChecksum);
            }

            var fresh = CoreDuelBattleStartup.Start(
                catalog,
                loaded.Metadata.GameVersion,
                loaded.Metadata.InitialSeed);
            var replay = BattleReplayRunnerV3.Replay(loaded, fresh.Session);
            replayedStateChecksum = replay.FinalSession.State.Checksum;
            replayedLogChecksum = replay.FinalSession.CommandLog.CurrentChecksum;
            RequireTerminalParity(loaded, terminalSession);
            RequireReplayParity(terminalSession, replay.FinalSession);
            if (!diskBytes.AsSpan().SequenceEqual(File.ReadAllBytes(outputPath!)))
            {
                return Failure(
                    "artifact_changed_during_verification",
                    terminalSession,
                    loaded,
                    artifactSha256,
                    artifactBytes,
                    replayedStateChecksum,
                    replayedLogChecksum);
            }

            return new CoreDuelReplayEvidence(
                outputPath!,
                loaded.Metadata.GameVersion,
                loaded.Metadata.InitialSeed,
                loaded.Metadata.ContentHash,
                loaded.Attempts.Count,
                loaded.Attempts.Count(attempt => attempt.Accepted),
                loaded.Terminal.OutcomeId,
                loaded.Terminal.EndReasonId,
                loaded.InitialStateChecksum,
                loaded.InitialLogChecksum,
                loaded.FinalStateChecksum,
                loaded.FinalLogChecksum,
                loaded.AttemptsChecksum,
                loaded.DocumentChecksum,
                artifactSha256,
                artifactBytes,
                replayedStateChecksum,
                replayedLogChecksum,
                verified: true,
                reasonId: "verified");
        }
        catch (ReplayValidationException exception)
        {
            return Failure(
                exception.ReasonId,
                terminalSession,
                document,
                artifactSha256,
                artifactBytes,
                replayedStateChecksum,
                replayedLogChecksum);
        }
        catch (EvidenceValidationException exception)
        {
            return Failure(
                exception.ReasonId,
                terminalSession,
                document,
                artifactSha256,
                artifactBytes,
                replayedStateChecksum,
                replayedLogChecksum);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return Failure(
                "artifact_io_failure",
                terminalSession,
                document,
                artifactSha256,
                artifactBytes,
                replayedStateChecksum,
                replayedLogChecksum);
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or
                NotSupportedException or OverflowException)
        {
            return Failure(
                "artifact_verification_failure",
                terminalSession,
                document,
                artifactSha256,
                artifactBytes,
                replayedStateChecksum,
                replayedLogChecksum);
        }
    }

    private CoreDuelReplayEvidence Failure(
        string reasonId,
        CoreDuelBattleSession finalSession,
        BattleReplayDocumentV3? document,
        string? artifactSha256,
        long? artifactBytes,
        string? replayedStateChecksum,
        string? replayedLogChecksum)
    {
        var metadata = document?.Metadata ?? initialSession.CommandLog.Metadata;
        return new CoreDuelReplayEvidence(
            outputPath!,
            metadata.GameVersion,
            metadata.InitialSeed,
            metadata.ContentHash,
            document?.Attempts.Count ?? results.Count,
            document?.Attempts.Count(attempt => attempt.Accepted) ??
                results.Count(result => result.Accepted),
            document?.Terminal.OutcomeId ?? finalSession.State.BattleState.OutcomeId,
            document?.Terminal.EndReasonId ?? finalSession.State.BattleState.EndReasonId,
            document?.InitialStateChecksum ?? initialSession.State.Checksum,
            document?.InitialLogChecksum ?? initialSession.CommandLog.CurrentChecksum,
            document?.FinalStateChecksum ?? finalSession.State.Checksum,
            document?.FinalLogChecksum ?? finalSession.CommandLog.CurrentChecksum,
            document?.AttemptsChecksum,
            document?.DocumentChecksum,
            artifactSha256,
            artifactBytes,
            replayedStateChecksum,
            replayedLogChecksum,
            verified: false,
            reasonId);
    }

    private static void SaveAtomicNoOverwrite(string path, ReadOnlySpan<byte> bytes)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Replay output path has no parent directory.");
        var temporaryPath = Path.Combine(
            directory,
            $".igorogue-replay-{Path.GetRandomFileName()}.tmp");
        var ownsTemporaryPath = false;
        try
        {
            using (var destination = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                ownsTemporaryPath = true;
                destination.Write(bytes);
                destination.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, path, overwrite: false);
        }
        finally
        {
            if (ownsTemporaryPath && File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void RequireTerminalParity(
        BattleReplayDocumentV3 document,
        CoreDuelBattleSession terminalSession)
    {
        if (!document.Terminal.IsTerminal ||
            !terminalSession.State.IsTerminal ||
            !StringComparer.Ordinal.Equals(
                document.Terminal.OutcomeId,
                terminalSession.State.BattleState.OutcomeId) ||
            !StringComparer.Ordinal.Equals(
                document.Terminal.EndReasonId,
                terminalSession.State.BattleState.EndReasonId) ||
            !StringComparer.Ordinal.Equals(
                document.FinalStateChecksum,
                terminalSession.State.Checksum) ||
            !StringComparer.Ordinal.Equals(
                document.FinalLogChecksum,
                terminalSession.CommandLog.CurrentChecksum))
        {
            throw new EvidenceValidationException(
                "terminal_parity_mismatch",
                "Captured replay terminal boundary does not match the human run.");
        }
    }

    private static void RequireReplayParity(
        CoreDuelBattleSession humanTerminal,
        CoreDuelBattleSession replayTerminal)
    {
        if (!replayTerminal.State.IsTerminal ||
            !StringComparer.Ordinal.Equals(
                humanTerminal.State.BattleState.OutcomeId,
                replayTerminal.State.BattleState.OutcomeId) ||
            !StringComparer.Ordinal.Equals(
                humanTerminal.State.BattleState.EndReasonId,
                replayTerminal.State.BattleState.EndReasonId) ||
            !StringComparer.Ordinal.Equals(
                humanTerminal.State.Checksum,
                replayTerminal.State.Checksum) ||
            !StringComparer.Ordinal.Equals(
                humanTerminal.CommandLog.CurrentChecksum,
                replayTerminal.CommandLog.CurrentChecksum))
        {
            throw new EvidenceValidationException(
                "replay_parity_mismatch",
                "Fresh replay terminal boundary differs from the human run.");
        }
    }

    private static string Sha256(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private sealed class EvidenceValidationException : Exception
    {
        internal EvidenceValidationException(string reasonId, string message)
            : base(message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reasonId);
            ReasonId = reasonId;
        }

        internal string ReasonId { get; }
    }
}
