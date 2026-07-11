namespace Igorogue.Domain.Board;

public sealed record BoardStone
{
    public BoardStone(
        StoneColor color,
        bool isKing,
        CanonicalPoint point)
    {
        if (color is not StoneColor.Black and not StoneColor.White)
        {
            throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown stone color.");
        }

        ArgumentNullException.ThrowIfNull(point);
        Color = color;
        IsKing = isKing;
        Point = point;
    }

    public StoneColor Color { get; }

    public bool IsKing { get; }

    public CanonicalPoint Point { get; }
}
