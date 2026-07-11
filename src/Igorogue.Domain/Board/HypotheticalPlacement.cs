using System.Collections.ObjectModel;

namespace Igorogue.Domain.Board;

public sealed class HypotheticalPlacement
{
    private readonly ReadOnlyCollection<StoneGroup> adjacentOpponentGroupView;

    internal HypotheticalPlacement(
        BoardStone placedStone,
        BoardState boardAfterPlacement,
        StoneGroupAnalysis groupsAfterPlacement,
        StoneGroup[] adjacentOpponentGroups)
    {
        PlacedStone = placedStone;
        BoardAfterPlacement = boardAfterPlacement;
        GroupsAfterPlacement = groupsAfterPlacement;
        adjacentOpponentGroupView = Array.AsReadOnly(
            (StoneGroup[])adjacentOpponentGroups.Clone());
    }

    public BoardStone PlacedStone { get; }

    public BoardState BoardAfterPlacement { get; }

    public StoneGroupAnalysis GroupsAfterPlacement { get; }

    public IReadOnlyList<StoneGroup> AdjacentOpponentGroups => adjacentOpponentGroupView;
}
