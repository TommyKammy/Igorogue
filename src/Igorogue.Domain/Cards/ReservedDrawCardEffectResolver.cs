using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Cards;

public sealed class ReservedDrawCardEffectResolution
{
    internal ReservedDrawCardEffectResolution(
        ClosedWindowResourceState source,
        ClosedWindowResourceState stateAfter,
        TurnReservedDrawChangedFact fact)
    {
        Source = source;
        StateAfter = stateAfter;
        Fact = fact;
    }

    public ClosedWindowResourceState Source { get; }

    public ClosedWindowResourceState StateAfter { get; }

    public TurnReservedDrawChangedFact Fact { get; }
}

public static class ReservedDrawCardEffectResolver
{
    public static ReservedDrawCardEffectResolution Apply(
        ClosedWindowResourceState source,
        string sourceCardInstanceId,
        int amount)
    {
        ArgumentNullException.ThrowIfNull(source);
        var sourceId = StableDomainId.Validate(
            sourceCardInstanceId,
            nameof(sourceCardInstanceId));
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Reserved draw from a card effect must be positive.");
        }

        var stateAfter = source.AddReservedDraw(amount);
        var triggerId = $"card.{sourceId}";
        return new ReservedDrawCardEffectResolution(
            source,
            stateAfter,
            new TurnReservedDrawChangedFact(
                triggerId,
                $"{triggerId}:reserve_draw_{amount}",
                source.TurnReservedDraw,
                stateAfter.TurnReservedDraw,
                amount));
    }
}
