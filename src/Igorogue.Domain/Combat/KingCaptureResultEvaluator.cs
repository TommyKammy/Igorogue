using Igorogue.Domain.Board;

namespace Igorogue.Domain.Combat;

internal static class KingCaptureResultEvaluator
{
    internal static KingCaptureResult EvaluateAtomicCapture(
        IReadOnlyList<StoneGroup> capturedGroups)
    {
        ArgumentNullException.ThrowIfNull(capturedGroups);

        var blackKingCaptured = false;
        var whiteKingCaptured = false;
        for (var index = 0; index < capturedGroups.Count; index++)
        {
            var group = capturedGroups[index]
                ?? throw new ArgumentNullException(nameof(capturedGroups));
            if (!group.Stones.Any(stone => stone.IsKing))
            {
                continue;
            }

            switch (group.Color)
            {
                case StoneColor.Black:
                    blackKingCaptured = true;
                    break;
                case StoneColor.White:
                    whiteKingCaptured = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(capturedGroups),
                        group.Color,
                        "Unknown captured king color.");
            }
        }

        return new KingCaptureResult(
            blackKingCaptured,
            whiteKingCaptured);
    }
}
