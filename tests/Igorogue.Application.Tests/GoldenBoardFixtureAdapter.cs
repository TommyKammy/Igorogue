using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

internal static class GoldenBoardFixtureAdapter
{
    internal const string RelativeCatalogPath = "tests/golden/v1/board_fixture_cases.json";
    internal const string SchemaId = "igorogue.golden-board-fixtures";
    internal const int SchemaVersion = 1;
    internal const string FactProjectionVersion = "igorogue-battle-fact-v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    internal static GoldenBoardCatalog Load()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root.FullName, RelativeCatalogPath);
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<GoldenBoardCatalog>(stream, JsonOptions)
            ?? throw new InvalidDataException("Golden board fixture catalog is empty.");
    }

    internal static DirectoryInfo FindRepositoryRoot()
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

        throw new DirectoryNotFoundException(
            "Could not find Igorogue.sln from the test output path.");
    }

    internal static string Sha256ForRepositoryFile(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var fullPath = RepositoryFilePath(relativePath);
        using var stream = File.OpenRead(fullPath);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    internal static bool RepositoryFileContains(string relativePath, string expectedText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedText);
        return File.ReadAllText(RepositoryFilePath(relativePath))
            .Contains(expectedText, StringComparison.Ordinal);
    }

    internal static IReadOnlySet<string> LoadSourceFixtureIds(GoldenSourceCatalog source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var path = RepositoryFilePath(source.Path);
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                $"Golden source catalog '{source.Id}' must contain a top-level array.");
        }

        return document.RootElement
            .EnumerateArray()
            .Select(element => element.GetProperty("id").GetString()
                ?? throw new InvalidDataException(
                    $"Golden source catalog '{source.Id}' contains a null fixture ID."))
            .ToHashSet(StringComparer.Ordinal);
    }

    internal static JsonElement LoadSourceFixture(
        GoldenSourceCatalog source,
        string fixtureId)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureId);
        using var stream = File.OpenRead(RepositoryFilePath(source.Path));
        using var document = JsonDocument.Parse(stream);
        var matches = document.RootElement
            .EnumerateArray()
            .Where(element => string.Equals(
                element.GetProperty("id").GetString(),
                fixtureId,
                StringComparison.Ordinal))
            .Select(element => element.Clone())
            .ToArray();
        return matches.Length == 1
            ? matches[0]
            : throw new InvalidDataException(
                $"Golden source catalog '{source.Id}' must contain exactly one '{fixtureId}'.");
    }

    internal static GoldenRunResult Run(
        GoldenBoardCatalog catalog,
        GoldenBoardCase fixture,
        bool reverseSetupEnumeration)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(fixture);

        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var stones = ParseBoard(geometry, fixture.Initial.Board).ToList();
        if (reverseSetupEnumeration)
        {
            stones.Reverse();
        }

        var board = BoardState.Create(geometry, stones);
        var facilities = fixture.Initial.Facilities
            .Select(item => new FacilityInstance(
                item.InstanceId,
                item.ContentId,
                ParseColor(item.Owner),
                Point(geometry, item.Point),
                item.BuildSequence,
                item.ExplicitDisableSources))
            .ToList();
        if (reverseSetupEnumeration)
        {
            facilities.Reverse();
        }

        var policy = new BattleRuntimePolicy(
            catalog.RuntimePolicy.PlayerTurnLimit,
            FacilityRuntimePolicy.Create(
                catalog.RuntimePolicy.TerritoryIncomeDivisor,
                catalog.RuntimePolicy.CapacityBands.Select(band =>
                    new FacilityCapacityBand(band.Min, band.Max, band.Slots)),
                catalog.RuntimePolicy.SlotCap,
                catalog.RuntimePolicy.TypeLimits
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)));
        var session = HeadlessBattleStateMachine.Start(
            board,
            FacilityState.Create(
                board,
                facilities,
                fixture.Initial.NextBuildSequence),
            policy,
            ReplayMetadata.Create(
                catalog.GameVersion,
                catalog.ContentHash,
                fixture.Seed));
        var initialSession = session;
        var initialStateChecksum = session.State.Checksum;
        var initialLogChecksum = session.CommandLog.CurrentChecksum;

        var boundaries = new List<GoldenActualBoundary>();
        var commandResults = new List<BattleCommandResult>();
        for (var stepIndex = 0; stepIndex < fixture.Steps.Count; stepIndex++)
        {
            var step = fixture.Steps[stepIndex];
            if (!string.Equals(
                    session.State.Checksum,
                    step.BeforeStateChecksum,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    session.CommandLog.CurrentChecksum,
                    step.BeforeLogChecksum,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"{fixture.Id}/{step.Type} precondition checksum does not match the live session.");
            }

            if (step.Type == "adapter.silent_candidate_filter")
            {
                var chosen = RunSilentCandidateFilter(fixture.Id, session, step, geometry);
                if (stepIndex + 1 >= fixture.Steps.Count)
                {
                    throw new InvalidDataException(
                        $"{fixture.Id} silent filter has no submitted command.");
                }

                var submitted = fixture.Steps[stepIndex + 1];
                if (submitted.Type != "battle.authorized_stone_placement" ||
                    submitted.Actor != "white" ||
                    submitted.Point is null ||
                    !chosen.Equals(Point(geometry, submitted.Point)))
                {
                    throw new InvalidDataException(
                        $"{fixture.Id} silent-filter choice is not the immediately submitted white command.");
                }

                boundaries.Add(new GoldenActualBoundary(
                    null,
                    "silent_filter",
                    session.State.Checksum,
                    session.CommandLog.CurrentChecksum,
                    [],
                    true,
                    session.CommandLog.Entries.Count,
                    session.CommandLog.Entries.Count));
                continue;
            }

            var command = CreateCommand(step, geometry);
            var before = session;
            var beforeCount = session.CommandLog.Entries.Count;
            var result = HeadlessBattleStateMachine.Execute(session, command);
            session = result.SessionAfter;
            commandResults.Add(result);
            boundaries.Add(new GoldenActualBoundary(
                result.Accepted,
                result.ReasonId,
                result.StateChecksum,
                result.LogChecksum,
                result.OrderedFacts.Select(ProjectFact).ToArray(),
                ReferenceEquals(before, result.SessionAfter),
                beforeCount,
                result.SessionAfter.CommandLog.Entries.Count));
        }

        return new GoldenRunResult(
            initialSession,
            session,
            initialStateChecksum,
            initialLogChecksum,
            commandResults.ToArray(),
            boundaries.ToArray(),
            new GoldenTerminalResult
            {
                IsTerminal = session.State.IsTerminal,
                Outcome = session.State.OutcomeId,
                EndReason = session.State.EndReasonId,
            });
    }

    internal static string ProjectFact(IBattleFact fact) => fact switch
    {
        StonePlacedFact placed =>
            $"stone_placed|color={ColorId(placed.Stone.Color)}|" +
            $"point={PointId(placed.Stone.Point)}|king={Flag(placed.Stone.IsKing)}",
        GroupCapturedFact captured =>
            $"group_captured|by={ColorId(captured.CapturingColor)}|" +
            $"anchor={PointId(captured.CapturedGroup.Anchor)}|" +
            $"stones={string.Join(';', captured.CapturedGroup.Stones.Select(stone => PointId(stone.Point)))}|" +
            $"king={Flag(captured.ContainsKing)}",
        FacilityDestroyedFact destroyed =>
            $"facility_destroyed|id={destroyed.Facility.InstanceId}|reason={destroyed.ReasonId}",
        FacilityBuiltFact built =>
            $"facility_built|id={built.Facility.InstanceId}|content={built.Facility.ContentId}|" +
            $"owner={ColorId(built.Facility.Owner)}|point={PointId(built.Facility.Point)}|" +
            $"sequence={built.Facility.BuildSequence.ToString(CultureInfo.InvariantCulture)}",
        FacilityActivatedFact activated =>
            $"facility_activated|id={activated.Facility.InstanceId}|reason={activated.ReasonId}|" +
            $"region={PointId(activated.Region.Anchor)}",
        FacilityDisabledFact disabled =>
            $"facility_disabled|id={disabled.Facility.InstanceId}|reason={disabled.ReasonId}|" +
            $"region={PointId(disabled.Region.Anchor)}",
        StoneTopologyRegisteredFact topology =>
            $"topology_registered|cells={topology.RegisteredTopologyKey.CanonicalCells}|" +
            $"observations={topology.HistoryAfterRegistration.ObservationCount.ToString(CultureInfo.InvariantCulture)}",
        KingCaptureEvaluatedFact king =>
            $"king_capture_evaluated|outcome={king.Result.OutcomeId}|reason={king.Result.EndReasonId}|" +
            $"black={Flag(king.Result.BlackKingCaptured)}|white={Flag(king.Result.WhiteKingCaptured)}",
        TerritoryEstablishedFact territory =>
            $"territory_established|actor={ColorId(territory.SourceActor)}|" +
            $"points={string.Join(';', territory.ChangedPoints.Select(PointId))}",
        EnemyPassedFact passed =>
            $"enemy_passed|turn={passed.PlayerTurnIndex.ToString(CultureInfo.InvariantCulture)}",
        BattleEndedFact ended =>
            $"battle_ended|outcome={OutcomeId(ended.Outcome)}|reason={ended.ReasonId}",
        CommandRejectedFact rejected =>
            $"command_rejected|reason={rejected.ReasonId}",
        _ => throw new InvalidOperationException(
            $"Unhandled golden battle fact type {fact.GetType().FullName}."),
    };

    private static IBattleCommand CreateCommand(
        GoldenBoardStep step,
        BoardGeometry geometry) => step.Type switch
    {
        "battle.authorized_stone_placement" => new AuthorizedStonePlacementCommand(
            step.BeforeStateChecksum,
            step.BeforeLogChecksum,
            ParseColor(Required(step.Actor, step.Type, "actor")),
            Point(geometry, Required(step.Point, step.Type, "point")),
            ParseAccessMode(Required(step.AccessMode, step.Type, "access_mode"))),
        "battle.authorized_facility_build" => new AuthorizedFacilityBuildCommand(
            step.BeforeStateChecksum,
            step.BeforeLogChecksum,
            Point(geometry, Required(step.Point, step.Type, "point")),
            Required(step.FacilityContentId, step.Type, "facility_content_id"),
            Required(step.InstanceId, step.Type, "instance_id")),
        "battle.end_player_turn" => new EndPlayerTurnCommand(
            step.BeforeStateChecksum,
            step.BeforeLogChecksum),
        "battle.resolve_enemy_pass" => new ResolveEnemyPassCommand(
            step.BeforeStateChecksum,
            step.BeforeLogChecksum),
        _ => throw new InvalidDataException($"Unknown golden command type '{step.Type}'."),
    };

    private static CanonicalPoint RunSilentCandidateFilter(
        string fixtureId,
        HeadlessBattleSession session,
        GoldenBoardStep step,
        BoardGeometry geometry)
    {
        if (step.Candidates is null || step.Candidates.Count == 0 ||
            step.ExpectedChosenPoint is null)
        {
            throw new InvalidDataException(
                $"{fixtureId} silent filter requires candidates and an expected chosen point.");
        }

        CanonicalPoint? chosen = null;
        foreach (var candidateInput in step.Candidates)
        {
            var point = Point(geometry, candidateInput.Point);
            var proposedStone = new BoardStone(StoneColor.White, false, point);
            if (!HypotheticalPlacementResolver.TryCreate(
                    session.State.Board,
                    proposedStone,
                    out var hypothetical) ||
                hypothetical is null)
            {
                throw new InvalidDataException(
                    $"{fixtureId} silent-filter candidate {PointId(point)} is occupied.");
            }

            var resolved = HypotheticalPlacementResolver.ResolveCaptures(
                hypothetical,
                RealLiberties(hypothetical.GroupsAfterPlacement));
            var evaluation = PlacementLegalityEvaluator.Evaluate(
                resolved,
                RealLiberties(resolved.GroupsAfterCapture),
                session.State.RepetitionHistory,
                PlacementAccessMode.Normal);
            if (evaluation.IsLegal != candidateInput.ExpectedLegal ||
                !string.Equals(
                    evaluation.ReasonId,
                    candidateInput.ExpectedReason,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"{fixtureId} silent-filter result drifted for {PointId(point)}.");
            }

            if (evaluation.IsLegal)
            {
                chosen = point;
                break;
            }
        }

        if (chosen is null ||
            !chosen.Equals(Point(geometry, step.ExpectedChosenPoint)))
        {
            throw new InvalidDataException($"{fixtureId} silent-filter chosen point drifted.");
        }

        return chosen;
    }

    private static IEnumerable<BoardStone> ParseBoard(
        BoardGeometry geometry,
        IReadOnlyList<string> rows)
    {
        if (rows.Count != geometry.Size)
        {
            throw new InvalidDataException("Golden board must contain exactly seven rows.");
        }

        for (var diagramRow = 0; diagramRow < rows.Count; diagramRow++)
        {
            var row = rows[diagramRow];
            if (row.Length != geometry.Size)
            {
                throw new InvalidDataException("Golden board rows must contain exactly seven cells.");
            }

            var y = geometry.CanonicalYFromDiagramRow(diagramRow);
            for (var column = 0; column < row.Length; column++)
            {
                var parsed = row[column] switch
                {
                    '.' => ((StoneColor Color, bool King)?)null,
                    'B' => (StoneColor.Black, false),
                    'W' => (StoneColor.White, false),
                    'K' => (StoneColor.Black, true),
                    'Q' => (StoneColor.White, true),
                    _ => throw new InvalidDataException(
                        $"Unknown golden board symbol '{row[column]}'."),
                };
                if (parsed is not null)
                {
                    yield return new BoardStone(
                        parsed.Value.Color,
                        parsed.Value.King,
                        geometry.CreateCanonicalPoint(column + 1, y));
                }
            }
        }
    }

    private static EffectiveLibertySnapshot RealLiberties(StoneGroupAnalysis analysis) =>
        EffectiveLibertySnapshot.Create(
            analysis,
            analysis.Groups.Select(group => new GroupEffectiveLiberty(
                group,
                group.RealLibertyCount)));

    private static string RepositoryFilePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var root = FindRepositoryRoot();
        var fullPath = Path.GetFullPath(Path.Combine(root.FullName, relativePath));
        var relative = Path.GetRelativePath(root.FullName, fullPath);
        if (Path.IsPathRooted(relative) ||
            relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Golden source path escapes the repository: '{relativePath}'.");
        }

        return fullPath;
    }

    private static CanonicalPoint Point(BoardGeometry geometry, IReadOnlyList<int> coordinates)
    {
        if (coordinates.Count != 2)
        {
            throw new InvalidDataException("Golden point must contain exactly two coordinates.");
        }

        return geometry.CreateCanonicalPoint(coordinates[0], coordinates[1]);
    }

    private static StoneColor ParseColor(string value) => value switch
    {
        "black" => StoneColor.Black,
        "white" => StoneColor.White,
        _ => throw new InvalidDataException($"Unknown golden stone color '{value}'."),
    };

    private static PlacementAccessMode ParseAccessMode(string value) => value switch
    {
        "normal" => PlacementAccessMode.Normal,
        "terminal_capture" => PlacementAccessMode.TerminalCapture,
        _ => throw new InvalidDataException($"Unknown golden access mode '{value}'."),
    };

    private static T Required<T>(T? value, string type, string fieldName)
        where T : class => value ?? throw new InvalidDataException(
            $"Golden step '{type}' requires '{fieldName}'.");

    private static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Unknown golden stone color."),
    };

    private static string OutcomeId(BattleOutcome outcome) => outcome switch
    {
        BattleOutcome.Ongoing => "ongoing",
        BattleOutcome.PlayerVictory => "win",
        BattleOutcome.PlayerDefeat => "loss",
        _ => throw new InvalidOperationException("Unknown golden battle outcome."),
    };

    private static string PointId(CanonicalPoint point) =>
        $"{point.X.ToString(CultureInfo.InvariantCulture)},{point.Y.ToString(CultureInfo.InvariantCulture)}";

    private static int Flag(bool value) => value ? 1 : 0;
}

