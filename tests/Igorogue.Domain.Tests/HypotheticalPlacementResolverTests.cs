using Igorogue.Domain.Board;

namespace Igorogue.Domain.Tests;

public sealed class HypotheticalPlacementResolverTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void EmptyPlacementProducesNewBoardAndStonePlacedFactWithoutMutatingSource()
    {
        var source = Board();
        var proposed = S(StoneColor.Black, 4, 4, isKing: true);
        var placement = Prepare(source, proposed);
        var resolution = Resolve(placement);

        Assert.Empty(source.OccupiedStones);
        Assert.Same(proposed, placement.BoardAfterPlacement.StoneAt(C(4, 4)));
        Assert.Same(proposed, resolution.BoardAfterCapture.StoneAt(C(4, 4)));
        Assert.Empty(resolution.CapturedGroups);
        Assert.Equal(0, resolution.CapturedStoneCount);
        Assert.Equal(
            new[] { C(4, 3), C(3, 4), C(5, 4), C(4, 5) },
            resolution.PlacedGroupAfterCapture.RealLiberties);
        Assert.Same(
            resolution.PlacedGroupAfterCapture,
            resolution.GroupsAfterCapture.GroupAt(C(4, 4)));
        var fact = Assert.IsType<StonePlacedFact>(Assert.Single(resolution.OrderedFacts));
        Assert.Same(proposed, fact.Stone);
    }

    [Fact]
    public void OccupiedPointReturnsFalseAndNullWithoutChangingSource()
    {
        var occupant = S(StoneColor.Black, 4, 4);
        var source = Board(occupant);

        var success = HypotheticalPlacementResolver.TryCreate(
            source,
            S(StoneColor.White, 4, 4),
            out var placement);

        Assert.False(success);
        Assert.Null(placement);
        Assert.Single(source.OccupiedStones);
        Assert.Same(occupant, source.StoneAt(C(4, 4)));
    }

    [Fact]
    public void Ko01RawPlacementCapturesOneGroupAndCreatesSelfLiberty()
    {
        var capturedKing = S(StoneColor.White, 3, 3, isKing: true);
        var source = Board(
            S(StoneColor.Black, 3, 4),
            S(StoneColor.Black, 2, 3),
            S(StoneColor.Black, 4, 3),
            capturedKing,
            S(StoneColor.White, 2, 2),
            S(StoneColor.White, 4, 2),
            S(StoneColor.White, 3, 1));
        var proposed = S(StoneColor.Black, 3, 2);
        var placement = Prepare(source, proposed);
        var resolution = Resolve(placement);

        var captured = Assert.Single(resolution.CapturedGroups);
        Assert.Equal(C(3, 3), captured.Anchor);
        Assert.Equal(new[] { C(3, 3) }, captured.StonePoints);
        Assert.Same(capturedKing, Assert.Single(captured.Stones));
        Assert.Equal(1, resolution.CapturedStoneCount);
        Assert.True(resolution.BoardAfterCapture.IsEmpty(C(3, 3)));
        Assert.Equal(new[] { C(3, 3) }, resolution.PlacedGroupAfterCapture.RealLiberties);
        Assert.NotNull(source.StoneAt(C(3, 3)));
        Assert.Null(source.StoneAt(C(3, 2)));

        Assert.Collection(
            resolution.OrderedFacts,
            fact => Assert.Same(proposed, Assert.IsType<StonePlacedFact>(fact).Stone),
            fact =>
            {
                var capturedFact = Assert.IsType<GroupCapturedFact>(fact);
                Assert.Same(captured, capturedFact.CapturedGroup);
                Assert.Equal(StoneColor.Black, capturedFact.CapturingColor);
                Assert.True(capturedFact.ContainsKing);
            });
    }

    [Fact]
    public void SameOpponentGroupTouchedFromTwoSidesIsCapturedOnce()
    {
        var source = Board(
            S(StoneColor.White, 2, 2),
            S(StoneColor.White, 3, 2),
            S(StoneColor.White, 2, 3),
            S(StoneColor.Black, 2, 1),
            S(StoneColor.Black, 3, 1),
            S(StoneColor.Black, 1, 2),
            S(StoneColor.Black, 4, 2),
            S(StoneColor.Black, 1, 3),
            S(StoneColor.Black, 2, 4));
        var placement = Prepare(source, S(StoneColor.Black, 3, 3));

        var adjacent = Assert.Single(placement.AdjacentOpponentGroups);
        Assert.Equal(C(2, 2), adjacent.Anchor);

        var resolution = Resolve(placement);

        var captured = Assert.Single(resolution.CapturedGroups);
        Assert.Same(adjacent, captured);
        Assert.Equal(new[] { C(2, 2), C(3, 2), C(2, 3) }, captured.StonePoints);
        Assert.Equal(3, resolution.CapturedStoneCount);
        Assert.Equal(2, resolution.OrderedFacts.Count);
    }

    [Fact]
    public void TwoOpponentGroupsAreRemovedTogetherInAnchorAndStoneOrder()
    {
        var source = SimultaneousCaptureBoard();
        var proposed = S(StoneColor.Black, 3, 3);
        var placement = Prepare(source, proposed);
        var resolution = Resolve(placement);

        Assert.Equal(
            new[] { C(2, 3), C(4, 3) },
            placement.AdjacentOpponentGroups.Select(group => group.Anchor).ToArray());
        Assert.Equal(
            new[] { C(2, 3), C(4, 3) },
            resolution.CapturedGroups.Select(group => group.Anchor).ToArray());
        Assert.Equal(
            new[] { C(2, 3), C(2, 4) },
            resolution.CapturedGroups[0].StonePoints);
        Assert.Equal(new[] { C(4, 3) }, resolution.CapturedGroups[1].StonePoints);
        Assert.Equal(3, resolution.CapturedStoneCount);
        Assert.NotNull(placement.BoardAfterPlacement.StoneAt(C(2, 3)));
        Assert.NotNull(placement.BoardAfterPlacement.StoneAt(C(2, 4)));
        Assert.NotNull(placement.BoardAfterPlacement.StoneAt(C(4, 3)));
        Assert.True(resolution.BoardAfterCapture.IsEmpty(C(2, 3)));
        Assert.True(resolution.BoardAfterCapture.IsEmpty(C(2, 4)));
        Assert.True(resolution.BoardAfterCapture.IsEmpty(C(4, 3)));
        Assert.NotNull(resolution.BoardAfterCapture.StoneAt(C(3, 3)));

        Assert.IsType<StonePlacedFact>(resolution.OrderedFacts[0]);
        Assert.Equal(
            new[] { C(2, 3), C(4, 3) },
            resolution.OrderedFacts.Skip(1)
                .Cast<GroupCapturedFact>()
                .Select(fact => fact.CapturedGroup.Anchor)
                .ToArray());
    }

    [Fact]
    public void RealZeroGroupWithPositiveEffectiveLibertySurvivesPlacement()
    {
        var source = Board(
            S(StoneColor.Black, 3, 4),
            S(StoneColor.Black, 2, 3),
            S(StoneColor.Black, 4, 3),
            S(StoneColor.White, 3, 3),
            S(StoneColor.White, 2, 2),
            S(StoneColor.White, 4, 2),
            S(StoneColor.White, 3, 1));
        var placement = Prepare(source, S(StoneColor.Black, 3, 2));
        var protectedGroup = Assert.Single(
            placement.AdjacentOpponentGroups,
            group => group.Anchor == C(3, 3));
        Assert.Equal(0, protectedGroup.RealLibertyCount);
        var effectiveLiberties = EffectiveFor(
            placement,
            (protectedGroup.Anchor, 1));

        var resolution = HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            effectiveLiberties);

        Assert.Empty(resolution.CapturedGroups);
        Assert.Same(
            placement.BoardAfterPlacement.StoneAt(C(3, 3)),
            resolution.BoardAfterCapture.StoneAt(C(3, 3)));
        Assert.Equal(1, effectiveLiberties.EffectiveLibertiesFor(protectedGroup));
    }

    [Fact]
    public void RealPositiveGroupWithZeroEffectiveLibertiesIsCaptured()
    {
        var source = Board(S(StoneColor.White, 3, 3));
        var placement = Prepare(source, S(StoneColor.Black, 3, 2));
        var opponent = Assert.Single(placement.AdjacentOpponentGroups);
        Assert.Equal(3, opponent.RealLibertyCount);
        var effectiveLiberties = EffectiveFor(placement, (opponent.Anchor, 0));

        var resolution = HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            effectiveLiberties);

        Assert.Same(opponent, Assert.Single(resolution.CapturedGroups));
        Assert.Equal(0, effectiveLiberties.EffectiveLibertiesFor(opponent));
        Assert.True(resolution.BoardAfterCapture.IsEmpty(C(3, 3)));
    }

    [Fact]
    public void NonAdjacentZeroEffectiveGroupIsNotGloballySwept()
    {
        var doomedButUnrelated = S(StoneColor.Black, 4, 4);
        var source = Board(
            doomedButUnrelated,
            S(StoneColor.White, 4, 3),
            S(StoneColor.White, 3, 4),
            S(StoneColor.White, 5, 4),
            S(StoneColor.White, 4, 5));
        var placement = Prepare(source, S(StoneColor.White, 1, 1));
        var unrelatedGroup = placement.GroupsAfterPlacement.GroupAt(C(4, 4));

        Assert.NotNull(unrelatedGroup);
        Assert.Equal(0, unrelatedGroup.RealLibertyCount);
        Assert.Empty(placement.AdjacentOpponentGroups);

        var resolution = Resolve(placement);

        Assert.Empty(resolution.CapturedGroups);
        Assert.Same(doomedButUnrelated, resolution.BoardAfterCapture.StoneAt(C(4, 4)));
    }

    [Fact]
    public void SelfZeroLibertyCandidateRemainsForTask0006Legality()
    {
        var source = Board(
            S(StoneColor.Black, 2, 1),
            S(StoneColor.Black, 1, 2),
            S(StoneColor.Black, 3, 2),
            S(StoneColor.Black, 2, 3));
        var proposed = S(StoneColor.White, 2, 2);
        var resolution = Resolve(Prepare(source, proposed));

        Assert.Empty(resolution.CapturedGroups);
        Assert.Same(proposed, resolution.BoardAfterCapture.StoneAt(C(2, 2)));
        Assert.Equal(StoneColor.White, resolution.PlacedGroupAfterCapture.Color);
        Assert.Equal(0, resolution.PlacedGroupAfterCapture.RealLibertyCount);
    }

    [Fact]
    public void Ko02RawResolutionCanRecreatePriorBoardWithoutApplyingRepetitionRule()
    {
        var initial = Board(
            S(StoneColor.Black, 3, 4),
            S(StoneColor.Black, 2, 3),
            S(StoneColor.Black, 4, 3),
            S(StoneColor.White, 3, 3),
            S(StoneColor.White, 2, 2),
            S(StoneColor.White, 4, 2),
            S(StoneColor.White, 3, 1));
        var blackCapture = Resolve(Prepare(initial, S(StoneColor.Black, 3, 2)));

        var whiteRecapture = Resolve(Prepare(
            blackCapture.BoardAfterCapture,
            S(StoneColor.White, 3, 3)));

        var recaptured = Assert.Single(whiteRecapture.CapturedGroups);
        Assert.Equal(C(3, 2), recaptured.Anchor);
        AssertBoardEqual(initial, whiteRecapture.BoardAfterCapture);
    }

    [Fact]
    public void InputPermutationProducesIdenticalCaptureFactsAndResultBoard()
    {
        var stones = SimultaneousCaptureBoard().OccupiedStones.ToArray();
        var canonical = Resolve(Prepare(Board(stones), S(StoneColor.Black, 3, 3)));
        var reversed = Resolve(Prepare(Board(stones.Reverse().ToArray()), S(StoneColor.Black, 3, 3)));
        var shuffled = Resolve(Prepare(
            Board(
                stones[4], stones[0], stones[10], stones[7], stones[2], stones[9],
                stones[1], stones[6], stones[3], stones[8], stones[5]),
            S(StoneColor.Black, 3, 3)));

        AssertResolutionEqual(canonical, reversed);
        AssertResolutionEqual(canonical, shuffled);
    }

    [Fact]
    public void EffectiveLibertySnapshotRejectsIncompleteDuplicateForeignAndNegativeFacts()
    {
        var placement = Prepare(Board(), S(StoneColor.Black, 4, 4));
        var group = Assert.Single(placement.GroupsAfterPlacement.Groups);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new GroupEffectiveLiberty(group, -1));
        Assert.Throws<ArgumentException>(
            () => EffectiveLibertySnapshot.Create(placement.GroupsAfterPlacement, []));
        Assert.Throws<ArgumentException>(() => EffectiveLibertySnapshot.Create(
            placement.GroupsAfterPlacement,
            [
                new GroupEffectiveLiberty(group, group.RealLibertyCount),
                new GroupEffectiveLiberty(group, group.RealLibertyCount),
            ]));

        var otherPlacement = Prepare(Board(), S(StoneColor.Black, 4, 4));
        var otherGroup = Assert.Single(otherPlacement.GroupsAfterPlacement.Groups);
        Assert.Throws<ArgumentException>(() => EffectiveLibertySnapshot.Create(
            placement.GroupsAfterPlacement,
            [new GroupEffectiveLiberty(otherGroup, otherGroup.RealLibertyCount)]));

        var otherSnapshot = EffectiveFor(otherPlacement);
        Assert.Throws<ArgumentException>(
            () => HypotheticalPlacementResolver.ResolveCaptures(placement, otherSnapshot));
        Assert.Throws<ArgumentException>(
            () => EffectiveFor(placement).EffectiveLibertiesFor(otherGroup));
    }

    [Fact]
    public void EffectiveLibertySnapshotCanonicalizesReversedFactInput()
    {
        var placement = Prepare(SimultaneousCaptureBoard(), S(StoneColor.Black, 3, 3));
        var reversedFacts = placement.GroupsAfterPlacement.Groups
            .Reverse()
            .Select(group => new GroupEffectiveLiberty(group, group.RealLibertyCount))
            .ToArray();

        var snapshot = EffectiveLibertySnapshot.Create(
            placement.GroupsAfterPlacement,
            reversedFacts);

        Assert.Equal(
            placement.GroupsAfterPlacement.Groups.Select(group => group.Anchor).ToArray(),
            snapshot.Groups.Select(group => group.GroupAnchor).ToArray());
    }

    [Fact]
    public void CaptureResultRetainsExactAnalysisForTask0006EffectiveLiberties()
    {
        var resolution = Resolve(Prepare(
            SimultaneousCaptureBoard(),
            S(StoneColor.Black, 3, 3)));
        var placedGroup = resolution.GroupsAfterCapture.GroupAt(C(3, 3));

        Assert.Same(resolution.PlacedGroupAfterCapture, placedGroup);

        var postCaptureEffectiveLiberties = EffectiveLibertySnapshot.Create(
            resolution.GroupsAfterCapture,
            resolution.GroupsAfterCapture.Groups.Select(group =>
                new GroupEffectiveLiberty(group, group.RealLibertyCount)));

        Assert.Equal(
            resolution.PlacedGroupAfterCapture.RealLibertyCount,
            postCaptureEffectiveLiberties.EffectiveLibertiesFor(
                resolution.PlacedGroupAfterCapture));
    }

    [Fact]
    public void PublicBoundariesRejectNullAndResultCollectionsAreReadOnly()
    {
        var proposed = S(StoneColor.Black, 3, 3);
        Assert.Throws<ArgumentNullException>(
            () => HypotheticalPlacementResolver.TryCreate(null!, proposed, out _));
        Assert.Throws<ArgumentNullException>(
            () => HypotheticalPlacementResolver.TryCreate(Board(), null!, out _));

        var placement = Prepare(SimultaneousCaptureBoard(), proposed);
        var effectiveLiberties = EffectiveFor(placement);
        Assert.Throws<ArgumentNullException>(
            () => HypotheticalPlacementResolver.ResolveCaptures(null!, effectiveLiberties));
        Assert.Throws<ArgumentNullException>(
            () => HypotheticalPlacementResolver.ResolveCaptures(placement, null!));
        Assert.Throws<ArgumentNullException>(
            () => EffectiveLibertySnapshot.Create(null!, []));
        Assert.Throws<ArgumentNullException>(
            () => EffectiveLibertySnapshot.Create(placement.GroupsAfterPlacement, null!));
        Assert.Throws<ArgumentNullException>(
            () => EffectiveLibertySnapshot.Create(
                placement.GroupsAfterPlacement,
                [(GroupEffectiveLiberty)null!]));
        Assert.Throws<ArgumentNullException>(() => new GroupEffectiveLiberty(null!, 0));

        var resolution = HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            effectiveLiberties);
        AssertReadOnly(placement.AdjacentOpponentGroups);
        AssertReadOnly(effectiveLiberties.Groups);
        AssertReadOnly(resolution.CapturedGroups);
        AssertReadOnly(resolution.OrderedFacts);
    }

    private static HypotheticalPlacement Prepare(BoardState source, BoardStone proposed)
    {
        if (!HypotheticalPlacementResolver.TryCreate(source, proposed, out var placement) ||
            placement is null)
        {
            throw new InvalidOperationException("Test placement unexpectedly targeted an occupied point.");
        }

        return placement;
    }

    private static HypotheticalPlacementResolution Resolve(HypotheticalPlacement placement) =>
        HypotheticalPlacementResolver.ResolveCaptures(placement, EffectiveFor(placement));

    private static EffectiveLibertySnapshot EffectiveFor(
        HypotheticalPlacement placement,
        params (CanonicalPoint Anchor, int Count)[] overrides)
    {
        var facts = placement.GroupsAfterPlacement.Groups
            .Select(group =>
            {
                var count = group.RealLibertyCount;
                foreach (var item in overrides)
                {
                    if (item.Anchor == group.Anchor)
                    {
                        count = item.Count;
                    }
                }

                return new GroupEffectiveLiberty(group, count);
            })
            .ToArray();
        return EffectiveLibertySnapshot.Create(placement.GroupsAfterPlacement, facts);
    }

    private static void AssertResolutionEqual(
        HypotheticalPlacementResolution expected,
        HypotheticalPlacementResolution actual)
    {
        AssertBoardEqual(expected.BoardAfterCapture, actual.BoardAfterCapture);
        Assert.Equal(expected.CapturedStoneCount, actual.CapturedStoneCount);
        Assert.Equal(
            expected.CapturedGroups.Select(ProjectGroup).ToArray(),
            actual.CapturedGroups.Select(ProjectGroup).ToArray());
        Assert.Equal(
            expected.OrderedFacts.Select(ProjectFact).ToArray(),
            actual.OrderedFacts.Select(ProjectFact).ToArray());
        Assert.Equal(
            expected.PlacedGroupAfterCapture.StonePoints,
            actual.PlacedGroupAfterCapture.StonePoints);
        Assert.Equal(
            expected.PlacedGroupAfterCapture.RealLiberties,
            actual.PlacedGroupAfterCapture.RealLiberties);
    }

    private static string ProjectGroup(StoneGroup group) =>
        $"{group.Color}:{group.Anchor}:{string.Join(";", group.StonePoints)}";

    private static string ProjectFact(PlacementCaptureFact fact) => fact switch
    {
        StonePlacedFact placed => $"placed:{placed.Stone.Color}:{placed.Stone.Point}",
        GroupCapturedFact captured =>
            $"captured:{captured.CapturingColor}:{ProjectGroup(captured.CapturedGroup)}",
        _ => throw new ArgumentOutOfRangeException(nameof(fact), fact, "Unknown placement fact."),
    };

    private static void AssertBoardEqual(BoardState expected, BoardState actual)
    {
        Assert.Equal(
            expected.OccupiedStones.Select(ProjectStone).ToArray(),
            actual.OccupiedStones.Select(ProjectStone).ToArray());
    }

    private static string ProjectStone(BoardStone stone) =>
        $"{stone.Color}:{stone.IsKing}:{stone.Point}";

    private static void AssertReadOnly<T>(IReadOnlyList<T> values)
    {
        var collection = Assert.IsAssignableFrom<ICollection<T>>(values);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Clear());
    }

    private static BoardState SimultaneousCaptureBoard() => Board(
        S(StoneColor.White, 2, 3),
        S(StoneColor.White, 2, 4),
        S(StoneColor.White, 4, 3),
        S(StoneColor.Black, 2, 2),
        S(StoneColor.Black, 4, 2),
        S(StoneColor.Black, 1, 3),
        S(StoneColor.Black, 5, 3),
        S(StoneColor.Black, 1, 4),
        S(StoneColor.Black, 3, 4),
        S(StoneColor.Black, 4, 4),
        S(StoneColor.Black, 2, 5));

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone S(
        StoneColor color,
        int x,
        int y,
        bool isKing = false) => new(color, isKing, C(x, y));

    private static CanonicalPoint C(int x, int y) => Geometry.CreateCanonicalPoint(x, y);
}
