namespace Igorogue.Domain.Combat;

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
    public BattleEndedFact(BattleOutcome outcome, string reasonId)
    {
        if (outcome is not BattleOutcome.PlayerVictory and not BattleOutcome.PlayerDefeat)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "A battle-ended fact requires a terminal outcome.");
        }

        Outcome = outcome;
        ReasonId = BattleFactReason.Validate(reasonId, nameof(reasonId));
    }

    public BattleOutcome Outcome { get; }

    public string ReasonId { get; }
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
