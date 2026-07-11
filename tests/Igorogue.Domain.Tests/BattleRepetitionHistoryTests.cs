using Igorogue.Domain.Board;

namespace Igorogue.Domain.Tests;

public sealed class BattleRepetitionHistoryTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void StartRegistersInitialBoardAtIndexZero()
    {
        var initial = Board(Stone(StoneColor.Black, 4, 1, isKing: true));

        var history = BattleRepetitionHistory.Start(initial);

        var initialKey = StoneTopologyKey.FromBoard(initial);
        Assert.Equal(1, history.ObservationCount);
        Assert.Equal(1, history.UniqueKeyCount);
        Assert.Equal(initialKey, Assert.Single(history.OrderedObservations));
        Assert.Equal(initialKey, history.Current);
        Assert.True(history.HasSeen(initialKey));
    }

    [Fact]
    public void RegisterLegalPlacementReturnsNewHistoryWithoutMutatingSource()
    {
        var initial = Board(Stone(StoneColor.Black, 4, 1, isKing: true));
        var nextBoard = Board(
            Stone(StoneColor.Black, 4, 1, isKing: true),
            Stone(StoneColor.Black, 4, 2));
        var history = BattleRepetitionHistory.Start(initial);
        var nextKey = StoneTopologyKey.FromBoard(nextBoard);

        var nextHistory = history.RegisterLegalPlacement(nextKey);

        Assert.Equal(1, history.ObservationCount);
        Assert.False(history.HasSeen(nextKey));
        Assert.Equal(2, nextHistory.ObservationCount);
        Assert.Equal(2, nextHistory.UniqueKeyCount);
        Assert.Equal(nextKey, nextHistory.Current);
        Assert.Equal(
            new[] { StoneTopologyKey.FromBoard(initial), nextKey },
            nextHistory.OrderedObservations);
        Assert.Throws<InvalidOperationException>(
            () => nextHistory.RegisterLegalPlacement(nextKey));
    }

    [Fact]
    public void RestoredObservationsPreserveOrderAndDuplicateMandatoryKeys()
    {
        var initial = Board(Stone(StoneColor.Black, 4, 1, isKing: true));
        var changed = Board(
            Stone(StoneColor.Black, 4, 1, isKing: true),
            Stone(StoneColor.White, 4, 7, isKing: true));

        var history = BattleRepetitionHistory.FromObservedBoards(
            [initial, changed, initial]);

        Assert.Equal(3, history.ObservationCount);
        Assert.Equal(2, history.UniqueKeyCount);
        Assert.Equal(StoneTopologyKey.FromBoard(initial), history.Current);
        var expectedText = string.Join(
            '\n',
            BattleRepetitionHistory.EncodingVersion,
            "3",
            StoneTopologyKey.FromBoard(initial).ToCanonicalText(),
            StoneTopologyKey.FromBoard(changed).ToCanonicalText(),
            StoneTopologyKey.FromBoard(initial).ToCanonicalText());
        Assert.Equal(expectedText, history.ToCanonicalText());
    }

    [Fact]
    public void HistoryBoundariesRejectNullEmptyAndDuplicateLegalRegistration()
    {
        var initial = Board();
        var history = BattleRepetitionHistory.Start(initial);
        var initialKey = StoneTopologyKey.FromBoard(initial);

        Assert.Throws<ArgumentNullException>(() => BattleRepetitionHistory.Start(null!));
        Assert.Throws<ArgumentNullException>(
            () => BattleRepetitionHistory.FromObservedBoards(null!));
        Assert.Throws<ArgumentNullException>(() =>
            BattleRepetitionHistory.FromObservedBoards([initial, null!]));
        Assert.Throws<ArgumentException>(() =>
            BattleRepetitionHistory.FromObservedBoards([]));
        Assert.Throws<ArgumentNullException>(() => history.HasSeen(null!));
        Assert.Throws<ArgumentNullException>(() => history.RegisterLegalPlacement(null!));
        Assert.Throws<InvalidOperationException>(() =>
            history.RegisterLegalPlacement(initialKey));

        var collection = Assert.IsAssignableFrom<ICollection<StoneTopologyKey>>(
            history.OrderedObservations);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Clear());
    }

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(
        StoneColor color,
        int x,
        int y,
        bool isKing = false) =>
        new(color, isKing, Geometry.CreateCanonicalPoint(x, y));
}
