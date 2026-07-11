using System.Globalization;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Battle;

public sealed class BattleRuntimePolicy
{
    public const string EncodingVersion = "battle-runtime-policy-v1";

    public BattleRuntimePolicy(
        int playerTurnLimit,
        FacilityRuntimePolicy facilityPolicy)
    {
        if (playerTurnLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerTurnLimit),
                playerTurnLimit,
                "Player turn limit must be positive.");
        }

        ArgumentNullException.ThrowIfNull(facilityPolicy);
        PlayerTurnLimit = playerTurnLimit;
        FacilityPolicy = facilityPolicy;
    }

    public int PlayerTurnLimit { get; }

    public FacilityRuntimePolicy FacilityPolicy { get; }

    public string ToCanonicalText()
    {
        var lines = new List<string>
        {
            EncodingVersion,
            $"player_turn_limit={PlayerTurnLimit.ToString(CultureInfo.InvariantCulture)}",
            $"territory_income_divisor={FacilityPolicy.TerritoryIncomeDivisor.ToString(CultureInfo.InvariantCulture)}",
            $"facility_slot_cap={FacilityPolicy.SlotCap.ToString(CultureInfo.InvariantCulture)}",
            $"capacity_band_count={FacilityPolicy.CapacityBands.Count.ToString(CultureInfo.InvariantCulture)}",
        };

        foreach (var band in FacilityPolicy.CapacityBands)
        {
            lines.Add(
                $"capacity_band={band.MinSize.ToString(CultureInfo.InvariantCulture)}," +
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

        return string.Join('\n', lines);
    }

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
}
