using System.Globalization;
using System.Text.Json;

using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

internal static class TemporaryLibertyFixtureData
{
    internal static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    internal static readonly string[] Task0027FixtureIds =
    [
        "TLE-01",
        "TLE-02",
        "TLE-03",
        "TLE-04",
        "TLE-05",
        "TLE-06",
        "TLE-07",
        "TLE-08",
        "TLE-11",
        "TLE-12",
        "TLE-13",
    ];

    internal static IReadOnlyDictionary<string, TemporaryLibertyFixture> LoadFixtures()
    {
        var path = Path.Combine(
            FindRepositoryRoot().FullName,
            "game_data",
            "fixtures",
            "temporary_liberty_expiry_fixtures.json");
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var version = RequiredString(root.GetProperty("version"), "fixture version");
        if (!StringComparer.Ordinal.Equals(version, "1.0.0"))
        {
            throw new InvalidDataException(
                $"Unsupported temporary-liberty fixture version '{version}'.");
        }

        return root.GetProperty("cases")
            .EnumerateArray()
            .Select(ParseFixture)
            .ToDictionary(fixture => fixture.Id, StringComparer.Ordinal);
    }

    internal static TemporaryLibertyFixtureExecution Execute(
        TemporaryLibertyFixture fixture,
        bool reverseEnumeration = false)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var board = fixture.Board
            ?? throw new ArgumentException(
                $"Fixture {fixture.Id} does not define an expiry-sweep board.",
                nameof(fixture));
        var enemyTurnIndex = fixture.EnemyTurnIndex
            ?? throw new ArgumentException(
                $"Fixture {fixture.Id} does not define an enemy-turn index.",
                nameof(fixture));
        var stones = CreateStoneRuntime(
            board,
            fixture.StoneOverrides,
            reverseEnumeration);
        var effects = fixture.Effects
            .Select(effect => effect.ToDomain(stones))
            .ToArray();
        var nextEffectSequence = effects.Length == 0
            ? 1L
            : checked(effects.Max(effect => effect.CreatedSequence) + 1L);
        var temporaryLiberties = TemporaryLibertyState.Create(
            stones,
            reverseEnumeration ? effects.Reverse() : effects,
            nextEffectSequence);
        var modifiers = fixture.ContinuousModifiers
            .Select(modifier => modifier.ToDomain(stones))
            .ToArray();
        var continuousLiberties = ContinuousLibertySnapshot.Create(
            stones,
            reverseEnumeration ? modifiers.Reverse() : modifiers);
        var history = CreateHistory(fixture);
        var resolution = TemporaryLibertyExpiryResolver.Resolve(
            stones,
            temporaryLiberties,
            continuousLiberties,
            history,
            enemyTurnIndex);

