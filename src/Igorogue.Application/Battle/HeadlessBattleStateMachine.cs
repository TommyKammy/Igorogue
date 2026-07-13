using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Battle;

public static class HeadlessBattleStateMachine
{
    public static HeadlessBattleSession Start(
        BoardState initialBoard,
        FacilityState initialFacilityState,
        BattleRuntimePolicy runtimePolicy,
        ReplayMetadata replayMetadata)
    {
        ArgumentNullException.ThrowIfNull(initialBoard);
        ArgumentNullException.ThrowIfNull(initialFacilityState);
        ArgumentNullException.ThrowIfNull(runtimePolicy);
        ArgumentNullException.ThrowIfNull(replayMetadata);

        var state = BattleState.Start(
            initialBoard,
            initialFacilityState,
            AuthoritativeRngState.Create(replayMetadata.InitialSeed),
            runtimePolicy);
        return new HeadlessBattleSession(
            state,
            OrderedCommandLog.Create(replayMetadata));
    }

    public static HeadlessBattleSession Start(
        BattleAuthoritativeInitialSnapshot initialSnapshot,
        ReplayMetadata replayMetadata)
    {
        ArgumentNullException.ThrowIfNull(initialSnapshot);
        ArgumentNullException.ThrowIfNull(replayMetadata);

        var state = BattleState.Start(
            initialSnapshot,
            AuthoritativeRngState.Create(replayMetadata.InitialSeed));
        return new HeadlessBattleSession(
            state,
            OrderedCommandLog.Create(replayMetadata));
    }

    public static BattleCommandResult Execute(
        HeadlessBattleSession session,
        IBattleCommand command)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(command);

        if (!string.Equals(
                command.ExpectedStateChecksum,
                session.State.Checksum,
                StringComparison.Ordinal))
        {
            return Reject(session, command, "stale_state");
        }

        if (!string.Equals(
                command.ExpectedLogChecksum,
                session.CommandLog.CurrentChecksum,
                StringComparison.Ordinal))
        {
            return Reject(session, command, "stale_session");
        }

        if (session.State.IsTerminal)
        {
            return Reject(session, command, "battle_terminal");
        }

        if (session.State.AuthoritativeRuntime is not null)
        {
            return AuthoritativeEnemyTurnStateMachine.Execute(session, command);
        }

