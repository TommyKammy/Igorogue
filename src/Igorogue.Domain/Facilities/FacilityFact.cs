using Igorogue.Domain.Board;

namespace Igorogue.Domain.Facilities;

public enum FacilityActivationReason : byte
{
    BuiltInControlledTerritory = 1,
    TerritoryControlRestored = 2,
}

public enum FacilityDisableReason : byte
{
    TerritoryControlLost = 1,
    ExplicitEffect = 2,
}

public enum FacilityDestructionReason : byte
{
    StoneOccupied = 1,
}

public abstract class FacilityFact
{
    internal FacilityFact(FacilityInstance facility)
    {
        ArgumentNullException.ThrowIfNull(facility);
        Facility = facility;
    }

    public FacilityInstance Facility { get; }
}

public sealed class FacilityBuiltFact : FacilityFact
{
    internal FacilityBuiltFact(FacilityInstance facility)
        : base(facility)
    {
    }
}

public sealed class FacilityActivatedFact : FacilityFact
{
    internal FacilityActivatedFact(
        FacilityInstance facility,
        TerritoryRegion region,
        FacilityActivationReason reason)
        : base(facility)
    {
        ArgumentNullException.ThrowIfNull(region);
        if (reason is not FacilityActivationReason.BuiltInControlledTerritory and
            not FacilityActivationReason.TerritoryControlRestored)
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown activation reason.");
        }

        Region = region;
        Reason = reason;
    }

    public TerritoryRegion Region { get; }

    public FacilityActivationReason Reason { get; }

    public string ReasonId => Reason switch
    {
        FacilityActivationReason.BuiltInControlledTerritory =>
            "built_in_controlled_territory",
        FacilityActivationReason.TerritoryControlRestored =>
            "territory_control_restored",
        _ => throw new InvalidOperationException("Unknown activation reason."),
    };
}

public sealed class FacilityDisabledFact : FacilityFact
{
    internal FacilityDisabledFact(
        FacilityInstance facility,
        TerritoryRegion region,
        FacilityDisableReason reason)
        : base(facility)
    {
        ArgumentNullException.ThrowIfNull(region);
        if (reason is not FacilityDisableReason.TerritoryControlLost and
            not FacilityDisableReason.ExplicitEffect)
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown disable reason.");
        }

        Region = region;
        Reason = reason;
    }

    public TerritoryRegion Region { get; }

    public FacilityDisableReason Reason { get; }

    public string ReasonId => Reason switch
    {
        FacilityDisableReason.TerritoryControlLost => "territory_control_lost",
        FacilityDisableReason.ExplicitEffect => "explicit_effect",
        _ => throw new InvalidOperationException("Unknown disable reason."),
    };
}

public sealed class FacilityDestroyedFact : FacilityFact, ICommittedPlacementFact
{
    internal FacilityDestroyedFact(
        FacilityInstance facility,
        BoardStone placedStone,
        FacilityDestructionReason reason)
        : base(facility)
    {
        ArgumentNullException.ThrowIfNull(placedStone);
        if (reason != FacilityDestructionReason.StoneOccupied)
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown destruction reason.");
        }

        PlacedStone = placedStone;
        Reason = reason;
    }

    public BoardStone PlacedStone { get; }

    public FacilityDestructionReason Reason { get; }

    public string ReasonId => Reason switch
    {
        FacilityDestructionReason.StoneOccupied => "stone_occupied",
        _ => throw new InvalidOperationException("Unknown destruction reason."),
    };
}
