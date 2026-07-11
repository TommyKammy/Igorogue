using System.Globalization;

namespace Igorogue.Domain.Board;

public sealed record InternalPoint
{
    internal InternalPoint(int ix, int iy)
    {
        ValidateCoordinate(ix, nameof(ix));
        ValidateCoordinate(iy, nameof(iy));
        Ix = ix;
        Iy = iy;
    }

    public int Ix { get; }

    public int Iy { get; }

    public override string ToString() =>
        $"({Ix.ToString(CultureInfo.InvariantCulture)},{Iy.ToString(CultureInfo.InvariantCulture)})";

    private static void ValidateCoordinate(int coordinate, string parameterName)
    {
        if (coordinate < 0 || coordinate >= BoardGeometry.AcceptedSize)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                coordinate,
                $"Internal coordinate must be between 0 and {BoardGeometry.AcceptedSize - 1}.");
        }
    }
}
