using Igorogue.Domain.Board;

namespace Igorogue.Domain.Tests;

public sealed class BoardCoordinateTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void Coord01LowerLeftConvertsToInternalOrigin()
    {
        Assert.Equal(I(0, 0), Geometry.ToInternal(C(1, 1)));
    }

    [Fact]
    public void Coord02UpperRightConvertsToInternalMaximum()
    {
        Assert.Equal(I(6, 6), Geometry.ToInternal(C(7, 7)));
    }

    [Fact]
    public void Coord03CenterRoundTripsAndUsesIndex24()
    {
        var canonical = C(4, 4);
        var internalPoint = Geometry.ToInternal(canonical);

        Assert.Equal(I(3, 3), internalPoint);
        Assert.Equal(canonical, Geometry.ToCanonical(internalPoint));
        Assert.Equal(24, Geometry.ToCanonicalIndex(canonical));
        Assert.Equal(canonical, Geometry.FromCanonicalIndex(24));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(8, 7)]
    [InlineData(1, 0)]
    [InlineData(7, 8)]
    public void Coord04CanonicalBoundsRejectWithoutClamping(int x, int y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Geometry.CreateCanonicalPoint(x, y));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(7, 6)]
    [InlineData(0, -1)]
    [InlineData(6, 7)]
    public void InternalBoundsRejectWithoutClamping(int ix, int iy)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Geometry.CreateInternalPoint(ix, iy));
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(7, 1, 6)]
    [InlineData(1, 2, 7)]
    [InlineData(7, 7, 48)]
    public void Coord05CanonicalIndicesMatchFixture(int x, int y, int expectedIndex)
    {
        var point = C(x, y);

        Assert.Equal(expectedIndex, Geometry.ToCanonicalIndex(point));
        Assert.Equal(point, Geometry.FromCanonicalIndex(expectedIndex));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(49)]
    public void CanonicalIndexBoundsRejectWithoutClamping(int index)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Geometry.FromCanonicalIndex(index));
    }

    [Fact]
    public void Coord06CornerHasTwoOrderedOrthogonalNeighbours()
    {
        Assert.Equal(
            new[] { C(2, 1), C(1, 2) },
            Geometry.GetOrthogonalNeighbours(C(1, 1)));
    }

    [Fact]
    public void Coord07EdgeHasThreeOrderedOrthogonalNeighbours()
    {
        Assert.Equal(
            new[] { C(3, 1), C(5, 1), C(4, 2) },
            Geometry.GetOrthogonalNeighbours(C(4, 1)));
    }

    [Fact]
    public void Coord08CenterHasFourOrderedOrthogonalNeighboursAndNoDiagonals()
    {
        var neighbours = Geometry.GetOrthogonalNeighbours(C(4, 4));

        Assert.Equal(
            new[] { C(4, 3), C(3, 4), C(5, 4), C(4, 5) },
            neighbours);
        Assert.DoesNotContain(C(3, 3), neighbours);
        Assert.DoesNotContain(C(5, 5), neighbours);
    }

    [Fact]
    public void Coord09ReflectionMatchesFixtureAndIsAnInvolutionForEveryPoint()
    {
        Assert.Equal(C(6, 5), Geometry.Reflect(C(2, 3)));
        Assert.Equal(C(4, 4), Geometry.Reflect(C(4, 4)));

        foreach (var point in Geometry.CanonicalPoints)
        {
            Assert.Equal(point, Geometry.Reflect(Geometry.Reflect(point)));
        }
    }

    [Fact]
    public void All49PointsRoundTripThroughInternalAndCanonicalIndex()
    {
        Assert.Equal(49, Geometry.PointCount);
        Assert.Equal(49, Geometry.CanonicalPoints.Distinct().Count());

        for (var index = 0; index < Geometry.PointCount; index++)
        {
            var point = Geometry.FromCanonicalIndex(index);
            Assert.Equal((index % 7) + 1, point.X);
            Assert.Equal((index / 7) + 1, point.Y);
            Assert.Equal(index, Geometry.ToCanonicalIndex(point));
            Assert.Equal(point, Geometry.ToCanonical(Geometry.ToInternal(point)));
        }
    }

    [Fact]
    public void CanonicalPointComparisonUsesYThenXOrder()
    {
        var reversed = Geometry.CanonicalPoints.Reverse().ToArray();

        Array.Sort(reversed);

        Assert.Equal(Geometry.CanonicalPoints, reversed);
    }

    [Fact]
    public void PointOperationsRejectNullValues()
    {
        Assert.Throws<ArgumentNullException>(() => Geometry.ToCanonicalIndex(null!));
        Assert.Throws<ArgumentNullException>(() => Geometry.ToInternal(null!));
        Assert.Throws<ArgumentNullException>(() => Geometry.Reflect(null!));
        Assert.Throws<ArgumentNullException>(() => Geometry.GetOrthogonalNeighbours(null!));
        Assert.Throws<ArgumentNullException>(() => Geometry.ToCanonical(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(8)]
    public void BoardSizeMustMatchAcceptedCoordinateContract(int invalidSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BoardGeometry.Create(invalidSize));
    }

    private static CanonicalPoint C(int x, int y) => Geometry.CreateCanonicalPoint(x, y);

    private static InternalPoint I(int ix, int iy) => Geometry.CreateInternalPoint(ix, iy);
}
