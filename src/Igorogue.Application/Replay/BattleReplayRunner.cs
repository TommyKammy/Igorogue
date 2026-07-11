using System.Collections.ObjectModel;
using Igorogue.Application.Battle;

namespace Igorogue.Application.Replay;

public static class BattleReplayRunner
{
    public static BattleReplayResult Replay(
        BattleReplayDocument document,
        HeadlessBattleSession initialSession)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(initialSession);
        document.ValidateIntegrity();

        if (!string.Equals(
                document.Metadata.ToCanonicalText(),
                initialSession.CommandLog.Metadata.ToCanonicalText(),
                StringComparison.Ordinal))
        {
            throw new ReplayValidationException(
                "metadata_mismatch",
                "Replay metadata does not match the caller-supplied canonical initial session.");
        }

        if (!string.Equals(
                document.InitialStateChecksum,
                initialSession.State.Checksum,
                StringComparison.Ordinal))
        {
            throw new ReplayValidationException(
                "initial_state_checksum_mismatch",
                "Replay initial state checksum does not match the supplied session.");
        }

        if (!string.Equals(
                document.InitialLogChecksum,
                initialSession.CommandLog.CurrentChecksum,
                StringComparison.Ordinal))
        {
            throw new ReplayValidationException(
                "initial_log_checksum_mismatch",
                "Replay initial command-log checksum does not match the supplied session.");
        }

        var session = initialSession;
        var results = new List<BattleCommandResult>(document.Attempts.Count);
        foreach (var attempt in document.Attempts)
        {
            if (!string.Equals(
                    attempt.BeforeStateChecksum,
                    session.State.Checksum,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    attempt.BeforeLogChecksum,
                    session.CommandLog.CurrentChecksum,
                    StringComparison.Ordinal))
            {
                throw new ReplayValidationException(
                    "boundary_precondition_mismatch",
                    "Replay live session does not match the persisted attempt boundary.",
                    attempt.AttemptSequence);
            }

            var command = BattleReplayCommandCodec.Decode(
                attempt,
                session.State.Board.Geometry);
            var result = HeadlessBattleStateMachine.Execute(session, command);
            if (result.Accepted != attempt.Accepted)
            {
                throw new ReplayValidationException(
                    "acceptance_mismatch",
                    "Replay command acceptance differs from the persisted boundary.",
                    attempt.AttemptSequence);
            }

            if (!string.Equals(result.ReasonId, attempt.ReasonId, StringComparison.Ordinal))
            {
                throw new ReplayValidationException(
                    "reason_mismatch",
                    "Replay command reason differs from the persisted boundary.",
                    attempt.AttemptSequence);
            }

            if (!string.Equals(
                    result.StateChecksum,
                    attempt.StateChecksum,
                    StringComparison.Ordinal))
            {
                throw new ReplayValidationException(
                    "state_checksum_mismatch",
                    "Replay state checksum differs from the persisted boundary.",
                    attempt.AttemptSequence);
            }

            if (!string.Equals(
                    result.LogChecksum,
                    attempt.LogChecksum,
                    StringComparison.Ordinal))
            {
                throw new ReplayValidationException(
                    "log_checksum_mismatch",
                    "Replay command-log checksum differs from the persisted boundary.",
                    attempt.AttemptSequence);
            }

            results.Add(result);
            session = result.SessionAfter;
        }

        if (!string.Equals(
                session.State.Checksum,
                document.FinalStateChecksum,
                StringComparison.Ordinal) ||
            !string.Equals(
                session.CommandLog.CurrentChecksum,
                document.FinalLogChecksum,
                StringComparison.Ordinal))
        {
            throw new ReplayValidationException(
                "final_checksum_mismatch",
                "Replay final checksums differ from the persisted result.");
        }

        if (session.State.IsTerminal != document.Terminal.IsTerminal ||
            !string.Equals(
                session.State.OutcomeId,
                document.Terminal.OutcomeId,
                StringComparison.Ordinal) ||
            !string.Equals(
                session.State.EndReasonId,
                document.Terminal.EndReasonId,
                StringComparison.Ordinal))
        {
            throw new ReplayValidationException(
                "terminal_mismatch",
                "Replay terminal result differs from the persisted result.");
        }

        return new BattleReplayResult(session, results);
    }
}

public sealed class BattleReplayResult
{
    private readonly BattleCommandResult[] commandResults;
    private readonly ReadOnlyCollection<BattleCommandResult> commandResultView;

    internal BattleReplayResult(
        HeadlessBattleSession finalSession,
        IEnumerable<BattleCommandResult> commandResults)
    {
        ArgumentNullException.ThrowIfNull(finalSession);
        ArgumentNullException.ThrowIfNull(commandResults);
        FinalSession = finalSession;
        this.commandResults = commandResults.ToArray();
        commandResultView = Array.AsReadOnly(this.commandResults);
    }

    public HeadlessBattleSession FinalSession { get; }

    public IReadOnlyList<BattleCommandResult> CommandResults => commandResultView;
}
