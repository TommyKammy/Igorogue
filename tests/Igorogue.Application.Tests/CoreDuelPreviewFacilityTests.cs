using System.Globalization;

using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

public sealed class CoreDuelPreviewFacilityTests
{
    [Fact]
    public void DevelopmentBuildAndSubsequentStoneDestructionAreProjectedFromCommits()
    {
        var session = StartFacilityFixture(seed: 5);
        var development = session.State.CardTurnState.Deck.Hand.Single(card =>
            StringComparer.Ordinal.Equals(card.ContentId, "card_development"));
        var developmentPreview = Query(session, development.InstanceId);
        var build = Candidate(developmentPreview, 2, 2, "none");

        Assert.True(build.Accepted, build.ReasonId);
        var buildResult = Assert.IsType<CoreDuelAcceptedCardPlayPreview>(
            build.AcceptedResult);
        var builtDelta = Assert.Single(buildResult.FacilityDeltas);
        Assert.True(builtDelta.WasBuilt);
        Assert.False(builtDelta.WasDestroyed);
        Assert.Null(builtDelta.Before);
        var built = Assert.IsType<CoreDuelFacilityPreview>(builtDelta.After);
        Assert.Equal("development", built.ContentId);
        Assert.Equal(CoreDuelBattleTestFixture.Point(2, 2), built.Point);
        Assert.True(built.IsActive);
        Assert.Equal("active", built.OperatingReasonId);

        var committedBuild = CoreDuelBattleStateMachine.Execute(
            session,
            build.CommitCommand);
        Assert.True(committedBuild.Accepted, committedBuild.ReasonId);
        Assert.Equal(buildResult.StateChecksum, committedBuild.StateChecksum);
        session = committedBuild.SessionAfter;

        var battlePreview = QueryBattle(session);
        var facilityPoint = battlePreview.BoardPoints.Single(point =>
            point.Point.Equals(CoreDuelBattleTestFixture.Point(2, 2)));
        var projectedFacility = Assert.IsType<CoreDuelFacilityPreview>(
            facilityPoint.Facility);
        Assert.Equal(built.InstanceId, projectedFacility.InstanceId);
        Assert.Equal(built.ContentId, projectedFacility.ContentId);
        Assert.Equal(built.OwnerId, projectedFacility.OwnerId);
        Assert.Equal(built.Point, projectedFacility.Point);
        Assert.Equal(built.BuildSequence, projectedFacility.BuildSequence);
        Assert.Equal(built.ExplicitDisableSources, projectedFacility.ExplicitDisableSources);
        Assert.Equal(built.IsActive, projectedFacility.IsActive);
        Assert.Equal(built.OperatingReasonId, projectedFacility.OperatingReasonId);

        var stone = session.State.CardTurnState.Deck.Hand.First(card =>
            StringComparer.Ordinal.Equals(card.ContentId, "card_basic_stone") ||
            StringComparer.Ordinal.Equals(card.ContentId, "card_extend"));
        var stonePreview = Query(session, stone.InstanceId);
        var destroy = Candidate(stonePreview, 2, 2, "frontline");

        Assert.True(destroy.Accepted, destroy.ReasonId);
        var destroyResult = Assert.IsType<CoreDuelAcceptedCardPlayPreview>(
            destroy.AcceptedResult);
        var destroyedDelta = Assert.Single(destroyResult.FacilityDeltas);
        Assert.Equal(builtDelta.InstanceId, destroyedDelta.InstanceId);
        Assert.False(destroyedDelta.WasBuilt);
        Assert.True(destroyedDelta.WasDestroyed);
        Assert.NotNull(destroyedDelta.Before);
        Assert.Null(destroyedDelta.After);
        Assert.Contains(
            destroyResult.TerritoryDeltas,
            delta => delta.Point.Equals(CoreDuelBattleTestFixture.Point(2, 2)) &&
                StringComparer.Ordinal.Equals(delta.OwnerBeforeId, "black") &&
                StringComparer.Ordinal.Equals(delta.OwnerAfterId, "none"));

        var committedDestroy = CoreDuelBattleStateMachine.Execute(
            session,
            destroy.CommitCommand);
        Assert.True(committedDestroy.Accepted, committedDestroy.ReasonId);
        Assert.Equal(destroyResult.StateChecksum, committedDestroy.StateChecksum);
        var afterDestroy = QueryBattle(committedDestroy.SessionAfter);
        var occupiedPoint = afterDestroy.BoardPoints.Single(point =>
            point.Point.Equals(CoreDuelBattleTestFixture.Point(2, 2)));
        Assert.Null(occupiedPoint.Facility);
        Assert.Equal("none", occupiedPoint.TerritoryOwnerId);
        var placedStone = Assert.IsType<CoreDuelStonePreview>(occupiedPoint.Stone);
        Assert.Equal("black", placedStone.ColorId);
        Assert.False(placedStone.IsKing);
        Assert.Contains(
            committedDestroy.OrderedFacts,
            fact => fact is FacilityDestroyedFact destroyed &&
                StringComparer.Ordinal.Equals(
                    destroyed.Facility.InstanceId,
                    destroyedDelta.InstanceId));
    }

