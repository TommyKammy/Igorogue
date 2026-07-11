namespace Igorogue.Domain.Combat;

public sealed record KingCaptureResult
{
    public const string EncodingVersion = "king-capture-result-v1";

    internal KingCaptureResult(
        bool blackKingCaptured,
        bool whiteKingCaptured)
    {
        BlackKingCaptured = blackKingCaptured;
        WhiteKingCaptured = whiteKingCaptured;
        EndReason = (blackKingCaptured, whiteKingCaptured) switch
        {
            (true, true) => BattleEndReason.BothKingsCaptured,
            (true, false) => BattleEndReason.BlackKingCaptured,
            (false, true) => BattleEndReason.WhiteKingCaptured,
            (false, false) => BattleEndReason.None,
        };
        Outcome = blackKingCaptured
            ? BattleOutcome.PlayerDefeat
            : whiteKingCaptured
                ? BattleOutcome.PlayerVictory
                : BattleOutcome.Ongoing;
    }

    public bool BlackKingCaptured { get; }

    public bool WhiteKingCaptured { get; }

    public bool HasKingCapture => BlackKingCaptured || WhiteKingCaptured;

    public bool IsTerminal => Outcome != BattleOutcome.Ongoing;

    public BattleOutcome Outcome { get; }

    public BattleEndReason EndReason { get; }

    public string EndReasonId => BattleEndReasonRules.ToReasonId(EndReason);

    public string OutcomeId => Outcome switch
    {
        BattleOutcome.Ongoing => "ongoing",
        BattleOutcome.PlayerVictory => "win",
        BattleOutcome.PlayerDefeat => "loss",
        _ => throw new InvalidOperationException("Unknown battle outcome."),
    };

    public string ToCanonicalText() =>
        $"{EncodingVersion}:{OutcomeId}:black={Flag(BlackKingCaptured)}:white={Flag(WhiteKingCaptured)}";

    public override string ToString() => ToCanonicalText();

    private static int Flag(bool value) => value ? 1 : 0;
}
