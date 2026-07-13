using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Cards;

public sealed class QiChangedFact : IBattleFact
{
    private QiChangedFact(
        int oldAmount,
        int newAmount,
        int delta,
        string reasonId,
        string sourceId)
    {
        OldAmount = oldAmount;
        NewAmount = newAmount;
        Delta = delta;
        ReasonId = reasonId;
        SourceId = sourceId;
    }

    public int OldAmount { get; }

    public int NewAmount { get; }

    public int Delta { get; }

    public string ReasonId { get; }

    public string SourceId { get; }

    public static QiChangedFact SpendCardCost(
        int oldAmount,
        int newAmount,
        string cardInstanceId)
    {
        if (oldAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(oldAmount),
                oldAmount,
                "Old qi cannot be negative.");
        }

        if (newAmount < 0 || newAmount > oldAmount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newAmount),
                newAmount,
                "New qi must be between zero and the old amount.");
        }

        return new QiChangedFact(
            oldAmount,
            newAmount,
            checked(newAmount - oldAmount),
            "card_cost",
            StableDomainId.Validate(cardInstanceId, nameof(cardInstanceId)));
    }
}
