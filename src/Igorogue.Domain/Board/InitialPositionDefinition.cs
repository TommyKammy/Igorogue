using System.Collections.ObjectModel;

namespace Igorogue.Domain.Board;

public sealed class InitialPositionDefinition
{
    private readonly Dictionary<CanonicalPoint, InitialStonePlacement> stonesByPoint;
    private readonly ReadOnlyCollection<InitialStonePlacement> stoneView;

    private InitialPositionDefinition(
        BoardGeometry geometry,
        string id,
        InitialStonePlacement[] stones)
    {
        Geometry = geometry;
        Id = id;
        stoneView = Array.AsReadOnly(stones);
        stonesByPoint = stones.ToDictionary(stone => stone.Point);
    }

    public BoardGeometry Geometry { get; }

    public string Id { get; }

    public IReadOnlyList<InitialStonePlacement> Stones => stoneView;

    public static InitialPositionDefinition Create(
        BoardGeometry geometry,
        string id,
        IEnumerable<InitialStonePlacement> stones)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ValidateId(id);
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

        for (var index = 1; index < ordered.Length; index++)
        {
            if (ordered[index - 1].Point == ordered[index].Point)
            {
                throw new ArgumentException(
                    $"Initial position contains duplicate point {ordered[index].Point}.",
                    nameof(stones));
            }
        }

        return new InitialPositionDefinition(geometry, id, ordered);
    }

    public bool IsOccupied(CanonicalPoint point)
    {
        Geometry.ToCanonicalIndex(point);
        return stonesByPoint.ContainsKey(point);
    }

    public InitialStonePlacement? StoneAt(CanonicalPoint point)
    {
        Geometry.ToCanonicalIndex(point);
        return stonesByPoint.GetValueOrDefault(point);
    }

    public bool HasRoleAwarePointReflectionSymmetry() =>
        stoneView.All(stone =>
        {
            var counterpart = StoneAt(Geometry.Reflect(stone.Point));
            return counterpart is not null &&
                counterpart.Color == Opposite(stone.Color) &&
                counterpart.Role == stone.Role;
        });

    private static StoneColor Opposite(StoneColor color) => color switch
    {
        StoneColor.Black => StoneColor.White,
        StoneColor.White => StoneColor.Black,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown stone color."),
    };

    private static void ValidateId(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
    }
}

public sealed record InitialStonePlacement
{
    public InitialStonePlacement(
        StoneColor color,
        InitialStoneRole role,
        CanonicalPoint point)
    {
        if (color is not StoneColor.Black and not StoneColor.White)
        {
            throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown stone color.");
        }

        if (role is not InitialStoneRole.King and not InitialStoneRole.Guard)
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown initial stone role.");
        }

        ArgumentNullException.ThrowIfNull(point);
        Color = color;
        Role = role;
        Point = point;
    }

    public StoneColor Color { get; }

    public InitialStoneRole Role { get; }

    public CanonicalPoint Point { get; }
}

public enum StoneColor : byte
{
    Black = 1,
    White = 2,
}

public enum InitialStoneRole : byte
{
    King = 1,
    Guard = 2,
}
