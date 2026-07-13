namespace Igorogue.Domain.Cards;

/// <summary>
/// A stable identity for one physical card in a battle recipe.
/// Multiple instances may share a content ID, but instance IDs are globally unique in a deck.
/// </summary>
public sealed record BattleCardInstance
{
    public BattleCardInstance(string instanceId, string contentId)
    {
        InstanceId = StableDomainId.Validate(instanceId, nameof(instanceId));
        ContentId = StableDomainId.Validate(contentId, nameof(contentId));
    }

    public string InstanceId { get; }

    public string ContentId { get; }

    public string ToCanonicalText() => $"{InstanceId}:{ContentId}";
}

public enum BattleCardResolutionStatus
{
    Active = 1,
    Resolved = 2,
}

public sealed record ResolvingBattleCard
{
    internal ResolvingBattleCard(
        BattleCardInstance card,
        BattleCardResolutionStatus status)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        Card = card;
        Status = status;
    }

    public BattleCardInstance Card { get; }

    public BattleCardResolutionStatus Status { get; }

    public bool IsActive => Status == BattleCardResolutionStatus.Active;

    public bool IsResolved => Status == BattleCardResolutionStatus.Resolved;
}
