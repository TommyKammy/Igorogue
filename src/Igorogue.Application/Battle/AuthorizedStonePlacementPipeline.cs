using System.Collections.ObjectModel;

using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Battle;

internal sealed class AuthorizedStonePlacementResolution
{
    private readonly ReadOnlyCollection<IBattleFact> orderedFactView;

    private AuthorizedStonePlacementResolution(
        BattleState stateBefore,
        BattleState stateAfter,
        bool accepted,
        string reasonId,
        IBattleFact[] orderedFacts)
    {
        StateBefore = stateBefore;
        StateAfter = stateAfter;
        Accepted = accepted;
        ReasonId = reasonId;
        orderedFactView = Array.AsReadOnly((IBattleFact[])orderedFacts.Clone());
    }

    internal BattleState StateBefore { get; }

    internal BattleState StateAfter { get; }

    internal bool Accepted { get; }

    internal string ReasonId { get; }

    internal IReadOnlyList<IBattleFact> OrderedFacts => orderedFactView;

    internal static AuthorizedStonePlacementResolution Accept(
        BattleState source,
        BattleState stateAfter,
        IEnumerable<IBattleFact> orderedFacts) =>
        new(source, stateAfter, true, "accepted", orderedFacts.ToArray());

    internal static AuthorizedStonePlacementResolution Reject(
        BattleState source,
        string reasonId) =>
        new(source, source, false, reasonId, []);
}

internal static class AuthorizedStonePlacementPipeline
{
    internal static AuthorizedStonePlacementResolution Resolve(
        BattleState source,
        StoneColor actor,
        CanonicalPoint point,
        PlacementAccessMode accessMode)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(point);
        var expectedActor = source.Phase switch
        {
            BattlePhase.PlayerAction => StoneColor.Black,
            BattlePhase.EnemyAction => StoneColor.White,
            _ => throw new InvalidOperationException(
                "Terminal phase must be rejected before placement resolution."),
        };
        if (actor != expectedActor)
        {
            return AuthorizedStonePlacementResolution.Reject(
                source,
                "wrong_actor_for_phase");
        }

        var proposedStone = new BoardStone(actor, false, point);
        if (!HypotheticalPlacementResolver.TryCreate(
                source.Board,
                proposedStone,
                out var hypothetical) ||
            hypothetical is null)
        {
            return AuthorizedStonePlacementResolution.Reject(source, "stone_occupied");
        }

        var resolved = HypotheticalPlacementResolver.ResolveCaptures(
            hypothetical,
            RealLiberties(hypothetical.GroupsAfterPlacement));
        var legality = PlacementLegalityEvaluator.Evaluate(
            resolved,
            RealLiberties(resolved.GroupsAfterCapture),
            source.RepetitionHistory,
            accessMode);
        if (!legality.IsLegal)
        {
            return AuthorizedStonePlacementResolution.Reject(
                source,
                legality.ReasonId);
        }

        var legalPlacement = source.RepetitionHistory.CommitLegalPlacement(legality);
        return Commit(source, actor, legalPlacement);
    }

    internal static AuthorizedStonePlacementResolution Commit(
        BattleState source,
        StoneColor actor,
        LegalPlacementCommit legalPlacement)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(legalPlacement);
        var expectedActor = source.Phase switch
        {
            BattlePhase.PlayerAction => StoneColor.Black,
            BattlePhase.EnemyAction => StoneColor.White,
            _ => throw new InvalidOperationException(
                "Terminal phase must be rejected before placement commit."),
        };
        if (actor != expectedActor)
        {
            return AuthorizedStonePlacementResolution.Reject(
                source,
                "wrong_actor_for_phase");
        }

        if (!ReferenceEquals(legalPlacement.Candidate.SourceBoard, source.Board))
        {
            throw new ArgumentException(
                "Legal placement must belong to the exact battle board snapshot.",
                nameof(legalPlacement));
        }

        var placementCommit = FacilityPlacementIntegrator.Apply(
            source.FacilityState,
            legalPlacement);
        var orderedFacts = placementCommit.OrderedFacts
            .Cast<IBattleFact>()
            .ToList();
        var territoryAfter = TerritoryAnalyzer.Analyze(placementCommit.BoardAfterCommit);

        if (placementCommit.KingCaptureResult.IsTerminal)
        {
            var terminal = placementCommit.KingCaptureResult;
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
                terminal.EndReason);
            orderedFacts.Add(new BattleEndedFact(terminal.Outcome, terminal.EndReason));
            return AuthorizedStonePlacementResolution.Accept(
                source,
                stateAfter,
                orderedFacts);
        }

        var territoryEstablished = TerritoryDeltaResolver.Resolve(
            source.TerritoryAnalysis,
            territoryAfter,
            placementCommit,
            actor);
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

        var next = actor == StoneColor.White
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
        return AuthorizedStonePlacementResolution.Accept(source, next, orderedFacts);
    }

    private static BattleState CompleteEnemyBoundary(
        BattleState source,
        FacilityPlacementCommit placementCommit,
        TerritoryAnalysis territoryAfter,
        FacilityRuntimeAnalysis facilityRuntimeAfter,
        ICollection<IBattleFact> orderedFacts)
    {
        if (source.PlayerTurnIndex >= source.RuntimePolicy.PlayerTurnLimit)
        {
            orderedFacts.Add(new BattleEndedFact(
                BattleOutcome.PlayerDefeat,
                BattleEndReason.TurnLimit));
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

    private static EffectiveLibertySnapshot RealLiberties(StoneGroupAnalysis analysis) =>
        EffectiveLibertySnapshot.Create(
            analysis,
            analysis.Groups.Select(group => new GroupEffectiveLiberty(
                group,
                group.RealLibertyCount)));
}
