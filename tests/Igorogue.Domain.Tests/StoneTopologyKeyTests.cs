using Igorogue.Domain.Board;

namespace Igorogue.Domain.Tests;

public sealed class StoneTopologyKeyTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void CanonicalCellsUseYThenXOrderAndDistinguishKings()
    {
        var board = BoardState.Create(
            Geometry,
            [
                Stone(StoneColor.Black, 1, 1),
                Stone(StoneColor.White, 7, 1),
                Stone(StoneColor.Black, 2, 2, isKing: true),
                Stone(StoneColor.White, 6, 7, isKing: true),
            ]);

        var key = StoneTopologyKey.FromBoard(board);

        Assert.Equal(StoneTopologyKey.CellCount, key.CanonicalCells.Length);
        Assert.Equal('B', key.CanonicalCells[0]);
        Assert.Equal('W', key.CanonicalCells[6]);
        Assert.Equal('K', key.CanonicalCells[8]);
        Assert.Equal('Q', key.CanonicalCells[47]);
        Assert.Equal(
            $"{StoneTopologyKey.EncodingVersion}:{key.CanonicalCells}",
            key.ToCanonicalText());
    }

    [Fact]
    public void EqualityUsesCompleteTopologyAcrossInstancesAndInputPermutations()
    {
        var first = BoardState.Create(
            Geometry,
            [
                Stone(StoneColor.Black, 2, 3),
                Stone(StoneColor.White, 5, 6, isKing: true),
            ]);
        var second = BoardState.Create(
            Geometry,
            [
                Stone(StoneColor.White, 5, 6, isKing: true),
                Stone(StoneColor.Black, 2, 3),
            ]);
        var changedKingRole = BoardState.Create(
            Geometry,
            [
                Stone(StoneColor.Black, 2, 3),
                Stone(StoneColor.White, 5, 6),
            ]);

        var firstKey = StoneTopologyKey.FromBoard(first);
        var secondKey = StoneTopologyKey.FromBoard(second);
        var changedKey = StoneTopologyKey.FromBoard(changedKingRole);

        Assert.Equal(firstKey, secondKey);
        Assert.Equal(firstKey.GetHashCode(), secondKey.GetHashCode());
        Assert.NotEqual(firstKey, changedKey);
        Assert.NotEqual(firstKey.CanonicalCells, changedKey.CanonicalCells);
    }

    [Fact]
    public void NullBoardIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => StoneTopologyKey.FromBoard(null!));
    }

    private static BoardStone Stone(
        StoneColor color,
        int x,
        int y,
        bool isKing = false) =>
        new(color, isKing, Geometry.CreateCanonicalPoint(x, y));
}
