using System.Collections.ObjectModel;
using System.Globalization;

using Igorogue.Application.Battle;
using Igorogue.Domain.Board;
using Igorogue.Domain.Determinism;

namespace Igorogue.Application.Replay;

public sealed class BattleReplayDocumentV3
{
    private readonly BattleReplayAttempt[] attempts;
    private readonly ReadOnlyCollection<BattleReplayAttempt> attemptView;

    private BattleReplayDocumentV3(
        ReplayMetadata metadata,
        string initialStateChecksum,
        string initialLogChecksum,
        BattleReplayAttempt[] attempts,
        string finalStateChecksum,
        string finalLogChecksum,
        BattleReplayTerminal terminal,
        string attemptsChecksum,
        string documentChecksum)
    {
        Metadata = metadata;
        InitialStateChecksum = initialStateChecksum;
        InitialLogChecksum = initialLogChecksum;
        this.attempts = attempts;
        attemptView = Array.AsReadOnly(attempts);
        FinalStateChecksum = finalStateChecksum;
        FinalLogChecksum = finalLogChecksum;
        Terminal = terminal;
        AttemptsChecksum = attemptsChecksum;
        DocumentChecksum = documentChecksum;
    }

    public ReplayMetadata Metadata { get; }

    public string StateProjection => BattleReplayFormatV3.StateProjection;

    public string InitialStateChecksum { get; }

    public string InitialLogChecksum { get; }

    public IReadOnlyList<BattleReplayAttempt> Attempts => attemptView;

    public string FinalStateChecksum { get; }

    public string FinalLogChecksum { get; }

    public BattleReplayTerminal Terminal { get; }

    public string IntegrityScheme => BattleReplayFormatV3.IntegrityScheme;

    public string AttemptsChecksum { get; }

    public string DocumentChecksum { get; }

    public static BattleReplayDocumentV3 Capture(
        CoreDuelBattleSession initialSession,
        IEnumerable<CoreDuelBattleCommandResult> commandResults)
    {
        ArgumentNullException.ThrowIfNull(initialSession);
        ArgumentNullException.ThrowIfNull(commandResults);
        BattleReplayV3SessionValidation.RequireCoreDuelV3(initialSession);

        var attempts = new List<BattleReplayAttempt>();
        var current = initialSession;
        var attemptChain = BattleReplayIntegrityV3.InitialAttemptChecksum(
            initialSession.CommandLog.Metadata,
            initialSession.State.Checksum,
            initialSession.CommandLog.CurrentChecksum);
        foreach (var result in commandResults)
        {
            ArgumentNullException.ThrowIfNull(result);
            if (attempts.Count >= BattleReplayFormatV3.MaxAttempts)
            {
                throw new ReplayValidationException(
                    "replay_too_many_attempts",
                    $"Replay capture exceeds the {BattleReplayFormatV3.MaxAttempts} attempt limit.");
            }

            if (!ReferenceEquals(result.SessionBefore, current))
            {
                throw new ReplayValidationException(
                    "capture_discontinuity",
                    "Replay capture results must form one exact immutable session chain.",
                    attempts.Count);
            }

            BattleReplayV3SessionValidation.RequireCoreDuelV3(
                result.SessionBefore,
                attempts.Count);
            BattleReplayV3SessionValidation.RequireCoreDuelV3(
                result.SessionAfter,
                attempts.Count);

            var canonicalPayload = result.Command.ToCanonicalPayload()
                ?? throw new ReplayValidationException(
                    "malformed_command_payload",
                    "Captured command returned a null canonical payload.",
                    attempts.Count);
            var attemptWithoutChecksum = new BattleReplayAttempt(
                attempts.Count,
                result.Command.CommandType,
                result.Command.CommandSchemaVersion,
                canonicalPayload,
                result.SessionBefore.State.Checksum,
                result.SessionBefore.CommandLog.CurrentChecksum,
                result.Accepted,
                result.ReasonId,
                result.StateChecksum,
                result.LogChecksum,
                attemptChecksum: string.Empty);
            BattleReplayCommandCodecV3.ValidateSupported(attemptWithoutChecksum);
            attemptChain = BattleReplayIntegrityV3.AppendAttempt(
                attemptChain,
                attemptWithoutChecksum);
            attempts.Add(attemptWithoutChecksum.WithAttemptChecksum(attemptChain));
            current = result.SessionAfter;
        }

        return CreateValidated(
            initialSession.CommandLog.Metadata,
            initialSession.State.Checksum,
            initialSession.CommandLog.CurrentChecksum,
            attempts,
            current.State.Checksum,
            current.CommandLog.CurrentChecksum,
            new BattleReplayTerminal(
                current.State.IsTerminal,
                current.State.BattleState.OutcomeId,
                current.State.BattleState.EndReasonId),
            persistedStateProjection: null,
            persistedAttemptsChecksum: null,
            persistedDocumentChecksum: null);
    }

