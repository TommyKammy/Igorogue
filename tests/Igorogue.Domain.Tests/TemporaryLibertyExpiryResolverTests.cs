using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

public sealed class TemporaryLibertyExpiryResolverTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void DyingOpposingGroupsAreSelectedFromOnePreRemovalSnapshot()
    {
        var board = Board(
            Stone(StoneColor.White, 3, 3),
            Stone(StoneColor.Black, 4, 3),
            Stone(StoneColor.White, 2, 4),
            Stone(StoneColor.Black, 3, 4),
            Stone(StoneColor.White, 4, 4),
            Stone(StoneColor.Black, 5, 4),
            Stone(StoneColor.White, 3, 5),
            Stone(StoneColor.Black, 4, 5));
        var stones = Runtime(board);
        var black = stones.InstanceAt(C(3, 4))!;
        var white = stones.InstanceAt(C(4, 4))!;
        var temporary = TemporaryLibertyState.Create(
            stones,
            [
                new TemporaryLibertyEffect("black", 1, StoneColor.Black, black.InstanceId, "test", 1, 1),
                new TemporaryLibertyEffect("white", 1, StoneColor.White, white.InstanceId, "test", 2, 1),
            ],
            3);

        var resolution = TemporaryLibertyExpiryResolver.Resolve(
            stones,
            temporary,
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(board),
            1);

        Assert.Equal(
            new[] { C(3, 4), C(4, 4) },
            resolution.CapturedGroups.Select(group => group.Anchor));
        Assert.Null(resolution.BoardAfterResolution.StoneAt(C(3, 4)));
        Assert.Null(resolution.BoardAfterResolution.StoneAt(C(4, 4)));
        Assert.True(resolution.CanProcessCaptureBenefits);
        Assert.False(resolution.BenefitsSuppressed);
        Assert.Equal(2, resolution.CapturedStoneInstances.Count);
        Assert.Equal(
            2,
            resolution.OrderedFacts.OfType<TemporaryLibertyGroupCapturedFact>().Count());
    }

    [Fact]
    public void AnchoredEffectFollowsRuntimeIdentityIntoMergedGroup()
    {
        var sourceBoard = Board(
            Stone(StoneColor.Black, 2, 4),
            Stone(StoneColor.Black, 4, 4));
        var sourceStones = Runtime(sourceBoard);
        var left = sourceStones.InstanceAt(C(2, 4))!;
        var temporary = TemporaryLibertyState.Create(
            sourceStones,
            [new TemporaryLibertyEffect(
                "merge-guard",
                1,
                StoneColor.Black,
                left.InstanceId,
                "test",
                1,
                4)],
            2);
        var connectorStone = Stone(StoneColor.Black, 3, 4);
        var mergedBoard = BoardState.Create(
            Geometry,
            sourceBoard.OccupiedStones.Append(connectorStone));
        var connectorRuntime = new StoneRuntimeInstance(
            "stone-connector",
            connectorStone,
            "basic",
            sourceStones.NextCreatedSequence,
            []);
        var mergedStones = StoneRuntimeState.Create(
            mergedBoard,
            sourceStones.Instances.Append(connectorRuntime),
            sourceStones.NextCreatedSequence + 1);

        var rebound = TemporaryLibertyCarrierRemovalResolver.Resolve(
            temporary,
            mergedStones);
        var analysis = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            mergedStones,
            rebound.StateAfterRemoval,
            ContinuousLibertySnapshot.Empty(mergedStones));
        var mergedGroup = Assert.Single(analysis.GroupAnalysis.Groups);

        Assert.Empty(rebound.RemovedEffects);
        Assert.Equal(new[] { C(2, 4), C(3, 4), C(4, 4) }, mergedGroup.StonePoints);
        Assert.Equal(1, analysis.BreakdownFor(mergedGroup).TimedAmount);
        Assert.Equal(left.InstanceId, Assert.Single(rebound.StateAfterRemoval.Effects).AnchorStoneInstanceId);
    }

    [Fact]
    public void ResolverRejectsForeignStateHistoryAndOverdueEffects()
    {
        var board = Board(Stone(StoneColor.Black, 4, 4));
        var stones = Runtime(board);
        var foreignStones = Runtime(board);
        var anchor = Assert.Single(stones.Instances);
        var state = TemporaryLibertyState.Create(
            stones,
            [new TemporaryLibertyEffect(
                "guard",
                1,
                StoneColor.Black,
                anchor.InstanceId,
                "test",
                1,
                1)],
            2);

        Assert.Throws<ArgumentException>(() => TemporaryLibertyExpiryResolver.Resolve(
            foreignStones,
            state,
            ContinuousLibertySnapshot.Empty(foreignStones),
            BattleRepetitionHistory.Start(board),
            1));

        var otherBoard = Board(Stone(StoneColor.White, 1, 1));
        Assert.Throws<ArgumentException>(() => TemporaryLibertyExpiryResolver.Resolve(
            stones,
            state,
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(otherBoard),
            1));

        Assert.Throws<InvalidOperationException>(() => TemporaryLibertyExpiryResolver.Resolve(
            stones,
            state,
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(board),
            2));
    }

    private static StoneRuntimeState Runtime(BoardState board)
    {
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                $"stone-{stone.Point.X}-{stone.Point.Y}",
                stone,
                "basic",
                index + 1,
                []))
            .ToArray();
        return StoneRuntimeState.Create(board, instances, instances.Length + 1L);
    }

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(StoneColor color, int x, int y) =>
        new(color, false, C(x, y));

    private static CanonicalPoint C(int x, int y) =>
        Geometry.CreateCanonicalPoint(x, y);
}
