using System.Collections.ObjectModel;

namespace Igorogue.Domain.Board;

public sealed class HypotheticalPlacementResolution
{
    private readonly ReadOnlyCollection<StoneGroup> capturedGroupView;
    private readonly ReadOnlyCollection<PlacementCaptureFact> orderedFactView;

    internal HypotheticalPlacementResolution(
        BoardStone placedStone,
        BoardState boardAfterCapture,
        StoneGroup[] capturedGroups,
        StoneGroupAnalysis groupsAfterCapture,
        StoneGroup placedGroupAfterCapture,
        PlacementCaptureFact[] orderedFacts)
    {
        PlacedStone = placedStone;
        BoardAfterCapture = boardAfterCapture;
        GroupsAfterCapture = groupsAfterCapture;
        PlacedGroupAfterCapture = placedGroupAfterCapture;
        capturedGroupView = Array.AsReadOnly((StoneGroup[])capturedGroups.Clone());
        orderedFactView = Array.AsReadOnly((PlacementCaptureFact[])orderedFacts.Clone());
        CapturedStoneCount = capturedGroups.Sum(group => group.Stones.Count);
    }

    public BoardStone PlacedStone { get; }

    public BoardState BoardAfterCapture { get; }

    public IReadOnlyList<StoneGroup> CapturedGroups => capturedGroupView;

    public StoneGroupAnalysis GroupsAfterCapture { get; }

    public StoneGroup PlacedGroupAfterCapture { get; }

    public IReadOnlyList<PlacementCaptureFact> OrderedFacts => orderedFactView;

    public int CapturedStoneCount { get; }
}
