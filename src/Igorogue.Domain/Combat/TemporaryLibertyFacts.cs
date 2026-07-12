using Igorogue.Domain.Board;

namespace Igorogue.Domain.Combat;

public sealed class TemporaryLibertyGrantedFact : IBattleFact
{
    internal TemporaryLibertyGrantedFact(
        TemporaryLibertyEffect effect,
        CanonicalPoint targetGroupAnchor)
    {
        Effect = effect;
        TargetGroupAnchor = targetGroupAnchor;
    }

    public TemporaryLibertyEffect Effect { get; }

    public CanonicalPoint TargetGroupAnchor { get; }
}

public enum TemporaryLibertyRemovalReason : byte
{
    CarrierRemoved = 1,
}

public sealed class TemporaryLibertyRemovedFact : IBattleFact
{
    internal TemporaryLibertyRemovedFact(
        TemporaryLibertyEffect effect,
        TemporaryLibertyRemovalReason reason)
    {
        Effect = effect;
        Reason = reason;
    }

    public TemporaryLibertyEffect Effect { get; }

    public TemporaryLibertyRemovalReason Reason { get; }

    public string ReasonId => Reason switch
    {
        TemporaryLibertyRemovalReason.CarrierRemoved => "carrier_removed",
        _ => throw new InvalidOperationException("Unknown temporary liberty removal reason."),
    };
}

public sealed class TemporaryLibertyExpirySweepStartedFact : IBattleFact
{
    internal TemporaryLibertyExpirySweepStartedFact(int enemyTurnIndex)
    {
        EnemyTurnIndex = enemyTurnIndex;
    }

    public int EnemyTurnIndex { get; }
}

public sealed class TemporaryLibertyExpiredFact : IBattleFact
{
    internal TemporaryLibertyExpiredFact(TemporaryLibertyEffect effect)
    {
        Effect = effect;
    }

    public TemporaryLibertyEffect Effect { get; }
}

public sealed class TemporaryLibertyGroupCapturedFact : IBattleFact
{
    internal TemporaryLibertyGroupCapturedFact(StoneGroup capturedGroup)
    {
        CapturedGroup = capturedGroup;
        CapturingColor = capturedGroup.Color switch
        {
            StoneColor.Black => StoneColor.White,
            StoneColor.White => StoneColor.Black,
            _ => throw new ArgumentOutOfRangeException(
                nameof(capturedGroup),
                capturedGroup.Color,
                "Unknown captured group color."),
        };
    }

    public StoneGroup CapturedGroup { get; }

    public StoneColor CapturingColor { get; }

    public bool ContainsKing => CapturedGroup.Stones.Any(stone => stone.IsKing);

    public string ReasonId => "temporary_liberty_expired";
}

public sealed class TemporaryLibertyKingGateFact : IBattleFact
{
    internal TemporaryLibertyKingGateFact(KingCaptureResult result)
    {
        if (!result.HasKingCapture)
        {
            throw new ArgumentException(
                "Temporary-liberty king gate facts require a captured king.",
                nameof(result));
        }

        Result = result;
    }

    public KingCaptureResult Result { get; }
}

public sealed class CaptureBenefitSuppressedFact : IBattleFact
{
    internal CaptureBenefitSuppressedFact(string reasonId)
    {
        ReasonId = BattleFactReason.Validate(reasonId, nameof(reasonId));
    }

    public string ReasonId { get; }
}

public sealed class TemporaryLibertyExpirySweepResolvedFact : IBattleFact
{
    internal TemporaryLibertyExpirySweepResolvedFact(
        int enemyTurnIndex,
        int capturedGroupCount,
        bool terminal)
    {
        EnemyTurnIndex = enemyTurnIndex;
        CapturedGroupCount = capturedGroupCount;
        Terminal = terminal;
    }

    public int EnemyTurnIndex { get; }

    public int CapturedGroupCount { get; }

    public bool Terminal { get; }
}

public enum TemporaryLibertyCaptureBenefitDisposition : byte
{
    NotApplicable = 1,
    PendingNonTerminalPipeline = 2,
    SuppressedByTerminalKingCapture = 3,
}
