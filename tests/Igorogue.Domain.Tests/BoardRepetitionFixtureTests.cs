using System.Text.Json;
using Igorogue.Domain.Board;

namespace Igorogue.Domain.Tests;

public sealed class BoardRepetitionFixtureTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);
    private static readonly Lazy<IReadOnlyList<RepetitionFixture>> Fixtures = new(LoadFixtures);

    [Theory]
    [InlineData("KO-01")]
    [InlineData("KO-02")]
    [InlineData("KO-03")]
    [InlineData("KO-04")]
    [InlineData("KO-05")]
    [InlineData("KO-06")]
    public void Ko01ThroughKo06MatchSharedKernelAndImmutableCommitSeam(string fixtureId)
    {
        var fixture = RequiredFixture(fixtureId);
        var point = Assert.IsType<CanonicalPoint>(fixture.Point);
        var expectedLegal = Assert.IsType<bool>(fixture.Expected.Legal);
        var expectedReason = Assert.IsType<string>(fixture.Expected.Reason);
        var history = BattleRepetitionHistory.FromObservedBoards(fixture.HistoryBoards);
        var historyBefore = history.ToCanonicalText();

        Assert.Equal(
            StoneTopologyKey.FromBoard(fixture.CurrentBoard),
            history.Current);

        var candidate = Resolve(fixture.CurrentBoard, fixture.Actor, point);
        var evaluation = Evaluate(candidate, history);
        var candidateKey = Assert.IsType<StoneTopologyKey>(evaluation.CandidateTopologyKey);

        Assert.Equal(expectedLegal, evaluation.IsLegal);
        Assert.Equal(expectedReason, evaluation.ReasonId);
        Assert.Equal(
            StoneTopologyKey.FromBoard(fixture.Expected.ResultBoard),
            candidateKey);
        Assert.Equal(
            fixture.Expected.CapturedPoints,
            CapturedPoints(candidate));
        Assert.All(
            candidate.CapturedGroups.SelectMany(group => group.Stones),
            stone => Assert.False(stone.IsKing));
        Assert.Same(fixture.CurrentBoard, candidate.SourceBoard);
        Assert.Equal(historyBefore, history.ToCanonicalText());

        var committed = CommitIfLegal(fixture.CurrentBoard, history, candidate, evaluation);
        if (expectedLegal)
        {
            Assert.Same(candidate.BoardAfterCapture, committed.Board);
            Assert.NotSame(history, committed.History);
            Assert.Equal(history.ObservationCount + 1, committed.History.ObservationCount);
            Assert.Equal(candidateKey, committed.History.Current);
            Assert.Equal(candidate.OrderedFacts, committed.PublishedFacts);
        }
        else
        {
            Assert.Same(fixture.CurrentBoard, committed.Board);
            Assert.Same(history, committed.History);
            Assert.Equal(historyBefore, committed.History.ToCanonicalText());
            Assert.Empty(committed.PublishedFacts);
        }
    }

    [Fact]
    public void Ko03AndKo06PreserveSpecialKindMetadataWhileTopologyIgnoresIt()
    {
        var basic = RequiredFixture("KO-02");
        var infiltrator = RequiredFixture("KO-03");
        var blood = RequiredFixture("KO-06");

        Assert.Equal("basic", basic.StoneKind);
        Assert.Equal("infiltrator", infiltrator.StoneKind);
        Assert.Equal("blood", blood.StoneKind);

        var basicResult = EvaluateFixture(basic);
        var infiltratorResult = EvaluateFixture(infiltrator);
        var bloodResult = EvaluateFixture(blood);

        Assert.Equal(
            StoneTopologyKey.FromBoard(basic.CurrentBoard),
            StoneTopologyKey.FromBoard(infiltrator.CurrentBoard));
        Assert.Equal(basicResult.Evaluation.Status, infiltratorResult.Evaluation.Status);
        Assert.Equal(
            basicResult.Evaluation.CandidateTopologyKey,
            infiltratorResult.Evaluation.CandidateTopologyKey);

        Assert.Equal(PlacementLegalityStatus.StoneTopologyRepetition, bloodResult.Evaluation.Status);
        Assert.Equal(
            bloodResult.History.OrderedObservations[2],
            bloodResult.Evaluation.CandidateTopologyKey);
    }

    [Fact]
    public void Ko04NonStoneMetadataCannotChangeTopologyOrRejectedCommitState()
    {
        var baseline = RequiredFixture("KO-02");
        var changed = RequiredFixture("KO-04");
        var metadata = Assert.IsType<NonStoneStateChanges>(changed.NonStoneStateChanges);

        Assert.Equal("facility_market_01", metadata.DestroyedFacilityId);
        Assert.Equal(3, metadata.QiDelta);
        Assert.Equal(2, metadata.CardsDrawn);
        Assert.Equal(2.5, metadata.BrilliantMultiplier);
        Assert.Equal(
            baseline.HistoryBoards.Select(StoneTopologyKey.FromBoard),
            changed.HistoryBoards.Select(StoneTopologyKey.FromBoard));

        var result = EvaluateFixture(changed);
        var historyBefore = result.History.ToCanonicalText();
        var committed = CommitIfLegal(
            changed.CurrentBoard,
            result.History,
            result.Candidate,
            result.Evaluation);

        Assert.Equal(PlacementLegalityStatus.StoneTopologyRepetition, result.Evaluation.Status);
        Assert.Same(changed.CurrentBoard, committed.Board);
        Assert.Same(result.History, committed.History);
        Assert.Equal(historyBefore, committed.History.ToCanonicalText());
        Assert.Empty(committed.PublishedFacts);
    }

    [Fact]
    public void Ko07SilentlyFiltersRepeatingFirstCandidateAndCommitsSecondCandidate()
    {
        var fixture = RequiredFixture("KO-07");
        var expectedChosenPoint = Assert.IsType<CanonicalPoint>(fixture.Expected.ChosenPoint);
        var history = BattleRepetitionHistory.FromObservedBoards(fixture.HistoryBoards);
        var historyBefore = history.ToCanonicalText();
        var rejected = new List<RejectedCandidate>();
        HypotheticalPlacementResolution? chosenCandidate = null;
        PlacementLegalityEvaluation? chosenEvaluation = null;
        FixtureCandidate? chosenInput = null;

        foreach (var input in fixture.Candidates)
        {
            var candidate = Resolve(fixture.CurrentBoard, fixture.Actor, input.Point);
            var evaluation = Evaluate(candidate, history);
            if (!evaluation.IsLegal)
            {
                rejected.Add(new RejectedCandidate(input.Point, evaluation.ReasonId));
                var filtered = CommitIfLegal(
                    fixture.CurrentBoard,
                    history,
                    candidate,
                    evaluation);
                Assert.Same(fixture.CurrentBoard, filtered.Board);
                Assert.Same(history, filtered.History);
                Assert.Empty(filtered.PublishedFacts);
                Assert.Equal(historyBefore, history.ToCanonicalText());
                continue;
            }

            chosenCandidate = candidate;
            chosenEvaluation = evaluation;
            chosenInput = input;
            break;
        }

        Assert.NotNull(chosenCandidate);
        Assert.NotNull(chosenEvaluation);
        Assert.NotNull(chosenInput);
        Assert.Equal(expectedChosenPoint, chosenInput.Point);
        Assert.Equal(fixture.Expected.RejectedCandidates, rejected);
        Assert.Equal("basic", fixture.Candidates[0].StoneKind);
        Assert.Equal("basic", fixture.Candidates[1].StoneKind);
        Assert.Equal(historyBefore, history.ToCanonicalText());

        var committed = CommitIfLegal(
            fixture.CurrentBoard,
            history,
            chosenCandidate,
            chosenEvaluation);

        Assert.Same(chosenCandidate.BoardAfterCapture, committed.Board);
        Assert.Equal(
            StoneTopologyKey.FromBoard(fixture.Expected.ResultBoard),
            StoneTopologyKey.FromBoard(committed.Board));
        Assert.Equal(history.ObservationCount + 1, committed.History.ObservationCount);
        Assert.Equal(chosenEvaluation.CandidateTopologyKey, committed.History.Current);
        var placed = Assert.IsType<StonePlacedFact>(Assert.Single(committed.PublishedFacts));
        Assert.Equal(expectedChosenPoint, placed.Stone.Point);
    }

    [Fact]
    public void FixtureTopologiesUseCanonicalBottomToTopCellOrder()
    {
        var expectedKeys = new[]
        {
            "..W.....W.W....BWB.....B.........................",
            "..W.....WBW....B.B.....B.........................",
            "..W....WWBW....B.B.....B.........................",
            "..W....WW.W....BWB.....B.........................",
        };
        var actualKeys = Fixtures.Value
            .SelectMany(fixture => fixture.HistoryBoards.Append(fixture.Expected.ResultBoard))
            .Select(StoneTopologyKey.FromBoard)
            .Select(key => key.CanonicalCells)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            expectedKeys.Order(StringComparer.Ordinal).ToArray(),
            actualKeys);
        Assert.All(actualKeys, key => Assert.Equal(StoneTopologyKey.CellCount, key.Length));
    }

    private static FixtureEvaluation EvaluateFixture(RepetitionFixture fixture)
    {
        var point = Assert.IsType<CanonicalPoint>(fixture.Point);
        var history = BattleRepetitionHistory.FromObservedBoards(fixture.HistoryBoards);
        var candidate = Resolve(fixture.CurrentBoard, fixture.Actor, point);
        return new FixtureEvaluation(candidate, Evaluate(candidate, history), history);
    }

    private static HypotheticalPlacementResolution Resolve(
        BoardState source,
        StoneColor actor,
        CanonicalPoint point)
    {
        var proposedStone = new BoardStone(actor, false, point);
        var created = HypotheticalPlacementResolver.TryCreate(
            source,
            proposedStone,
            out var placement);
        Assert.True(created);
        Assert.NotNull(placement);

        return HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            RealLiberties(placement.GroupsAfterPlacement));
    }

    private static PlacementLegalityEvaluation Evaluate(
        HypotheticalPlacementResolution candidate,
        BattleRepetitionHistory history) =>
        PlacementLegalityEvaluator.Evaluate(
            candidate,
            RealLiberties(candidate.GroupsAfterCapture),
            history,
            PlacementAccessMode.Normal);

    private static EffectiveLibertySnapshot RealLiberties(StoneGroupAnalysis analysis) =>
        EffectiveLibertySnapshot.Create(
            analysis,
            analysis.Groups.Select(group => new GroupEffectiveLiberty(
                group,
                group.RealLibertyCount)));

    private static CommittedPlacement CommitIfLegal(
        BoardState source,
        BattleRepetitionHistory history,
        HypotheticalPlacementResolution candidate,
        PlacementLegalityEvaluation evaluation)
    {
        if (!evaluation.IsLegal)
        {
            return new CommittedPlacement(
                source,
                history,
                Array.Empty<PlacementCaptureFact>());
        }

        var candidateKey = evaluation.CandidateTopologyKey
            ?? throw new InvalidOperationException("Legal placement must provide a topology key.");
        return new CommittedPlacement(
            candidate.BoardAfterCapture,
            history.RegisterLegalPlacement(candidateKey),
            candidate.OrderedFacts);
    }

    private static IReadOnlyList<CanonicalPoint> CapturedPoints(
        HypotheticalPlacementResolution candidate) =>
        candidate.CapturedGroups
            .SelectMany(group => group.StonePoints)
            .OrderBy(Geometry.ToCanonicalIndex)
            .ToArray();

    private static RepetitionFixture RequiredFixture(string fixtureId) =>
        Assert.Single(Fixtures.Value, fixture => fixture.Id == fixtureId);

    private static IReadOnlyList<RepetitionFixture> LoadFixtures()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(
            root.FullName,
            "game_data",
            "fixtures",
            "board_repetition_fixtures.json");
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var fixtures = document.RootElement
            .EnumerateArray()
            .Select(ParseFixture)
            .ToArray();

        Assert.Equal(
            Enumerable.Range(1, 7).Select(index => $"KO-{index:00}").ToArray(),
            fixtures.Select(fixture => fixture.Id).ToArray());
        return fixtures;
    }

    private static RepetitionFixture ParseFixture(JsonElement element)
    {
        var historyBoards = element.GetProperty("history_boards")
            .EnumerateArray()
            .Select(ParseBoard)
            .ToArray();
        var currentBoard = ParseBoard(element.GetProperty("current_board"));
        var candidates = element.TryGetProperty("candidate_points", out var candidateElements)
            ? candidateElements.EnumerateArray().Select(ParseCandidate).ToArray()
            : Array.Empty<FixtureCandidate>();
        var expected = ParseExpected(element.GetProperty("expected"));
        var point = element.TryGetProperty("point", out var pointElement)
            ? ParsePoint(pointElement)
            : null;
        var stoneKind = element.TryGetProperty("stone_kind", out var kindElement)
            ? RequiredString(kindElement, "stone_kind")
            : null;
        var nonStoneStateChanges = element.TryGetProperty(
            "non_stone_state_changes",
            out var nonStoneElement)
            ? new NonStoneStateChanges(
                RequiredString(nonStoneElement.GetProperty("facility_destroyed"), "facility_destroyed"),
                nonStoneElement.GetProperty("qi_delta").GetInt32(),
                nonStoneElement.GetProperty("cards_drawn").GetInt32(),
                nonStoneElement.GetProperty("brilliant_multiplier").GetDouble())
            : null;

        Assert.NotEmpty(historyBoards);
        Assert.Equal(
            StoneTopologyKey.FromBoard(historyBoards[^1]),
            StoneTopologyKey.FromBoard(currentBoard));

        return new RepetitionFixture(
            RequiredString(element.GetProperty("id"), "id"),
            historyBoards,
            currentBoard,
            ParseColor(RequiredString(element.GetProperty("actor"), "actor")),
            point,
            stoneKind,
            candidates,
            nonStoneStateChanges,
            expected);
    }

    private static FixtureCandidate ParseCandidate(JsonElement element) =>
        new(
            ParsePoint(element.GetProperty("point")),
            RequiredString(element.GetProperty("stone_kind"), "candidate stone_kind"));

    private static FixtureExpected ParseExpected(JsonElement element)
    {
        bool? legal = element.TryGetProperty("legal", out var legalElement)
            ? legalElement.GetBoolean()
            : null;
        var reason = element.TryGetProperty("reason", out var reasonElement)
            ? RequiredString(reasonElement, "reason")
            : null;
        var capturedPoints = element.TryGetProperty("captured", out var capturedElement)
            ? capturedElement.EnumerateArray().Select(ParsePoint).OrderBy(Geometry.ToCanonicalIndex).ToArray()
            : Array.Empty<CanonicalPoint>();
        var chosenPoint = element.TryGetProperty("chosen_point", out var chosenElement)
            ? ParsePoint(chosenElement)
            : null;
        var rejected = element.TryGetProperty("rejected", out var rejectedElement)
            ? rejectedElement.EnumerateArray()
                .Select(item => new RejectedCandidate(
                    ParsePoint(item.GetProperty("point")),
                    RequiredString(item.GetProperty("reason"), "rejected reason")))
                .ToArray()
            : Array.Empty<RejectedCandidate>();

        return new FixtureExpected(
            legal,
            reason,
            capturedPoints,
            ParseBoard(element.GetProperty("result_board")),
            chosenPoint,
            rejected);
    }

    private static BoardState ParseBoard(JsonElement element)
    {
        var rows = element.EnumerateArray()
            .Select(row => RequiredString(row, "board row"))
            .ToArray();
        Assert.Equal(Geometry.Size, rows.Length);
        var stones = new List<BoardStone>();
        for (var diagramRow = 0; diagramRow < rows.Length; diagramRow++)
        {
            Assert.Equal(Geometry.Size, rows[diagramRow].Length);
            var y = Geometry.CanonicalYFromDiagramRow(diagramRow);
            for (var column = 0; column < rows[diagramRow].Length; column++)
            {
                var symbol = rows[diagramRow][column];
                if (symbol == '.')
                {
                    continue;
                }

                var (color, isKing) = symbol switch
                {
                    'B' => (StoneColor.Black, false),
                    'W' => (StoneColor.White, false),
                    'K' => (StoneColor.Black, true),
                    'Q' => (StoneColor.White, true),
                    _ => throw new InvalidDataException($"Unknown fixture board symbol '{symbol}'."),
                };
                stones.Add(new BoardStone(
                    color,
                    isKing,
                    Geometry.CreateCanonicalPoint(column + 1, y)));
            }
        }

        return BoardState.Create(Geometry, stones);
    }

    private static CanonicalPoint ParsePoint(JsonElement element)
    {
        var coordinates = element.EnumerateArray().Select(value => value.GetInt32()).ToArray();
        if (coordinates.Length != 2)
        {
            throw new InvalidDataException("Fixture point must contain exactly two coordinates.");
        }

        return Geometry.CreateCanonicalPoint(coordinates[0], coordinates[1]);
    }

    private static StoneColor ParseColor(string value) => value switch
    {
        "black" => StoneColor.Black,
        "white" => StoneColor.White,
        _ => throw new InvalidDataException($"Unknown fixture actor '{value}'."),
    };

    private static string RequiredString(JsonElement element, string fieldName) =>
        element.GetString()
        ?? throw new InvalidDataException($"Fixture {fieldName} cannot be null.");

    private static DirectoryInfo FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Igorogue.sln")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find Igorogue.sln from test output path.");
    }

    private sealed record RepetitionFixture(
        string Id,
        IReadOnlyList<BoardState> HistoryBoards,
        BoardState CurrentBoard,
        StoneColor Actor,
        CanonicalPoint? Point,
        string? StoneKind,
        IReadOnlyList<FixtureCandidate> Candidates,
        NonStoneStateChanges? NonStoneStateChanges,
        FixtureExpected Expected);

    private sealed record FixtureCandidate(CanonicalPoint Point, string StoneKind);

    private sealed record NonStoneStateChanges(
        string DestroyedFacilityId,
        int QiDelta,
        int CardsDrawn,
        double BrilliantMultiplier);

    private sealed record FixtureExpected(
        bool? Legal,
        string? Reason,
        IReadOnlyList<CanonicalPoint> CapturedPoints,
        BoardState ResultBoard,
        CanonicalPoint? ChosenPoint,
        IReadOnlyList<RejectedCandidate> RejectedCandidates);

    private sealed record RejectedCandidate(CanonicalPoint Point, string Reason);

    private sealed record FixtureEvaluation(
        HypotheticalPlacementResolution Candidate,
        PlacementLegalityEvaluation Evaluation,
        BattleRepetitionHistory History);

    private sealed record CommittedPlacement(
        BoardState Board,
        BattleRepetitionHistory History,
        IReadOnlyList<PlacementCaptureFact> PublishedFacts);
}