        return new TemporaryLibertyFixtureExecution(
            stones,
            temporaryLiberties,
            continuousLiberties,
            history,
            resolution);
    }

    internal static StoneRuntimeState CreateStoneRuntime(
        BoardState board,
        bool reverseEnumeration = false) =>
        CreateStoneRuntime(board, [], reverseEnumeration);

    internal static StoneRuntimeState CreateStoneRuntime(
        BoardState board,
        IReadOnlyList<TemporaryLibertyFixtureStoneOverride> stoneOverrides,
        bool reverseEnumeration = false)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(stoneOverrides);
        var overridesByPoint = stoneOverrides.ToDictionary(
            item => item.Point,
            item => item);
        var instances = board.OccupiedStones
            .Select((stone, index) =>
            {
                overridesByPoint.TryGetValue(stone.Point, out var itemOverride);
                return new StoneRuntimeInstance(
                    itemOverride?.InstanceId ??
                        $"stone_{(index + 1).ToString("D2", CultureInfo.InvariantCulture)}",
                    stone,
                    itemOverride?.KindId ?? (stone.IsKing ? "king" : "standard"),
                    index + 1L,
                    []);
            })
            .ToArray();

        if (overridesByPoint.Keys.Any(point => board.StoneAt(point) is null))
        {
            throw new InvalidDataException(
                "Temporary-liberty stone override targets an empty point.");
        }

        return StoneRuntimeState.Create(
            board,
            reverseEnumeration ? instances.Reverse() : instances,
            instances.Length + 1L);
    }

    private static BattleRepetitionHistory CreateHistory(
        TemporaryLibertyFixture fixture)
    {
        var board = fixture.Board
            ?? throw new InvalidOperationException($"Fixture {fixture.Id} has no board.");
        if (!fixture.ResultTopologySeenBefore)
        {
            return BattleRepetitionHistory.Start(board);
        }

        var groups = StoneGroupAnalyzer.Analyze(board);
        var removedPoints = new HashSet<CanonicalPoint>();
        foreach (var expectedGroup in fixture.Expected.CapturedGroups ?? [])
        {
            var group = groups.GroupAt(expectedGroup.Anchor)
                ?? throw new InvalidDataException(
                    $"Fixture {fixture.Id} expected capture anchor {expectedGroup.Anchor} is empty.");
            foreach (var point in group.StonePoints)
            {
                removedPoints.Add(point);
            }
        }

        if (removedPoints.Count == 0)
        {
            throw new InvalidDataException(
                $"Fixture {fixture.Id} requests a seen result topology without captured groups.");
        }

        var previouslySeenResult = BoardState.Create(
            board.Geometry,
            board.OccupiedStones.Where(stone => !removedPoints.Contains(stone.Point)));

        // This is a restored production-form history: the result topology was observed,
        // play later returned to the current source topology, and mandatory expiry now
        // observes the result again. No test-only history mutation is used.
        return BattleRepetitionHistory.FromObservedBoards([previouslySeenResult, board]);
    }

    private static TemporaryLibertyFixture ParseFixture(JsonElement element)
    {
        var id = RequiredString(element.GetProperty("id"), "fixture id");
        var effects = element.TryGetProperty("effects", out var effectElement)
            ? effectElement.EnumerateArray().Select(effect => ParseEffect(effect, id)).ToArray()
            : [];
        var modifiers = element.TryGetProperty(
                "continuous_modifiers",
                out var modifierElement)
            ? modifierElement
                .EnumerateArray()
                .Select(modifier => ParseContinuousModifier(modifier, id))
                .ToArray()
            : [];
        var territoryQueries = element.TryGetProperty(
                "territory_queries",
                out var queryElement)
            ? queryElement
                .EnumerateArray()
                .Select(point => ParsePoint(point, $"{id}/territory_queries"))
                .ToArray()
            : [];
        var stoneOverrides = element.TryGetProperty(
                "stone_overrides",
                out var overrideElement)
            ? overrideElement
                .EnumerateArray()
                .Select(item => ParseStoneOverride(item, id))
                .ToArray()
            : [];

        return new TemporaryLibertyFixture(
            id,
            RequiredString(element.GetProperty("operation"), $"{id}/operation"),
            RequiredString(element.GetProperty("title"), $"{id}/title"),
            element.TryGetProperty("enemy_turn_index", out var enemyTurn)
                ? enemyTurn.GetInt32()
                : null,
            element.TryGetProperty("board", out var board)
                ? ParseBoard(board, $"{id}/board")
                : null,
            stoneOverrides,
            effects,
            modifiers,
            element.TryGetProperty("equipped_seals", out var equippedSeals)
                ? equippedSeals
                    .EnumerateArray()
                    .Select(value => RequiredString(value, $"{id}/equipped_seals"))
                    .ToArray()
                : [],
            element.TryGetProperty("equipped_relics", out var equippedRelics)
                ? equippedRelics
                    .EnumerateArray()
                    .Select(value => RequiredString(value, $"{id}/equipped_relics"))
                    .ToArray()
                : [],
            element.TryGetProperty("style_id", out var styleId)
                ? RequiredString(styleId, $"{id}/style_id")
                : null,
            element.TryGetProperty("armed_capture_chain", out var captureChain) &&
                captureChain.GetBoolean(),
            element.TryGetProperty("style_first_capture_used", out var styleUsed) &&
                styleUsed.GetBoolean(),
            element.TryGetProperty("seal_first_capture_used", out var sealUsed) &&
                sealUsed.GetBoolean(),
            element.TryGetProperty("start_sacrifice_remainder", out var remainder)
                ? remainder.GetInt32()
                : 0,
            element.TryGetProperty("start_counterattack_units", out var counterattackUnits)
                ? counterattackUnits.GetInt32()
                : null,
            element.TryGetProperty("komi", out var komi) ? komi.GetInt32() : 0,
            element.TryGetProperty("result_topology_seen_before", out var resultSeen) &&
                resultSeen.GetBoolean(),
            territoryQueries,
            ParseExpected(element.GetProperty("expected"), id));
    }

    private static TemporaryLibertyFixtureStoneOverride ParseStoneOverride(
        JsonElement element,
        string fixtureId) =>
        new(
            ParsePoint(
                element.GetProperty("point"),
                $"{fixtureId}/stone_overrides/point"),
            RequiredString(
                element.GetProperty("instance_id"),
                $"{fixtureId}/stone_overrides/instance_id"),
            RequiredString(
                element.GetProperty("kind"),
                $"{fixtureId}/stone_overrides/kind"));

    private static TemporaryLibertyFixtureEffect ParseEffect(
        JsonElement element,
        string fixtureId) =>
        new(
            RequiredString(element.GetProperty("id"), $"{fixtureId}/effects/id"),
            ParsePoint(element.GetProperty("anchor_point"), $"{fixtureId}/effects/anchor_point"),
            element.GetProperty("amount").GetInt32(),
            element.GetProperty("created_sequence").GetInt64(),
            element.GetProperty("expires_after_enemy_turn_index").GetInt32(),
            RequiredString(
                element.GetProperty("source_id"),
                $"{fixtureId}/effects/source_id"));

    private static TemporaryLibertyFixtureContinuousModifier ParseContinuousModifier(
        JsonElement element,
        string fixtureId) =>
        new(
            RequiredString(
                element.GetProperty("id"),
                $"{fixtureId}/continuous_modifiers/id"),
            ParsePoint(
                element.GetProperty("anchor_point"),
                $"{fixtureId}/continuous_modifiers/anchor_point"),
            element.GetProperty("amount").GetInt32(),
            RequiredString(
                element.GetProperty("source_id"),
                $"{fixtureId}/continuous_modifiers/source_id"));

    private static TemporaryLibertyFixtureExpected ParseExpected(
        JsonElement element,
        string fixtureId)
    {
        IReadOnlyList<TemporaryLibertyFixtureCapturedGroup>? capturedGroups =
            element.TryGetProperty(
                "captured_groups",
                out var capturedElement)
            ? capturedElement
                .EnumerateArray()
                .Select(group => ParseCapturedGroup(group, fixtureId))
                .ToArray()
            : null;
        var territories = element.TryGetProperty("territories", out var territoryElement)
            ? territoryElement
                .EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => ParseTerritory(property.Value, fixtureId, property.Name),
                    StringComparer.Ordinal)
            : new Dictionary<string, TemporaryLibertyFixtureTerritory>(
                StringComparer.Ordinal);

        return new TemporaryLibertyFixtureExpected(
            OptionalStrings(element, "expired_effect_ids", fixtureId),
            OptionalStrings(element, "remaining_effect_ids", fixtureId),
            OptionalStrings(element, "continuous_modifier_ids", fixtureId),
            capturedGroups,
            element.TryGetProperty("battle_result", out var battleResult)
                ? RequiredString(battleResult, $"{fixtureId}/expected/battle_result")
                : null,
            element.TryGetProperty("topology_first_seen", out var firstSeen)
                ? firstSeen.GetBoolean()
                : null,
            element.TryGetProperty("group_capture_event_count", out var eventCount)
                ? eventCount.GetInt32()
                : null,
            element.TryGetProperty("pre_expiry_anchor_group", out var preExpiry)
                ? ParsePreExpiryGroup(preExpiry, fixtureId)
                : null,
            element.TryGetProperty("benefits_suppressed", out var suppressed)
                ? suppressed.GetBoolean()
                : null,
            element.TryGetProperty("reserved_draw_delta", out var reservedDrawDelta)
                ? reservedDrawDelta.GetInt32()
                : null,
            element.TryGetProperty("soul_delta", out var soulDelta)
                ? soulDelta.GetInt32()
                : null,
            element.TryGetProperty(
                "counterattack_delta_units",
                out var counterattackDeltaUnits)
                ? counterattackDeltaUnits.GetInt32()
                : null,
            OptionalStrings(element, "captured_king_colors", fixtureId),
            element.TryGetProperty(
                "capture_was_blocked_by_repetition",
                out var repetitionBlocked)
                ? repetitionBlocked.GetBoolean()
                : null,
            territories,
            element.TryGetProperty("momentum_delta", out var momentumDelta)
                ? momentumDelta.GetInt32()
                : null,
            element.TryGetProperty("brilliant_delta", out var brilliantDelta)
                ? brilliantDelta.GetDouble()
                : null,
            OptionalStrings(element, "event_order", fixtureId),
            OptionalStrings(element, "capturing_colors", fixtureId),
            element.TryGetProperty("reserved_qi_delta", out var reservedQiDelta)
                ? reservedQiDelta.GetInt32()
                : null,
            OptionalStrings(element, "deferred_choices", fixtureId),
            OptionalStrings(element, "benefit_event_order", fixtureId),
            element.TryGetProperty("sacrifice_remainder", out var sacrificeRemainder)
                ? sacrificeRemainder.GetInt32()
                : null,
            element.TryGetProperty("counterattack_advances", out var advances)
                ? advances
                    .EnumerateArray()
                    .Select(advance => ParseCounterattackAdvance(advance, fixtureId))
                    .ToArray()
                : null,
            element.TryGetProperty(
                "end_counterattack_units",
                out var endCounterattackUnits)
                ? endCounterattackUnits.GetInt32()
                : null,
            element.TryGetProperty("pending", out var pending)
                ? pending.GetBoolean()
                : null);
    }

    private static TemporaryLibertyFixtureCounterattackAdvance ParseCounterattackAdvance(
        JsonElement element,
        string fixtureId) =>
        new(
            RequiredString(
                element.GetProperty("reason"),
                $"{fixtureId}/expected/counterattack_advances/reason"),
            element.GetProperty("delta_units").GetInt32());

    private static TemporaryLibertyFixtureCapturedGroup ParseCapturedGroup(
        JsonElement element,
        string fixtureId) =>
        new(
            ParseStoneColor(RequiredString(
                element.GetProperty("color"),
                $"{fixtureId}/expected/captured_groups/color")),
            ParsePoint(
                element.GetProperty("anchor"),
                $"{fixtureId}/expected/captured_groups/anchor"),
            element.GetProperty("count").GetInt32(),
            element.GetProperty("contains_king").GetBoolean());

    private static TemporaryLibertyFixturePreExpiryGroup ParsePreExpiryGroup(
        JsonElement element,
        string fixtureId) =>
        new(
            ParsePoint(
                element.GetProperty("anchor"),
                $"{fixtureId}/expected/pre_expiry_anchor_group/anchor"),
            element.GetProperty("count").GetInt32(),
            element.GetProperty("temporary_bonus").GetInt32());

    private static TemporaryLibertyFixtureTerritory ParseTerritory(
        JsonElement element,
        string fixtureId,
        string pointKey) =>
        new(
            ParseTerritoryOwner(RequiredString(
                element.GetProperty("owner"),
                $"{fixtureId}/expected/territories/{pointKey}/owner")),
            element.GetProperty("size").GetInt32(),
            element.GetProperty("basic_income").GetInt32());

    private static IReadOnlyList<string>? OptionalStrings(
        JsonElement element,
        string propertyName,
        string fixtureId) =>
        element.TryGetProperty(propertyName, out var values)
            ? values
                .EnumerateArray()
                .Select(value => RequiredString(
                    value,
                    $"{fixtureId}/expected/{propertyName}"))
                .ToArray()
            : null;

    private static BoardState ParseBoard(JsonElement element, string label)
    {
        var rows = element.EnumerateArray()
            .Select(row => RequiredString(row, $"{label}/row"))
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
                        $"{label} contains unknown symbol '{symbol}'."),
                };
                stones.Add(new BoardStone(
                    color,
                    isKing,
                    Geometry.CreateCanonicalPoint(column + 1, y)));
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

        return Geometry.CreateCanonicalPoint(coordinates[0], coordinates[1]);
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

