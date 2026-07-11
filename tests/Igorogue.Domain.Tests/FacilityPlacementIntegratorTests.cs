using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Tests;

public sealed class FacilityPlacementIntegratorTests
{
    private static readonly BoardGeometry Geometry = FacilityFixtureData.Geometry;

    [Fact]
    public void Fac03LegalPlacementDestroysOnlyThePlacementPointFacilityInTypedOrder()
    {
        var fixture = FacilityFixtureData.LoadFixtures()["FAC-03"];
        var state = FacilityFixtureData.CreateInitialState(fixture);
        var history = BattleRepetitionHistory.Start(fixture.Board);
        var accepted = CommitLegalPlacement(
            fixture.Board,
            Assert.IsType<StoneColor>(fixture.Actor),
            Assert.IsType<CanonicalPoint>(fixture.Point),
            history);

        var commit = FacilityPlacementIntegrator.Apply(state, accepted);

        Assert.Same(state, commit.SourceFacilityState);
        Assert.Same(accepted.Candidate, commit.Candidate);
        Assert.Same(accepted.BoardAfterCommit, commit.BoardAfterCommit);
        Assert.Same(accepted.BoardAfterCommit, commit.FacilityStateAfterCommit.SourceBoard);
        Assert.Same(accepted.RegisteredTopologyKey, commit.RegisteredTopologyKey);
        Assert.Same(accepted.HistoryAfterCommit, commit.HistoryAfterCommit);
        Assert.Same(accepted.KingCaptureResult, commit.KingCaptureResult);
        Assert.Equal(2, commit.FacilityStateAfterCommit.NextBuildSequence);
        Assert.Empty(commit.FacilityStateAfterCommit.InstalledFacilities);
        Assert.Equal("facility_03", commit.DestructionFact?.Facility.InstanceId);
        Assert.Equal("stone_occupied", commit.DestructionFact?.ReasonId);
        Assert.Equal(
            new[]
            {
                typeof(StonePlacedFact),
                typeof(FacilityDestroyedFact),
                typeof(StoneTopologyRegisteredFact),
                typeof(KingCaptureEvaluatedFact),
            },
            commit.OrderedFacts.Select(fact => fact.GetType()));
        Assert.Equal(fixture.Expected.Events, FixtureEvents(commit.OrderedFacts));
        Assert.Equal(
            StoneTopologyKey.FromBoard(Assert.IsType<BoardState>(fixture.Expected.ResultBoard)),
            StoneTopologyKey.FromBoard(commit.BoardAfterCommit));
        Assert.Equal(2, commit.HistoryAfterCommit.ObservationCount);
        Assert.Equal(1, history.ObservationCount);
        var topologyFact = Assert.IsType<StoneTopologyRegisteredFact>(commit.OrderedFacts[^2]);
        Assert.Same(commit.RegisteredTopologyKey, topologyFact.RegisteredTopologyKey);
        Assert.Same(commit.HistoryAfterCommit, topologyFact.HistoryAfterRegistration);
        var kingFact = Assert.IsType<KingCaptureEvaluatedFact>(commit.OrderedFacts[^1]);
        Assert.Same(commit.KingCaptureResult, kingFact.Result);

        var facts = Assert.IsAssignableFrom<ICollection<ICommittedPlacementFact>>(
            commit.OrderedFacts);
        Assert.True(facts.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => facts.Add(commit.OrderedFacts[0]));
    }

    [Fact]
    public void CaptureFactsPrecedeFacilityDestructionTopologyAndTerminalResult()
    {
        var source = SingleWhiteKingCaptureBoard();
        var point = C(3, 2);
        var facility = new FacilityInstance(
            "terminal-site",
            "development",
            StoneColor.White,
            point,
            4);
        var state = FacilityState.Create(source, [facility], 9);
        var accepted = CommitLegalPlacement(
            source,
            StoneColor.Black,
            point,
            BattleRepetitionHistory.Start(source));

        var commit = FacilityPlacementIntegrator.Apply(state, accepted);

        Assert.Equal(
            new[]
            {
                typeof(StonePlacedFact),
                typeof(GroupCapturedFact),
                typeof(FacilityDestroyedFact),
                typeof(StoneTopologyRegisteredFact),
                typeof(KingCaptureEvaluatedFact),
            },
            commit.OrderedFacts.Select(fact => fact.GetType()));
        var capture = Assert.IsType<GroupCapturedFact>(commit.OrderedFacts[1]);
        Assert.True(capture.ContainsKing);
        Assert.Equal(BattleOutcome.PlayerVictory, commit.KingCaptureResult.Outcome);
        Assert.Equal(9, commit.FacilityStateAfterCommit.NextBuildSequence);
    }

