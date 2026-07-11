using System.Collections.ObjectModel;

namespace Igorogue.Domain.Board;

public sealed class TerritoryRegion
{
    private readonly ReadOnlyCollection<CanonicalPoint> pointView;

    internal TerritoryRegion(
        TerritoryOwner owner,
        CanonicalPoint[] canonicalPoints)
    {
        if (owner is not TerritoryOwner.Neutral and
            not TerritoryOwner.Black and
            not TerritoryOwner.White)
        {
            throw new ArgumentOutOfRangeException(nameof(owner), owner, "Unknown territory owner.");
        }

        ArgumentNullException.ThrowIfNull(canonicalPoints);
        if (canonicalPoints.Length == 0)
        {
            throw new ArgumentException(
                "A territory region must contain at least one empty point.",
                nameof(canonicalPoints));
        }

        var points = (CanonicalPoint[])canonicalPoints.Clone();
        for (var index = 0; index < points.Length; index++)
        {
            ArgumentNullException.ThrowIfNull(points[index]);
            if (index > 0 && points[index - 1].CompareTo(points[index]) >= 0)
            {
                throw new ArgumentException(
                    "Territory region points must be unique and in canonical order.",
                    nameof(canonicalPoints));
            }
        }

        Owner = owner;
        Anchor = points[0];
        pointView = Array.AsReadOnly(points);
    }

    public TerritoryOwner Owner { get; }

    public CanonicalPoint Anchor { get; }

    public IReadOnlyList<CanonicalPoint> Points => pointView;

    public int Size => pointView.Count;
}