    internal static BattleReplayDocumentV3 CreateValidated(
        ReplayMetadata metadata,
        string initialStateChecksum,
        string initialLogChecksum,
        IEnumerable<BattleReplayAttempt> attempts,
        string finalStateChecksum,
        string finalLogChecksum,
        BattleReplayTerminal terminal,
        string? persistedStateProjection,
        string? persistedAttemptsChecksum,
        string? persistedDocumentChecksum)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(attempts);
        ArgumentNullException.ThrowIfNull(terminal);
        if (persistedStateProjection is not null &&
            !string.Equals(
                persistedStateProjection,
                BattleReplayFormatV3.StateProjection,
                StringComparison.Ordinal))
        {
            throw new ReplayValidationException(
                "unsupported_state_projection",
                "Replay state projection is not supported by replay schema 3.");
        }

        ValidateChecksum(initialStateChecksum, "initial_state_checksum");
        ValidateChecksum(initialLogChecksum, "initial_log_checksum");
        ValidateChecksum(finalStateChecksum, "final_state_checksum");
        ValidateChecksum(finalLogChecksum, "final_log_checksum");
        ValidateTerminal(terminal);

        var copiedAttempts = attempts.ToArray();
        if (copiedAttempts.Length > BattleReplayFormatV3.MaxAttempts)
        {
            throw new ReplayValidationException(
                "replay_too_many_attempts",
                $"Replay document exceeds the {BattleReplayFormatV3.MaxAttempts} attempt limit.");
        }

        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var commandLog = OrderedCommandLog.Create(metadata);
        if (!string.Equals(
                commandLog.CurrentChecksum,
                initialLogChecksum,
                StringComparison.Ordinal))
        {
            throw new ReplayValidationException(
                "command_log_checksum_mismatch",
                "Initial command-log checksum does not match replay metadata.");
        }

        var expectedStateChecksum = initialStateChecksum;
        var expectedLogChecksum = initialLogChecksum;
        var attemptChain = BattleReplayIntegrityV3.InitialAttemptChecksum(
            metadata,
            initialStateChecksum,
            initialLogChecksum);
        for (var index = 0; index < copiedAttempts.Length; index++)
        {
            var attempt = copiedAttempts[index]
                ?? throw new ReplayValidationException(
                    "malformed_replay",
                    "Replay attempt cannot be null.",
                    index);
            ValidateAttempt(attempt, index);
            if (!string.Equals(
                    attempt.BeforeStateChecksum,
                    expectedStateChecksum,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    attempt.BeforeLogChecksum,
                    expectedLogChecksum,
                    StringComparison.Ordinal))
            {
                throw new ReplayValidationException(
                    "boundary_chain_mismatch",
                    "Replay attempt does not begin at the previous persisted boundary.",
                    index);
            }

            var command = BattleReplayCommandCodecV3.Decode(attempt, geometry);
            if (attempt.Accepted)
            {
                if (!string.Equals(attempt.ReasonId, "accepted", StringComparison.Ordinal))
                {
                    throw new ReplayValidationException(
                        "acceptance_reason_mismatch",
                        "Accepted replay attempt must use reason 'accepted'.",
                        index);
                }

                commandLog = commandLog.Append(command, attempt.StateChecksum);
            }
            else
            {
                if (string.Equals(attempt.ReasonId, "accepted", StringComparison.Ordinal))
                {
                    throw new ReplayValidationException(
                        "acceptance_reason_mismatch",
                        "Rejected replay attempt cannot use reason 'accepted'.",
                        index);
                }

                if (!string.Equals(
                        attempt.StateChecksum,
                        attempt.BeforeStateChecksum,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        attempt.LogChecksum,
                        attempt.BeforeLogChecksum,
                        StringComparison.Ordinal))
                {
                    throw new ReplayValidationException(
                        "rejected_boundary_mutation",
                        "Rejected replay attempt must preserve state and command-log checksums.",
                        index);
                }
            }

            if (!string.Equals(
                    commandLog.CurrentChecksum,
                    attempt.LogChecksum,
                    StringComparison.Ordinal))
            {
                throw new ReplayValidationException(
                    "command_log_checksum_mismatch",
                    "Persisted command-log checksum does not match the recomputed accepted-only chain.",
                    index);
            }

            attemptChain = BattleReplayIntegrityV3.AppendAttempt(attemptChain, attempt);
            if (!string.Equals(
                    attempt.AttemptChecksum,
                    attemptChain,
                    StringComparison.Ordinal))
            {
                throw new ReplayValidationException(
                    "attempt_chain_checksum_mismatch",
                    "Replay attempt checksum does not match its chain prefix.",
                    index);
            }

            expectedStateChecksum = attempt.StateChecksum;
            expectedLogChecksum = attempt.LogChecksum;
        }