    [Fact]
    public void Fac04IllegalSuicideCannotProduceAPlacementOrDestructionCommit()
    {
        var fixture = FacilityFixtureData.LoadFixtures()["FAC-04"];
        var state = FacilityFixtureData.CreateInitialState(fixture);
        var stateBefore = state.ToCanonicalText();
        var history = BattleRepetitionHistory.Start(fixture.Board);
        var historyBefore = history.ToCanonicalText();
        var candidate = Resolve(
            fixture.Board,
            Assert.IsType<StoneColor>(fixture.Actor),
            Assert.IsType<CanonicalPoint>(fixture.Point));
        var evaluation = PlacementLegalityEvaluator.Evaluate(
            candidate,
            RealLiberties(candidate.GroupsAfterCapture),
            history,
            PlacementAccessMode.Normal);

        Assert.False(evaluation.IsLegal);
        Assert.Equal(PlacementLegalityStatus.Suicide, evaluation.Status);
        Assert.Equal(fixture.Expected.Reason, evaluation.ReasonId);
        Assert.Throws<InvalidOperationException>(() => history.CommitLegalPlacement(evaluation));
        Assert.Equal(stateBefore, state.ToCanonicalText());
        Assert.Equal(historyBefore, history.ToCanonicalText());
        Assert.Equal(new[] { "facility_04" }, state.InstalledFacilities.Select(f => f.InstanceId));
    }

    [Fact]
    public void PlacementRejectsEquivalentForeignSnapshotAndNeverRestoresDestroyedFacility()
    {
        var fixture = FacilityFixtureData.LoadFixtures()["FAC-03"];
        var accepted = CommitLegalPlacement(
            fixture.Board,
            Assert.IsType<StoneColor>(fixture.Actor),
            Assert.IsType<CanonicalPoint>(fixture.Point),
            BattleRepetitionHistory.Start(fixture.Board));
        var equivalentBoard = BoardState.Create(
            Geometry,
            fixture.Board.OccupiedStones.Reverse());
        var foreignState = FacilityState.Create(
            equivalentBoard,
            fixture.Facilities.Select(facility => facility.ToDomain()),
            fixture.InitialNextBuildSequence);

        Assert.Throws<ArgumentException>(() =>
            FacilityPlacementIntegrator.Apply(foreignState, accepted));

        var sourceState = FacilityFixtureData.CreateInitialState(fixture);
        var committed = FacilityPlacementIntegrator.Apply(sourceState, accepted);
        var placedPoint = accepted.Candidate.PlacedStone.Point;
        var laterEmptyBoard = BoardState.Create(
            Geometry,
            committed.BoardAfterCommit.OccupiedStones
                .Where(stone => stone.Point != placedPoint));
        var laterState = FacilityState.Create(
            laterEmptyBoard,
            committed.FacilityStateAfterCommit.InstalledFacilities,
            committed.FacilityStateAfterCommit.NextBuildSequence);

        Assert.True(laterEmptyBoard.IsEmpty(placedPoint));
        Assert.Null(laterState.FacilityAt(placedPoint));
        Assert.Empty(laterState.InstalledFacilities);
    }

    [Fact]
    public void LegalPlacementWithoutFacilityStillPublishesExactlyOneTopologyStep()
    {
        var board = BoardState.Create(Geometry, []);
        var state = FacilityState.Create(board, [], 1);
        var accepted = CommitLegalPlacement(
            board,
            StoneColor.Black,
            C(4, 4),
            BattleRepetitionHistory.Start(board));

        var commit = FacilityPlacementIntegrator.Apply(state, accepted);

        Assert.Null(commit.DestructionFact);
        Assert.Single(commit.OrderedFacts.OfType<StoneTopologyRegisteredFact>());
        Assert.IsType<StonePlacedFact>(commit.OrderedFacts[0]);
        Assert.IsType<StoneTopologyRegisteredFact>(commit.OrderedFacts[1]);
        Assert.IsType<KingCaptureEvaluatedFact>(commit.OrderedFacts[2]);
        Assert.Equal(2, commit.HistoryAfterCommit.ObservationCount);
    }

