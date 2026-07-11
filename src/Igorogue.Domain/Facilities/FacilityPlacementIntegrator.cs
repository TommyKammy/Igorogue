using Igorogue.Domain.Board;

namespace Igorogue.Domain.Facilities;

public static class FacilityPlacementIntegrator
{
    public static FacilityPlacementCommit Apply(
        FacilityState sourceFacilityState,
        LegalPlacementCommit acceptedPlacement)
    {
        ArgumentNullException.ThrowIfNull(sourceFacilityState);
        ArgumentNullException.ThrowIfNull(acceptedPlacement);
        if (!ReferenceEquals(
                sourceFacilityState.SourceBoard,
                acceptedPlacement.Candidate.SourceBoard))
        {
            throw new ArgumentException(
                "Facility state must belong to the accepted placement's exact source board.",
                nameof(sourceFacilityState));
        }

        var placedStone = acceptedPlacement.Candidate.PlacedStone;
        var destroyed = sourceFacilityState.FacilityAt(placedStone.Point);
        var remaining = destroyed is null
            ? sourceFacilityState.InstalledFacilities
            : sourceFacilityState.InstalledFacilities
                .Where(facility => !ReferenceEquals(facility, destroyed))
                .ToArray();
        var stateAfterCommit = FacilityState.Create(
            acceptedPlacement.BoardAfterCommit,
            remaining,
            sourceFacilityState.NextBuildSequence);

        FacilityDestroyedFact? destructionFact = null;
        var orderedFacts = new List<ICommittedPlacementFact>(
            acceptedPlacement.OrderedFacts.Count + 3);
        orderedFacts.AddRange(acceptedPlacement.OrderedFacts);
        if (destroyed is not null)
        {
            destructionFact = new FacilityDestroyedFact(
                destroyed,
                placedStone,
                FacilityDestructionReason.StoneOccupied);
            orderedFacts.Add(destructionFact);
        }

        orderedFacts.Add(new StoneTopologyRegisteredFact(
            acceptedPlacement.RegisteredTopologyKey,
            acceptedPlacement.HistoryAfterCommit));
        orderedFacts.Add(new KingCaptureEvaluatedFact(
            acceptedPlacement.KingCaptureResult));

        return new FacilityPlacementCommit(
            sourceFacilityState,
            stateAfterCommit,
            acceptedPlacement,
            destructionFact,
            orderedFacts.ToArray());
    }
}