internal sealed record GoldenRunResult(
    HeadlessBattleSession InitialSession,
    HeadlessBattleSession FinalSession,
    string InitialStateChecksum,
    string InitialLogChecksum,
    IReadOnlyList<BattleCommandResult> CommandResults,
    IReadOnlyList<GoldenActualBoundary> Boundaries,
    GoldenTerminalResult Terminal);

internal sealed record GoldenActualBoundary(
    bool? Accepted,
    string Reason,
    string StateChecksum,
    string LogChecksum,
    IReadOnlyList<string> OrderedFacts,
    bool ExactNoOp,
    int LogCountBefore,
    int LogCountAfter);

internal sealed record GoldenBoardCatalog
{
    [JsonPropertyName("schema_id")]
    public required string SchemaId { get; init; }

    [JsonPropertyName("schema_version")]
    public required int SchemaVersion { get; init; }

    [JsonPropertyName("fact_projection")]
    public required string FactProjection { get; init; }

    [JsonPropertyName("game_version")]
    public required string GameVersion { get; init; }

    [JsonPropertyName("content_hash")]
    public required string ContentHash { get; init; }

    [JsonPropertyName("runtime_policy")]
    public required GoldenRuntimePolicy RuntimePolicy { get; init; }

    [JsonPropertyName("source_catalogs")]
    public required IReadOnlyList<GoldenSourceCatalog> SourceCatalogs { get; init; }

