using System.Collections.ObjectModel;

namespace Igorogue.Domain.Content;

public enum CardRarity : byte
{
    Starter = 1,
    Common = 2,
    Uncommon = 3,
    Rare = 4,
    Curse = 5,
}

public enum CardContentType : byte
{
    Stone = 1,
    Technique = 2,
    Territory = 3,
    Catalyst = 4,
    Curse = 5,
}

public enum CardTargetKind : byte
{
    None = 0,
    FriendlyGroup = 1,
    BlackTerritoryEmpty = 2,
}

public enum CardPlacementTag : byte
{
    Contact = 1,
    Edge = 2,
    Frontline = 3,
    Invasion = 4,
    Jump = 5,
    Terminal = 6,
}

public enum CardOperationKind : byte
{
    BuildFacility = 1,
    DrawIfRealLibertiesAtLeast = 2,
    DrawIfTargetAtari = 3,
    GainQiIfEnemyAtari = 4,
    PlaceStone = 5,
    ReserveDraw = 6,
    TemporaryLiberty = 7,
}

public enum StoneContentKind : byte
{
    Basic = 1,
    Lure = 2,
}

public enum TemporaryLibertyDurationKind : byte
{
    EnemyTurnEnd = 1,
}

public enum TemporaryLibertyTiming : byte
{
    FirstEnemyTurnEndAtOrAfterGrant = 1,
}

public enum TemporaryLibertyStacking : byte
{
    AdditivePerEffectInstance = 1,
}

public abstract class CardOperationDefinition
{
    protected CardOperationDefinition(CardOperationKind kind)
    {
        Kind = kind;
    }

    public CardOperationKind Kind { get; }
}

public sealed class PlaceStoneOperationDefinition : CardOperationDefinition
{
    public PlaceStoneOperationDefinition(StoneContentKind stoneKind)
        : base(CardOperationKind.PlaceStone)
    {
        if (!Enum.IsDefined(stoneKind))
        {
            throw new ArgumentOutOfRangeException(nameof(stoneKind), stoneKind, "Unknown stone kind.");
        }

        StoneKind = stoneKind;
    }

    public StoneContentKind StoneKind { get; }
}

public sealed class DrawIfRealLibertiesAtLeastOperationDefinition : CardOperationDefinition
{
    public DrawIfRealLibertiesAtLeastOperationDefinition(int minimumRealLiberties, int cards)
        : base(CardOperationKind.DrawIfRealLibertiesAtLeast)
    {
        MinimumRealLiberties = Positive(
            minimumRealLiberties,
            nameof(minimumRealLiberties),
            "Minimum real liberties");
        Cards = Positive(cards, nameof(cards), "Draw count");
    }

    public int MinimumRealLiberties { get; }

    public int Cards { get; }

    private static int Positive(int value, string parameterName, string label)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{label} must be positive.");
        }

        return value;
    }
}

public sealed class GainQiIfEnemyAtariOperationDefinition : CardOperationDefinition
{
    public GainQiIfEnemyAtariOperationDefinition(int amount)
        : base(CardOperationKind.GainQiIfEnemyAtari)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Qi gain must be positive.");
        }

        Amount = amount;
    }

    public int Amount { get; }
}

public sealed class TemporaryLibertyOperationDefinition : CardOperationDefinition
{
    public TemporaryLibertyOperationDefinition(
        int amount,
        TemporaryLibertyDurationKind durationKind,
        TemporaryLibertyTiming timing,
        TemporaryLibertyStacking stacking)
        : base(CardOperationKind.TemporaryLiberty)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Temporary liberty amount must be positive.");
        }

        if (!Enum.IsDefined(durationKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationKind),
                durationKind,
                "Unknown temporary-liberty duration kind.");
        }

        if (!Enum.IsDefined(timing))
        {
            throw new ArgumentOutOfRangeException(
                nameof(timing),
                timing,
                "Unknown temporary-liberty timing.");
        }

        if (!Enum.IsDefined(stacking))
        {
            throw new ArgumentOutOfRangeException(
                nameof(stacking),
                stacking,
                "Unknown temporary-liberty stacking rule.");
        }

        Amount = amount;
        DurationKind = durationKind;
        Timing = timing;
        Stacking = stacking;
    }

    public int Amount { get; }

    public TemporaryLibertyDurationKind DurationKind { get; }

    public TemporaryLibertyTiming Timing { get; }

    public TemporaryLibertyStacking Stacking { get; }
}

