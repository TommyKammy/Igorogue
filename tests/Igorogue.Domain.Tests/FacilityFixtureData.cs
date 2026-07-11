using System.Text.Json;
using Igorogue.Domain.Board;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Tests;

internal static class FacilityFixtureData
{
    internal static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    internal static IReadOnlyDictionary<string, FacilityFixture> LoadFixtures()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(
            root.FullName,
            "game_data/fixtures/facility_intersection_fixtures.json");
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);

        return document.RootElement
            .EnumerateArray()
            .Select(ParseFixture)
            .ToDictionary(fixture => fixture.Id, StringComparer.Ordinal);
    }

    internal static FacilityRuntimePolicy LoadRuntimePolicy()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root.FullName, "game_data/balance/system.json");
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var system = document.RootElement;
        var typeLimits = system.GetProperty("facility_type_limits_per_region");

        return FacilityRuntimePolicy.Create(
            system.GetProperty("territory_income_divisor").GetInt32(),
            system.GetProperty("facility_capacity")
                .EnumerateArray()
                .Select(element => new FacilityCapacityBand(
                    element.GetProperty("min").GetInt32(),
                    element.GetProperty("max").GetInt32(),
                    element.GetProperty("slots").GetInt32())),
            system.GetProperty("facility_slot_cap").GetInt32(),
            typeLimits
                .EnumerateObject()
                .Select(property => new KeyValuePair<string, int>(
                    property.Name,
                    property.Value.GetInt32())));
    }

    internal static FacilityState CreateInitialState(FacilityFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        return FacilityState.Create(
            fixture.Board,
            fixture.Facilities.Select(facility => facility.ToDomain()),
            fixture.InitialNextBuildSequence);
    }

    internal static CanonicalPoint C(int x, int y) =>
        Geometry.CreateCanonicalPoint(x, y);

    private static FacilityFixture ParseFixture(JsonElement element)
    {
        var id = RequiredString(element.GetProperty("id"), "facility fixture id");
        var facilities = element.GetProperty("facilities")
            .EnumerateArray()
            .Select(facility => ParseFacility(facility, id))
            .ToArray();
        var queries = element.TryGetProperty("queries", out var queryElement)
            ? new FacilityFixtureQueries(
                queryElement.TryGetProperty("group_point", out var groupPoint)
                    ? ParsePoint(groupPoint, $"{id}/queries/group_point")
                    : null,
                queryElement.TryGetProperty("territory_point", out var territoryPoint)
                    ? ParsePoint(territoryPoint, $"{id}/queries/territory_point")
                    : null)
            : new FacilityFixtureQueries(null, null);
        StoneColor? actor = element.TryGetProperty("actor", out var actorElement)
            ? ParseStoneColor(RequiredString(actorElement, $"{id}/actor"))
            : null;
        var point = element.TryGetProperty("point", out var pointElement)
            ? ParsePoint(pointElement, $"{id}/point")
            : null;
        var nextBoards = element.TryGetProperty("next_boards", out var nextBoardElement)
            ? nextBoardElement
                .EnumerateArray()
                .Select((board, index) => ParseBoard(board, $"{id}/next_boards[{index}]"))
                .ToArray()
            : [];
        var initialNextBuildSequence = facilities.Length == 0
            ? 1
            : checked(facilities.Max(facility => facility.BuildSequence) + 1L);

        return new FacilityFixture(
            id,
            RequiredString(element.GetProperty("title"), $"{id}/title"),
            RequiredString(element.GetProperty("operation"), $"{id}/operation"),
            ParseBoard(element.GetProperty("board"), $"{id}/board"),
            facilities,
            queries,
            actor,
            point,
            element.TryGetProperty("facility_id", out var facilityId)
                ? RequiredString(facilityId, $"{id}/facility_id")
                : null,
            nextBoards,
            ParseExpected(element.GetProperty("expected"), id),
            initialNextBuildSequence);
    }

    private static FacilityFixtureInstance ParseFacility(JsonElement element, string fixtureId)
    {
        var instanceId = RequiredString(
            element.GetProperty("instance_id"),
            $"{fixtureId}/facilities/instance_id");
        var explicitDisableSources = element.TryGetProperty(
                "explicit_disable_sources",
                out var sourcesElement)
            ? sourcesElement
                .EnumerateArray()
                .Select(source => RequiredString(
                    source,
                    $"{fixtureId}/facilities/{instanceId}/explicit_disable_sources"))
                .ToArray()
            : [];

        return new FacilityFixtureInstance(
            instanceId,
            RequiredString(
                element.GetProperty("facility_id"),
                $"{fixtureId}/facilities/{instanceId}/facility_id"),
            ParseStoneColor(RequiredString(
                element.GetProperty("owner"),
                $"{fixtureId}/facilities/{instanceId}/owner")),
            ParsePoint(
                element.GetProperty("point"),
                $"{fixtureId}/facilities/{instanceId}/point"),
            element.GetProperty("build_sequence").GetInt64(),
            explicitDisableSources);
    }

    private static FacilityFixtureExpected ParseExpected(
        JsonElement element,
        string fixtureId)
    {
        var events = OptionalStrings(element, "events", $"{fixtureId}/expected/events");
        var remainingFacilities = OptionalStrings(
            element,
            "remaining_facilities",
            $"{fixtureId}/expected/remaining_facilities");
        var destroyedFacilities = OptionalStrings(
            element,
            "destroyed_facilities",
            $"{fixtureId}/expected/destroyed_facilities");
        var groupLiberties = element.TryGetProperty(
                "group_liberties",
                out var libertyElement)
            ? libertyElement
                .EnumerateArray()
                .Select(point => ParsePoint(
                    point,
                    $"{fixtureId}/expected/group_liberties"))
                .OrderBy(Geometry.ToCanonicalIndex)
                .ToArray()
            : [];
        var facilityStates = element.TryGetProperty(
                "facility_states",
                out var stateElement)
            ? stateElement
                .EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => RequiredString(
                        property.Value,
                        $"{fixtureId}/expected/facility_states/{property.Name}"),
                    StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        var facilityOwners = element.TryGetProperty(
                "facility_owners",
                out var ownerElement)
            ? ownerElement
                .EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => ParseStoneColor(RequiredString(
                        property.Value,
                        $"{fixtureId}/expected/facility_owners/{property.Name}")),
                    StringComparer.Ordinal)
            : new Dictionary<string, StoneColor>(StringComparer.Ordinal);

        return new FacilityFixtureExpected(
            element.TryGetProperty("legal", out var legal) ? legal.GetBoolean() : null,
            element.TryGetProperty("reason", out var reason)
                ? RequiredString(reason, $"{fixtureId}/expected/reason")
                : null,
            events,
            remainingFacilities,
            destroyedFacilities,
            element.TryGetProperty("result_board", out var resultBoard)
                ? ParseBoard(resultBoard, $"{fixtureId}/expected/result_board")
                : null,
            element.TryGetProperty("territory", out var territory)
                ? ParseExpectedTerritory(territory, fixtureId)
                : null,
            facilityStates,
            facilityOwners,
            element.TryGetProperty("owner", out var expectedOwner)
                ? ParseStoneColor(RequiredString(
                    expectedOwner,
                    $"{fixtureId}/expected/owner"))
                : null,
            element.TryGetProperty("build_sequence", out var buildSequence)
                ? buildSequence.GetInt64()
                : null,
            element.TryGetProperty("topology_unchanged", out var topologyUnchanged)
                ? topologyUnchanged.GetBoolean()
                : null,
            groupLiberties);
    }

    private static FacilityFixtureTerritory ParseExpectedTerritory(
        JsonElement element,
        string fixtureId) =>
        new(
            ParseTerritoryOwner(RequiredString(
                element.GetProperty("owner"),
                $"{fixtureId}/expected/territory/owner")),
            element.GetProperty("size").GetInt32(),
            element.GetProperty("basic_income").GetInt32(),
            element.GetProperty("construction_capacity").GetInt32(),
            element.TryGetProperty("installed_count", out var installedCount)
                ? installedCount.GetInt32()
                : null,
            element.TryGetProperty("is_over_capacity", out var isOverCapacity)
                ? isOverCapacity.GetBoolean()
                : null);

    private static IReadOnlyList<string> OptionalStrings(
        JsonElement element,
        string propertyName,
        string label) =>
        element.TryGetProperty(propertyName, out var values)
            ? values
                .EnumerateArray()
                .Select(value => RequiredString(value, label))
                .ToArray()
            : [];

    private static BoardState ParseBoard(JsonElement rowsElement, string label)
    {
        var rows = rowsElement.EnumerateArray()
            .Select(row => RequiredString(row, $"{label} row"))
            .ToArray();
        if (rows.Length != Geometry.Size || rows.Any(row => row.Length != Geometry.Size))
        {
            throw new InvalidDataException($"{label} must be a 7x7 diagram.");
        }

        var stones = new List<BoardStone>();
        for (var row = 0; row < rows.Length; row++)
        {
            var y = Geometry.CanonicalYFromDiagramRow(row);
            for (var column = 0; column < rows[row].Length; column++)
            {
                var symbol = rows[row][column];
                if (symbol == '.')
                {
                    continue;
                }

                var (color, isKing) = symbol switch
                {
                    'B' => (StoneColor.Black, false),
                    'K' => (StoneColor.Black, true),
                    'W' => (StoneColor.White, false),
                    'Q' => (StoneColor.White, true),
                    _ => throw new InvalidDataException(
                        $"{label} contains unknown symbol {symbol}."),
                };
                stones.Add(new BoardStone(color, isKing, C(column + 1, y)));
            }
        }

        return BoardState.Create(Geometry, stones);
    }

    private static CanonicalPoint ParsePoint(JsonElement element, string label)
    {
        var coordinates = element.EnumerateArray().Select(value => value.GetInt32()).ToArray();
        if (coordinates.Length != 2)
        {
            throw new InvalidDataException($"{label} must contain [x,y].");
        }

        return C(coordinates[0], coordinates[1]);
    }

    private static StoneColor ParseStoneColor(string value) => value switch
    {
        "black" => StoneColor.Black,
        "white" => StoneColor.White,
        _ => throw new InvalidDataException($"Unknown fixture stone color '{value}'."),
    };

    private static TerritoryOwner ParseTerritoryOwner(string value) => value switch
    {
        "black" => TerritoryOwner.Black,
        "white" => TerritoryOwner.White,
        "neutral" => TerritoryOwner.Neutral,
        _ => throw new InvalidDataException($"Unknown fixture territory owner '{value}'."),
    };

    private static string RequiredString(JsonElement element, string label) =>
        element.GetString()
        ?? throw new InvalidDataException($"{label} cannot be null.");

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

        throw new DirectoryNotFoundException(
            "Could not find Igorogue.sln from test output path.");
    }
}

