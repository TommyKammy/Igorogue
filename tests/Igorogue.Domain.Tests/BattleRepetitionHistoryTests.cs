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
    public void CommitLegalPlacementReturnsBoundBoardFactsAndNewHistory()
    {
        var initial = Board(Stone(StoneColor.Black, 4, 1, isKing: true));
        var history = BattleRepetitionHistory.Start(initial);
        var (candidate, evaluation) = Evaluate(
            initial,
            Stone(StoneColor.Black, 4, 2),
            history);
        var nextKey = StoneTopologyKey.FromBoard(candidate.BoardAfterCapture);

        var commit = history.CommitLegalPlacement(evaluation);
        var nextHistory = commit.HistoryAfterCommit;

        Assert.Equal(1, history.ObservationCount);
        Assert.False(history.HasSeen(nextKey));
        Assert.Same(candidate, commit.Candidate);
        Assert.Same(candidate.BoardAfterCapture, commit.BoardAfterCommit);
        Assert.Equal(candidate.OrderedFacts, commit.OrderedFacts);
        Assert.Equal(nextKey, commit.RegisteredTopologyKey);
        Assert.Equal(2, nextHistory.ObservationCount);
        Assert.Equal(2, nextHistory.UniqueKeyCount);
        Assert.Equal(nextKey, nextHistory.Current);
        Assert.Equal(
            new[] { StoneTopologyKey.FromBoard(initial), nextKey },
            nextHistory.OrderedObservations);
        Assert.Throws<ArgumentException>(() =>
            nextHistory.CommitLegalPlacement(evaluation));
    }

    [Fact]
    public void CommitRejectsStaleOrEquivalentButDifferentHistoryInstances()
    {
        var source = Board();
        var history = BattleRepetitionHistory.Start(source);
        var (_, evaluation) = Evaluate(
            source,
            Stone(StoneColor.Black, 4, 4),
            history);
        var advanced = history.CommitLegalPlacement(evaluation).HistoryAfterCommit;
        var equivalentButDifferent = BattleRepetitionHistory.Start(source);

        Assert.Throws<ArgumentException>(() =>
            advanced.CommitLegalPlacement(evaluation));
        Assert.Throws<ArgumentException>(() =>
            equivalentButDifferent.CommitLegalPlacement(evaluation));
    }

    [Fact]
    public void CommitUsesTheCandidateBoundToTheLegalEvaluation()
    {
        var source = Board();
        var history = BattleRepetitionHistory.Start(source);
        var (firstCandidate, firstEvaluation) = Evaluate(
            source,
            Stone(StoneColor.Black, 2, 2),
            history);
        var (otherCandidate, _) = Evaluate(
            source,
            Stone(StoneColor.Black, 6, 6),
            history);

        var commit = history.CommitLegalPlacement(firstEvaluation);

        Assert.Same(firstCandidate, firstEvaluation.AcceptedCandidate);
        Assert.Same(firstCandidate, commit.Candidate);
        Assert.NotSame(otherCandidate, commit.Candidate);
        Assert.Equal(
            StoneTopologyKey.FromBoard(firstCandidate.BoardAfterCapture),
            commit.HistoryAfterCommit.Current);
        Assert.NotEqual(
            StoneTopologyKey.FromBoard(otherCandidate.BoardAfterCapture),
            commit.HistoryAfterCommit.Current);
    }

    [Fact]
    public void CommitRejectsIllegalEvaluationAndHasNoRawKeyOverload()
    {
        var source = Board();
        var history = BattleRepetitionHistory.Start(source);
        var (_, terminalWithoutCapture) = Evaluate(
            source,
            Stone(StoneColor.Black, 4, 4),
            history,
            PlacementAccessMode.TerminalCapture);

        Assert.False(terminalWithoutCapture.IsLegal);
        Assert.Null(terminalWithoutCapture.AcceptedCandidate);
        Assert.Throws<InvalidOperationException>(() =>
            history.CommitLegalPlacement(terminalWithoutCapture));
        Assert.DoesNotContain(
            typeof(BattleRepetitionHistory).GetMethods(),
            method => method.Name == "RegisterLegalPlacement");
        Assert.DoesNotContain(
            typeof(BattleRepetitionHistory).GetMethods(),
            method => method.Name.Contains("Mandatory", StringComparison.Ordinal));
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
    public void HistoryBoundariesRejectNullAndEmptyInput()
    {
        var initial = Board();
        var history = BattleRepetitionHistory.Start(initial);

        Assert.Throws<ArgumentNullException>(() => BattleRepetitionHistory.Start(null!));
        Assert.Throws<ArgumentNullException>(
            () => BattleRepetitionHistory.FromObservedBoards(null!));
        Assert.Throws<ArgumentNullException>(() =>
            BattleRepetitionHistory.FromObservedBoards([initial, null!]));
        Assert.Throws<ArgumentException>(() =>
            BattleRepetitionHistory.FromObservedBoards([]));
        Assert.Throws<ArgumentNullException>(() => history.HasSeen(null!));
        Assert.Throws<ArgumentNullException>(() => history.CommitLegalPlacement(null!));

        var collection = Assert.IsAssignableFrom<ICollection<StoneTopologyKey>>(
            history.OrderedObservations);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Clear());
    }

    private static (
        HypotheticalPlacementResolution Candidate,
        PlacementLegalityEvaluation Evaluation) Evaluate(
        BoardState source,
        BoardStone proposedStone,
        BattleRepetitionHistory history,
        PlacementAccessMode accessMode = PlacementAccessMode.Normal)
    {
        if (!HypotheticalPlacementResolver.TryCreate(source, proposedStone, out var placement) ||
            placement is null)
        {
            throw new InvalidOperationException("Test candidate unexpectedly targeted an occupied point.");
        }

        var candidate = HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            RealLiberties(placement.GroupsAfterPlacement));
        var evaluation = PlacementLegalityEvaluator.Evaluate(
            candidate,
            RealLiberties(candidate.GroupsAfterCapture),
            history,
            accessMode);
        return (candidate, evaluation);
    }

    private static EffectiveLibertySnapshot RealLiberties(StoneGroupAnalysis analysis) =>
        EffectiveLibertySnapshot.Create(
            analysis,
            analysis.Groups.Select(group => new GroupEffectiveLiberty(
                group,
                group.RealLibertyCount)));

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(
        StoneColor color,
        int x,
        int y,
        bool isKing = false) =>
        new(color, isKing, Geometry.CreateCanonicalPoint(x, y));
}