internal sealed record TemporaryLibertyFixtureStoneOverride(
    CanonicalPoint Point,
    string InstanceId,
    string KindId);

internal sealed record TemporaryLibertyFixtureEffect(
    string EffectInstanceId,
    CanonicalPoint AnchorPoint,
    int Amount,
    long CreatedSequence,
    int ExpiresAfterEnemyTurnIndex,
    string SourceId)
{
    internal TemporaryLibertyEffect ToDomain(StoneRuntimeState stones)
    {
        var anchor = stones.InstanceAt(AnchorPoint)
            ?? throw new InvalidDataException(
                $"Temporary-liberty fixture anchor {AnchorPoint} is empty.");
        return new TemporaryLibertyEffect(
            EffectInstanceId,
            Amount,
            anchor.Color,
            anchor.InstanceId,
            SourceId,
            CreatedSequence,
            ExpiresAfterEnemyTurnIndex);
    }
}

internal sealed record TemporaryLibertyFixtureContinuousModifier(
    string ModifierInstanceId,
    CanonicalPoint AnchorPoint,
    int Amount,
    string SourceId)
{
    internal ContinuousLibertyModifier ToDomain(StoneRuntimeState stones)
    {
        var anchor = stones.InstanceAt(AnchorPoint)
            ?? throw new InvalidDataException(
                $"Continuous-liberty fixture anchor {AnchorPoint} is empty.");
        return new ContinuousLibertyModifier(
            ModifierInstanceId,
            Amount,
            anchor.Color,
            anchor.InstanceId,
            SourceId);
    }
}

