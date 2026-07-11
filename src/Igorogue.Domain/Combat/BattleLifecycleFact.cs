namespace Igorogue.Domain.Combat;

public enum BattleEndReason : byte
{
    None = 1,
    WhiteKingCaptured = 2,
    BlackKingCaptured = 3,
    BothKingsCaptured = 4,
    TurnLimit = 5,
}

public static class BattleEndReasonRules
{
    public static string ToReasonId(BattleEndReason reason) => reason switch
    {
        BattleEndReason.None => "none",
        BattleEndReason.WhiteKingCaptured => "white_king_captured",
        BattleEndReason.BlackKingCaptured => "black_king_captured",
        BattleEndReason.BothKingsCaptured => "both_kings_captured",
        BattleEndReason.TurnLimit => "turn_limit",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown battle end reason."),
    };

    public static void ValidateTerminalPair(
        BattleOutcome outcome,
        BattleEndReason reason)
    {
        var valid = (outcome, reason) switch
        {
            (BattleOutcome.PlayerVictory, BattleEndReason.WhiteKingCaptured) => true,
            (BattleOutcome.PlayerDefeat, BattleEndReason.BlackKingCaptured) => true,
            (BattleOutcome.PlayerDefeat, BattleEndReason.BothKingsCaptured) => true,
            (BattleOutcome.PlayerDefeat, BattleEndReason.TurnLimit) => true,
            _ => false,
        };
        if (!valid)
        {
            throw new ArgumentException(
                "Battle outcome and end reason do not form a valid terminal result.");
        }
    }
}

public sealed class CommandRejectedFact : IBattleFact
{
    public CommandRejectedFact(string reasonId)
    {
        ReasonId = BattleFactReason.Validate(reasonId, nameof(reasonId));
    }

    public string ReasonId { get; }
}

public sealed class EnemyPassedFact : IBattleFact
{
    public EnemyPassedFact(int playerTurnIndex)
    {
        if (playerTurnIndex <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(playerTurnIndex),
                playerTurnIndex,
                "Player turn index must be positive.");
        }

        PlayerTurnIndex = playerTurnIndex;
    }

    public int PlayerTurnIndex { get; }
}

public sealed class BattleEndedFact : IBattleFact
{
    public BattleEndedFact(BattleOutcome outcome, BattleEndReason reason)
    {
        BattleEndReasonRules.ValidateTerminalPair(outcome, reason);

        Outcome = outcome;
        Reason = reason;
    }

    public BattleOutcome Outcome { get; }

    public BattleEndReason Reason { get; }

    public string ReasonId => BattleEndReasonRules.ToReasonId(Reason);
}

internal static class BattleFactReason
{
    public static string Validate(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '.' and not '_' and not '-'))
        {
            throw new ArgumentException(
                "Battle fact reason IDs may contain only ASCII letters, digits, '.', '_', or '-'.",
                parameterName);
        }

        return value;
    }
}
