using System.Collections.ObjectModel;

namespace Igorogue.Domain.Board;

public sealed class HypotheticalPlacementResolution
{
    private readonly ReadOnlyCollection<StoneGroup> capturedGroupView;
    private readonly ReadOnlyCollection<PlacementCaptureFact> orderedFactView;

    internal HypotheticalPlacementResolution(
        HypotheticalPlacement sourcePlacement,
        EffectiveLibertySnapshot captureEffectiveLiberties,
        BoardState boardAfterCapture,
        StoneGroup[] capturedGroups,
        StoneGroupAnalysis groupsAfterCapture,
        StoneGroup placedGroupAfterCapture,
        PlacementCaptureFact[] orderedFacts)
    {
        SourcePlacement = sourcePlacement;
        CaptureEffectiveLiberties = captureEffectiveLiberties;
        SourceBoard = sourcePlacement.SourceBoard;
        PlacedStone = sourcePlacement.PlacedStone;
        BoardAfterCapture = boardAfterCapture;
        GroupsAfterCapture = groupsAfterCapture;
        PlacedGroupAfterCapture = placedGroupAfterCapture;
        capturedGroupView = Array.AsReadOnly((StoneGroup[])capturedGroups.Clone());
        orderedFactView = Array.AsReadOnly((PlacementCaptureFact[])orderedFacts.Clone());
        CapturedStoneCount = capturedGroups.Sum(group => group.Stones.Count);
    }

    public BoardState SourceBoard { get; }

    public BoardStone PlacedStone { get; }

    public BoardState BoardAfterCapture { get; }

    public IReadOnlyList<StoneGroup> CapturedGroups => capturedGroupView;

    public StoneGroupAnalysis GroupsAfterCapture { get; }

    public StoneGroup PlacedGroupAfterCapture { get; }

    public IReadOnlyList<PlacementCaptureFact> OrderedFacts => orderedFactView;

    public int CapturedStoneCount { get; }

    public bool SatisfiesTerminalCaptureCondition => capturedGroupView.Count > 0;

    internal HypotheticalPlacement SourcePlacement { get; }

    internal EffectiveLibertySnapshot CaptureEffectiveLiberties { get; }
}