    [Fact]
    public void ReinforceProjectsTheSharedEffectiveLibertyAnalysis()
    {
        var session = CoreDuelBattleTestFixture.Start(seed: 0).Session;
        var reinforce = session.State.CardTurnState.Deck.Hand.Single(card =>
            StringComparer.Ordinal.Equals(card.ContentId, "card_reinforce"));
        var preview = Query(session, reinforce.InstanceId);
        var candidate = Candidate(preview, 2, 2, "none");

        Assert.True(candidate.Accepted, candidate.ReasonId);
        var accepted = Assert.IsType<CoreDuelAcceptedCardPlayPreview>(
            candidate.AcceptedResult);
        var before = Assert.IsType<CoreDuelGroupPreview>(preview.BlackKingRisk!.Group);
        var after = Assert.IsType<CoreDuelGroupPreview>(accepted.ResultingTargetGroup);
        Assert.Equal(before.Anchor, after.Anchor);
        Assert.Equal(before.RealLibertyPoints, after.RealLibertyPoints);
        Assert.Equal(before.TimedLibertyAmount + 1, after.TimedLibertyAmount);
        Assert.Equal(before.ContinuousLibertyAmount, after.ContinuousLibertyAmount);
        Assert.Equal(before.EffectiveLibertyCount + 1, after.EffectiveLibertyCount);
        var resultingGroup = accepted.ResultingGroups.Single(group =>
            group.Anchor.Equals(after.Anchor));
        Assert.Equal(after.TimedLibertyAmount, resultingGroup.TimedLibertyAmount);
        Assert.Equal(after.EffectiveLibertyCount, resultingGroup.EffectiveLibertyCount);
        Assert.Empty(accepted.CapturedGroups);
        Assert.Empty(accepted.TerritoryDeltas);
        Assert.Empty(accepted.FacilityDeltas);

        var committed = CoreDuelBattleStateMachine.Execute(
            session,
            candidate.CommitCommand);
        Assert.True(committed.Accepted, committed.ReasonId);
        Assert.Equal(accepted.StateChecksum, committed.StateChecksum);
        Assert.Equal(accepted.LogChecksum, committed.LogChecksum);
    }

    private static CoreDuelCardPreviewResult Query(
        CoreDuelBattleSession session,
        string cardInstanceId) =>
        CoreDuelBattlePreviewQuery.Evaluate(
            session,
            new CoreDuelCardPreviewRequest(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum,
                cardInstanceId));

    private static CoreDuelBattlePreviewResult QueryBattle(
        CoreDuelBattleSession session) =>
        CoreDuelBattlePreviewQuery.Evaluate(
            session,
            new CoreDuelBattlePreviewRequest(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));

    private static CoreDuelCardCandidatePreview Candidate(
        CoreDuelCardPreviewResult preview,
        int x,
        int y,
        string modeId) =>
        preview.Candidates.Single(candidate =>
            candidate.Target.X == x &&
            candidate.Target.Y == y &&
            StringComparer.Ordinal.Equals(candidate.PlacementModeId, modeId));

    private static CoreDuelBattleSession StartFacilityFixture(long seed)
    {
        var catalog = CoreDuelBattleTestFixture.LoadCatalog();
        return CoreDuelBattleStateMachine.Start(
            FacilityInitialSnapshot(),
            catalog,
            ReplayMetadata.Create(
                CoreDuelBattleTestFixture.GameVersion,
                catalog.ContentHash,
                seed)).Session;
    }

    private static BattleAuthoritativeInitialSnapshot FacilityInitialSnapshot()
    {
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var board = BoardState.Create(
            geometry,
            [
                Stone(geometry, StoneColor.Black, isKing: true, 1, 2),
                Stone(geometry, StoneColor.Black, isKing: false, 2, 1),
                Stone(geometry, StoneColor.Black, isKing: false, 3, 2),
                Stone(geometry, StoneColor.Black, isKing: false, 2, 3),
                Stone(geometry, StoneColor.White, isKing: true, 7, 7),
            ]);
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                "preview.facility.stone." +
                index.ToString("D2", CultureInfo.InvariantCulture),
                stone,
                stone.IsKing ? "king" : "standard",
                index + 1L,
                []))
            .ToArray();
        var runtime = StoneRuntimeState.Create(
            board,
            instances,
            instances.Length + 1L);
        var standard = CoreDuelBattleTestFixture.InitialSnapshot();
        return BattleAuthoritativeInitialSnapshot.Create(
            runtime,
            TemporaryLibertyState.Create(runtime, [], nextCreatedSequence: 1),
            ContinuousLibertySnapshot.Empty(runtime),
            BattleRepetitionHistory.Start(board),
            FacilityState.Create(board, [], nextBuildSequence: 1),
            ClosedWindowResourceState.Empty([]),
            CaptureBenefitTriggerPlan.Create([]),
            standard.CounterattackState,
            standard.CounterattackPolicy,
            standard.RuntimePolicy,
            playerTurnIndex: 1);
    }

    private static BoardStone Stone(
        BoardGeometry geometry,
        StoneColor color,
        bool isKing,
        int x,
        int y) =>
        new(color, isKing, geometry.CreateCanonicalPoint(x, y));
}
