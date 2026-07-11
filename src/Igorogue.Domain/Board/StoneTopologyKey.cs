namespace Igorogue.Domain.Board;

public sealed class StoneTopologyKey : IEquatable<StoneTopologyKey>
{
    public const string EncodingVersion = "stone-topology-v1";
    public const int CellCount = BoardGeometry.AcceptedSize * BoardGeometry.AcceptedSize;

    private StoneTopologyKey(string canonicalCells)
    {
        CanonicalCells = canonicalCells;
    }

    public string CanonicalCells { get; }

    public static StoneTopologyKey FromBoard(BoardState board)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.Geometry.PointCount != CellCount)
        {
            throw new ArgumentException(
                $"Stone topology requires exactly {CellCount} intersections.",
                nameof(board));
        }

        var cells = new string('.', CellCount).ToCharArray();
        foreach (var stone in board.OccupiedStones)
        {
            cells[board.Geometry.ToCanonicalIndex(stone.Point)] = stone switch
            {
                { Color: StoneColor.Black, IsKing: false } => 'B',
                { Color: StoneColor.White, IsKing: false } => 'W',
                { Color: StoneColor.Black, IsKing: true } => 'K',
                { Color: StoneColor.White, IsKing: true } => 'Q',
                _ => throw new ArgumentOutOfRangeException(
                    nameof(board),
                    stone.Color,
                    "Unknown stone topology value."),
            };
        }

        return new StoneTopologyKey(new string(cells));
    }

    public string ToCanonicalText() => $"{EncodingVersion}:{CanonicalCells}";

    public bool Equals(StoneTopologyKey? other) =>
        other is not null &&
        StringComparer.Ordinal.Equals(CanonicalCells, other.CanonicalCells);

    public override bool Equals(object? obj) => Equals(obj as StoneTopologyKey);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(CanonicalCells);

    public override string ToString() => ToCanonicalText();
}
