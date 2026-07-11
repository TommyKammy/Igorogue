using System.Text.Json;
using Igorogue.Domain.Board;

namespace Igorogue.Domain.Tests;

public sealed class TerritoryFacilityFixtureTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);
    private static readonly Lazy<IReadOnlyDictionary<string, FacilityFixture>> Fixtures =
        new(LoadFixtures);

    [Fact]
    public void CanonicalFacilityFixtureInventoryIsPresent()
    {
        Assert.Equal(
            Enumerable.Range(1, 9).Select(index => $"FAC-{index:00}"),
            Fixtures.Value.Keys.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("FAC-01")]
    [InlineData("FAC-02")]
    [InlineData("FAC-06")]
    [InlineData("FAC-07")]
    [InlineData("FAC-09")]
    public void AcceptedFacilityFixturesUseStoneOnlyTerritoryProjection(string fixtureId)
    {
        var fixture = RequiredFixture(fixtureId);
        var queryPoint = Assert.IsType<CanonicalPoint>(fixture.TerritoryPoint);
        var expected = Assert.IsType<ExpectedTerritory>(fixture.ExpectedTerritory);

        Assert.All(fixture.FacilityPoints, point => Assert.True(fixture.Board.IsEmpty(point)));
        if (fixture.ActionPoint is not null)
        {
            Assert.True(fixture.Board.IsEmpty(fixture.ActionPoint));
        }

        var region = TerritoryAnalyzer.Analyze(fixture.Board).RegionAt(queryPoint);

        Assert.NotNull(region);
        Assert.Equal(expected.Owner, region.Owner);
        Assert.Equal(expected.Size, region.Size);
        Assert.Contains(queryPoint, region.Points);
    }

    [Fact]
    public void Fac01FacilityPointIsBothBlackTerritoryAndRealLiberty()
    {
        var fixture = RequiredFixture("FAC-01");
        var facilityPoint = Assert.Single(fixture.FacilityPoints);
        var groupPoint = Assert.IsType<CanonicalPoint>(fixture.GroupPoint);
        var group = StoneGroupAnalyzer.Analyze(fixture.Board).GroupAt(groupPoint);
        var region = TerritoryAnalyzer.Analyze(fixture.Board).RegionAt(facilityPoint);

        Assert.NotNull(group);
        Assert.Equal(fixture.ExpectedGroupLiberties, group.RealLiberties);
        Assert.Contains(facilityPoint, group.RealLiberties);
        Assert.NotNull(region);
        Assert.Equal(TerritoryOwner.Black, region.Owner);
        Assert.Equal(1, region.Size);
    }

    [Fact]
    public void Fac05NeutralizationAndRestorationRecalculateFromEachStoneBoard()
    {
        var fixture = RequiredFixture("FAC-05");
        var facilityPoint = Assert.Single(fixture.FacilityPoints);
        var boards = new[] { fixture.Board }.Concat(fixture.NextBoards).ToArray();
        var expectedOwners = new[]
        {
            TerritoryOwner.Black,
            TerritoryOwner.Neutral,
            TerritoryOwner.Black,
        };

        Assert.Equal(expectedOwners.Length, boards.Length);
        for (var index = 0; index < boards.Length; index++)
        {
            Assert.True(boards[index].IsEmpty(facilityPoint));
            var region = TerritoryAnalyzer.Analyze(boards[index]).RegionAt(facilityPoint);

            Assert.NotNull(region);
            Assert.Equal(expectedOwners[index], region.Owner);
            Assert.Equal(1, region.Size);
        }
    }

    [Fact]
    public void Fac07SplitRegionHasAcceptedCanonicalPoints()
    {
        var fixture = RequiredFixture("FAC-07");
        var queryPoint = Assert.IsType<CanonicalPoint>(fixture.TerritoryPoint);

        var region = TerritoryAnalyzer.Analyze(fixture.Board).RegionAt(queryPoint);

        Assert.NotNull(region);
        Assert.Equal(TerritoryOwner.Black, region.Owner);
        Assert.Equal(C(4, 1), region.Anchor);
        Assert.Equal(new[] { C(4, 1), C(4, 2), C(4, 3) }, region.Points);
        Assert.All(fixture.FacilityPoints, point => Assert.Contains(point, region.Points));
    }

    [Fact]
    public void Fac02AndFac09FacilityPresenceDifferenceHasIdenticalStoneDerivedResults()
    {
        var withFacility = RequiredFixture("FAC-02");
        var withoutFacility = RequiredFixture("FAC-09");
        var facilityPoint = Assert.Single(withFacility.FacilityPoints);
        var buildPoint = Assert.IsType<CanonicalPoint>(withoutFacility.ActionPoint);

        Assert.Empty(withoutFacility.FacilityPoints);
        Assert.Equal(facilityPoint, buildPoint);
        Assert.NotSame(withFacility.Board, withoutFacility.Board);
        Assert.True(withFacility.Board.IsEmpty(facilityPoint));
        Assert.True(withoutFacility.Board.IsEmpty(facilityPoint));
        Assert.Equal(
            StoneTopologyKey.FromBoard(withFacility.Board),
            StoneTopologyKey.FromBoard(withoutFacility.Board));

        var territoryWithFacility = TerritoryAnalyzer.Analyze(withFacility.Board);
        var territoryWithoutFacility = TerritoryAnalyzer.Analyze(withoutFacility.Board);
        var groupsWithFacility = StoneGroupAnalyzer.Analyze(withFacility.Board);
        var groupsWithoutFacility = StoneGroupAnalyzer.Analyze(withoutFacility.Board);

        Assert.Equal(
            TerritoryProjection(territoryWithFacility),
            TerritoryProjection(territoryWithoutFacility));
        Assert.Equal(
            GroupProjection(groupsWithFacility),
            GroupProjection(groupsWithoutFacility));
        Assert.Equal(TerritoryOwner.Black, territoryWithFacility.RegionAt(facilityPoint)?.Owner);
        Assert.Equal(3, territoryWithFacility.RegionAt(facilityPoint)?.Size);
    }

    private static string[] TerritoryProjection(TerritoryAnalysis analysis) =>
        analysis.Regions
            .Select(region =>
                $"{region.Owner}:{region.Anchor}:{string.Join(";", region.Points)}")
            .ToArray();

    private static string[] GroupProjection(StoneGroupAnalysis analysis) =>
        analysis.Groups
            .Select(group =>
                $"{group.Color}:{group.Anchor}:{string.Join(";", group.RealLiberties)}")
            .ToArray();

    private static FacilityFixture RequiredFixture(string fixtureId) =>
        Fixtures.Value.TryGetValue(fixtureId, out var fixture)
            ? fixture
            : throw new InvalidOperationException($"Missing facility fixture {fixtureId}.");

    private static IReadOnlyDictionary<string, FacilityFixture> LoadFixtures()
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

    private static FacilityFixture ParseFixture(JsonElement element)
    {
        var id = element.GetProperty("id").GetString()
            ?? throw new InvalidDataException("Facility fixture id cannot be null.");
        var board = ParseBoard(element.GetProperty("board"), $"{id}/board");
        var facilityPoints = element.GetProperty("facilities")
            .EnumerateArray()
            .Select(facility => ParsePoint(facility.GetProperty("point"), $"{id}/facilities"))
            .OrderBy(Geometry.ToCanonicalIndex)
            .ToArray();
        var queries = element.TryGetProperty("queries", out var queryElement)
            ? queryElement
            : default;
        var groupPoint = queries.ValueKind == JsonValueKind.Object &&
            queries.TryGetProperty("group_point", out var groupPointElement)
                ? ParsePoint(groupPointElement, $"{id}/queries/group_point")
                : null;
        var territoryPoint = queries.ValueKind == JsonValueKind.Object &&
            queries.TryGetProperty("territory_point", out var territoryPointElement)
                ? ParsePoint(territoryPointElement, $"{id}/queries/territory_point")
                : element.TryGetProperty("point", out var actionTerritoryPoint)
                    ? ParsePoint(actionTerritoryPoint, $"{id}/point")
                    : null;
        var actionPoint = element.TryGetProperty("point", out var actionPointElement)
            ? ParsePoint(actionPointElement, $"{id}/point")
            : null;
        var expected = element.GetProperty("expected");
        var expectedTerritory = expected.TryGetProperty("territory", out var expectedTerritoryElement)
            ? new ExpectedTerritory(
                ParseOwner(expectedTerritoryElement.GetProperty("owner").GetString()),
                expectedTerritoryElement.GetProperty("size").GetInt32())
            : null;
        var expectedGroupLiberties = expected.TryGetProperty(
                "group_liberties",
                out var groupLibertyElement)
            ? groupLibertyElement
                .EnumerateArray()
                .Select(point => ParsePoint(point, $"{id}/expected/group_liberties"))
                .OrderBy(Geometry.ToCanonicalIndex)
                .ToArray()
            : [];
        var nextBoards = element.TryGetProperty("next_boards", out var nextBoardElement)
            ? nextBoardElement
                .EnumerateArray()
                .Select((rows, index) => ParseBoard(rows, $"{id}/next_boards[{index}]"))
                .ToArray()
            : [];

        return new FacilityFixture(
            id,
            board,
            facilityPoints,
            groupPoint,
            territoryPoint,
            actionPoint,
            expectedTerritory,
            expectedGroupLiberties,
            nextBoards);
    }

    private static BoardState ParseBoard(JsonElement rowsElement, string label)
    {
        var rows = rowsElement.EnumerateArray()
            .Select(row => row.GetString()
                ?? throw new InvalidDataException($"{label} row cannot be null."))
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
                    _ => throw new InvalidDataException($"{label} contains unknown symbol {symbol}."),
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

    private static TerritoryOwner ParseOwner(string? owner) => owner switch
    {
        "black" => TerritoryOwner.Black,
        "white" => TerritoryOwner.White,
        "neutral" => TerritoryOwner.Neutral,
        _ => throw new InvalidDataException($"Unknown territory owner {owner ?? "<null>"}."),
    };

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

    private static CanonicalPoint C(int x, int y) => Geometry.CreateCanonicalPoint(x, y);

    private sealed record ExpectedTerritory(TerritoryOwner Owner, int Size);

    private sealed record FacilityFixture(
        string Id,
        BoardState Board,
        IReadOnlyList<CanonicalPoint> FacilityPoints,
        CanonicalPoint? GroupPoint,
        CanonicalPoint? TerritoryPoint,
        CanonicalPoint? ActionPoint,
        ExpectedTerritory? ExpectedTerritory,
        IReadOnlyList<CanonicalPoint> ExpectedGroupLiberties,
        IReadOnlyList<BoardState> NextBoards);
}