    [Fact]
    public void PlacementLeavesEveryFacilityOutsideThePlacementPointUntouched()
    {
        var board = BoardState.Create(Geometry, []);
        var target = new FacilityInstance(
            "target",
            "development",
            StoneColor.Black,
            C(4, 4),
            1);
        var survivor = new FacilityInstance(
            "survivor",
            "furnace",
            StoneColor.White,
            C(1, 1),
            2);
        var state = FacilityState.Create(board, [survivor, target], 3);
        var accepted = CommitLegalPlacement(
            board,
            StoneColor.Black,
            target.Point,
            BattleRepetitionHistory.Start(board));

        var commit = FacilityPlacementIntegrator.Apply(state, accepted);

        Assert.Same(target, commit.DestructionFact?.Facility);
        Assert.Single(commit.FacilityStateAfterCommit.InstalledFacilities);
        Assert.Same(survivor, commit.FacilityStateAfterCommit.FacilityAt(survivor.Point));
        Assert.Equal(3, commit.FacilityStateAfterCommit.NextBuildSequence);
    }

    private static LegalPlacementCommit CommitLegalPlacement(
        BoardState source,
        StoneColor actor,
        CanonicalPoint point,
        BattleRepetitionHistory history)
    {
        var candidate = Resolve(source, actor, point);
        var evaluation = PlacementLegalityEvaluator.Evaluate(
            candidate,
            RealLiberties(candidate.GroupsAfterCapture),
            history,
            PlacementAccessMode.Normal);
        Assert.True(evaluation.IsLegal);
        return history.CommitLegalPlacement(evaluation);
    }

    private static HypotheticalPlacementResolution Resolve(
        BoardState source,
        StoneColor actor,
        CanonicalPoint point)
    {
        if (!HypotheticalPlacementResolver.TryCreate(
                source,
                new BoardStone(actor, false, point),
                out var placement) ||
            placement is null)
        {
            throw new InvalidOperationException("Test placement unexpectedly targeted a stone.");
        }

        return HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            RealLiberties(placement.GroupsAfterPlacement));
    }

    private static EffectiveLibertySnapshot RealLiberties(StoneGroupAnalysis analysis) =>
        EffectiveLibertySnapshot.Create(
            analysis,
            analysis.Groups.Select(group => new GroupEffectiveLiberty(
                group,
                group.RealLibertyCount)));

    private static string[] FixtureEvents(
        IEnumerable<ICommittedPlacementFact> facts) =>
        facts.Select(fact => fact switch
            {
                StonePlacedFact placed =>
                    $"StonePlaced:{ColorId(placed.Stone.Color)}:{placed.Stone.Point.X},{placed.Stone.Point.Y}",
                GroupCapturedFact captured =>
                    $"GroupCaptured:{captured.CapturedGroup.Anchor}",
                FacilityDestroyedFact destroyed =>
                    $"FacilityDestroyed:{destroyed.Facility.InstanceId}:{destroyed.ReasonId}",
                _ => null,
            })
            .Where(value => value is not null)
            .Select(value => value!)
            .ToArray();

    private static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Unknown test stone color."),
    };

    private static BoardState SingleWhiteKingCaptureBoard() => BoardState.Create(
        Geometry,
        [
            new BoardStone(StoneColor.Black, false, C(3, 4)),
            new BoardStone(StoneColor.Black, false, C(2, 3)),
            new BoardStone(StoneColor.Black, false, C(4, 3)),
            new BoardStone(StoneColor.White, true, C(3, 3)),
            new BoardStone(StoneColor.White, false, C(2, 2)),
            new BoardStone(StoneColor.White, false, C(4, 2)),
            new BoardStone(StoneColor.White, false, C(3, 1)),
            new BoardStone(StoneColor.Black, true, C(7, 7)),
        ]);

    private static CanonicalPoint C(int x, int y) => Geometry.CreateCanonicalPoint(x, y);
}
