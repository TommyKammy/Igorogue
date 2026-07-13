using System.Collections.ObjectModel;

namespace Igorogue.Domain.Content;

public sealed class CoreDuelContentCatalog
{
    private const int StarterCardTypeCount = 6;
    private const string BanditContentId = "enemy_bandit";
    private const string StartingDeckRecipeId = "core_duel";

    private readonly ReadOnlyCollection<CardContentDefinition> starterCardView;
    private readonly ReadOnlyDictionary<string, CardContentDefinition> starterCardsById;

    private CoreDuelContentCatalog(
        string contentHash,
        CardContentDefinition[] starterCards,
        StartingDeckRecipe startingDeck,
        EnemyContentDefinition bandit,
        CoreDuelSystemPolicy systemPolicy)
    {
        ContentHash = contentHash;
        starterCardView = Array.AsReadOnly(starterCards);
        starterCardsById = new ReadOnlyDictionary<string, CardContentDefinition>(
            starterCards.ToDictionary(card => card.Id, StringComparer.Ordinal));
        StartingDeck = startingDeck;
        Bandit = bandit;
        SystemPolicy = systemPolicy;
    }

    public string ContentHash { get; }

    public IReadOnlyList<CardContentDefinition> StarterCards => starterCardView;

    public StartingDeckRecipe StartingDeck { get; }

    public EnemyContentDefinition Bandit { get; }

    public CoreDuelSystemPolicy SystemPolicy { get; }

    public CardContentDefinition StarterCard(string contentId)
    {
        var stableId = StableDomainId.Validate(contentId, nameof(contentId));
        return starterCardsById.TryGetValue(stableId, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Starter card content was not found: {stableId}.");
    }

    public static CoreDuelContentCatalog Create(
        string contentHash,
        IEnumerable<CardContentDefinition> starterCards,
        StartingDeckRecipe startingDeck,
        EnemyContentDefinition bandit,
        CoreDuelSystemPolicy systemPolicy)
    {
        ValidateContentHash(contentHash);
        ArgumentNullException.ThrowIfNull(starterCards);
        ArgumentNullException.ThrowIfNull(startingDeck);
        ArgumentNullException.ThrowIfNull(bandit);
        ArgumentNullException.ThrowIfNull(systemPolicy);

        var canonicalCards = starterCards.ToArray();
        foreach (var card in canonicalCards)
        {
            ArgumentNullException.ThrowIfNull(card);
            if (card.Rarity != CardRarity.Starter)
            {
                throw new ArgumentException(
                    $"Core Duel catalog card {card.Id} is not starter rarity.",
                    nameof(starterCards));
            }
        }

        Array.Sort(
            canonicalCards,
            (left, right) => string.CompareOrdinal(left.Id, right.Id));
        if (canonicalCards.Length != StarterCardTypeCount)
        {
            throw new ArgumentException(
                $"Core Duel catalog requires exactly {StarterCardTypeCount} starter card types.",
                nameof(starterCards));
        }

        if (canonicalCards.Select(card => card.Id).Distinct(StringComparer.Ordinal).Count() !=
            canonicalCards.Length)
        {
            throw new ArgumentException("Starter card content IDs must be unique.", nameof(starterCards));
        }

        if (!string.Equals(startingDeck.Id, StartingDeckRecipeId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Core Duel catalog requires starting-deck recipe {StartingDeckRecipeId}.",
                nameof(startingDeck));
        }

        var starterCardIds = canonicalCards.Select(card => card.Id);
        var recipeCardIds = startingDeck.Entries.Select(entry => entry.CardId);
        if (!starterCardIds.SequenceEqual(recipeCardIds, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "Core Duel starting-deck recipe must contain every starter card ID exactly once.",
                nameof(startingDeck));
        }

        if (!string.Equals(bandit.Id, BanditContentId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Core Duel catalog requires enemy content {BanditContentId}.",
                nameof(bandit));
        }

        return new CoreDuelContentCatalog(
            contentHash,
            canonicalCards,
            startingDeck,
            bandit,
            systemPolicy);
    }

    private static void ValidateContentHash(string contentHash)
    {
        const string prefix = "sha256:";
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        if (!contentHash.StartsWith(prefix, StringComparison.Ordinal) ||
            contentHash.Length != prefix.Length + 64 ||
            !contentHash[prefix.Length..].All(Uri.IsHexDigit))
        {
            throw new ArgumentException(
                "Content hash must use sha256:<64 hex digits>.",
                nameof(contentHash));
        }
    }
}
