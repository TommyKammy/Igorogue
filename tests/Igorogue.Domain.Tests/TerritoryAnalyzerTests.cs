using Igorogue.Domain.Board;

namespace Igorogue.Domain.Tests;

public sealed class TerritoryAnalyzerTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void EmptyBoardIsOneCanonicalNeutralRegion()
    {
        var board = Board([]);

        var analysis = TerritoryAnalyzer.Analyze(board);
        var region = Assert.Single(analysis.Regions);

        Assert.Same(board, analysis.SourceBoard);
        Assert.Equal(TerritoryOwner.Neutral, region.Owner);
        Assert.Equal(C(1, 1), region.Anchor);
        Assert.Equal(49, region.Size);
        Assert.Equal(Geometry.CanonicalPoints, region.Points);
        Assert.All(region.Points, point => Assert.Same(region, analysis.RegionAt(point)));
    }

    [Fact]
    public void FullyOccupiedBoardHasNoRegions()
    {
        var board = Board(Geometry.CanonicalPoints.Select(point => S(StoneColor.Black, point)));

        var analysis = TerritoryAnalyzer.Analyze(board);

        Assert.Empty(analysis.Regions);
        Assert.All(Geometry.CanonicalPoints, point => Assert.Null(analysis.RegionAt(point)));
    }

    [Theory]
    [InlineData(StoneColor.Black, TerritoryOwner.Black)]
    [InlineData(StoneColor.White, TerritoryOwner.White)]
    public void CornerOffBoardBoundaryAddsNoColor(
        StoneColor surroundingColor,
        TerritoryOwner expectedOwner)
    {
        var board = FilledExcept(surroundingColor, C(1, 1));

        var region = Assert.Single(TerritoryAnalyzer.Analyze(board).Regions);

        Assert.Equal(expectedOwner, region.Owner);
        Assert.Equal(C(1, 1), region.Anchor);
        Assert.Equal(new[] { C(1, 1) }, region.Points);
    }

    [Fact]
    public void EdgeRegionUsesOrthogonalConnectivityAndCanonicalPointOrder()
    {
        var board = FilledExcept(
            StoneColor.Black,
            C(1, 1),
            C(2, 1),
            C(1, 2));

        var region = Assert.Single(TerritoryAnalyzer.Analyze(board).Regions);

        Assert.Equal(TerritoryOwner.Black, region.Owner);
        Assert.Equal(C(1, 1), region.Anchor);
        Assert.Equal(new[] { C(1, 1), C(2, 1), C(1, 2) }, region.Points);
    }

    [Fact]
    public void RegionTouchingBothColorsIsNeutral()
    {
        var center = C(4, 4);
        var whiteBoundary = C(4, 5);
        var stones = Geometry.CanonicalPoints
            .Where(point => point != center)
            .Select(point => S(
                point == whiteBoundary ? StoneColor.White : StoneColor.Black,
                point))
            .ToArray();

        var region = Assert.Single(TerritoryAnalyzer.Analyze(Board(stones)).Regions);

        Assert.Equal(TerritoryOwner.Neutral, region.Owner);
        Assert.Equal(center, region.Anchor);
        Assert.Equal(1, region.Size);
    }

    [Fact]
    public void DiagonallyTouchingEmptyPointsRemainSeparateRegions()
    {
        var board = FilledExcept(
            StoneColor.Black,
            C(1, 1),
            C(2, 2));

        var regions = TerritoryAnalyzer.Analyze(board).Regions;

        Assert.Equal(new[] { C(1, 1), C(2, 2) }, regions.Select(region => region.Anchor));
        Assert.All(regions, region => Assert.Equal(TerritoryOwner.Black, region.Owner));
        Assert.All(regions, region => Assert.Equal(1, region.Size));
    }

    [Fact]
    public void MultipleRegionsPartitionOnlyEmptyPointsInCanonicalOrder()
    {
        var board = FilledExcept(
            StoneColor.Black,
            C(6, 1),
            C(2, 2),
            C(1, 7),
            C(2, 7));

        var analysis = TerritoryAnalyzer.Analyze(board);
        var expectedAnchors = new[] { C(6, 1), C(2, 2), C(1, 7) };
        var expectedEmptyPoints = Geometry.CanonicalPoints.Where(board.IsEmpty).ToArray();
        var actualEmptyPoints = analysis.Regions
            .SelectMany(region => region.Points)
            .OrderBy(Geometry.ToCanonicalIndex)
            .ToArray();

        Assert.Equal(expectedAnchors, analysis.Regions.Select(region => region.Anchor));
        Assert.Equal(expectedEmptyPoints, actualEmptyPoints);
        Assert.Equal(actualEmptyPoints.Length, actualEmptyPoints.Distinct().Count());
        Assert.All(
            analysis.Regions,
            region => Assert.Equal(
                region.Points.OrderBy(Geometry.ToCanonicalIndex),
                region.Points));
        Assert.All(
            analysis.Regions,
            region => Assert.All(
                region.Points,
                point => Assert.Same(region, analysis.RegionAt(point))));
        Assert.All(board.OccupiedStones, stone => Assert.Null(analysis.RegionAt(stone.Point)));
    }

    [Fact]
    public void KingBoundaryUsesOnlyStoneColor()
    {
        var center = C(4, 4);
        var ordinary = FilledExcept(StoneColor.Black, center);
        var kings = Board(Geometry.CanonicalPoints
            .Where(point => point != center)
            .Select(point => S(
                StoneColor.Black,
                point,
                Geometry.GetOrthogonalNeighbours(center).Contains(point))));

        Assert.Equal(
            Projection(TerritoryAnalyzer.Analyze(ordinary)),
            Projection(TerritoryAnalyzer.Analyze(kings)));
    }

    [Fact]
    public void AnalysisIsIdenticalForEveryFixedStoneInputPermutation()
    {
        var stones = new[]
        {
            S(StoneColor.Black, C(2, 2), isKing: true),
            S(StoneColor.Black, C(4, 3)),
            S(StoneColor.White, C(6, 5)),
            S(StoneColor.White, C(7, 7), isKing: true),
        };
        var reversed = stones.Reverse().ToArray();
        var permuted = new[] { stones[2], stones[0], stones[3], stones[1] };

        var expected = Projection(TerritoryAnalyzer.Analyze(Board(stones)));

        Assert.Equal(expected, Projection(TerritoryAnalyzer.Analyze(Board(reversed))));
        Assert.Equal(expected, Projection(TerritoryAnalyzer.Analyze(Board(permuted))));
    }

    [Fact]
    public void AnalysisAndRegionCollectionsAreReadOnly()
    {
        var board = Board([S(StoneColor.Black, C(4, 4))]);
        var analysis = TerritoryAnalyzer.Analyze(board);
        var region = Assert.Single(analysis.Regions);
        var regions = Assert.IsAssignableFrom<ICollection<TerritoryRegion>>(analysis.Regions);
        var points = Assert.IsAssignableFrom<ICollection<CanonicalPoint>>(region.Points);

        Assert.True(regions.IsReadOnly);
        Assert.True(points.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => regions.Clear());
        Assert.Throws<NotSupportedException>(() => points.Clear());
        Assert.Same(board, analysis.SourceBoard);
        Assert.NotNull(board.StoneAt(C(4, 4)));
    }

    [Fact]
    public void AnalyzerAndLookupRejectNullInput()
    {
        Assert.Throws<ArgumentNullException>(() => TerritoryAnalyzer.Analyze(null!));
        Assert.Throws<ArgumentNullException>(() =>
            TerritoryAnalyzer.Analyze(Board([])).RegionAt(null!));
    }

    private static string[] Projection(TerritoryAnalysis analysis) =>
        analysis.Regions
            .Select(region =>
                $"{region.Owner}:{region.Anchor}:{string.Join(';', region.Points)}")
            .ToArray();

    private static BoardState FilledExcept(
        StoneColor fillColor,
        params CanonicalPoint[] emptyPoints)
    {
        var empty = emptyPoints.ToHashSet();
        return Board(Geometry.CanonicalPoints
            .Where(point => !empty.Contains(point))
            .Select(point => S(fillColor, point)));
    }

    private static BoardState Board(IEnumerable<BoardStone> stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone S(
        StoneColor color,
        CanonicalPoint point,
        bool isKing = false) =>
        new(color, isKing, point);

    private static CanonicalPoint C(int x, int y) => Geometry.CreateCanonicalPoint(x, y);
}
