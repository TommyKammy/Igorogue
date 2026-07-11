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

        var orderedFacts = new List<FacilityFact>();
        for (var index = 0; index < stateBefore.InstalledFacilities.Count; index++)
        {
            var facilityBefore = stateBefore.InstalledFacilities[index];
            var facilityAfter = stateAfter.InstalledFacilities[index];
            if (!ReferenceEquals(facilityBefore, facilityAfter))
            {
                throw new ArgumentException(
                    "Operating transitions must preserve each exact facility instance.",
                    nameof(analysisAfter));
            }

            var operatingBefore = analysisBefore.OperatingStateFor(facilityBefore);
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