        return command switch
        {
            AuthorizedStonePlacementCommand placement => ExecutePlacement(session, placement),
            AuthorizedFacilityBuildCommand facilityBuild =>
                ExecuteFacilityBuild(session, facilityBuild),
            EndPlayerTurnCommand endPlayerTurn => ExecuteEndPlayerTurn(session, endPlayerTurn),
            ResolveEnemyPassCommand enemyPass => ExecuteEnemyPass(session, enemyPass),
            _ => Reject(session, command, "unsupported_command"),
        };
    }

    private static BattleCommandResult ExecutePlacement(
        HeadlessBattleSession session,
        AuthorizedStonePlacementCommand command)
    {
        var resolution = AuthorizedStonePlacementPipeline.Resolve(
            session.State,
            command.Actor,
            command.Point,
            command.AccessMode);
        return resolution.Accepted
            ? Accept(
                session,
                command,
                resolution.StateAfter,
                resolution.OrderedFacts)
            : Reject(session, command, resolution.ReasonId);
    }

    private static BattleCommandResult ExecuteFacilityBuild(
        HeadlessBattleSession session,
        AuthorizedFacilityBuildCommand command)
    {
        var source = session.State;
        if (source.Phase != BattlePhase.PlayerAction)
        {
            return Reject(session, command, "wrong_phase");
        }

        if (source.FacilityState.FacilityById(command.InstanceId) is not null)
        {
            return Reject(session, command, "facility_instance_exists");
        }

        var request = new FacilityBuildRequest(
            StoneColor.Black,
            command.Point,
            command.FacilityContentId,
            command.InstanceId);
        var evaluation = FacilityBuildEvaluator.Evaluate(
            source.FacilityRuntimeAnalysis,
            request);
        if (!evaluation.IsLegal)
        {
            return Reject(session, command, evaluation.ReasonId);
        }

        var commit = FacilityBuildEvaluator.Commit(evaluation);
        var stateAfter = BattleState.Transition(
            source,
            source.Board,
            source.RepetitionHistory,
            commit.StateAfterCommit,
            source.TerritoryAnalysis,
            commit.AnalysisAfterCommit,
            source.PlayerTurnIndex,
            source.Phase,
            source.Outcome,
            source.EndReason);
        return Accept(
            session,
            command,
            stateAfter,
            commit.OrderedFacts.Cast<IBattleFact>());
    }

    private static BattleCommandResult ExecuteEndPlayerTurn(
        HeadlessBattleSession session,
        EndPlayerTurnCommand command)
    {
        var source = session.State;
        if (source.Phase != BattlePhase.PlayerAction)
        {
            return Reject(session, command, "wrong_phase");
        }

        var stateAfter = BattleState.Transition(
            source,
            source.Board,
            source.RepetitionHistory,
            source.FacilityState,
            source.TerritoryAnalysis,
            source.FacilityRuntimeAnalysis,
            source.PlayerTurnIndex,
            BattlePhase.EnemyAction,
            BattleOutcome.Ongoing,
            BattleEndReason.None);
        return Accept(session, command, stateAfter, []);
    }

    private static BattleCommandResult ExecuteEnemyPass(
        HeadlessBattleSession session,
        ResolveEnemyPassCommand command)
    {
        var source = session.State;
        if (source.Phase != BattlePhase.EnemyAction)
        {
            return Reject(session, command, "wrong_phase");
        }

        var facts = new List<IBattleFact>
        {
            new EnemyPassedFact(source.PlayerTurnIndex),
        };
        var stateAfter = CompleteEnemyBoundary(source, facts);
        return Accept(session, command, stateAfter, facts);
    }

    private static BattleState CompleteEnemyBoundary(
        BattleState source,
        List<IBattleFact> orderedFacts)
    {
        if (source.PlayerTurnIndex >= source.RuntimePolicy.PlayerTurnLimit)
        {
            orderedFacts.Add(new BattleEndedFact(
                BattleOutcome.PlayerDefeat,
                BattleEndReason.TurnLimit));
            return BattleState.Transition(
                source,
                source.Board,
                source.RepetitionHistory,
                source.FacilityState,
                source.TerritoryAnalysis,
                source.FacilityRuntimeAnalysis,
                source.PlayerTurnIndex,
                BattlePhase.Ended,
                BattleOutcome.PlayerDefeat,
                BattleEndReason.TurnLimit);
        }

        return BattleState.Transition(
            source,
            source.Board,
            source.RepetitionHistory,
            source.FacilityState,
            source.TerritoryAnalysis,
            source.FacilityRuntimeAnalysis,
            source.PlayerTurnIndex + 1,
            BattlePhase.PlayerAction,
            BattleOutcome.Ongoing,
            BattleEndReason.None);
    }

    internal static BattleCommandResult Accept(
        HeadlessBattleSession session,
        IBattleCommand command,
        BattleState stateAfter,
        IEnumerable<IBattleFact> orderedFacts)
    {
        var nextLog = session.CommandLog.Append(command, stateAfter.Checksum);
        var nextSession = new HeadlessBattleSession(stateAfter, nextLog);
        return new BattleCommandResult(
            session,
            nextSession,
            command,
            true,
            "accepted",
            orderedFacts.ToArray());
    }

    internal static BattleCommandResult Reject(
        HeadlessBattleSession session,
        IBattleCommand command,
        string reasonId) =>
        new(
            session,
            session,
            command,
            false,
            reasonId,
            [new CommandRejectedFact(reasonId)]);

}
