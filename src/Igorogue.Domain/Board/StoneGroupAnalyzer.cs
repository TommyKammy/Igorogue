namespace Igorogue.Domain.Board;

public static class StoneGroupAnalyzer
{
    public static StoneGroupAnalysis Analyze(BoardState board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var geometry = board.Geometry;
        var visited = new bool[geometry.PointCount];
        var groupsByCanonicalIndex = new StoneGroup?[geometry.PointCount];
        var groups = new List<StoneGroup>();

        for (var startIndex = 0; startIndex < geometry.PointCount; startIndex++)
        {
            var startPoint = geometry.FromCanonicalIndex(startIndex);
            var startStone = board.StoneAt(startPoint);
            if (startStone is null || visited[startIndex])
            {
                continue;
            }

            var memberMask = new bool[geometry.PointCount];
            var libertyMask = new bool[geometry.PointCount];
            var pending = new Queue<int>();
            visited[startIndex] = true;
            pending.Enqueue(startIndex);

            while (pending.Count > 0)
            {
                var currentIndex = pending.Dequeue();
                var currentPoint = geometry.FromCanonicalIndex(currentIndex);
                memberMask[currentIndex] = true;

                foreach (var neighbour in geometry.GetOrthogonalNeighbours(currentPoint))
                {
                    var neighbourIndex = geometry.ToCanonicalIndex(neighbour);
                    var neighbourStone = board.StoneAt(neighbour);
                    if (neighbourStone is null)
                    {
                        libertyMask[neighbourIndex] = true;
                    }
                    else if (neighbourStone.Color == startStone.Color && !visited[neighbourIndex])
                    {
                        visited[neighbourIndex] = true;
                        pending.Enqueue(neighbourIndex);
                    }
                }
            }

            var group = new StoneGroup(
                startStone.Color,
                MaterializeStones(board, memberMask),
                MaterializePoints(geometry, libertyMask));
            groups.Add(group);

            for (var index = 0; index < memberMask.Length; index++)
            {
                if (memberMask[index])
                {
                    groupsByCanonicalIndex[index] = group;
                }
            }
        }

        groups.Sort((left, right) => geometry.ToCanonicalIndex(left.Anchor)
            .CompareTo(geometry.ToCanonicalIndex(right.Anchor)));
        return new StoneGroupAnalysis(board, groups.ToArray(), groupsByCanonicalIndex);
    }

    private static BoardStone[] MaterializeStones(BoardState board, IReadOnlyList<bool> memberMask)
    {
        var stones = new List<BoardStone>();
        for (var index = 0; index < memberMask.Count; index++)
        {
            if (!memberMask[index])
            {
                continue;
            }

            var stone = board.StoneAt(board.Geometry.FromCanonicalIndex(index))
                ?? throw new InvalidOperationException("Group member must refer to an occupied point.");
            stones.Add(stone);
        }

        return stones.ToArray();
    }

    private static CanonicalPoint[] MaterializePoints(
        BoardGeometry geometry,
        IReadOnlyList<bool> pointMask)
    {
        var points = new List<CanonicalPoint>();
        for (var index = 0; index < pointMask.Count; index++)
        {
            if (pointMask[index])
            {
                points.Add(geometry.FromCanonicalIndex(index));
            }
        }

        return points.ToArray();
    }
}