        if (!string.Equals(finalStateChecksum, expectedStateChecksum, StringComparison.Ordinal) ||
            !string.Equals(finalLogChecksum, expectedLogChecksum, StringComparison.Ordinal))
        {
            throw new ReplayValidationException(
                "final_checksum_mismatch",
                "Final replay checksums do not match the last persisted boundary.");
        }

        if (persistedAttemptsChecksum is not null)
        {
            ValidateChecksum(persistedAttemptsChecksum, "attempts_checksum");
            if (!string.Equals(
                    persistedAttemptsChecksum,
                    attemptChain,
                    StringComparison.Ordinal))
            {
                throw new ReplayValidationException(
                    "attempt_chain_checksum_mismatch",
                    "Replay attempt-chain checksum does not match its persisted attempts.");
            }
        }

        var documentChecksum = BattleReplayIntegrityV3.DocumentChecksum(
            metadata,
            initialStateChecksum,
            initialLogChecksum,
            attemptChain,
            finalStateChecksum,
            finalLogChecksum,
            terminal);
        if (persistedDocumentChecksum is not null)
        {
            ValidateChecksum(persistedDocumentChecksum, "document_checksum");
            if (!string.Equals(
                    persistedDocumentChecksum,
                    documentChecksum,
                    StringComparison.Ordinal))
            {
                throw new ReplayValidationException(
                    "document_checksum_mismatch",
                    "Replay document checksum does not match its persisted identity and result.");
            }
        }

        return new BattleReplayDocumentV3(
            metadata,
            initialStateChecksum,
            initialLogChecksum,
            copiedAttempts,
            finalStateChecksum,
            finalLogChecksum,
            terminal,
            attemptChain,
            documentChecksum);
    }

    internal void ValidateIntegrity() => CreateValidated(
        Metadata,
        InitialStateChecksum,
        InitialLogChecksum,
        attempts,
        FinalStateChecksum,
        FinalLogChecksum,
        Terminal,
        StateProjection,
        AttemptsChecksum,
        DocumentChecksum);

    private static void ValidateAttempt(BattleReplayAttempt attempt, int expectedIndex)
    {
        if (attempt.AttemptSequence != expectedIndex)
        {
            throw new ReplayValidationException(
                "invalid_attempt_sequence",
                "Replay attempt sequence must be contiguous and zero-based.",
                expectedIndex);
        }

        ValidateToken(attempt.CommandType, "command_type", expectedIndex);
        if (attempt.CommandSchemaVersion <= 0)
        {
            throw new ReplayValidationException(
                "unsupported_command_schema",
                "Replay command schema version must be positive.",
                expectedIndex);
        }

        if (attempt.CanonicalPayload is null)
        {
            throw new ReplayValidationException(
                "malformed_command_payload",
                "Replay canonical payload cannot be null.",
                expectedIndex);
        }

        ValidateChecksum(attempt.BeforeStateChecksum, "before_state_checksum", expectedIndex);
        ValidateChecksum(attempt.BeforeLogChecksum, "before_log_checksum", expectedIndex);
        ValidateToken(attempt.ReasonId, "reason_id", expectedIndex);
        ValidateChecksum(attempt.StateChecksum, "state_checksum", expectedIndex);
        ValidateChecksum(attempt.LogChecksum, "log_checksum", expectedIndex);
        ValidateChecksum(attempt.AttemptChecksum, "attempt_checksum", expectedIndex);
    }

    private static void ValidateTerminal(BattleReplayTerminal terminal)
    {
        ValidateToken(terminal.OutcomeId, "terminal.outcome", attemptIndex: null);
        ValidateToken(terminal.EndReasonId, "terminal.end_reason", attemptIndex: null);
        var valid = terminal.IsTerminal
            ? terminal.OutcomeId is "win" or "loss" && terminal.EndReasonId != "none"
            : terminal.OutcomeId == "ongoing" && terminal.EndReasonId == "none";
        if (!valid)
        {
            throw new ReplayValidationException(
                "malformed_terminal",
                "Replay terminal tuple is internally inconsistent.");
        }
    }

    private static void ValidateChecksum(
        string value,
        string fieldName,
        int? attemptIndex = null)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length != 64 ||
            value.Any(character => character is not (>= '0' and <= '9') and
                not (>= 'a' and <= 'f')))
        {
            throw new ReplayValidationException(
                "invalid_checksum",
                $"{fieldName} must contain exactly 64 lowercase hexadecimal digits.",
                attemptIndex);
        }
    }

    private static void ValidateToken(
        string value,
        string fieldName,
        int? attemptIndex)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '_' and not '-'))
        {
            throw new ReplayValidationException(
                "malformed_replay",
                $"{fieldName} must be a stable ASCII token.",
                attemptIndex);
        }
    }
}