internal sealed record TemporaryLibertyFixtureCapturedGroup(
    StoneColor Color,
    CanonicalPoint Anchor,
    int Count,
    bool ContainsKing);

internal sealed record TemporaryLibertyFixturePreExpiryGroup(
    CanonicalPoint Anchor,
    int Count,
    int TemporaryBonus);

internal sealed record TemporaryLibertyFixtureTerritory(
    TerritoryOwner Owner,
    int Size,
    int BasicIncome);

internal sealed record TemporaryLibertyFixtureCounterattackAdvance(
    string Reason,
    int DeltaUnits);

internal sealed record TemporaryLibertyFixtureExpected(
    IReadOnlyList<string>? ExpiredEffectIds,
    IReadOnlyList<string>? RemainingEffectIds,
    IReadOnlyList<string>? ContinuousModifierIds,
    IReadOnlyList<TemporaryLibertyFixtureCapturedGroup>? CapturedGroups,
    string? BattleResult,
    bool? TopologyFirstSeen,
    int? GroupCaptureEventCount,
    TemporaryLibertyFixturePreExpiryGroup? PreExpiryAnchorGroup,
    bool? BenefitsSuppressed,
    int? ReservedDrawDelta,
    int? SoulDelta,
    int? CounterattackDeltaUnits,
    IReadOnlyList<string>? CapturedKingColors,
    bool? CaptureWasBlockedByRepetition,
    IReadOnlyDictionary<string, TemporaryLibertyFixtureTerritory> Territories,
    int? MomentumDelta,
    double? BrilliantDelta,
    IReadOnlyList<string>? EventOrder,
    IReadOnlyList<string>? CapturingColors,
    int? ReservedQiDelta,
    IReadOnlyList<string>? DeferredChoices,
    IReadOnlyList<string>? BenefitEventOrder,
    int? SacrificeRemainder,
    IReadOnlyList<TemporaryLibertyFixtureCounterattackAdvance>? CounterattackAdvances,
    int? EndCounterattackUnits,
    bool? Pending);

