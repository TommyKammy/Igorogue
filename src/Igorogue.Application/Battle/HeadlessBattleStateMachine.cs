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

        if (session.State.IsTerminal)
        {
            return Reject(session, command, "battle_terminal");
        }

        return command switch
        {
            AuthorizedStonePlacementCommand placement => ExecutePlacement(session, placement),
            EndPlayerTurnCommand endPlayerTurn => ExecuteEndPlayerTurn(session, endPlayerTurn),
            ResolveEnemyPassCommand enemyPass => ExecuteEnemyPass(session, enemyPass),
            _ => Reject(session, command, "unsupported_command"),
        };
    }

    private static BattleCommandResult ExecutePlacement(
        HeadlessBattleSession session,
        AuthorizedStonePlacementCommand command)
    {
        var source = session.State;
        var expectedActor = source.Phase switch
        {
            BattlePhase.PlayerAction => StoneColor.Black,
            BattlePhase.EnemyAction => StoneColor.White,
            _ => throw new InvalidOperationException("Terminal phase was not rejected before placement."),
        };
        if (command.Actor != expectedActor)
        {
            return Reject(session, command, "wrong_actor_for_phase");
        }

        var proposedStone = new BoardStone(command.Actor, false, command.Point);
        if (!HypotheticalPlacementResolver.TryCreate(
                source.Board,
                proposedStone,
                out var hypothetical) ||
            hypothetical is null)
        {
            return Reject(session, command, "stone_occupied");
        }

        var resolved = HypotheticalPlacementResolver.ResolveCaptures(
            hypothetical,
            RealLiberties(hypothetical.GroupsAfterPlacement));
        var legality = PlacementLegalityEvaluator.Evaluate(
            resolved,
            RealLiberties(resolved.GroupsAfterCapture),
            source.RepetitionHistory,
            command.AccessMode);
        if (!legality.IsLegal)
        {
            return Reject(session, command, legality.ReasonId);
        }

        var legalPlacement = source.RepetitionHistory.CommitLegalPlacement(legality);
        var placementCommit = FacilityPlacementIntegrator.Apply(
            source.FacilityState,
            legalPlacement);
        var orderedFacts = placementCommit.OrderedFacts
            .Cast<IBattleFact>()
            .ToList();
        var territoryAfter = TerritoryAnalyzer.Analyze(placementCommit.BoardAfterCommit);

        if (placementCommit.KingCaptureResult.IsTerminal)
        {
            var terminal = TerminalFromKingCapture(placementCommit.KingCaptureResult);
            var runtimeAfter = FacilityRuntimeAnalyzer.Analyze(
                placementCommit.FacilityStateAfterCommit,
                territoryAfter,
                source.RuntimePolicy.FacilityPolicy);
            var stateAfter = BattleState.Transition(
                source,
                placementCommit.BoardAfterCommit,
                placementCommit.HistoryAfterCommit,
                placementCommit.FacilityStateAfterCommit,
                territoryAfter,
                runtimeAfter,
                source.PlayerTurnIndex,
                BattlePhase.Ended,
                terminal.Outcome,
                terminal.Reason);
            orderedFacts.Add(new BattleEndedFact(terminal.Outcome, stateAfter.EndReasonId));
            return Accept(session, command, stateAfter, orderedFacts);
        }

        var territoryEstablished = TerritoryDeltaResolver.Resolve(
            source.TerritoryAnalysis,
            territoryAfter,
            command.Actor);
        if (territoryEstablished is not null)
        {
            orderedFacts.Add(territoryEstablished);
        }

        var facilityTransition = FacilityOperatingTransitionResolver
            .ReassociateAfterPlacement(
                source.FacilityRuntimeAnalysis,
                placementCommit,
                territoryAfter);
        orderedFacts.AddRange(facilityTransition.OrderedFacts.Cast<IBattleFact>());

        var next = command.Actor == StoneColor.White
            ? CompleteEnemyBoundary(
                source,
                placementCommit,
                territoryAfter,
                facilityTransition.AnalysisAfter,
                orderedFacts)
            : BattleState.Transition(
                source,
                placementCommit.BoardAfterCommit,
                placementCommit.HistoryAfterCommit,
                placementCommit.FacilityStateAfterCommit,
                territoryAfter,
                facilityTransition.AnalysisAfter,
                source.PlayerTurnIndex,
                BattlePhase.PlayerAction,
                BattleOutcome.Ongoing,
                BattleEndReason.None);
        return Accept(session, command, next, orderedFacts);
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
        FacilityPlacementCommit placementCommit,
        TerritoryAnalysis territoryAfter,
        FacilityRuntimeAnalysis facilityRuntimeAfter,
        List<IBattleFact> orderedFacts)
    {
        if (source.PlayerTurnIndex >= source.RuntimePolicy.PlayerTurnLimit)
        {
            orderedFacts.Add(new BattleEndedFact(BattleOutcome.PlayerDefeat, "turn_limit"));
            return BattleState.Transition(
                source,
                placementCommit.BoardAfterCommit,
                placementCommit.HistoryAfterCommit,
                placementCommit.FacilityStateAfterCommit,
                territoryAfter,
                facilityRuntimeAfter,
                source.PlayerTurnIndex,
                BattlePhase.Ended,
                BattleOutcome.PlayerDefeat,
                BattleEndReason.TurnLimit);
        }

        return BattleState.Transition(
            source,
            placementCommit.BoardAfterCommit,
            placementCommit.HistoryAfterCommit,
            placementCommit.FacilityStateAfterCommit,
            territoryAfter,
            facilityRuntimeAfter,
            source.PlayerTurnIndex + 1,
            BattlePhase.PlayerAction,
            BattleOutcome.Ongoing,
            BattleEndReason.None);
    }

    private static BattleState CompleteEnemyBoundary(
        BattleState source,
        List<IBattleFact> orderedFacts)
    {
        if (source.PlayerTurnIndex >= source.RuntimePolicy.PlayerTurnLimit)
        {
            orderedFacts.Add(new BattleEndedFact(BattleOutcome.PlayerDefeat, "turn_limit"));
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

    private static BattleCommandResult Accept(
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

    private static BattleCommandResult Reject(
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

    private static EffectiveLibertySnapshot RealLiberties(StoneGroupAnalysis analysis) =>
        EffectiveLibertySnapshot.Create(
            analysis,
            analysis.Groups.Select(group => new GroupEffectiveLiberty(
                group,
                group.RealLibertyCount)));

    private static (BattleOutcome Outcome, BattleEndReason Reason) TerminalFromKingCapture(
        KingCaptureResult result)
    {
        if (result.BlackKingCaptured && result.WhiteKingCaptured)
        {
            return (BattleOutcome.PlayerDefeat, BattleEndReason.BothKingsCaptured);
        }

        if (result.BlackKingCaptured)
        {
            return (BattleOutcome.PlayerDefeat, BattleEndReason.BlackKingCaptured);
        }

        if (result.WhiteKingCaptured)
        {
            return (BattleOutcome.PlayerVictory, BattleEndReason.WhiteKingCaptured);
        }

        throw new ArgumentException("Terminal king result contains no captured king.", nameof(result));
    }
}
