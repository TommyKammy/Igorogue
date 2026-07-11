using System.Collections.ObjectModel;

namespace Igorogue.Domain.Board;

public sealed class StoneGroup
{
    private readonly ReadOnlyCollection<BoardStone> stoneView;
    private readonly ReadOnlyCollection<CanonicalPoint> stonePointView;
    private readonly ReadOnlyCollection<CanonicalPoint> realLibertyView;

    internal StoneGroup(
        StoneColor color,
        BoardStone[] stones,
        CanonicalPoint[] realLiberties)
    {
        if (stones.Length == 0)
        {
            throw new ArgumentException("A stone group must contain at least one stone.", nameof(stones));
        }

        Color = color;
        Anchor = stones[0].Point;
        stoneView = Array.AsReadOnly((BoardStone[])stones.Clone());
        stonePointView = Array.AsReadOnly(stones.Select(stone => stone.Point).ToArray());
        realLibertyView = Array.AsReadOnly((CanonicalPoint[])realLiberties.Clone());
    }

    public StoneColor Color { get; }

    public CanonicalPoint Anchor { get; }

    public IReadOnlyList<BoardStone> Stones => stoneView;

    public IReadOnlyList<CanonicalPoint> StonePoints => stonePointView;

    public IReadOnlyList<CanonicalPoint> RealLiberties => realLibertyView;

    public int RealLibertyCount => realLibertyView.Count;
}
