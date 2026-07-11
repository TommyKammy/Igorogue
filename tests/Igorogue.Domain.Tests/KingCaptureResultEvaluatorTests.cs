using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

public sealed class KingCaptureResultEvaluatorTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void EmptyAndOrdinaryCaptureBatchesRemainOngoing()
    {
        var empty = KingCaptureResultEvaluator.EvaluateAtomicCapture([]);
        var ordinary = KingCaptureResultEvaluator.EvaluateAtomicCapture(Groups(
            Stone(StoneColor.Black, 1, 1),
            Stone(StoneColor.White, 7, 7)));

        Assert.Equal(BattleOutcome.Ongoing, empty.Outcome);
        Assert.Equal("ongoing", empty.OutcomeId);
        Assert.False(empty.HasKingCapture);
        Assert.False(empty.IsTerminal);
        Assert.Equal(
            "king-capture-result-v1:ongoing:black=0:white=0",
            empty.ToCanonicalText());
        Assert.Equal(empty, ordinary);
    }

    [Fact]
    public void WhiteKingInMultiStoneGroupProducesPlayerVictory()
    {
        var result = KingCaptureResultEvaluator.EvaluateAtomicCapture(Groups(
            Stone(StoneColor.White, 2, 2, isKing: true),
            Stone(StoneColor.White, 2, 3),
            Stone(StoneColor.Black, 6, 6)));

        Assert.Equal(BattleOutcome.PlayerVictory, result.Outcome);
        Assert.Equal("win", result.OutcomeId);
        Assert.False(result.BlackKingCaptured);
        Assert.True(result.WhiteKingCaptured);
        Assert.True(result.HasKingCapture);
        Assert.True(result.IsTerminal);
    }

    [Fact]
    public void BlackKingInMultiStoneGroupProducesPlayerDefeat()
    {
        var result = KingCaptureResultEvaluator.EvaluateAtomicCapture(Groups(
            Stone(StoneColor.Black, 2, 2, isKing: true),
            Stone(StoneColor.Black, 3, 2),
            Stone(StoneColor.White, 6, 6)));

        Assert.Equal(BattleOutcome.PlayerDefeat, result.Outcome);
        Assert.Equal("loss", result.OutcomeId);
        Assert.True(result.BlackKingCaptured);
        Assert.False(result.WhiteKingCaptured);
        Assert.True(result.HasKingCapture);
        Assert.True(result.IsTerminal);
    }

    [Fact]
    public void BothKingsProducePlayerDefeatRegardlessOfInputOrder()
    {
        var groups = Groups(
            Stone(StoneColor.Black, 2, 2, isKing: true),
            Stone(StoneColor.Black, 2, 3),
            Stone(StoneColor.White, 6, 6, isKing: true),
            Stone(StoneColor.White, 6, 5));

        var canonical = KingCaptureResultEvaluator.EvaluateAtomicCapture(groups);
        var reversed = KingCaptureResultEvaluator.EvaluateAtomicCapture(
            groups.Reverse().ToArray());

        Assert.Equal(canonical, reversed);
        Assert.Equal(BattleOutcome.PlayerDefeat, canonical.Outcome);
        Assert.True(canonical.BlackKingCaptured);
        Assert.True(canonical.WhiteKingCaptured);
        Assert.Equal(
            "king-capture-result-v1:loss:black=1:white=1",
            canonical.ToCanonicalText());
    }

    [Theory]
    [InlineData(StoneColor.White, false, BattleOutcome.Ongoing)]
    [InlineData(StoneColor.White, true, BattleOutcome.PlayerVictory)]
    [InlineData(StoneColor.Black, true, BattleOutcome.PlayerDefeat)]
    public void LegalPlacementCommitDerivesResultAfterBoardAndHistoryCommit(
        StoneColor capturedColor,
        bool capturedStoneIsKing,
        BattleOutcome expectedOutcome)
    {
        var source = SingleGroupCaptureBoard(capturedColor, capturedStoneIsKing);
        var history = BattleRepetitionHistory.Start(source);
        var proposed = Stone(Opposite(capturedColor), 3, 2);
        var candidate = Resolve(source, proposed);
        var evaluation = PlacementLegalityEvaluator.Evaluate(
            candidate,
            RealLiberties(candidate.GroupsAfterCapture),
            history,
            PlacementAccessMode.Normal);

        var commit = history.CommitLegalPlacement(evaluation);

        Assert.Equal(expectedOutcome, commit.KingCaptureResult.Outcome);
        Assert.Equal(
            capturedStoneIsKing && capturedColor == StoneColor.Black,
            commit.KingCaptureResult.BlackKingCaptured);
        Assert.Equal(
            capturedStoneIsKing && capturedColor == StoneColor.White,
            commit.KingCaptureResult.WhiteKingCaptured);
        Assert.Equal(capturedStoneIsKing, commit.KingCaptureResult.IsTerminal);
        Assert.Same(candidate, commit.Candidate);
        Assert.Same(candidate.BoardAfterCapture, commit.BoardAfterCommit);
        Assert.Equal(
            StoneTopologyKey.FromBoard(commit.BoardAfterCommit),
            commit.RegisteredTopologyKey);
        Assert.Equal(commit.RegisteredTopologyKey, commit.HistoryAfterCommit.Current);
        var capturedFact = Assert.IsType<GroupCapturedFact>(
            Assert.Single(commit.OrderedFacts.Skip(1)));
        Assert.Equal(capturedStoneIsKing, capturedFact.ContainsKing);
        Assert.Equal(Opposite(capturedColor), capturedFact.CapturingColor);
        Assert.Equal(
            capturedStoneIsKing ? 1 : 2,
            commit.BoardAfterCommit.OccupiedStones.Count(stone => stone.IsKing));
        Assert.Equal(1, history.ObservationCount);
    }

    [Fact]
    public void EvaluatorRejectsNullCollectionAndNullGroup()
    {
        var group = Assert.Single(Groups(Stone(StoneColor.Black, 4, 4)));

        Assert.Throws<ArgumentNullException>(() =>
            KingCaptureResultEvaluator.EvaluateAtomicCapture(null!));
        Assert.Throws<ArgumentNullException>(() =>
            KingCaptureResultEvaluator.EvaluateAtomicCapture([group, null!]));
    }

    private static HypotheticalPlacementResolution Resolve(
        BoardState source,
        BoardStone proposedStone)
    {
        if (!HypotheticalPlacementResolver.TryCreate(source, proposedStone, out var placement) ||
            placement is null)
        {
            throw new InvalidOperationException("Test placement unexpectedly targeted an occupied point.");
        }

        return HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            RealLiberties(placement.GroupsAfterPlacement));
    }

    private static EffectiveLibertySnapshot RealLiberties(StoneGroupAnalysis analysis) =>
        EffectiveLibertySnapshot.Create(
            analysis,
            analysis.Groups.Select(group => new GroupEffectiveLiberty(
                group,
                group.RealLibertyCount)));

    private static IReadOnlyList<StoneGroup> Groups(params BoardStone[] stones) =>
        StoneGroupAnalyzer.Analyze(Board(stones)).Groups;

    private static BoardState SingleGroupCaptureBoard(
        StoneColor capturedColor,
        bool capturedStoneIsKing)
    {
        var capturingColor = Opposite(capturedColor);
        var stones = new List<BoardStone>
        {
            Stone(capturingColor, 3, 4),
            Stone(capturingColor, 2, 3),
            Stone(capturingColor, 4, 3),
            Stone(capturedColor, 3, 3, capturedStoneIsKing),
            Stone(capturedColor, 2, 2),
            Stone(capturedColor, 4, 2),
            Stone(capturedColor, 3, 1),
            Stone(capturingColor, 7, 7, isKing: true),
        };
        if (!capturedStoneIsKing)
        {
            stones.Add(Stone(capturedColor, 1, 7, isKing: true));
        }

        return Board(stones.ToArray());
    }

    private static StoneColor Opposite(StoneColor color) => color switch
    {
        StoneColor.Black => StoneColor.White,
        StoneColor.White => StoneColor.Black,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown stone color."),
    };

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(
        StoneColor color,
        int x,
        int y,
        bool isKing = false) =>
        new(color, isKing, Geometry.CreateCanonicalPoint(x, y));
}
