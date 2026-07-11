using System.Text.Json;
using Igorogue.Domain.Board;

namespace Igorogue.Domain.Tests;

public sealed class InitialPositionFixtureTests
{
    private static readonly Lazy<FixtureContext> Fixture = new(LoadStandardFixture);

    [Fact]
    public void Coord10StandardPositionMatchesDataAndRoleAwarePointSymmetry()
    {
        var context = Fixture.Value;
        var position = context.Position;
        var actual = position.Stones
            .Select(stone => (stone.Color, stone.Role, stone.Point.X, stone.Point.Y))
            .ToArray();
        var expected = new[]
        {
            (StoneColor.Black, InitialStoneRole.King, 2, 2),
            (StoneColor.Black, InitialStoneRole.Guard, 3, 2),
            (StoneColor.Black, InitialStoneRole.Guard, 2, 3),
            (StoneColor.White, InitialStoneRole.Guard, 6, 5),
            (StoneColor.White, InitialStoneRole.Guard, 5, 6),
            (StoneColor.White, InitialStoneRole.King, 6, 6),
        };

        Assert.Equal(7, context.Geometry.Size);
        Assert.Equal("standard_v0_2", position.Id);
        Assert.Equal("point_reflection_with_color_and_role_swap", context.SymmetryId);
        Assert.Equal(expected, actual);
        Assert.Equal(6, position.Stones.Count);
        Assert.Equal(1, position.Stones.Count(stone =>
            stone.Color == StoneColor.Black && stone.Role == InitialStoneRole.King));
        Assert.Equal(2, position.Stones.Count(stone =>
            stone.Color == StoneColor.Black && stone.Role == InitialStoneRole.Guard));
        Assert.Equal(1, position.Stones.Count(stone =>
            stone.Color == StoneColor.White && stone.Role == InitialStoneRole.King));
        Assert.Equal(2, position.Stones.Count(stone =>
            stone.Color == StoneColor.White && stone.Role == InitialStoneRole.Guard));
        Assert.False(position.IsOccupied(C(context, 4, 4)));
        Assert.True(position.HasRoleAwarePointReflectionSymmetry());

        foreach (var stone in position.Stones)
        {
            var counterpart = position.StoneAt(context.Geometry.Reflect(stone.Point));
            Assert.NotNull(counterpart);
            Assert.Equal(Opposite(stone.Color), counterpart.Color);
            Assert.Equal(stone.Role, counterpart.Role);
        }
    }

    [Fact]
    public void Coord11EachInitialKingGroupHasThreeConnectedStonesAndSevenUniqueLiberties()
    {
        var context = Fixture.Value;
        var analysis = StoneGroupAnalyzer.Analyze(BoardState.FromInitialPosition(context.Position));

        Assert.Equal(2, analysis.Groups.Count);
        Assert.Equal(
            new[] { C(context, 2, 2), C(context, 6, 5) },
            analysis.Groups.Select(group => group.Anchor).ToArray());

        AssertInitialKingGroup(
            context,
            analysis,
            StoneColor.Black,
            [C(context, 2, 1), C(context, 3, 1), C(context, 1, 2), C(context, 4, 2),
                C(context, 1, 3), C(context, 3, 3), C(context, 2, 4)]);
        AssertInitialKingGroup(
            context,
            analysis,
            StoneColor.White,
            [C(context, 6, 4), C(context, 5, 5), C(context, 7, 5), C(context, 4, 6),
                C(context, 7, 6), C(context, 5, 7), C(context, 6, 7)]);
    }

    [Fact]
    public void Coord12DiagramRowsAndCanonicalPointOrderMatchFixture()
    {
        var context = Fixture.Value;
        var geometry = context.Geometry;

        Assert.Equal(
            new[] { 7, 6, 5, 4, 3, 2, 1 },
            Enumerable.Range(0, geometry.Size)
                .Select(geometry.CanonicalYFromDiagramRow)
                .ToArray());
        Assert.Equal(
            new[] { C(context, 1, 1), C(context, 2, 1), C(context, 3, 1) },
            geometry.CanonicalPoints.Take(3).ToArray());
        Assert.Equal(C(context, 7, 7), geometry.CanonicalPoints[^1]);
        Assert.Equal(49, geometry.CanonicalPoints.Count);
        Assert.Equal(49, geometry.CanonicalPoints.Distinct().Count());
        Assert.Equal(
            new[]
            {
                ".......",
                "....WQ.",
                ".....W.",
                ".......",
                ".B.....",
                ".KB....",
                ".......",
            },
            RenderDiagram(context));
    }

    [Fact]
    public void InitialPositionFactoryCopiesAndCanonicalizesInput()
    {
        var geometry = BoardGeometry.Create(7);
        var input = new List<InitialStonePlacement>
        {
            new(StoneColor.White, InitialStoneRole.King, geometry.CreateCanonicalPoint(6, 6)),
            new(StoneColor.Black, InitialStoneRole.King, geometry.CreateCanonicalPoint(2, 2)),
        };

        var position = InitialPositionDefinition.Create(geometry, "copy_test", input);
        input.Clear();

        Assert.Equal(2, position.Stones.Count);
        Assert.Equal(geometry.CreateCanonicalPoint(2, 2), position.Stones[0].Point);
        Assert.Equal(geometry.CreateCanonicalPoint(6, 6), position.Stones[1].Point);
    }