internal sealed record FacilityFixtureInstance(
    string InstanceId,
    string FacilityContentId,
    StoneColor OwnerColor,
    CanonicalPoint Point,
    long BuildSequence,
    IReadOnlyList<string> ExplicitDisableSources)
{
    internal FacilityInstance ToDomain() =>
        new(
            InstanceId,
            FacilityContentId,
            OwnerColor,
            Point,
            BuildSequence,
            ExplicitDisableSources);
}

internal sealed record FacilityFixtureQueries(
    CanonicalPoint? GroupPoint,
    CanonicalPoint? TerritoryPoint);

internal sealed record FacilityFixtureTerritory(
    TerritoryOwner Owner,
    int Size,
    int BasicIncome,
    int ConstructionCapacity,
    int? InstalledCount,
    bool? IsOverCapacity);

internal sealed record FacilityFixtureExpected(
    bool? Legal,
    string? Reason,
    IReadOnlyList<string> Events,
    IReadOnlyList<string> RemainingFacilities,
    IReadOnlyList<string> DestroyedFacilities,
    BoardState? ResultBoard,
    FacilityFixtureTerritory? Territory,
    IReadOnlyDictionary<string, string> FacilityStates,
    IReadOnlyDictionary<string, StoneColor> FacilityOwners,
    StoneColor? Owner,
    long? BuildSequence,
    bool? TopologyUnchanged,
    IReadOnlyList<CanonicalPoint> GroupLiberties);

internal sealed record FacilityFixture(
    string Id,
    string Title,
    string Operation,
    BoardState Board,
    IReadOnlyList<FacilityFixtureInstance> Facilities,
    FacilityFixtureQueries Queries,
    StoneColor? Actor,
    CanonicalPoint? Point,
    string? FacilityContentId,
    IReadOnlyList<BoardState> NextBoards,
    FacilityFixtureExpected Expected,
    long InitialNextBuildSequence)
{
    internal IReadOnlyList<CanonicalPoint> FacilityPoints =>
        Facilities.Select(facility => facility.Point).ToArray();

    internal CanonicalPoint? GroupPoint => Queries.GroupPoint;

    internal CanonicalPoint? TerritoryPoint => Queries.TerritoryPoint ?? Point;

    internal CanonicalPoint? ActionPoint => Point;

    internal FacilityFixtureTerritory? ExpectedTerritory => Expected.Territory;

    internal IReadOnlyList<CanonicalPoint> ExpectedGroupLiberties =>
        Expected.GroupLiberties;
}
