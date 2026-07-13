using System.Globalization;
using System.Text;

using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Content;

/// <summary>
/// Typed, content-owned inputs required to create a fresh Core Duel runtime.
/// This definition contains no host or presentation state and does not create
/// the Application aggregate itself.
/// </summary>
public sealed class CoreDuelBattleSetupDefinition
{
    public const string EncodingVersion = "core-duel-battle-setup-v1";
    private const string SupportedInitialPositionId = "standard_v0_2";

    private CoreDuelBattleSetupDefinition(
        InitialPositionDefinition initialPosition,
        int playerTurnLimit,
        FacilityRuntimePolicy facilityPolicy,
        CounterattackBoundaryPolicy counterattackPolicy,
        int counterattackStartGaugeUnits)
    {
        InitialPosition = initialPosition;
        PlayerTurnLimit = playerTurnLimit;
        FacilityPolicy = facilityPolicy;
        CounterattackPolicy = counterattackPolicy;
        CounterattackStartGaugeUnits = counterattackStartGaugeUnits;
        CanonicalText = CreateCanonicalText();
    }

    public InitialPositionDefinition InitialPosition { get; }

    public int PlayerTurnLimit { get; }

    public FacilityRuntimePolicy FacilityPolicy { get; }

    public CounterattackBoundaryPolicy CounterattackPolicy { get; }

    public int CounterattackStartGaugeUnits { get; }

    public string CanonicalText { get; }

    public static CoreDuelBattleSetupDefinition Create(
        InitialPositionDefinition initialPosition,
        int playerTurnLimit,
        FacilityRuntimePolicy facilityPolicy,
        CounterattackBoundaryPolicy counterattackPolicy,
        int counterattackStartGaugeUnits)
    {
        ArgumentNullException.ThrowIfNull(initialPosition);
        ValidateInitialPosition(initialPosition);
        if (playerTurnLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerTurnLimit),
                playerTurnLimit,
                "Core Duel player-turn limit must be positive.");
        }

        ArgumentNullException.ThrowIfNull(facilityPolicy);
        ArgumentNullException.ThrowIfNull(counterattackPolicy);
        _ = CounterattackBoundaryState.Create(
            counterattackStartGaugeUnits,
            pending: false,
            sacrificeStoneRemainder: 0,
            counterattackPolicy);

        return new CoreDuelBattleSetupDefinition(
            initialPosition,
            playerTurnLimit,
            facilityPolicy,
            counterattackPolicy,
            counterattackStartGaugeUnits);
    }

    public string ToCanonicalText() => CanonicalText;

    private static void ValidateInitialPosition(InitialPositionDefinition position)
    {
        if (!string.Equals(position.Id, SupportedInitialPositionId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Core Duel supports only initial position {SupportedInitialPositionId}.",
                nameof(position));
        }

        var geometry = position.Geometry;
        var centerCoordinate = (geometry.Size + 1) / 2;
        var center = geometry.CreateCanonicalPoint(centerCoordinate, centerCoordinate);
        if (position.IsOccupied(center))
        {
            throw new ArgumentException(
                "The accepted Core Duel initial position must leave the center point empty.",
                nameof(position));
        }

        if (!position.HasRoleAwarePointReflectionSymmetry())
        {
            throw new ArgumentException(
                "The accepted Core Duel initial position must have role-aware point-reflection symmetry.",
                nameof(position));
        }

        foreach (var color in new[] { StoneColor.Black, StoneColor.White })
        {
            var colorStones = position.Stones
                .Where(stone => stone.Color == color)
                .ToArray();
            if (colorStones.Count(stone => stone.Role == InitialStoneRole.King) != 1 ||
                colorStones.Count(stone => stone.Role == InitialStoneRole.Guard) != 2)
            {
                throw new ArgumentException(
                    "The accepted Core Duel initial position requires exactly one king and two guards per color.",
                    nameof(position));
            }
        }

        var board = BoardState.FromInitialPosition(position);
        var groups = StoneGroupAnalyzer.Analyze(board);
        foreach (var color in new[] { StoneColor.Black, StoneColor.White })
        {
            var king = position.Stones.Single(stone =>
                stone.Color == color && stone.Role == InitialStoneRole.King);
            var kingGroup = groups.GroupAt(king.Point);
            if (kingGroup is null ||
                kingGroup.Stones.Count != 3 ||
                kingGroup.Stones.Count(stone => stone.IsKing) != 1)
            {
                throw new ArgumentException(
                    "Each accepted Core Duel initial king group must contain its three connected same-color stones.",
                    nameof(position));
            }

            if (kingGroup.RealLibertyCount != 7)
            {
                throw new ArgumentException(
                    "Each accepted Core Duel initial king group must have exactly seven real liberties.",
                    nameof(position));
            }
        }
    }

    private string CreateCanonicalText()
    {
        var lines = new List<string>
        {
            EncodingVersion,
            $"initial_position_id={EncodeStableText(InitialPosition.Id)}",
            $"initial_stone_count={InitialPosition.Stones.Count.ToString(CultureInfo.InvariantCulture)}",
        };
        foreach (var stone in InitialPosition.Stones)
        {
            lines.Add(
                $"initial_stone={ColorId(stone.Color)},{RoleId(stone.Role)}," +
                $"{stone.Point.X.ToString(CultureInfo.InvariantCulture)}," +
                stone.Point.Y.ToString(CultureInfo.InvariantCulture));
        }

        lines.Add($"player_turn_limit={PlayerTurnLimit.ToString(CultureInfo.InvariantCulture)}");
        lines.Add(
            $"territory_income_divisor={FacilityPolicy.TerritoryIncomeDivisor.ToString(CultureInfo.InvariantCulture)}");
        lines.Add($"facility_slot_cap={FacilityPolicy.SlotCap.ToString(CultureInfo.InvariantCulture)}");
        lines.Add(
            $"facility_capacity_band_count={FacilityPolicy.CapacityBands.Count.ToString(CultureInfo.InvariantCulture)}");
        foreach (var band in FacilityPolicy.CapacityBands)
        {
            lines.Add(
                $"facility_capacity_band={band.MinSize.ToString(CultureInfo.InvariantCulture)}," +
                $"{band.MaxSize.ToString(CultureInfo.InvariantCulture)}," +
                band.Slots.ToString(CultureInfo.InvariantCulture));
        }

        lines.Add(
            $"facility_type_limit_count={FacilityPolicy.TypeLimits.Count.ToString(CultureInfo.InvariantCulture)}");
        foreach (var pair in FacilityPolicy.TypeLimits)
        {
            lines.Add(
                $"facility_type_limit={EncodeStableText(pair.Key)}," +
                pair.Value.ToString(CultureInfo.InvariantCulture));
        }

        lines.Add("counterattack_policy_begin");
        lines.Add(CounterattackPolicy.ToCanonicalText());
        lines.Add("counterattack_policy_end");
        lines.Add(
            $"counterattack_start_gauge_units={CounterattackStartGaugeUnits.ToString(CultureInfo.InvariantCulture)}");
        return string.Join('\n', lines);
    }

    private static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Unknown initial stone color."),
    };

    private static string RoleId(InitialStoneRole role) => role switch
    {
        InitialStoneRole.King => "king",
        InitialStoneRole.Guard => "guard",
        _ => throw new InvalidOperationException("Unknown initial stone role."),
    };

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}
