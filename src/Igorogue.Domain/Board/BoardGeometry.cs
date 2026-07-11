using System.Collections.ObjectModel;

namespace Igorogue.Domain.Board;

public sealed class BoardGeometry
{
    // CanonicalPoint is a versioned 7x7 product contract. A size change requires
    // coordinated specification, content, replay, and fixture supersession.
    public const int AcceptedSize = 7;

    private readonly CanonicalPoint[] canonicalPoints;
    private readonly InternalPoint[] internalPoints;
    private readonly ReadOnlyCollection<CanonicalPoint> canonicalPointView;
    private readonly ReadOnlyCollection<CanonicalPoint>[] neighbourViews;

    private BoardGeometry(int size)
    {
        Size = size;
        PointCount = checked(size * size);
        canonicalPoints = new CanonicalPoint[PointCount];
        internalPoints = new InternalPoint[PointCount];

        for (var index = 0; index < PointCount; index++)
        {
            var ix = index % Size;
            var iy = index / Size;
            canonicalPoints[index] = new CanonicalPoint(ix + 1, iy + 1);
            internalPoints[index] = new InternalPoint(ix, iy);
        }

        canonicalPointView = Array.AsReadOnly(canonicalPoints);
        neighbourViews = CreateNeighbourViews();
    }

    public int Size { get; }

    public int PointCount { get; }

    public IReadOnlyList<CanonicalPoint> CanonicalPoints => canonicalPointView;

    public static BoardGeometry Create(int size)
    {
        if (size != AcceptedSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                size,
                $"The current coordinate contract requires board size {AcceptedSize}.");
        }

        return new BoardGeometry(size);
    }

    public CanonicalPoint CreateCanonicalPoint(int x, int y)
    {
        ValidateCanonicalCoordinate(x, nameof(x));
        ValidateCanonicalCoordinate(y, nameof(y));
        return canonicalPoints[CanonicalIndexUnchecked(x, y)];
    }

    public InternalPoint CreateInternalPoint(int ix, int iy)
    {
        ValidateInternalCoordinate(ix, nameof(ix));
        ValidateInternalCoordinate(iy, nameof(iy));
        return internalPoints[InternalIndexUnchecked(ix, iy)];
    }

    public InternalPoint ToInternal(CanonicalPoint point)
    {
        var index = ValidateCanonicalPoint(point);
        return internalPoints[index];
    }

    public CanonicalPoint ToCanonical(InternalPoint point)
    {
        var index = ValidateInternalPoint(point);
        return canonicalPoints[index];
    }

    public int ToCanonicalIndex(CanonicalPoint point) => ValidateCanonicalPoint(point);

    public CanonicalPoint FromCanonicalIndex(int index)
    {
        if (index < 0 || index >= PointCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Canonical index must be between 0 and {PointCount - 1}.");
        }

        return canonicalPoints[index];
    }

    public CanonicalPoint Reflect(CanonicalPoint point)
    {
        ValidateCanonicalPoint(point);
        return CreateCanonicalPoint(Size + 1 - point.X, Size + 1 - point.Y);
    }

    public IReadOnlyList<CanonicalPoint> GetOrthogonalNeighbours(CanonicalPoint point)
    {
        var index = ValidateCanonicalPoint(point);
        return neighbourViews[index];
    }

    public int CanonicalYFromDiagramRow(int row)
    {
        ValidateInternalCoordinate(row, nameof(row));
        return Size - row;
    }

    private ReadOnlyCollection<CanonicalPoint>[] CreateNeighbourViews()
    {
        var views = new ReadOnlyCollection<CanonicalPoint>[PointCount];
        foreach (var point in canonicalPoints)
        {
            var neighbours = new List<CanonicalPoint>(4);
            if (point.Y > 1)
            {
                neighbours.Add(CreateCanonicalPoint(point.X, point.Y - 1));
            }

            if (point.X > 1)
            {
                neighbours.Add(CreateCanonicalPoint(point.X - 1, point.Y));
            }

            if (point.X < Size)
            {
                neighbours.Add(CreateCanonicalPoint(point.X + 1, point.Y));
            }

            if (point.Y < Size)
            {
                neighbours.Add(CreateCanonicalPoint(point.X, point.Y + 1));
            }

            views[CanonicalIndexUnchecked(point.X, point.Y)] = neighbours.AsReadOnly();
        }

        return views;
    }

    private int ValidateCanonicalPoint(CanonicalPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);
        ValidateCanonicalCoordinate(point.X, nameof(point));
        ValidateCanonicalCoordinate(point.Y, nameof(point));
        return CanonicalIndexUnchecked(point.X, point.Y);
    }

    private int ValidateInternalPoint(InternalPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);
        ValidateInternalCoordinate(point.Ix, nameof(point));
        ValidateInternalCoordinate(point.Iy, nameof(point));
        return InternalIndexUnchecked(point.Ix, point.Iy);
    }

    private void ValidateCanonicalCoordinate(int coordinate, string parameterName)
    {
        if (coordinate < 1 || coordinate > Size)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                coordinate,
                $"Canonical coordinate must be between 1 and {Size}.");
        }
    }

    private void ValidateInternalCoordinate(int coordinate, string parameterName)
    {
        if (coordinate < 0 || coordinate >= Size)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                coordinate,
                $"Internal coordinate must be between 0 and {Size - 1}.");
        }
    }

    private int CanonicalIndexUnchecked(int x, int y) => ((y - 1) * Size) + (x - 1);

    private int InternalIndexUnchecked(int ix, int iy) => (iy * Size) + ix;
}
