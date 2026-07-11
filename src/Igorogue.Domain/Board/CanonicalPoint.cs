using System.Globalization;

namespace Igorogue.Domain.Board;

public sealed record CanonicalPoint : IComparable<CanonicalPoint>
{
    internal CanonicalPoint(int x, int y)
    {
        ValidateCoordinate(x, nameof(x));
        ValidateCoordinate(y, nameof(y));
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public int CompareTo(CanonicalPoint? other)
    {
        if (other is null)
        {
            return 1;
        }

        var yComparison = Y.CompareTo(other.Y);
        return yComparison != 0 ? yComparison : X.CompareTo(other.X);
    }

    public override string ToString() =>
        $"({X.ToString(CultureInfo.InvariantCulture)},{Y.ToString(CultureInfo.InvariantCulture)})";

    private static void ValidateCoordinate(int coordinate, string parameterName)
    {
        if (coordinate < 1 || coordinate > BoardGeometry.AcceptedSize)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                coordinate,
                $"Canonical coordinate must be between 1 and {BoardGeometry.AcceptedSize}.");
        }
    }
}