internal static class BattleReplayFormatV3
{
    internal const string SchemaId = "igorogue.battle-replay";
    internal const int SchemaVersion = 3;
    internal const string StateProjection = "headless-core-duel-state-v1";
    internal const string IntegrityScheme = "sha256-length-prefixed-v1";
    internal const string AttemptHeaderVersion = "igorogue-battle-replay-attempt-chain-v3";
    internal const string AttemptEntryVersion = "igorogue-battle-replay-attempt-v3";
    internal const string DocumentVersion = "igorogue-battle-replay-document-v3";
    internal const int MaxAttempts = 4096;
}

internal static class BattleReplayIntegrityV3
{
    internal static string InitialAttemptChecksum(
        ReplayMetadata metadata,
        string initialStateChecksum,
        string initialLogChecksum) =>
        DeterministicChecksum.Combine(
            BattleReplayFormatV3.AttemptHeaderVersion,
            BattleReplayFormatV3.SchemaId,
            BattleReplayFormatV3.SchemaVersion.ToString(CultureInfo.InvariantCulture),
            BattleReplayFormatV3.StateProjection,
            metadata.ToCanonicalText(),
            initialStateChecksum,
            initialLogChecksum);

    internal static string AppendAttempt(
        string previousChecksum,
        BattleReplayAttempt attempt) =>
        DeterministicChecksum.Combine(
            BattleReplayFormatV3.AttemptEntryVersion,
            BattleReplayFormatV3.SchemaId,
            BattleReplayFormatV3.SchemaVersion.ToString(CultureInfo.InvariantCulture),
            BattleReplayFormatV3.StateProjection,
            previousChecksum,
            attempt.AttemptSequence.ToString(CultureInfo.InvariantCulture),
            attempt.CommandType,
            attempt.CommandSchemaVersion.ToString(CultureInfo.InvariantCulture),
            attempt.CanonicalPayload,
            attempt.BeforeStateChecksum,
            attempt.BeforeLogChecksum,
            attempt.Accepted ? "1" : "0",
            attempt.ReasonId,
            attempt.StateChecksum,
            attempt.LogChecksum);

    internal static string DocumentChecksum(
        ReplayMetadata metadata,
        string initialStateChecksum,
        string initialLogChecksum,
        string attemptsChecksum,
        string finalStateChecksum,
        string finalLogChecksum,
        BattleReplayTerminal terminal) =>
        DeterministicChecksum.Combine(
            BattleReplayFormatV3.DocumentVersion,
            BattleReplayFormatV3.SchemaId,
            BattleReplayFormatV3.SchemaVersion.ToString(CultureInfo.InvariantCulture),
            BattleReplayFormatV3.StateProjection,
            BattleReplayFormatV3.IntegrityScheme,
            metadata.ToCanonicalText(),
            initialStateChecksum,
            initialLogChecksum,
            attemptsChecksum,
            finalStateChecksum,
            finalLogChecksum,
            terminal.IsTerminal ? "1" : "0",
            terminal.OutcomeId,
            terminal.EndReasonId);
}

internal static class BattleReplayV3SessionValidation
{
    internal static void RequireCoreDuelV3(
        CoreDuelBattleSession session,
        int? attemptIndex = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!string.Equals(
                CoreDuelBattleState.EncodingVersion,
                BattleReplayFormatV3.StateProjection,
                StringComparison.Ordinal))
        {
            throw new ReplayValidationException(
                "unsupported_state_projection",
                "Replay schema 3 requires a headless Core Duel state v1 session.",
                attemptIndex);
        }
    }
}
