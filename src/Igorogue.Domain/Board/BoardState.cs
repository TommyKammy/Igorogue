using System.Collections.ObjectModel;

namespace Igorogue.Domain.Board;

public sealed class BoardState
{
    private readonly BoardStone?[] stonesByCanonicalIndex;
    private readonly ReadOnlyCollection<BoardStone> occupiedStoneView;

    private BoardState(
        BoardGeometry geometry,
        BoardStone[] occupiedStones,
        BoardStone?[] stonesByCanonicalIndex)
    {
        Geometry = geometry;
        occupiedStoneView = Array.AsReadOnly(occupiedStones);
        this.stonesByCanonicalIndex = stonesByCanonicalIndex;
    }

    public BoardGeometry Geometry { get; }

    public IReadOnlyList<BoardStone> OccupiedStones => occupiedStoneView;

    public static BoardState Create(
        BoardGeometry geometry,
        IEnumerable<BoardStone> stones)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(stones);

        var ordered = stones.ToArray();
        foreach (var stone in ordered)
        {
            ArgumentNullException.ThrowIfNull(stone);
            geometry.ToCanonicalIndex(stone.Point);
        }

        Array.Sort(
            ordered,
            (left, right) => geometry.ToCanonicalIndex(left.Point)
                .CompareTo(geometry.ToCanonicalIndex(right.Point)));

        var byCanonicalIndex = new BoardStone?[geometry.PointCount];
        foreach (var stone in ordered)
        {
            var index = geometry.ToCanonicalIndex(stone.Point);
            if (byCanonicalIndex[index] is not null)
            {
                throw new ArgumentException(
                    $"Board state contains duplicate point {stone.Point}.",
                    nameof(stones));
            }

            byCanonicalIndex[index] = stone;
        }

        return new BoardState(geometry, ordered, byCanonicalIndex);
    }

    public static BoardState FromInitialPosition(InitialPositionDefinition position)
    {
        ArgumentNullException.ThrowIfNull(position);

        return Create(
            position.Geometry,
            position.Stones.Select(stone => new BoardStone(
                stone.Color,
                stone.Role switch
                {
                    InitialStoneRole.King => true,
                    InitialStoneRole.Guard => false,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(position),
                        stone.Role,
                        "Unknown initial stone role."),
                },
                stone.Point)));
    }

    public BoardStone? StoneAt(CanonicalPoint point)
    {
        var index = Geometry.ToCanonicalIndex(point);
        return stonesByCanonicalIndex[index];
    }

    public bool IsEmpty(CanonicalPoint point) => StoneAt(point) is null;
}
