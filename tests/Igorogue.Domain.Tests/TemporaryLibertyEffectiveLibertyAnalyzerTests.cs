using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

public sealed class TemporaryLibertyEffectiveLibertyAnalyzerTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void AnalyzerAddsRealTimedAndContinuousOncePerInstanceAcrossMergedGroup()
    {
        var board = Board(
            Stone(StoneColor.Black, 4, 4),
            Stone(StoneColor.Black, 3, 4));
        var stones = Runtime(board);
        var left = stones.InstanceAt(C(3, 4))!;
        var right = stones.InstanceAt(C(4, 4))!;
        var temporary = TemporaryLibertyState.Create(
            stones,
            [
                new TemporaryLibertyEffect("effect-left", 1, StoneColor.Black, left.InstanceId, "test", 1, 2),
                new TemporaryLibertyEffect("effect-right", 2, StoneColor.Black, right.InstanceId, "test", 2, 5),
            ],
            3);
        var continuous = ContinuousLibertySnapshot.Create(
            stones,
            [new ContinuousLibertyModifier(
                "spring",
                3,
                StoneColor.Black,
                right.InstanceId,
                "board_spirit_spring")]);

        var analysis = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            stones,
            temporary,
            continuous);
        var group = Assert.Single(analysis.GroupAnalysis.Groups);
        var breakdown = analysis.BreakdownFor(group);

        Assert.Same(board, analysis.GroupAnalysis.SourceBoard);
        Assert.Equal(group.RealLibertyCount, breakdown.RealLibertyCount);
        Assert.Equal(3, breakdown.TimedAmount);
        Assert.Equal(3, breakdown.ContinuousAmount);
        Assert.Equal(group.RealLibertyCount + 6, breakdown.EffectiveLibertyCount);
        Assert.Equal(
            breakdown.EffectiveLibertyCount,
            analysis.EffectiveLiberties.EffectiveLibertiesFor(group));
    }

    [Fact]
    public void ReversedEffectsAndModifiersHaveIdenticalCanonicalAnalysis()
    {
        var board = Board(Stone(StoneColor.Black, 4, 4));
        var stones = Runtime(board);
        var anchor = Assert.Single(stones.Instances);
        var effects = new[]
        {
            new TemporaryLibertyEffect("b", 2, StoneColor.Black, anchor.InstanceId, "test", 2, 4),
            new TemporaryLibertyEffect("a", 1, StoneColor.Black, anchor.InstanceId, "test", 1, 3),
        };
        var modifiers = new[]
        {
            new ContinuousLibertyModifier("z", 2, StoneColor.Black, anchor.InstanceId, "spring-z"),
            new ContinuousLibertyModifier("a", -1, StoneColor.Black, anchor.InstanceId, "spring-a"),
        };

        var first = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            stones,
            TemporaryLibertyState.Create(stones, effects, 3),
            ContinuousLibertySnapshot.Create(stones, modifiers));
        var reversed = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            stones,
            TemporaryLibertyState.Create(stones, effects.Reverse(), 3),
            ContinuousLibertySnapshot.Create(stones, modifiers.Reverse()));

        Assert.Equal(first.ToCanonicalText(), reversed.ToCanonicalText());
    }

    [Fact]
    public void AnalyzerRejectsForeignSnapshotsNegativeTotalsAndOverflow()
    {
        var board = Board(Stone(StoneColor.Black, 4, 4));
        var stones = Runtime(board);
        var equivalentStones = Runtime(board);
        var anchor = Assert.Single(stones.Instances);
        var empty = TemporaryLibertyState.Create(stones, [], 1);

        Assert.Throws<ArgumentException>(() =>
            TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
                equivalentStones,
                empty,
                ContinuousLibertySnapshot.Empty(equivalentStones)));
        Assert.Throws<ArgumentException>(() =>
            ContinuousLibertySnapshot.Create(
                stones,
                [
                    new ContinuousLibertyModifier("dup", 1, StoneColor.Black, anchor.InstanceId, "a"),
                    new ContinuousLibertyModifier("dup", 1, StoneColor.Black, anchor.InstanceId, "b"),
                ]));
        Assert.Throws<ArgumentException>(() =>
            ContinuousLibertySnapshot.Create(
                stones,
                [new ContinuousLibertyModifier("wrong", 1, StoneColor.White, anchor.InstanceId, "a")]));

        var negative = ContinuousLibertySnapshot.Create(
            stones,
            [new ContinuousLibertyModifier(
                "negative",
                -5,
                StoneColor.Black,
                anchor.InstanceId,
                "test")]);
        Assert.Throws<InvalidOperationException>(() =>
            TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(stones, empty, negative));

        var overflow = TemporaryLibertyState.Create(
            stones,
            [new TemporaryLibertyEffect(
                "huge",
                int.MaxValue,
                StoneColor.Black,
                anchor.InstanceId,
                "test",
                1,
                3)],
            2);
        Assert.Throws<InvalidOperationException>(() =>
            TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
                stones,
                overflow,
                ContinuousLibertySnapshot.Empty(stones)));
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