    [JsonPropertyName("cases")]
    public required IReadOnlyList<GoldenBoardCase> Cases { get; init; }
}

internal sealed record GoldenRuntimePolicy
{
    [JsonPropertyName("player_turn_limit")]
    public required int PlayerTurnLimit { get; init; }

    [JsonPropertyName("territory_income_divisor")]
    public required int TerritoryIncomeDivisor { get; init; }

    [JsonPropertyName("capacity_bands")]
    public required IReadOnlyList<GoldenCapacityBand> CapacityBands { get; init; }

    [JsonPropertyName("slot_cap")]
    public required int SlotCap { get; init; }

    [JsonPropertyName("type_limits")]
    public required IReadOnlyDictionary<string, int> TypeLimits { get; init; }
}

internal sealed record GoldenCapacityBand
{
    [JsonPropertyName("min")]
    public required int Min { get; init; }

    [JsonPropertyName("max")]
    public required int Max { get; init; }

    [JsonPropertyName("slots")]
    public required int Slots { get; init; }
}

internal sealed record GoldenSourceCatalog
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }
}

internal sealed record GoldenBoardCase
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("source")]
    public required GoldenCaseSource Source { get; init; }

    [JsonPropertyName("evidence")]
    public required GoldenEvidence Evidence { get; init; }

    [JsonPropertyName("source_normalization")]
    public GoldenSourceNormalization? SourceNormalization { get; init; }

    [JsonPropertyName("seed")]
    public required long Seed { get; init; }

    [JsonPropertyName("initial")]
    public required GoldenInitialState Initial { get; init; }

    [JsonPropertyName("steps")]
    public required IReadOnlyList<GoldenBoardStep> Steps { get; init; }

    [JsonPropertyName("terminal")]
    public required GoldenTerminalResult Terminal { get; init; }
}

