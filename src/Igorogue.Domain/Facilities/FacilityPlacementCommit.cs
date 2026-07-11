using System.Collections.ObjectModel;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Facilities;

public sealed class FacilityPlacementCommit
{
    private readonly LegalPlacementCommit acceptedPlacement;
    private readonly ReadOnlyCollection<ICommittedPlacementFact> orderedFactView;

    internal FacilityPlacementCommit(
        FacilityState sourceFacilityState,
        FacilityState facilityStateAfterCommit,
        LegalPlacementCommit acceptedPlacement,
        FacilityDestroyedFact? destructionFact,
        ICommittedPlacementFact[] orderedFacts)
    {
        ArgumentNullException.ThrowIfNull(sourceFacilityState);
        ArgumentNullException.ThrowIfNull(facilityStateAfterCommit);
        ArgumentNullException.ThrowIfNull(acceptedPlacement);
        ArgumentNullException.ThrowIfNull(orderedFacts);

        SourceFacilityState = sourceFacilityState;
        FacilityStateAfterCommit = facilityStateAfterCommit;
        this.acceptedPlacement = acceptedPlacement;
        DestructionFact = destructionFact;
        orderedFactView = Array.AsReadOnly(
            (ICommittedPlacementFact[])orderedFacts.Clone());
    }

    public FacilityState SourceFacilityState { get; }

    public FacilityState FacilityStateAfterCommit { get; }

    public FacilityDestroyedFact? DestructionFact { get; }

    public IReadOnlyList<ICommittedPlacementFact> OrderedFacts => orderedFactView;

    public HypotheticalPlacementResolution Candidate => acceptedPlacement.Candidate;

    public BoardState BoardAfterCommit => acceptedPlacement.BoardAfterCommit;

    public StoneTopologyKey RegisteredTopologyKey =>
        acceptedPlacement.RegisteredTopologyKey;

    public BattleRepetitionHistory HistoryAfterCommit =>
        acceptedPlacement.HistoryAfterCommit;

    public KingCaptureResult KingCaptureResult => acceptedPlacement.KingCaptureResult;

    internal LegalPlacementCommit AcceptedPlacement => acceptedPlacement;
}
