using Igorogue.Domain.Board;

namespace Igorogue.Domain.Tests;

public sealed class BoardStateTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void CreateCopiesAndCanonicalizesOccupiedStones()
    {
        var input = new List<BoardStone>
        {
            S(StoneColor.White, false, 7, 7),
            S(StoneColor.Black, true, 1, 1),
            S(StoneColor.Black, false, 4, 3),
        };

        var board = BoardState.Create(Geometry, input);
        input.Clear();

        Assert.Equal(
            new[] { C(1, 1), C(4, 3), C(7, 7) },
            board.OccupiedStones.Select(stone => stone.Point).ToArray());
        Assert.True(board.StoneAt(C(1, 1))?.IsKing);
        Assert.Equal(StoneColor.White, board.StoneAt(C(7, 7))?.Color);
        Assert.True(board.IsEmpty(C(4, 4)));
        Assert.Equal(3, board.OccupiedStones.Count);
    }

    [Fact]
    public void FromInitialPositionMapsKingAndGuardToRuntimeStoneMetadata()
    {
        var initial = InitialPositionDefinition.Create(
            Geometry,
            "runtime_mapping",
            [
                new InitialStonePlacement(StoneColor.Black, InitialStoneRole.King, C(2, 2)),
                new InitialStonePlacement(StoneColor.Black, InitialStoneRole.Guard, C(3, 2)),
            ]);

        var board = BoardState.FromInitialPosition(initial);

        Assert.True(board.StoneAt(C(2, 2))?.IsKing);
        Assert.False(board.StoneAt(C(3, 2))?.IsKing);
    }

    [Theory]
    [InlineData(StoneColor.Black)]
    [InlineData(StoneColor.White)]
    public void CreateRejectsDuplicatePointsWithoutLastWriteWins(StoneColor duplicateColor)
    {
        var duplicate = new[]
        {
            S(StoneColor.Black, false, 2, 2),
            S(duplicateColor, true, 2, 2),
        };

        Assert.Throws<ArgumentException>(() => BoardState.Create(Geometry, duplicate));
    }

    [Fact]
    public void StoneAndBoardBoundariesRejectInvalidOrNullInput()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BoardStone((StoneColor)99, false, C(1, 1)));
        Assert.Throws<ArgumentNullException>(
            () => new BoardStone(StoneColor.Black, false, null!));
        Assert.Throws<ArgumentNullException>(
            () => BoardState.Create(null!, []));
        Assert.Throws<ArgumentNullException>(
            () => BoardState.Create(Geometry, null!));
        Assert.Throws<ArgumentNullException>(
            () => BoardState.Create(Geometry, [(BoardStone)null!]));
        Assert.Throws<ArgumentNullException>(
            () => BoardState.FromInitialPosition(null!));
        Assert.Throws<ArgumentNullException>(
            () => BoardState.Create(Geometry, []).StoneAt(null!));
    }

    [Fact]
    public void OccupiedStoneViewCannotMutateBoardState()
    {
        var board = BoardState.Create(Geometry, [S(StoneColor.Black, false, 1, 1)]);
        var collection = Assert.IsAssignableFrom<ICollection<BoardStone>>(board.OccupiedStones);

        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Clear());
        Assert.NotNull(board.StoneAt(C(1, 1)));
    }

    private static BoardStone S(StoneColor color, bool isKing, int x, int y) =>
        new(color, isKing, C(x, y));

    private static CanonicalPoint C(int x, int y) => Geometry.CreateCanonicalPoint(x, y);
}