internal sealed record GoldenCaseSource
{
    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("catalog")]
    public string? Catalog { get; init; }

    [JsonPropertyName("fixture_id")]
    public string? FixtureId { get; init; }

    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("test_case")]
    public string? TestCase { get; init; }
}

internal sealed record GoldenEvidence
{
    [JsonPropertyName("domain")]
    public required string Domain { get; init; }

    [JsonPropertyName("application")]
    public required string Application { get; init; }

    [JsonPropertyName("relation")]
    public required string Relation { get; init; }

    [JsonPropertyName("limitations")]
    public required IReadOnlyList<string> Limitations { get; init; }
}

internal sealed record GoldenSourceNormalization
{
    [JsonPropertyName("source_stone_kind")]
    public required string SourceStoneKind { get; init; }

    [JsonPropertyName("source_non_topology_metadata")]
    public required IReadOnlyDictionary<string, JsonElement> SourceNonTopologyMetadata { get; init; }

    [JsonPropertyName("application_projection")]
    public required string ApplicationProjection { get; init; }

    [JsonPropertyName("omitted_fields")]
    public required IReadOnlyList<string> OmittedFields { get; init; }
}

internal sealed record GoldenInitialState
{
    [JsonPropertyName("board")]
    public required IReadOnlyList<string> Board { get; init; }

