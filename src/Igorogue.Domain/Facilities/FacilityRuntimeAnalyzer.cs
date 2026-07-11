using Igorogue.Domain.Board;

namespace Igorogue.Domain.Facilities;

public static class FacilityRuntimeAnalyzer
{
    public static FacilityRuntimeAnalysis Analyze(
        FacilityState facilityState,
        TerritoryAnalysis territoryAnalysis,
        FacilityRuntimePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(facilityState);
        ArgumentNullException.ThrowIfNull(territoryAnalysis);
        ArgumentNullException.ThrowIfNull(policy);
        if (!ReferenceEquals(facilityState.SourceBoard, territoryAnalysis.SourceBoard))
        {
            throw new ArgumentException(
                "Facility state and territory analysis must belong to the exact same board snapshot.",
                nameof(territoryAnalysis));
        }

        var geometry = facilityState.SourceBoard.Geometry;
        var installedByRegion = new Dictionary<TerritoryRegion, List<FacilityInstance>>();
        foreach (var region in territoryAnalysis.Regions)
        {
            installedByRegion.Add(region, []);
        }

        var facilityStates = new FacilityOperatingState[facilityState.InstalledFacilities.Count];
        var facilityStatesByPoint = new FacilityOperatingState?[geometry.PointCount];
        for (var index = 0; index < facilityState.InstalledFacilities.Count; index++)
        {
            var facility = facilityState.InstalledFacilities[index];
            var region = territoryAnalysis.RegionAt(facility.Point)
                ?? throw new InvalidOperationException(
                    $"Facility point {facility.Point} is missing its current empty-point region.");
            installedByRegion[region].Add(facility);

            var operatingState = new FacilityOperatingState(
                facility,
                region,
                ResolveOperatingKind(facility, region));
            facilityStates[index] = operatingState;
            facilityStatesByPoint[geometry.ToCanonicalIndex(facility.Point)] = operatingState;
        }

        var regions = new FacilityRegionRuntimeAnalysis[territoryAnalysis.Regions.Count];
        var regionsByPoint = new FacilityRegionRuntimeAnalysis?[geometry.PointCount];
        for (var index = 0; index < territoryAnalysis.Regions.Count; index++)
        {
            var region = territoryAnalysis.Regions[index];
            var installed = installedByRegion[region].ToArray();
            var countsByType = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var facility in installed)
            {
                countsByType[facility.ContentId] =
                    countsByType.GetValueOrDefault(facility.ContentId) + 1;
            }

            var runtimeRegion = new FacilityRegionRuntimeAnalysis(
                region,
                installed,
                countsByType,
                policy);
            regions[index] = runtimeRegion;
            foreach (var point in region.Points)
            {
                regionsByPoint[geometry.ToCanonicalIndex(point)] = runtimeRegion;
            }
        }

        return new FacilityRuntimeAnalysis(
            facilityState,
            territoryAnalysis,
            policy,
            facilityStates,
            regions,
            facilityStatesByPoint,
            regionsByPoint);
    }

    private static FacilityOperatingKind ResolveOperatingKind(
        FacilityInstance facility,
        TerritoryRegion region)
    {
        if (facility.IsExplicitlyDisabled)
        {
            return FacilityOperatingKind.ExplicitEffect;
        }

        var ownerControlsRegion = (facility.Owner, region.Owner) switch
        {
            (StoneColor.Black, TerritoryOwner.Black) => true,
            (StoneColor.White, TerritoryOwner.White) => true,
            _ => false,
        };
        return ownerControlsRegion
            ? FacilityOperatingKind.Active
            : FacilityOperatingKind.TerritoryControlLost;
    }
}
