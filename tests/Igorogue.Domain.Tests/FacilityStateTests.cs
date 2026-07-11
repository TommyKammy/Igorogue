using Igorogue.Domain.Board;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Tests;

public sealed class FacilityStateTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void CreateCopiesCanonicalizesAndIndexesAuthoritativeFacilities()
    {
        var board = Board();
        var laterPoint = Facility("later", "furnace", 3, 2, 2, ["z", "a", "z"]);
        var earlierPoint = Facility("earlier", "development", 2, 1, 1);
        var input = new[] { laterPoint, earlierPoint };

        var state = FacilityState.Create(board, input, 9);
        input[0] = earlierPoint;

        Assert.Same(board, state.SourceBoard);
        Assert.Equal(new[] { earlierPoint, laterPoint }, state.InstalledFacilities);
        Assert.Same(earlierPoint, state.FacilityAt(C(2, 1)));
        Assert.Same(laterPoint, state.FacilityById("later"));
        Assert.Null(state.FacilityAt(C(7, 7)));
        Assert.Null(state.FacilityById("missing"));
        Assert.Equal(new[] { "a", "z" }, laterPoint.ExplicitDisableSources);
        Assert.True(laterPoint.IsExplicitlyDisabled);
        Assert.Equal(9, state.NextBuildSequence);

        var collection = Assert.IsAssignableFrom<ICollection<FacilityInstance>>(
            state.InstalledFacilities);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Add(earlierPoint));
        var sources = Assert.IsAssignableFrom<ICollection<string>>(
            laterPoint.ExplicitDisableSources);
        Assert.True(sources.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => sources.Add("other"));
    }

    [Fact]
    public void CanonicalTextIgnoresInputOrderAndStoneSnapshotMetadata()
    {
        var firstBoard = Board();
        var equivalentFacilityBoard = Board(Stone(StoneColor.Black, 7, 7));
        var first = Facility("facility-a", "development", 1, 1, 1, ["effect-b", "effect-a"]);
        var second = Facility("facility-b", "furnace", 2, 1, 2);

        var canonical = FacilityState.Create(firstBoard, [second, first], 8);
        var reversed = FacilityState.Create(
            equivalentFacilityBoard,
            [
                Facility("facility-a", "development", 1, 1, 1, ["effect-a", "effect-b"]),
                Facility("facility-b", "furnace", 2, 1, 2),
            ],
            8);

        Assert.Equal(canonical.ToCanonicalText(), reversed.ToCanonicalText());
        Assert.StartsWith(FacilityState.EncodingVersion, canonical.ToCanonicalText());
        Assert.Contains("next_build_sequence=8", canonical.ToCanonicalText(), StringComparison.Ordinal);
        Assert.DoesNotContain(
            StoneTopologyKey.FromBoard(equivalentFacilityBoard).CanonicalCells,
            canonical.ToCanonicalText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void NextSequenceRemainsAuthoritativeAfterDestroyedInstancesDisappear()
    {
        var board = Board();
        var installed = FacilityState.Create(
            board,
            [Facility("survivor", "development", 1, 1, 2)],
            10);

        var afterRemoval = FacilityState.Create(board, [], installed.NextBuildSequence);

        Assert.Empty(afterRemoval.InstalledFacilities);
        Assert.Equal(10, afterRemoval.NextBuildSequence);
        Assert.Contains("next_build_sequence=10", afterRemoval.ToCanonicalText(), StringComparison.Ordinal);
    }

    [Fact]
    public void StateRejectsDuplicateIdentityPointSequenceAndStoneCoexistence()
    {
        var empty = Board();
        var first = Facility("one", "development", 1, 1, 1);

        Assert.Throws<ArgumentException>(() => FacilityState.Create(
            empty,
            [first, Facility("one", "furnace", 2, 1, 2)],
            3));
        Assert.Throws<ArgumentException>(() => FacilityState.Create(
            empty,
            [first, Facility("two", "furnace", 1, 1, 2)],
            3));
        Assert.Throws<ArgumentException>(() => FacilityState.Create(
            empty,
            [first, Facility("two", "furnace", 2, 1, 1)],
            3));
        Assert.Throws<ArgumentOutOfRangeException>(() => FacilityState.Create(
            empty,
            [first],
            1));
        Assert.Throws<ArgumentException>(() => FacilityState.Create(
            Board(Stone(StoneColor.Black, 1, 1)),
            [first],
            2));
    }

    [Fact]
    public void InstanceRejectsInvalidAuthoritativeFields()
    {
        Assert.Throws<ArgumentException>(() =>
            new FacilityInstance(" ", "development", StoneColor.Black, C(1, 1), 1));
        Assert.Throws<ArgumentException>(() =>
            new FacilityInstance("facility", "bad:id", StoneColor.Black, C(1, 1), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FacilityInstance("facility", "development", (StoneColor)0, C(1, 1), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FacilityInstance("facility", "development", StoneColor.Black, C(1, 1), 0));
        Assert.Throws<ArgumentException>(() =>
            new FacilityInstance(
                "facility",
                "development",
                StoneColor.Black,
                C(1, 1),
                1,
                ["bad:source"]));
    }

    private static FacilityInstance Facility(
        string id,
        string contentId,
        int x,
        int y,
        long sequence,
        IEnumerable<string>? explicitSources = null) =>
        new(
            id,
            contentId,
            StoneColor.Black,
            C(x, y),
            sequence,
            explicitSources ?? []);

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(StoneColor color, int x, int y) =>
        new(color, false, C(x, y));

    private static CanonicalPoint C(int x, int y) => Geometry.CreateCanonicalPoint(x, y);
}
