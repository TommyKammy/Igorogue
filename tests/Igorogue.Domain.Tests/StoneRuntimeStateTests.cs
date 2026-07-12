using Igorogue.Domain.Board;
using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Tests;

public sealed class StoneRuntimeStateTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void CreateBindsEveryExactBoardStoneAndCanonicalizesInput()
    {
        var board = Board(
            Stone(StoneColor.White, 6, 6, isKing: true),
            Stone(StoneColor.Black, 2, 2, isKing: true),
            Stone(StoneColor.Black, 3, 2));
        var instances = CreateInstances(board);

        var canonical = StoneRuntimeState.Create(board, instances, 4);
        var reversed = StoneRuntimeState.Create(board, instances.Reverse(), 4);

        Assert.Same(board, canonical.SourceBoard);
        Assert.Equal(
            new[] { C(2, 2), C(3, 2), C(6, 6) },
            canonical.Instances.Select(instance => instance.Point));
        Assert.Equal(canonical.ToCanonicalText(), reversed.ToCanonicalText());
        Assert.Equal(
            DeterministicChecksum.Sha256Hex(canonical.ToCanonicalText()),
            DeterministicChecksum.Sha256Hex(reversed.ToCanonicalText()));
        Assert.Same(canonical.Instances[0], canonical.InstanceAt(C(2, 2)));
        Assert.Same(
            canonical.Instances[0],
            canonical.InstanceById(canonical.Instances[0].InstanceId));
    }

    [Fact]
    public void KindAndEffectMetadataNeverChangeStoneTopologyProjection()
    {
        var board = Board(Stone(StoneColor.Black, 4, 4));
        var stone = Assert.Single(board.OccupiedStones);
        var basic = StoneRuntimeState.Create(
            board,
            [new StoneRuntimeInstance("stone-a", stone, "basic", 1, [])],
            2);
        var blood = StoneRuntimeState.Create(
            board,
            [new StoneRuntimeInstance("stone-b", stone, "blood", 1, ["on_captured.draw", "on_captured.soul"])],
            2);

        Assert.NotEqual(basic.ToCanonicalText(), blood.ToCanonicalText());
        Assert.Equal(
            StoneTopologyKey.FromBoard(basic.SourceBoard),
            StoneTopologyKey.FromBoard(blood.SourceBoard));
    }

    [Fact]
    public void CreateRejectsCoverageIdentityAndSequenceDrift()
    {
        var board = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.White, 6, 6));
        var firstStone = board.StoneAt(C(2, 2))!;
        var secondStone = board.StoneAt(C(6, 6))!;
        var first = new StoneRuntimeInstance("stone-a", firstStone, "basic", 1, []);
        var second = new StoneRuntimeInstance("stone-b", secondStone, "basic", 2, []);

        Assert.Throws<ArgumentException>(() =>
            StoneRuntimeState.Create(board, [first], 3));
        Assert.Throws<ArgumentException>(() =>
            StoneRuntimeState.Create(
                board,
                [first, new StoneRuntimeInstance("stone-a", secondStone, "basic", 2, [])],
                3));
        var simultaneous = StoneRuntimeState.Create(
            board,
            [first, new StoneRuntimeInstance("stone-b", secondStone, "basic", 1, [])],
            2);
        Assert.Equal(2, simultaneous.Instances.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StoneRuntimeState.Create(board, [first, second], 2));

        var equivalentButForeignStone = new BoardStone(
            firstStone.Color,
            firstStone.IsKing,
            firstStone.Point);
        Assert.Throws<ArgumentException>(() =>
            StoneRuntimeState.Create(
                board,
                [
                    new StoneRuntimeInstance("stone-a", equivalentButForeignStone, "basic", 1, []),
                    second,
                ],
                3));
    }

    [Fact]
    public void InstanceAndStateBoundariesAreImmutableAndValidateStableIds()
    {
        var board = Board(Stone(StoneColor.Black, 4, 4));
        var stone = Assert.Single(board.OccupiedStones);
        var instance = new StoneRuntimeInstance(
            "stone-a",
            stone,
            "blood",
            1,
            ["effect.first", "effect.second"]);
        var state = StoneRuntimeState.Create(board, [instance], 2);

        var instances = Assert.IsAssignableFrom<ICollection<StoneRuntimeInstance>>(state.Instances);
        var metadata = Assert.IsAssignableFrom<ICollection<string>>(
            instance.OrderedEffectMetadata);
        Assert.True(instances.IsReadOnly);
        Assert.True(metadata.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => instances.Clear());
        Assert.Throws<NotSupportedException>(() => metadata.Clear());
        Assert.Throws<ArgumentException>(() =>
            new StoneRuntimeInstance("bad id", stone, "basic", 1, []));
        Assert.Throws<ArgumentException>(() =>
            new StoneRuntimeInstance("stone", stone, "bad kind", 1, []));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StoneRuntimeInstance("stone", stone, "basic", 0, []));
        Assert.Throws<ArgumentException>(() => state.InstanceById("bad id"));
        Assert.Null(state.InstanceAt(C(1, 1)));
    }

    private static StoneRuntimeInstance[] CreateInstances(BoardState board) =>
        board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                $"stone-{index + 1}",
                stone,
                "basic",
                index + 1,
                []))
            .ToArray();

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(
        StoneColor color,
        int x,
        int y,
        bool isKing = false) =>
        new(color, isKing, C(x, y));

    private static CanonicalPoint C(int x, int y) =>
        Geometry.CreateCanonicalPoint(x, y);
}
