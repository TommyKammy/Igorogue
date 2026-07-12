using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Tests;

public sealed class TerritoryDeltaResolverTests
{
    private static readonly BoardGeometry Geometry =
        BoardGeometry.Create(BoardGeometry.AcceptedSize);

    [Fact]
    public void BlackSourceEmitsOneFactWithEveryNewBlackPointInCanonicalOrder()
    {
        var source = Board();
        var commit = Commit(source, Stone(StoneColor.Black, 7, 7));
        var before = TerritoryAnalyzer.Analyze(source);
        var after = TerritoryAnalyzer.Analyze(commit.BoardAfterCommit);

        var fact = Assert.IsType<TerritoryEstablishedFact>(
            TerritoryDeltaResolver.Resolve(
                before,
                after,
                commit,
                StoneColor.Black));

        Assert.Equal(StoneColor.Black, fact.SourceActor);
        Assert.Equal(TerritoryEstablishmentSourceKind.Placement, fact.SourceKind);
        Assert.Equal(TerritoryDeltaResolver.StonePlacementSourceReasonId, fact.SourceReasonId);
        Assert.True(fact.ImplicitMomentumEligible);
        Assert.Equal(
            Geometry.CanonicalPoints.Where(point => point != C(7, 7)),
            fact.ChangedPoints);
        Assert.True(fact.ChangedPoints
            .Zip(fact.ChangedPoints.Skip(1))
            .All(pair => pair.First.CompareTo(pair.Second) < 0));
        Assert.IsAssignableFrom<IBattleFact>(fact);

        var points = Assert.IsAssignableFrom<ICollection<CanonicalPoint>>(
            fact.ChangedPoints);
        Assert.True(points.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => points.Add(C(7, 7)));
    }

    [Fact]
    public void DeltaIsDirectionalAndPreservesEitherSourceActor()
    {
        var neutral = TerritoryAnalyzer.Analyze(Board());
        var black = TerritoryAnalyzer.Analyze(Board(
            Stone(StoneColor.Black, 7, 7)));
        var white = TerritoryAnalyzer.Analyze(Board(
            Stone(StoneColor.White, 7, 7)));

        Assert.NotNull(TerritoryDeltaResolver.ResolveCore(
            neutral,
            black,
            StoneColor.Black));
        Assert.Null(TerritoryDeltaResolver.ResolveCore(
            black,
            neutral,
            StoneColor.Black));
        var whiteSourceFact = Assert.IsType<TerritoryEstablishedFact>(
            TerritoryDeltaResolver.ResolveCore(
                neutral,
                black,
                StoneColor.White));
        Assert.Equal(StoneColor.White, whiteSourceFact.SourceActor);
        Assert.Equal(
            TerritoryEstablishmentSourceKind.Placement,
            whiteSourceFact.SourceKind);
        Assert.Equal(
            TerritoryDeltaResolver.StonePlacementSourceReasonId,
            whiteSourceFact.SourceReasonId);
        Assert.False(whiteSourceFact.ImplicitMomentumEligible);
        Assert.Null(TerritoryDeltaResolver.ResolveCore(
            neutral,
            white,
            StoneColor.Black));
    }

    [Fact]
    public void TemporaryLibertyExpiryEstablishesCanonicalTerritoryWithoutImplicitMomentum()
    {
        var (before, expiry) = CenterTerritoryExpiry();

        var fact = Assert.IsType<TerritoryEstablishedFact>(
            TerritoryDeltaResolver.ResolveAfterExpiry(before, expiry));

        var captured = Assert.Single(expiry.CapturedGroups);
        Assert.Equal(StoneColor.White, captured.Color);
        Assert.Equal(C(4, 4), captured.Anchor);
        Assert.Equal(StoneColor.White, fact.SourceActor);
        Assert.Equal(
            TerritoryEstablishmentSourceKind.TemporaryLibertyExpiry,
            fact.SourceKind);
        Assert.Equal(
            TemporaryLibertyExpiryResolver.TopologySourceReasonId,
            fact.SourceReasonId);
        Assert.False(fact.ImplicitMomentumEligible);
        Assert.Equal([C(4, 4)], fact.ChangedPoints);
    }

    [Fact]
    public void ExpiryTerritoryDeltaRejectsForeignAndStaleBeforeSnapshots()
    {
        var (_, expiry) = CenterTerritoryExpiry();
        var source = expiry.SourceStones.SourceBoard;
        var foreignBoard = BoardState.Create(
            Geometry,
            source.OccupiedStones.Select(stone => new BoardStone(
                stone.Color,
                stone.IsKing,
                stone.Point)));
        var foreignBefore = TerritoryAnalyzer.Analyze(foreignBoard);
        var staleBefore = TerritoryAnalyzer.Analyze(expiry.BoardAfterResolution);

        Assert.Throws<ArgumentException>(() =>
            TerritoryDeltaResolver.ResolveAfterExpiry(foreignBefore, expiry));
        Assert.Throws<ArgumentException>(() =>
            TerritoryDeltaResolver.ResolveAfterExpiry(staleBefore, expiry));
    }

