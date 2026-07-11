namespace Igorogue.Domain.Board;

public abstract class PlacementCaptureFact
{
    internal PlacementCaptureFact()
    {
    }
}

public sealed class StonePlacedFact : PlacementCaptureFact
{
    internal StonePlacedFact(BoardStone stone)
    {
        Stone = stone;
    }

    public BoardStone Stone { get; }
}

public sealed class GroupCapturedFact : PlacementCaptureFact
{
    internal GroupCapturedFact(StoneGroup capturedGroup, StoneColor capturingColor)
    {
        CapturedGroup = capturedGroup;
        CapturingColor = capturingColor;
    }

    public StoneGroup CapturedGroup { get; }

    public StoneColor CapturingColor { get; }

    public bool ContainsKing => CapturedGroup.Stones.Any(stone => stone.IsKing);
}
