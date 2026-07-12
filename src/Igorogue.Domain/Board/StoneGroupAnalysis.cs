using System.Collections.ObjectModel;

namespace Igorogue.Domain.Board;

public sealed class StoneGroupAnalysis
{
    private readonly BoardGeometry geometry;
    private readonly ReadOnlyCollection<StoneGroup> groupView;
    private readonly StoneGroup?[] groupsByCanonicalIndex;

    internal StoneGroupAnalysis(
        BoardState sourceBoard,
        StoneGroup[] groups,
        StoneGroup?[] groupsByCanonicalIndex)
    {
        ArgumentNullException.ThrowIfNull(sourceBoard);
        SourceBoard = sourceBoard;
        geometry = sourceBoard.Geometry;
        groupView = Array.AsReadOnly((StoneGroup[])groups.Clone());
        this.groupsByCanonicalIndex = (StoneGroup?[])groupsByCanonicalIndex.Clone();
    }

    public IReadOnlyList<StoneGroup> Groups => groupView;

    public BoardState SourceBoard { get; }

    internal BoardGeometry Geometry => geometry;

    public StoneGroup? GroupAt(CanonicalPoint point)
    {
        var index = geometry.ToCanonicalIndex(point);
        return groupsByCanonicalIndex[index];
    }
}