    [Fact]
    public void ExistingBlackPointsAreExcludedWhileNewlyEmptiedBlackPointsAreIncluded()
    {
        var separatingWall = Enumerable.Range(1, 7)
            .Select(y => Stone(StoneColor.Black, 2, y))
            .ToArray();
        var before = TerritoryAnalyzer.Analyze(Board(
            separatingWall.Append(Stone(StoneColor.White, 1, 1)).ToArray()));
        var after = TerritoryAnalyzer.Analyze(Board(separatingWall));

        var fact = Assert.IsType<TerritoryEstablishedFact>(
            TerritoryDeltaResolver.ResolveCore(before, after, StoneColor.Black));

        Assert.Equal(
            Enumerable.Range(1, 7).Select(y => C(1, y)),
            fact.ChangedPoints);
        Assert.DoesNotContain(C(3, 1), fact.ChangedPoints);
    }

    [Fact]
    public void ResolverRejectsUnknownActorForeignGeometryAndStalePlacementSnapshots()
    {
        var before = TerritoryAnalyzer.Analyze(Board());
        var foreignGeometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var foreignAfter = TerritoryAnalyzer.Analyze(BoardState.Create(
            foreignGeometry,
            [new BoardStone(
                StoneColor.Black,
                false,
                foreignGeometry.CreateCanonicalPoint(7, 7))]));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TerritoryDeltaResolver.ResolveCore(before, before, (StoneColor)99));
        Assert.Throws<ArgumentException>(() =>
            TerritoryDeltaResolver.ResolveCore(before, foreignAfter, StoneColor.Black));

        var source = Board();
        var commit = Commit(source, Stone(StoneColor.Black, 7, 7));
        var exactBefore = TerritoryAnalyzer.Analyze(source);
        var exactAfter = TerritoryAnalyzer.Analyze(commit.BoardAfterCommit);
        var staleBefore = TerritoryAnalyzer.Analyze(Board());
        var staleAfter = TerritoryAnalyzer.Analyze(Board(
            Stone(StoneColor.Black, 7, 7)));

        Assert.Throws<ArgumentException>(() => TerritoryDeltaResolver.Resolve(
            staleBefore,
            exactAfter,
            commit,
            StoneColor.Black));
        Assert.Throws<ArgumentException>(() => TerritoryDeltaResolver.Resolve(
            exactBefore,
            staleAfter,
            commit,
            StoneColor.Black));
    }

    private static FacilityPlacementCommit Commit(
        BoardState source,
        BoardStone proposedStone)
    {
        if (!HypotheticalPlacementResolver.TryCreate(source, proposedStone, out var placement) ||
            placement is null)
        {
            throw new InvalidOperationException("Test placement unexpectedly targeted an occupied point.");
        }

        var candidate = HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            RealLiberties(placement.GroupsAfterPlacement));
        var history = BattleRepetitionHistory.Start(source);
        var evaluation = PlacementLegalityEvaluator.Evaluate(
            candidate,
            RealLiberties(candidate.GroupsAfterCapture),
            history,
            PlacementAccessMode.Normal);
        return FacilityPlacementIntegrator.Apply(
            FacilityState.Create(source, [], 1),
            history.CommitLegalPlacement(evaluation));
    }

    private static EffectiveLibertySnapshot RealLiberties(StoneGroupAnalysis analysis) =>
        EffectiveLibertySnapshot.Create(
            analysis,
            analysis.Groups.Select(group => new GroupEffectiveLiberty(
                group,
                group.RealLibertyCount)));

    private static StoneRuntimeState StoneRuntime(BoardState board)
    {
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                $"stone_{index + 1}",
                stone,
                stone.IsKing ? "king" : "standard",
                index + 1L,
                []))
            .ToArray();
        return StoneRuntimeState.Create(board, instances, instances.Length + 1L);
    }

    private static (
        TerritoryAnalysis Before,
        TemporaryLibertyExpiryResolution Expiry) CenterTerritoryExpiry()
    {
        var source = Board(
            Stone(StoneColor.Black, 4, 3),
            Stone(StoneColor.Black, 3, 4),
            Stone(StoneColor.White, 4, 4),
            Stone(StoneColor.Black, 5, 4),
            Stone(StoneColor.Black, 4, 5));
        var stones = StoneRuntime(source);
        var white = Assert.IsType<StoneRuntimeInstance>(stones.InstanceAt(C(4, 4)));
        var temporary = TemporaryLibertyState.Create(
            stones,
            [
                new TemporaryLibertyEffect(
                    "territory_guard",
                    1,
                    StoneColor.White,
                    white.InstanceId,
                    "test",
                    1,
                    12),
            ],
            2);
        var expiry = TemporaryLibertyExpiryResolver.Resolve(
            stones,
            temporary,
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(source),
            12);
        return (TerritoryAnalyzer.Analyze(source), expiry);
    }

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(StoneColor color, int x, int y) =>
        new(color, false, C(x, y));

    private static CanonicalPoint C(int x, int y) =>
        Geometry.CreateCanonicalPoint(x, y);
}