internal sealed record TemporaryLibertyFixture(
    string Id,
    string Operation,
    string Title,
    int? EnemyTurnIndex,
    BoardState? Board,
    IReadOnlyList<TemporaryLibertyFixtureStoneOverride> StoneOverrides,
    IReadOnlyList<TemporaryLibertyFixtureEffect> Effects,
    IReadOnlyList<TemporaryLibertyFixtureContinuousModifier> ContinuousModifiers,
    IReadOnlyList<string> EquippedSeals,
    IReadOnlyList<string> EquippedRelics,
    string? StyleId,
    bool ArmedCaptureChain,
    bool StyleFirstCaptureUsed,
    bool SealFirstCaptureUsed,
    int StartSacrificeRemainder,
    int? StartCounterattackUnits,
    int Komi,
    bool ResultTopologySeenBefore,
    IReadOnlyList<CanonicalPoint> TerritoryQueries,
    TemporaryLibertyFixtureExpected Expected);

internal sealed record TemporaryLibertyFixtureExecution(
    StoneRuntimeState SourceStones,
    TemporaryLibertyState SourceTemporaryLiberties,
    ContinuousLibertySnapshot ContinuousLiberties,
    BattleRepetitionHistory SourceHistory,
    TemporaryLibertyExpiryResolution Resolution);
