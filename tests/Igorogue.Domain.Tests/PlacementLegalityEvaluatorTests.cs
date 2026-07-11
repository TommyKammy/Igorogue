using Igorogue.Domain.Board;

namespace Igorogue.Domain.Tests;

public sealed class PlacementLegalityEvaluatorTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void CaptureCreatedLibertyIsLegalAndTerminalEligible()
    {
        var source = KoBeforeCapture();
        var candidate = Resolve(source, Stone(StoneColor.Black, 3, 2));
        var history = BattleRepetitionHistory.Start(source);

        var evaluation = Evaluate(candidate, history, PlacementAccessMode.TerminalCapture);

        Assert.True(candidate.SatisfiesTerminalCaptureCondition);
        Assert.Equal(1, candidate.PlacedGroupAfterCapture.RealLibertyCount);
        Assert.True(evaluation.IsLegal);
        Assert.Equal(PlacementLegalityStatus.Legal, evaluation.Status);
        Assert.Equal("legal", evaluation.ReasonId);
        Assert.Equal(
            StoneTopologyKey.FromBoard(candidate.BoardAfterCapture),
            evaluation.CandidateTopologyKey);
        Assert.Same(candidate, evaluation.AcceptedCandidate);
        Assert.Equal(1, history.ObservationCount);
        Assert.Same(source, candidate.SourceBoard);
    }

    [Fact]
    public void TerminalModeRequiresImmediateOpponentCapture()
    {
        var source = Board(Stone(StoneColor.Black, 4, 4));
        var candidate = Resolve(source, Stone(StoneColor.Black, 7, 7));
        var history = BattleRepetitionHistory.Start(source);

        var terminal = Evaluate(candidate, history, PlacementAccessMode.TerminalCapture);
        var normal = Evaluate(candidate, history, PlacementAccessMode.Normal);

        Assert.False(candidate.SatisfiesTerminalCaptureCondition);
        Assert.Equal(PlacementLegalityStatus.TerminalCaptureRequired, terminal.Status);
        Assert.Equal("terminal_capture_required", terminal.ReasonId);
        Assert.Null(terminal.CandidateTopologyKey);
        Assert.Null(terminal.AcceptedCandidate);
        Assert.True(normal.IsLegal);
        Assert.Same(candidate, normal.AcceptedCandidate);
    }

    [Fact]
    public void EffectiveLibertyZeroRejectsSuicideEvenAfterTerminalCapture()
    {
        var source = KoBeforeCapture();
        var candidate = Resolve(source, Stone(StoneColor.Black, 3, 2));
        var history = BattleRepetitionHistory.Start(source);
        var postCapture = EffectiveFor(
            candidate.GroupsAfterCapture,
            (candidate.PlacedGroupAfterCapture.Anchor, 0));

        var evaluation = PlacementLegalityEvaluator.Evaluate(
            candidate,
            postCapture,
            history,
            PlacementAccessMode.TerminalCapture);

        Assert.True(candidate.SatisfiesTerminalCaptureCondition);
        Assert.Equal(PlacementLegalityStatus.Suicide, evaluation.Status);
        Assert.Equal("suicide", evaluation.ReasonId);
        Assert.Null(evaluation.CandidateTopologyKey);
        Assert.Null(evaluation.AcceptedCandidate);
        Assert.Equal(1, history.ObservationCount);
    }

    [Fact]
    public void PositiveEffectiveLibertyAllowsRealZeroGroup()
    {
        var source = Board(
            Stone(StoneColor.Black, 2, 1),
            Stone(StoneColor.Black, 1, 2),
            Stone(StoneColor.Black, 3, 2),
            Stone(StoneColor.Black, 2, 3));
        var candidate = Resolve(source, Stone(StoneColor.White, 2, 2));
        var history = BattleRepetitionHistory.Start(source);
        var postCapture = EffectiveFor(
            candidate.GroupsAfterCapture,
            (candidate.PlacedGroupAfterCapture.Anchor, 1));

        var evaluation = PlacementLegalityEvaluator.Evaluate(
            candidate,
            postCapture,
            history,
            PlacementAccessMode.Normal);

        Assert.Equal(0, candidate.PlacedGroupAfterCapture.RealLibertyCount);
        Assert.True(evaluation.IsLegal);
        Assert.Same(candidate, evaluation.AcceptedCandidate);
    }

    [Fact]
    public void RepetitionRejectsTerminalAndKingCapturesWithoutRegisteringHistory()
    {
        var source = KoBeforeCapture();
        var blackCapture = Resolve(
            source,
            Stone(StoneColor.Black, 3, 2, isKing: true));
        var afterBlack = blackCapture.BoardAfterCapture;
        var history = BattleRepetitionHistory.FromObservedBoards([source, afterBlack]);
        var whiteRecapture = Resolve(
            afterBlack,
            Stone(StoneColor.White, 3, 3));

        var evaluation = Evaluate(
            whiteRecapture,
            history,
            PlacementAccessMode.TerminalCapture);

        Assert.True(whiteRecapture.SatisfiesTerminalCaptureCondition);
        Assert.Contains(
            whiteRecapture.CapturedGroups,
            group => group.Stones.Any(stone => stone.IsKing));
        Assert.Equal(PlacementLegalityStatus.StoneTopologyRepetition, evaluation.Status);
        Assert.Equal("stone_topology_repetition", evaluation.ReasonId);
        Assert.Null(evaluation.AcceptedCandidate);
        Assert.Equal(2, history.ObservationCount);
        Assert.Same(afterBlack, whiteRecapture.SourceBoard);
    }

    [Fact]
    public void SuicideIsCheckedBeforeRepetition()
    {
        var source = Board(Stone(StoneColor.Black, 4, 4));
        var candidate = Resolve(source, Stone(StoneColor.White, 4, 5));
        var candidateBoard = candidate.BoardAfterCapture;
        var history = BattleRepetitionHistory.FromObservedBoards(
            [candidateBoard, source]);
        var postCapture = EffectiveFor(
            candidate.GroupsAfterCapture,
            (candidate.PlacedGroupAfterCapture.Anchor, 0));

        var evaluation = PlacementLegalityEvaluator.Evaluate(
            candidate,
            postCapture,
            history,
            PlacementAccessMode.Normal);

        Assert.True(history.HasSeen(StoneTopologyKey.FromBoard(candidateBoard)));
        Assert.Equal(PlacementLegalityStatus.Suicide, evaluation.Status);
        Assert.Null(evaluation.AcceptedCandidate);
    }

    [Fact]
    public void OccupiedPointStillStopsBeforeHypotheticalLegality()
    {
        var source = Board(Stone(StoneColor.Black, 4, 4));

        var created = HypotheticalPlacementResolver.TryCreate(
            source,
            Stone(StoneColor.White, 4, 4),
            out var placement);

        Assert.False(created);
        Assert.Null(placement);
        Assert.Single(source.OccupiedStones);
    }

    [Fact]
    public void EvaluatorRejectsForeignSnapshotsHistoriesModesAndNulls()
    {
        var source = Board();
        var candidate = Resolve(source, Stone(StoneColor.Black, 4, 4));
        var history = BattleRepetitionHistory.Start(source);
        var foreignCandidate = Resolve(source, Stone(StoneColor.Black, 3, 3));
        var foreignSnapshot = EffectiveFor(foreignCandidate.GroupsAfterCapture);
        var foreignHistory = BattleRepetitionHistory.Start(
            Board(Stone(StoneColor.White, 7, 7)));
        var validSnapshot = EffectiveFor(candidate.GroupsAfterCapture);

        Assert.Throws<ArgumentException>(() => PlacementLegalityEvaluator.Evaluate(
            candidate,
            foreignSnapshot,
            history,
            PlacementAccessMode.Normal));
        Assert.Throws<ArgumentException>(() => PlacementLegalityEvaluator.Evaluate(
            candidate,
            validSnapshot,
            foreignHistory,
            PlacementAccessMode.Normal));
        Assert.Throws<ArgumentOutOfRangeException>(() => PlacementLegalityEvaluator.Evaluate(
            candidate,
            validSnapshot,
            history,
            (PlacementAccessMode)99));
        Assert.Throws<ArgumentNullException>(() => PlacementLegalityEvaluator.Evaluate(
            null!,
            validSnapshot,
            history,
            PlacementAccessMode.Normal));
        Assert.Throws<ArgumentNullException>(() => PlacementLegalityEvaluator.Evaluate(
            candidate,
            null!,
            history,
            PlacementAccessMode.Normal));
        Assert.Throws<ArgumentNullException>(() => PlacementLegalityEvaluator.Evaluate(
            candidate,
            validSnapshot,
            null!,
            PlacementAccessMode.Normal));
    }

    private static PlacementLegalityEvaluation Evaluate(
        HypotheticalPlacementResolution candidate,
        BattleRepetitionHistory history,
        PlacementAccessMode accessMode) =>
        PlacementLegalityEvaluator.Evaluate(
            candidate,
            EffectiveFor(candidate.GroupsAfterCapture),
            history,
            accessMode);

    private static HypotheticalPlacementResolution Resolve(
        BoardState source,
        BoardStone proposedStone)
    {
        if (!HypotheticalPlacementResolver.TryCreate(source, proposedStone, out var placement) ||
            placement is null)
        {
            throw new InvalidOperationException("Test candidate unexpectedly targeted an occupied point.");
        }

        return HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            EffectiveFor(placement.GroupsAfterPlacement));
    }

    private static EffectiveLibertySnapshot EffectiveFor(
        StoneGroupAnalysis analysis,
        params (CanonicalPoint Anchor, int Count)[] overrides)
    {
        var facts = analysis.Groups.Select(group =>
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
        });
        return EffectiveLibertySnapshot.Create(analysis, facts);
    }

    private static BoardState KoBeforeCapture() => Board(
        Stone(StoneColor.Black, 3, 4),
        Stone(StoneColor.Black, 2, 3),
        Stone(StoneColor.Black, 4, 3),
        Stone(StoneColor.White, 3, 3),
        Stone(StoneColor.White, 2, 2),
        Stone(StoneColor.White, 4, 2),
        Stone(StoneColor.White, 3, 1));

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(
        StoneColor color,
        int x,
        int y,
        bool isKing = false) =>
        new(color, isKing, Geometry.CreateCanonicalPoint(x, y));
}
