using System.Collections.ObjectModel;

using Igorogue.Domain.Board;

namespace Igorogue.Domain.Facilities;

public enum FacilityOperatingKind : byte
{
    Active = 1,
    ExplicitEffect = 2,
    TerritoryControlLost = 3,
}

public sealed class FacilityOperatingState
{
    internal FacilityOperatingState(
        FacilityInstance facility,
        TerritoryRegion currentRegion,
        FacilityOperatingKind kind)
    {
        Facility = facility;
        CurrentRegion = currentRegion;
        Kind = kind;
    }

    public FacilityInstance Facility { get; }

    public TerritoryRegion CurrentRegion { get; }

    public FacilityOperatingKind Kind { get; }

    public bool IsActive => Kind == FacilityOperatingKind.Active;

    public string ReasonId => Kind switch
    {
        FacilityOperatingKind.Active => "active",
        FacilityOperatingKind.ExplicitEffect => "explicit_effect",
        FacilityOperatingKind.TerritoryControlLost => "territory_control_lost",
        _ => throw new InvalidOperationException("Unknown facility operating kind."),
    };
}

public sealed class FacilityRegionRuntimeAnalysis
{
    private readonly ReadOnlyCollection<FacilityInstance> installedFacilityView;
    private readonly ReadOnlyDictionary<string, int> installedCountByTypeView;
    private readonly FacilityRuntimePolicy policy;

    internal FacilityRegionRuntimeAnalysis(
        TerritoryRegion region,
        FacilityInstance[] installedFacilities,
        SortedDictionary<string, int> installedCountsByType,
        FacilityRuntimePolicy policy)
    {
        Region = region;
        installedFacilityView = Array.AsReadOnly(
            (FacilityInstance[])installedFacilities.Clone());
        installedCountByTypeView = new ReadOnlyDictionary<string, int>(
            new SortedDictionary<string, int>(installedCountsByType, StringComparer.Ordinal));
        this.policy = policy;
        BasicIncome = policy.TerritoryIncomeForSize(region.Size);
        BaseConstructionCapacity = policy.BaseCapacityForSize(region.Size);
    }

    public TerritoryRegion Region { get; }

    public int BasicIncome { get; }

    public int BaseConstructionCapacity { get; }

    public int ConstructionCapacity => BaseConstructionCapacity;

    public IReadOnlyList<FacilityInstance> InstalledFacilities => installedFacilityView;

    public IReadOnlyDictionary<string, int> InstalledCountsByType => installedCountByTypeView;

    public int InstalledCount => installedFacilityView.Count;

    public bool IsOverCapacity => InstalledCount > ConstructionCapacity;

    public int InstalledCountFor(string contentId)
    {
        var stableContentId = FacilityInstance.ValidateStableId(contentId, nameof(contentId));
        return installedCountByTypeView.GetValueOrDefault(stableContentId);
    }

    public int ConstructionCapacityWithModifier(int nonnegativeModifier) =>
        policy.EffectiveCapacityForSize(Region.Size, nonnegativeModifier);

    public bool IsOverCapacityWithModifier(int nonnegativeModifier) =>
        InstalledCount > ConstructionCapacityWithModifier(nonnegativeModifier);
}

public sealed class FacilityRuntimeAnalysis
{
    private readonly ReadOnlyCollection<FacilityOperatingState> facilityStateView;
    private readonly ReadOnlyCollection<FacilityRegionRuntimeAnalysis> regionView;
    private readonly FacilityOperatingState?[] facilityStatesByCanonicalIndex;
    private readonly FacilityRegionRuntimeAnalysis?[] regionsByCanonicalIndex;

    internal FacilityRuntimeAnalysis(
        FacilityState facilityState,
        TerritoryAnalysis territoryAnalysis,
        FacilityRuntimePolicy policy,
        FacilityOperatingState[] canonicalFacilityStates,
        FacilityRegionRuntimeAnalysis[] canonicalRegions,
        FacilityOperatingState?[] facilityStatesByCanonicalIndex,
        FacilityRegionRuntimeAnalysis?[] regionsByCanonicalIndex)
    {
        FacilityState = facilityState;
        TerritoryAnalysis = territoryAnalysis;
        Policy = policy;
        facilityStateView = Array.AsReadOnly(
            (FacilityOperatingState[])canonicalFacilityStates.Clone());
        regionView = Array.AsReadOnly(
            (FacilityRegionRuntimeAnalysis[])canonicalRegions.Clone());
        this.facilityStatesByCanonicalIndex =
            (FacilityOperatingState?[])facilityStatesByCanonicalIndex.Clone();
        this.regionsByCanonicalIndex =
            (FacilityRegionRuntimeAnalysis?[])regionsByCanonicalIndex.Clone();
    }

    public FacilityState FacilityState { get; }

    public TerritoryAnalysis TerritoryAnalysis { get; }

    public FacilityRuntimePolicy Policy { get; }

    public BoardState SourceBoard => FacilityState.SourceBoard;

    public IReadOnlyList<FacilityOperatingState> FacilityStates => facilityStateView;

    public IReadOnlyList<FacilityRegionRuntimeAnalysis> Regions => regionView;

    public FacilityOperatingState? FacilityAt(CanonicalPoint point)
    {
        var index = SourceBoard.Geometry.ToCanonicalIndex(point);
        return facilityStatesByCanonicalIndex[index];
    }

    public FacilityOperatingState OperatingStateFor(FacilityInstance facility)
    {
        ArgumentNullException.ThrowIfNull(facility);
        var installed = FacilityState.FacilityAt(facility.Point);
        if (!ReferenceEquals(installed, facility))
        {
            throw new ArgumentException(
                "Facility instance does not belong to this runtime analysis.",
                nameof(facility));
        }

        return FacilityAt(facility.Point)
            ?? throw new InvalidOperationException("Installed facility is missing its operating state.");
    }

    public FacilityRegionRuntimeAnalysis? RegionAt(CanonicalPoint point)
    {
        var index = SourceBoard.Geometry.ToCanonicalIndex(point);
        return regionsByCanonicalIndex[index];
    }
}
