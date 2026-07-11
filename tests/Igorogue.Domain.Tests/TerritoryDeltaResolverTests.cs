using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

public sealed class TerritoryDeltaResolverTests
{
    private static readonly BoardGeometry Geometry =
        BoardGeometry.Create(BoardGeometry.AcceptedSize);

    [Fact]
    public void BlackSourceEmitsOneFactWithEveryNewBlackPointInCanonicalOrder()
    {
        var before = TerritoryAnalyzer.Analyze(Board());
        var after = TerritoryAnalyzer.Analyze(Board(
            Stone(StoneColor.Black, 7, 7)));

        var fact = Assert.IsType<TerritoryEstablishedFact>(
            TerritoryDeltaResolver.Resolve(before, after, StoneColor.Black));

        Assert.Equal(StoneColor.Black, fact.SourceActor);
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

        Assert.NotNull(TerritoryDeltaResolver.Resolve(
            neutral,
            black,
            StoneColor.Black));
        Assert.Null(TerritoryDeltaResolver.Resolve(
            black,
            neutral,
            StoneColor.Black));
        var whiteSourceFact = Assert.IsType<TerritoryEstablishedFact>(
            TerritoryDeltaResolver.Resolve(
                neutral,
                black,
                StoneColor.White));
        Assert.Equal(StoneColor.White, whiteSourceFact.SourceActor);
        Assert.Null(TerritoryDeltaResolver.Resolve(
            neutral,
            white,
            StoneColor.Black));
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
            TerritoryDeltaResolver.Resolve(before, after, StoneColor.Black));

        Assert.Equal(
            Enumerable.Range(1, 7).Select(y => C(1, y)),
            fact.ChangedPoints);
        Assert.DoesNotContain(C(3, 1), fact.ChangedPoints);
    }

    [Fact]
    public void ResolverRejectsUnknownActorAndEquivalentForeignGeometry()
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
            TerritoryDeltaResolver.Resolve(before, before, (StoneColor)99));
        Assert.Throws<ArgumentException>(() =>
            TerritoryDeltaResolver.Resolve(before, foreignAfter, StoneColor.Black));
    }

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(StoneColor color, int x, int y) =>
        new(color, false, C(x, y));

    private static CanonicalPoint C(int x, int y) =>
        Geometry.CreateCanonicalPoint(x, y);
}
