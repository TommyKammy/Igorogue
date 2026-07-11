using System.Collections.ObjectModel;

namespace Igorogue.Domain.Board;

public sealed class HypotheticalPlacement
{
    private readonly ReadOnlyCollection<StoneGroup> adjacentOpponentGroupView;

    internal HypotheticalPlacement(
        BoardState sourceBoard,
        BoardStone placedStone,
        BoardState boardAfterPlacement,
        StoneGroupAnalysis groupsAfterPlacement,
        StoneGroup[] adjacentOpponentGroups)
    {
        SourceBoard = sourceBoard;
        PlacedStone = placedStone;
        BoardAfterPlacement = boardAfterPlacement;
        GroupsAfterPlacement = groupsAfterPlacement;
        adjacentOpponentGroupView = Array.AsReadOnly(
            (StoneGroup[])adjacentOpponentGroups.Clone());
    }

    public BoardState SourceBoard { get; }

    public BoardStone PlacedStone { get; }

    public BoardState BoardAfterPlacement { get; }

    public StoneGroupAnalysis GroupsAfterPlacement { get; }

    public IReadOnlyList<StoneGroup> AdjacentOpponentGroups => adjacentOpponentGroupView;
}