    [JsonPropertyName("facilities")]
    public required IReadOnlyList<GoldenInitialFacility> Facilities { get; init; }

    [JsonPropertyName("next_build_sequence")]
    public required long NextBuildSequence { get; init; }

    [JsonPropertyName("state_checksum")]
    public required string StateChecksum { get; init; }

    [JsonPropertyName("log_checksum")]
    public required string LogChecksum { get; init; }
}

internal sealed record GoldenInitialFacility
{
    [JsonPropertyName("instance_id")]
    public required string InstanceId { get; init; }

    [JsonPropertyName("content_id")]
    public required string ContentId { get; init; }

    [JsonPropertyName("owner")]
    public required string Owner { get; init; }

    [JsonPropertyName("point")]
    public required IReadOnlyList<int> Point { get; init; }

    [JsonPropertyName("build_sequence")]
    public required long BuildSequence { get; init; }

    [JsonPropertyName("explicit_disable_sources")]
    public required IReadOnlyList<string> ExplicitDisableSources { get; init; }
}

internal sealed record GoldenBoardStep
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("actor")]
    public string? Actor { get; init; }

    [JsonPropertyName("point")]
    public IReadOnlyList<int>? Point { get; init; }

    [JsonPropertyName("access_mode")]
    public string? AccessMode { get; init; }

    [JsonPropertyName("facility_content_id")]
    public string? FacilityContentId { get; init; }

    [JsonPropertyName("instance_id")]
    public string? InstanceId { get; init; }

    [JsonPropertyName("candidates")]
    public IReadOnlyList<GoldenCandidate>? Candidates { get; init; }

    [JsonPropertyName("expected_chosen_point")]
    public IReadOnlyList<int>? ExpectedChosenPoint { get; init; }

    [JsonPropertyName("before_state_checksum")]
    public required string BeforeStateChecksum { get; init; }

    [JsonPropertyName("before_log_checksum")]
    public required string BeforeLogChecksum { get; init; }

    [JsonPropertyName("expected")]
    public required GoldenExpectedBoundary Expected { get; init; }
}

internal sealed record GoldenCandidate
{
    [JsonPropertyName("point")]
    public required IReadOnlyList<int> Point { get; init; }

    [JsonPropertyName("expected_legal")]
    public required bool ExpectedLegal { get; init; }

    [JsonPropertyName("expected_reason")]
    public required string ExpectedReason { get; init; }
}

internal sealed record GoldenExpectedBoundary
{
    [JsonPropertyName("accepted")]
    public bool? Accepted { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    [JsonPropertyName("state_checksum")]
    public required string StateChecksum { get; init; }

    [JsonPropertyName("log_checksum")]
    public required string LogChecksum { get; init; }

    [JsonPropertyName("ordered_facts")]
    public required IReadOnlyList<string> OrderedFacts { get; init; }
}

internal sealed record GoldenTerminalResult
{
    [JsonPropertyName("is_terminal")]
    public required bool IsTerminal { get; init; }

    [JsonPropertyName("outcome")]
    public required string Outcome { get; init; }

    [JsonPropertyName("end_reason")]
    public required string EndReason { get; init; }
}