    [Fact]
    public void InitialPositionFactoryRejectsDuplicatePoints()
    {
        var geometry = BoardGeometry.Create(7);
        var point = geometry.CreateCanonicalPoint(2, 2);
        var duplicate = new[]
        {
            new InitialStonePlacement(StoneColor.Black, InitialStoneRole.King, point),
            new InitialStonePlacement(StoneColor.Black, InitialStoneRole.Guard, point),
        };

        Assert.Throws<ArgumentException>(
            () => InitialPositionDefinition.Create(geometry, "duplicate", duplicate));
    }

    [Fact]
    public void InitialPositionIdentifiersAndEnumsRejectInvalidValues()
    {
        var geometry = BoardGeometry.Create(7);
        var point = geometry.CreateCanonicalPoint(1, 1);

        Assert.Throws<ArgumentException>(
            () => InitialPositionDefinition.Create(geometry, " ", []));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new InitialStonePlacement((StoneColor)99, InitialStoneRole.King, point));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new InitialStonePlacement(StoneColor.Black, (InitialStoneRole)99, point));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void DiagramRowsRejectOutOfRangeValues(int row)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Fixture.Value.Geometry.CanonicalYFromDiagramRow(row));
    }

    private static void AssertInitialKingGroup(
        FixtureContext context,
        StoneGroupAnalysis analysis,
        StoneColor color,
        IReadOnlyList<CanonicalPoint> expectedLiberties)
    {
        var colorStones = context.Position.Stones
            .Where(stone => stone.Color == color)
            .ToArray();
        var king = Assert.Single(colorStones, stone => stone.Role == InitialStoneRole.King);
        var group = analysis.GroupAt(king.Point);

        Assert.NotNull(group);
        Assert.Equal(color, group.Color);
        Assert.Equal(3, group.Stones.Count);
        Assert.Single(group.Stones, stone => stone.IsKing);
        Assert.Contains(king.Point, group.StonePoints);
        Assert.Equal(
            colorStones.Select(stone => stone.Point)
                .OrderBy(context.Geometry.ToCanonicalIndex)
                .ToArray(),
            group.StonePoints);
        Assert.Equal(expectedLiberties, group.RealLiberties);
    }

    private static string[] RenderDiagram(FixtureContext context)
    {
        var symbols = context.Position.Stones.ToDictionary(stone => stone.Point, Symbol);
        var rows = new string[context.Geometry.Size];
        for (var row = 0; row < rows.Length; row++)
        {
            var y = context.Geometry.CanonicalYFromDiagramRow(row);
            var characters = new char[context.Geometry.Size];
            for (var column = 0; column < characters.Length; column++)
            {
                var point = C(context, column + 1, y);
                characters[column] = symbols.GetValueOrDefault(point, '.');
            }

            rows[row] = new string(characters);
        }

        return rows;
    }

    private static char Symbol(InitialStonePlacement stone) => (stone.Color, stone.Role) switch
    {
        (StoneColor.Black, InitialStoneRole.King) => 'K',
        (StoneColor.Black, InitialStoneRole.Guard) => 'B',
        (StoneColor.White, InitialStoneRole.King) => 'Q',
        (StoneColor.White, InitialStoneRole.Guard) => 'W',
        _ => throw new ArgumentOutOfRangeException(nameof(stone), stone, "Unknown initial stone."),
    };

    private static StoneColor Opposite(StoneColor color) => color switch
    {
        StoneColor.Black => StoneColor.White,
        StoneColor.White => StoneColor.Black,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown stone color."),
    };

    private static CanonicalPoint C(FixtureContext context, int x, int y) =>
        context.Geometry.CreateCanonicalPoint(x, y);

    private static FixtureContext LoadStandardFixture()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root.FullName, "game_data/balance/system.json");
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var rootElement = document.RootElement;
        var geometry = BoardGeometry.Create(rootElement.GetProperty("board_size").GetInt32());
        var initial = rootElement.GetProperty("initial_position");
        var id = initial.GetProperty("id").GetString()
            ?? throw new InvalidDataException("initial_position.id cannot be null.");
        var symmetryId = initial.GetProperty("symmetry").GetString()
            ?? throw new InvalidDataException("initial_position.symmetry cannot be null.");
        var stones = initial.GetProperty("stones")
            .EnumerateArray()
            .Select(element => ParseStone(geometry, element))
            .ToArray();

        return new FixtureContext(
            geometry,
            InitialPositionDefinition.Create(geometry, id, stones),
            symmetryId);
    }

    private static InitialStonePlacement ParseStone(BoardGeometry geometry, JsonElement element)
    {
        var point = element.GetProperty("point").EnumerateArray().Select(value => value.GetInt32()).ToArray();
        if (point.Length != 2)
        {
            throw new InvalidDataException("Initial stone point must contain exactly two coordinates.");
        }

        return new InitialStonePlacement(
            ParseColor(element.GetProperty("color").GetString()),
            ParseRole(element.GetProperty("role").GetString()),
            geometry.CreateCanonicalPoint(point[0], point[1]));
    }

    private static StoneColor ParseColor(string? value) => value switch
    {
        "black" => StoneColor.Black,
        "white" => StoneColor.White,
        _ => throw new InvalidDataException($"Unknown initial stone color '{value}'."),
    };

    private static InitialStoneRole ParseRole(string? value) => value switch
    {
        "king" => InitialStoneRole.King,
        "guard" => InitialStoneRole.Guard,
        _ => throw new InvalidDataException($"Unknown initial stone role '{value}'."),
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

    private sealed record FixtureContext(
        BoardGeometry Geometry,
        InitialPositionDefinition Position,
        string SymmetryId);
}
