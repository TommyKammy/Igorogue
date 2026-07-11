using System.Collections.ObjectModel;
using Igorogue.Domain.Board;

namespace Igorogue.Domain.Facilities;

public sealed class FacilityOperatingTransition
{
    private readonly ReadOnlyCollection<FacilityFact> orderedFactView;

    internal FacilityOperatingTransition(
        FacilityRuntimeAnalysis analysisBefore,
        FacilityRuntimeAnalysis analysisAfter,
        FacilityFact[] orderedFacts)
    {
        ArgumentNullException.ThrowIfNull(analysisBefore);
        ArgumentNullException.ThrowIfNull(analysisAfter);
        ArgumentNullException.ThrowIfNull(orderedFacts);

        AnalysisBefore = analysisBefore;
        AnalysisAfter = analysisAfter;
        orderedFactView = Array.AsReadOnly((FacilityFact[])orderedFacts.Clone());
    }

    public FacilityRuntimeAnalysis AnalysisBefore { get; }

    public FacilityRuntimeAnalysis AnalysisAfter { get; }

    public FacilityState StateAfterTransition => AnalysisAfter.FacilityState;

    public IReadOnlyList<FacilityFact> OrderedFacts => orderedFactView;
}

public static class FacilityOperatingTransitionResolver
{
    public static FacilityOperatingTransition ReassociateAfterPlacement(
        FacilityRuntimeAnalysis analysisBefore,
        FacilityPlacementCommit placementCommit,
        TerritoryAnalysis territoryAfter)
    {
        ArgumentNullException.ThrowIfNull(analysisBefore);
        ArgumentNullException.ThrowIfNull(placementCommit);
        ArgumentNullException.ThrowIfNull(territoryAfter);
        if (!ReferenceEquals(
                analysisBefore.FacilityState,
                placementCommit.SourceFacilityState))
        {
            throw new ArgumentException(
                "Facility analysis must belong to the placement commit's exact source state.",
                nameof(placementCommit));
        }

        if (!ReferenceEquals(territoryAfter.SourceBoard, placementCommit.BoardAfterCommit))
        {
            throw new ArgumentException(
                "Territory analysis must belong to the placement commit's exact result board.",
                nameof(territoryAfter));
        }

        var stateBefore = analysisBefore.FacilityState;
        var stateAfter = placementCommit.FacilityStateAfterCommit;
        var destroyed = placementCommit.DestructionFact?.Facility;
        var expectedAfterCount = stateBefore.InstalledFacilities.Count -
            (destroyed is null ? 0 : 1);
        if (stateAfter.NextBuildSequence != stateBefore.NextBuildSequence ||
            stateAfter.InstalledFacilities.Count != expectedAfterCount)
        {
            throw new ArgumentException(
                "Placement facility state must preserve metadata and remove only its destruction target.",
                nameof(placementCommit));
        }

        foreach (var facilityBefore in stateBefore.InstalledFacilities)
        {
            var facilityAfter = stateAfter.FacilityById(facilityBefore.InstanceId);
            if (ReferenceEquals(facilityBefore, destroyed))
            {
                if (facilityAfter is not null)
                {
                    throw new ArgumentException(
                        "Destroyed facility cannot survive its placement commit.",
                        nameof(placementCommit));
                }

                continue;
            }

            if (!ReferenceEquals(facilityBefore, facilityAfter))
            {
                throw new ArgumentException(
                    "Placement commit must preserve each surviving exact facility instance.",
                    nameof(placementCommit));
            }
        }

        var analysisAfter = FacilityRuntimeAnalyzer.Analyze(
            stateAfter,
            territoryAfter,
            analysisBefore.Policy);
        return ResolvePreservedFacilities(analysisBefore, analysisAfter);
    }

    public static FacilityOperatingTransition Reassociate(
        FacilityRuntimeAnalysis analysisBefore,
        TerritoryAnalysis territoryAfter)
    {
        ArgumentNullException.ThrowIfNull(analysisBefore);
        ArgumentNullException.ThrowIfNull(territoryAfter);

        var stateBefore = analysisBefore.FacilityState;
        var stateAfter = FacilityState.Create(
            territoryAfter.SourceBoard,
            stateBefore.InstalledFacilities,
            stateBefore.NextBuildSequence);
        var analysisAfter = FacilityRuntimeAnalyzer.Analyze(
            stateAfter,
            territoryAfter,
            analysisBefore.Policy);
        return Resolve(analysisBefore, analysisAfter);
    }

    public static FacilityOperatingTransition Resolve(
        FacilityRuntimeAnalysis analysisBefore,
        FacilityRuntimeAnalysis analysisAfter)
    {
        ArgumentNullException.ThrowIfNull(analysisBefore);
        ArgumentNullException.ThrowIfNull(analysisAfter);
        if (!ReferenceEquals(analysisBefore.Policy, analysisAfter.Policy))
        {
            throw new ArgumentException(
                "Facility transition analyses must use the exact same runtime policy.",
                nameof(analysisAfter));
        }

        var stateBefore = analysisBefore.FacilityState;
        var stateAfter = analysisAfter.FacilityState;
        if (stateBefore.NextBuildSequence != stateAfter.NextBuildSequence ||
            stateBefore.InstalledFacilities.Count != stateAfter.InstalledFacilities.Count)
        {
            throw new ArgumentException(
                "Operating transitions cannot build or destroy facility instances.",
                nameof(analysisAfter));
        }

        return ResolvePreservedFacilities(analysisBefore, analysisAfter);
    }

    private static FacilityOperatingTransition ResolvePreservedFacilities(
        FacilityRuntimeAnalysis analysisBefore,
        FacilityRuntimeAnalysis analysisAfter)
    {
        var orderedFacts = new List<FacilityFact>();
        foreach (var facilityAfter in analysisAfter.FacilityState.InstalledFacilities)
        {
            var facilityBefore = analysisBefore.FacilityState.FacilityById(
                facilityAfter.InstanceId);
            if (!ReferenceEquals(facilityBefore, facilityAfter))
            {
                throw new ArgumentException(
                    "Operating transitions must preserve each exact facility instance.",
                    nameof(analysisAfter));
            }

            var operatingBefore = analysisBefore.OperatingStateFor(facilityAfter);
            var operatingAfter = analysisAfter.OperatingStateFor(facilityAfter);
            if (operatingBefore.IsActive == operatingAfter.IsActive)
            {
                continue;
            }

            if (operatingBefore.IsActive)
            {
                if (operatingAfter.Kind != FacilityOperatingKind.TerritoryControlLost)
                {
                    throw new InvalidOperationException(
                        "Territory reassociation cannot add an explicit disable source.");
                }

                orderedFacts.Add(new FacilityDisabledFact(
                    facilityAfter,
                    operatingAfter.CurrentRegion,
                    FacilityDisableReason.TerritoryControlLost));
                continue;
            }

            if (operatingBefore.Kind != FacilityOperatingKind.TerritoryControlLost ||
                operatingAfter.Kind != FacilityOperatingKind.Active)
            {
                throw new InvalidOperationException(
                    "Territory reassociation cannot remove an explicit disable source.");
            }

            orderedFacts.Add(new FacilityActivatedFact(
                facilityAfter,
                operatingAfter.CurrentRegion,
                FacilityActivationReason.TerritoryControlRestored));
        }

        return new FacilityOperatingTransition(
            analysisBefore,
            analysisAfter,
            orderedFacts.ToArray());
    }
}
