namespace Igorogue.Domain.Board;

public static class TerritoryAnalyzer
{
    public static TerritoryAnalysis Analyze(BoardState board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var geometry = board.Geometry;
        var visited = new bool[geometry.PointCount];
        var regionsByCanonicalIndex = new TerritoryRegion?[geometry.PointCount];
        var regions = new List<TerritoryRegion>();

        for (var startIndex = 0; startIndex < geometry.PointCount; startIndex++)
        {
            var startPoint = geometry.FromCanonicalIndex(startIndex);
            if (visited[startIndex] || !board.IsEmpty(startPoint))
            {
                continue;
            }

            var memberMask = new bool[geometry.PointCount];
            var pending = new Queue<int>();
            var touchesBlack = false;
            var touchesWhite = false;
            visited[startIndex] = true;
            pending.Enqueue(startIndex);

            while (pending.Count > 0)
            {
                var currentIndex = pending.Dequeue();
                var currentPoint = geometry.FromCanonicalIndex(currentIndex);
                memberMask[currentIndex] = true;

                foreach (var neighbour in geometry.GetOrthogonalNeighbours(currentPoint))
                {
                    var neighbourStone = board.StoneAt(neighbour);
                    if (neighbourStone is null)
                    {
                        var neighbourIndex = geometry.ToCanonicalIndex(neighbour);
                        if (!visited[neighbourIndex])
                        {
                            visited[neighbourIndex] = true;
                            pending.Enqueue(neighbourIndex);
                        }

                        continue;
                    }

                    switch (neighbourStone.Color)
                    {
                        case StoneColor.Black:
                            touchesBlack = true;
                            break;
                        case StoneColor.White:
                            touchesWhite = true;
                            break;
                        default:
                            throw new InvalidOperationException("Board contains an unknown stone color.");
                    }
                }
            }

            var region = new TerritoryRegion(
                ResolveOwner(touchesBlack, touchesWhite),
                MaterializePoints(geometry, memberMask));
            regions.Add(region);

            for (var index = 0; index < memberMask.Length; index++)
            {
                if (memberMask[index])
                {
                    regionsByCanonicalIndex[index] = region;
                }
            }
        }

        regions.Sort((left, right) => geometry.ToCanonicalIndex(left.Anchor)
            .CompareTo(geometry.ToCanonicalIndex(right.Anchor)));
        return new TerritoryAnalysis(board, regions.ToArray(), regionsByCanonicalIndex);
    }

    private static TerritoryOwner ResolveOwner(bool touchesBlack, bool touchesWhite) =>
        (touchesBlack, touchesWhite) switch
        {
            (true, false) => TerritoryOwner.Black,
            (false, true) => TerritoryOwner.White,
            _ => TerritoryOwner.Neutral,
        };

    private static CanonicalPoint[] MaterializePoints(
        BoardGeometry geometry,
        IReadOnlyList<bool> memberMask)
    {
        var points = new List<CanonicalPoint>();
        for (var index = 0; index < memberMask.Count; index++)
        {
            if (memberMask[index])
            {
                points.Add(geometry.FromCanonicalIndex(index));
            }
        }

        return points.ToArray();
    }
}
