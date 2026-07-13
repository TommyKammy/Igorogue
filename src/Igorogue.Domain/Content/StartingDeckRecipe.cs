using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Igorogue.Domain.Content;

public sealed record StartingDeckCardCount
{
    public StartingDeckCardCount(string cardId, int count)
    {
        CardId = StableDomainId.Validate(cardId, nameof(cardId));
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                "Starting-deck card count must be positive.");
        }

        Count = count;
    }

    public string CardId { get; }

    public int Count { get; }
}

public sealed class StartingDeckRecipe
{
    public const string EncodingVersion = "starting-deck-recipe-v1";

    private readonly ReadOnlyCollection<StartingDeckCardCount> entryView;

    private StartingDeckRecipe(
        string id,
        int totalCards,
        StartingDeckCardCount[] entries)
    {
        Id = id;
        TotalCards = totalCards;
        entryView = Array.AsReadOnly(entries);
        CanonicalText = CreateCanonicalText();
    }

    public string Id { get; }

    public int TotalCards { get; }

    public IReadOnlyList<StartingDeckCardCount> Entries => entryView;

    public string CanonicalText { get; }

    public static StartingDeckRecipe Create(
        string id,
        int totalCards,
        IEnumerable<StartingDeckCardCount> entries)
    {
        var stableId = StableDomainId.Validate(id, nameof(id));
        if (totalCards <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalCards),
                totalCards,
                "Starting-deck total card count must be positive.");
        }

        ArgumentNullException.ThrowIfNull(entries);
        var canonicalEntries = entries.ToArray();
        foreach (var entry in canonicalEntries)
        {
            ArgumentNullException.ThrowIfNull(entry);
        }

        Array.Sort(
            canonicalEntries,
            (left, right) => StringComparer.Ordinal.Compare(left.CardId, right.CardId));
        for (var index = 1; index < canonicalEntries.Length; index++)
        {
            if (StringComparer.Ordinal.Equals(
                    canonicalEntries[index - 1].CardId,
                    canonicalEntries[index].CardId))
            {
                throw new ArgumentException(
                    $"Starting-deck card ID is duplicated: {canonicalEntries[index].CardId}.",
                    nameof(entries));
            }
        }

        var actualTotal = canonicalEntries.Sum(entry => (long)entry.Count);
        if (actualTotal != totalCards)
        {
            throw new ArgumentException(
                $"Starting-deck entry counts total {actualTotal.ToString(CultureInfo.InvariantCulture)}, " +
                $"but totalCards declares {totalCards.ToString(CultureInfo.InvariantCulture)}.",
                nameof(entries));
        }

        return new StartingDeckRecipe(stableId, totalCards, canonicalEntries);
    }

    public string ToCanonicalText() => CanonicalText;

    private string CreateCanonicalText()
    {
        var lines = new List<string>
        {
            EncodingVersion,
            $"id={EncodeStableText(Id)}",
            $"total_cards={TotalCards.ToString(CultureInfo.InvariantCulture)}",
            $"entry_count={entryView.Count.ToString(CultureInfo.InvariantCulture)}",
        };
        for (var index = 0; index < entryView.Count; index++)
        {
            var entry = entryView[index];
            var prefix = $"entry_{index.ToString(CultureInfo.InvariantCulture)}";
            lines.Add($"{prefix}_card_id={EncodeStableText(entry.CardId)}");
            lines.Add($"{prefix}_count={entry.Count.ToString(CultureInfo.InvariantCulture)}");
        }

        return string.Join('\n', lines);
    }

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}
