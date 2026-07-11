using Igorogue.Domain.Board;

namespace Igorogue.Domain.Tests;

public sealed class StoneGroupAnalyzerTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void EmptyBoardProducesNoGroupsAndEmptyPointHasNoGroup()
    {
        var analysis = Analyze([]);

        Assert.Empty(analysis.Groups);
        Assert.Null(analysis.GroupAt(C(4, 4)));
    }

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(4, 1, 3)]
    [InlineData(4, 4, 4)]
    public void SingleStoneHasOnlyOnBoardOrthogonalLiberties(
        int x,
        int y,
        int expectedLibertyCount)
    {
        var group = RequiredGroup(Analyze([S(StoneColor.Black, x, y)]), x, y);

        Assert.Equal(expectedLibertyCount, group.RealLibertyCount);
        Assert.Equal(expectedLibertyCount, group.RealLiberties.Distinct().Count());
    }

    [Fact]
    public void EverySingleStoneLibertySetMatchesOrderedOrthogonalNeighbours()
    {
        foreach (var point in Geometry.CanonicalPoints)
        {
            var group = RequiredGroup(
                Analyze([new BoardStone(StoneColor.Black, false, point)]),
                point.X,
                point.Y);

            Assert.Equal(Geometry.GetOrthogonalNeighbours(point), group.RealLiberties);
        }
    }

    [Fact]
    public void LShapeDeduplicatesSharedLibertyAndReturnsCanonicalOrder()
    {
        var analysis = Analyze(
            [
                S(StoneColor.Black, 2, 3),
                S(StoneColor.Black, 3, 2),
                S(StoneColor.Black, 2, 2),
            ]);
        var group = Assert.Single(analysis.Groups);

        Assert.Equal(StoneColor.Black, group.Color);
        Assert.Equal(C(2, 2), group.Anchor);
        Assert.Equal(
            new[] { C(2, 2), C(3, 2), C(2, 3) },
            group.StonePoints);
        Assert.Equal(
            new[] { C(2, 1), C(3, 1), C(1, 2), C(4, 2), C(1, 3), C(3, 3), C(2, 4) },
            group.RealLiberties);
        Assert.Equal(1, group.RealLiberties.Count(point => point == C(3, 3)));
        Assert.Same(group, analysis.GroupAt(C(3, 2)));
        Assert.Same(group, analysis.GroupAt(C(2, 3)));
    }

    [Fact]
    public void DiagonalStonesRemainSeparateGroupsOrderedByAnchor()
    {
        var analysis = Analyze(
            [
                S(StoneColor.Black, 2, 3),
                S(StoneColor.Black, 3, 2),
                S(StoneColor.Black, 1, 2),
                S(StoneColor.Black, 2, 1),
            ]);

        Assert.Equal(
            new[] { C(2, 1), C(1, 2), C(3, 2), C(2, 3) },
            analysis.Groups.Select(group => group.Anchor).ToArray());
        Assert.All(analysis.Groups, group => Assert.Single(group.Stones));
        Assert.All(analysis.Groups, group => Assert.Contains(C(2, 2), group.RealLiberties));
        Assert.Equal(
            new[] { C(1, 1), C(2, 2), C(1, 3) },
            RequiredGroup(analysis, 1, 2).RealLiberties);
    }

    [Fact]
    public void OrthogonallyAdjacentOpposingColorsRemainSeparateAndBlockLiberties()
    {
        var analysis = Analyze(
            [
                S(StoneColor.White, 4, 3),
                S(StoneColor.Black, 3, 3),
            ]);
        var black = RequiredGroup(analysis, 3, 3);
        var white = RequiredGroup(analysis, 4, 3);

        Assert.Equal(2, analysis.Groups.Count);
        Assert.Equal(new[] { C(3, 2), C(2, 3), C(3, 4) }, black.RealLiberties);
        Assert.Equal(new[] { C(4, 2), C(5, 3), C(4, 4) }, white.RealLiberties);
        Assert.DoesNotContain(C(4, 3), black.RealLiberties);
        Assert.DoesNotContain(C(3, 3), white.RealLiberties);
    }

    [Fact]
    public void SurroundedMultiStoneGroupRemainsVisibleWithZeroRealLiberties()
    {
        var analysis = Analyze(
            [
                S(StoneColor.Black, 3, 4),
                S(StoneColor.Black, 4, 4),
                S(StoneColor.White, 3, 3),
                S(StoneColor.White, 4, 3),
                S(StoneColor.White, 2, 4),
                S(StoneColor.White, 5, 4),
                S(StoneColor.White, 3, 5),
                S(StoneColor.White, 4, 5),
            ]);
        var group = RequiredGroup(analysis, 3, 4);

        Assert.Equal(new[] { C(3, 4), C(4, 4) }, group.StonePoints);
        Assert.Empty(group.RealLiberties);
        Assert.Equal(0, group.RealLibertyCount);
    }

    [Fact]
    public void AnalysisIsIdenticalForEveryFixedInputPermutation()
    {
        var stones = new[]
        {
            S(StoneColor.White, 7, 7),
            S(StoneColor.Black, 2, 2),
            S(StoneColor.White, 5, 6),
            S(StoneColor.Black, 2, 3),
            S(StoneColor.Black, 6, 1),
            S(StoneColor.White, 6, 6),
        };
        var reversed = stones.Reverse().ToArray();
        var permuted = new[] { stones[3], stones[0], stones[5], stones[2], stones[4], stones[1] };

        AssertSameAnalysis(Analyze(stones), Analyze(reversed));
        AssertSameAnalysis(Analyze(stones), Analyze(permuted));
    }

    [Fact]
    public void AnalysisAndGroupCollectionsAreReadOnly()
    {
        var analysis = Analyze([S(StoneColor.Black, 4, 4)]);
        var group = Assert.Single(analysis.Groups);
        var groups = Assert.IsAssignableFrom<ICollection<StoneGroup>>(analysis.Groups);
        var stones = Assert.IsAssignableFrom<ICollection<BoardStone>>(group.Stones);
        var stonePoints = Assert.IsAssignableFrom<ICollection<CanonicalPoint>>(group.StonePoints);
        var liberties = Assert.IsAssignableFrom<ICollection<CanonicalPoint>>(group.RealLiberties);

        Assert.Throws<NotSupportedException>(() => groups.Clear());
        Assert.Throws<NotSupportedException>(() => stones.Clear());
        Assert.Throws<NotSupportedException>(() => stonePoints.Clear());
        Assert.Throws<NotSupportedException>(() => liberties.Clear());
        Assert.Equal(4, RequiredGroup(analysis, 4, 4).RealLibertyCount);
    }

    [Fact]
    public void AnalyzerAndLookupRejectNullInput()
    {
        Assert.Throws<ArgumentNullException>(() => StoneGroupAnalyzer.Analyze(null!));
        Assert.Throws<ArgumentNullException>(() => Analyze([]).GroupAt(null!));
    }

    private static StoneGroupAnalysis Analyze(IEnumerable<BoardStone> stones) =>
        StoneGroupAnalyzer.Analyze(BoardState.Create(Geometry, stones));

    private static StoneGroup RequiredGroup(StoneGroupAnalysis analysis, int x, int y)
    {
        var group = analysis.GroupAt(C(x, y));
        Assert.NotNull(group);
        return group;
    }

    private static void AssertSameAnalysis(StoneGroupAnalysis expected, StoneGroupAnalysis actual)
    {
        Assert.Equal(expected.Groups.Count, actual.Groups.Count);
        for (var index = 0; index < expected.Groups.Count; index++)
        {
            var expectedGroup = expected.Groups[index];
            var actualGroup = actual.Groups[index];
            Assert.Equal(expectedGroup.Color, actualGroup.Color);
            Assert.Equal(expectedGroup.Anchor, actualGroup.Anchor);
            Assert.Equal(expectedGroup.StonePoints, actualGroup.StonePoints);
            Assert.Equal(expectedGroup.RealLiberties, actualGroup.RealLiberties);
        }
    }

    private static BoardStone S(StoneColor color, int x, int y) =>
        new(color, false, C(x, y));

    private static CanonicalPoint C(int x, int y) => Geometry.CreateCanonicalPoint(x, y);
}