public sealed class DrawIfTargetAtariOperationDefinition : CardOperationDefinition
{
    public DrawIfTargetAtariOperationDefinition(int cards)
        : base(CardOperationKind.DrawIfTargetAtari)
    {
        if (cards <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cards), cards, "Draw count must be positive.");
        }

        Cards = cards;
    }

    public int Cards { get; }
}

public sealed class BuildFacilityOperationDefinition : CardOperationDefinition
{
    public BuildFacilityOperationDefinition(string facilityContentId)
        : base(CardOperationKind.BuildFacility)
    {
        FacilityContentId = StableDomainId.Validate(facilityContentId, nameof(facilityContentId));
    }

    public string FacilityContentId { get; }
}

public sealed class ReserveDrawOperationDefinition : CardOperationDefinition
{
    public ReserveDrawOperationDefinition(int cards)
        : base(CardOperationKind.ReserveDraw)
    {
        if (cards <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cards), cards, "Reserved draw must be positive.");
        }

        Cards = cards;
    }

    public int Cards { get; }
}

public sealed class CardContentDefinition
{
    private readonly ReadOnlyCollection<CardPlacementTag> placementTagView;
    private readonly ReadOnlyCollection<CardOperationDefinition> effectView;
    private readonly ReadOnlyCollection<CardOperationDefinition> onCapturedView;

    private CardContentDefinition(
        string id,
        CardRarity rarity,
        int cost,
        CardContentType type,
        CardTargetKind target,
        CardPlacementTag[] placementTags,
        CardOperationDefinition[] effects,
        CardOperationDefinition[] onCaptured)
    {
        Id = id;
        Rarity = rarity;
        Cost = cost;
        Type = type;
        Target = target;
        placementTagView = Array.AsReadOnly(placementTags);
        effectView = Array.AsReadOnly(effects);
        onCapturedView = Array.AsReadOnly(onCaptured);
    }

    public string Id { get; }

    public CardRarity Rarity { get; }

    public int Cost { get; }

    public CardContentType Type { get; }

    public CardTargetKind Target { get; }

    public IReadOnlyList<CardPlacementTag> PlacementTags => placementTagView;

    public IReadOnlyList<CardOperationDefinition> Effects => effectView;

    public IReadOnlyList<CardOperationDefinition> OnCaptured => onCapturedView;

    public static CardContentDefinition Create(
        string id,
        CardRarity rarity,
        int cost,
        CardContentType type,
        CardTargetKind target,
        IEnumerable<CardPlacementTag> placementTags,
        IEnumerable<CardOperationDefinition> effects,
        IEnumerable<CardOperationDefinition>? onCaptured = null)
    {
        var stableId = StableDomainId.Validate(id, nameof(id));
        if (!stableId.StartsWith("card_", StringComparison.Ordinal))
        {
            throw new ArgumentException("Card content IDs must start with 'card_'.", nameof(id));
        }

        if (!Enum.IsDefined(rarity))
        {
            throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unknown card rarity.");
        }

        if (cost is < 0 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), cost, "Card cost must be between 0 and 9.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown card type.");
        }

        if (!Enum.IsDefined(target))
        {
            throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown card target.");
        }

        ArgumentNullException.ThrowIfNull(placementTags);
        var canonicalTags = placementTags.ToArray();
        foreach (var tag in canonicalTags)
        {
            if (!Enum.IsDefined(tag))
            {
                throw new ArgumentOutOfRangeException(nameof(placementTags), tag, "Unknown placement tag.");
            }
        }

        if (canonicalTags.Distinct().Count() != canonicalTags.Length)
        {
            throw new ArgumentException("Card placement tags must be unique.", nameof(placementTags));
        }

        Array.Sort(canonicalTags);

        ArgumentNullException.ThrowIfNull(effects);
        var orderedEffects = effects.ToArray();
        if (orderedEffects.Length == 0)
        {
            throw new ArgumentException("Card content must define at least one effect.", nameof(effects));
        }

        ValidateOperations(orderedEffects, nameof(effects));

        var orderedOnCaptured = onCaptured?.ToArray() ?? [];
        ValidateOperations(orderedOnCaptured, nameof(onCaptured));

        return new CardContentDefinition(
            stableId,
            rarity,
            cost,
            type,
            target,
            canonicalTags,
            orderedEffects,
            orderedOnCaptured);
    }

    private static void ValidateOperations(
        IEnumerable<CardOperationDefinition> operations,
        string parameterName)
    {
        foreach (var operation in operations)
        {
            if (operation is null)
            {
                throw new ArgumentException("Card operations cannot contain null.", parameterName);
            }
        }
    }
}
