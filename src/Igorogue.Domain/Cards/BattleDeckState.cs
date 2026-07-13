using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Cards;

/// <summary>
/// Immutable ordered battle card zones. DrawPile index zero is always the top.
/// </summary>
public sealed class BattleDeckState
{
    public const string EncodingVersion = "battle-deck-state-v1";
    public const string DrawPileTopConvention = "index_zero";

    private readonly ReadOnlyCollection<BattleCardInstance> drawPileView;
    private readonly ReadOnlyCollection<BattleCardInstance> handView;
    private readonly ReadOnlyCollection<ResolvingBattleCard> resolvingView;
    private readonly ReadOnlyCollection<BattleCardInstance> discardPileView;
    private readonly ReadOnlyCollection<BattleCardInstance> exhaustPileView;

    private BattleDeckState(
        BattleCardInstance[] drawPile,
        BattleCardInstance[] hand,
        ResolvingBattleCard[] resolving,
        BattleCardInstance[] discardPile,
        BattleCardInstance[] exhaustPile)
    {
        ValidateZones(drawPile, hand, resolving, discardPile, exhaustPile);
        drawPileView = ReadOnlyCopy(drawPile);
        handView = ReadOnlyCopy(hand);
        resolvingView = ReadOnlyCopy(resolving);
        discardPileView = ReadOnlyCopy(discardPile);
        exhaustPileView = ReadOnlyCopy(exhaustPile);
        CanonicalText = CreateCanonicalText();
        Checksum = DeterministicChecksum.Sha256Hex(CanonicalText);
    }

    public IReadOnlyList<BattleCardInstance> DrawPile => drawPileView;

    public IReadOnlyList<BattleCardInstance> Hand => handView;

    public IReadOnlyList<ResolvingBattleCard> Resolving => resolvingView;

    public IReadOnlyList<BattleCardInstance> DiscardPile => discardPileView;

    public IReadOnlyList<BattleCardInstance> ExhaustPile => exhaustPileView;

    public string CanonicalText { get; }

    public string Checksum { get; }

    public static BattleDeckInitialization CreateShuffled(
        IEnumerable<BattleCardInstance> recipe,
        AuthoritativeRngState rng)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(rng);

        var canonicalRecipe = recipe.ToArray();
        foreach (var card in canonicalRecipe)
        {
            ArgumentNullException.ThrowIfNull(card);
        }

        Array.Sort(
            canonicalRecipe,
            (left, right) => StringComparer.Ordinal.Compare(
                left.InstanceId,
                right.InstanceId));
        RejectDuplicateInstanceIds(canonicalRecipe, nameof(recipe));

        var shuffled = ShuffleDescending(canonicalRecipe, rng);
        return new BattleDeckInitialization(
            new BattleDeckState(shuffled.Cards, [], [], [], []),
            shuffled.RngAfter);
    }

    public BattleDeckTransition Draw(int count, AuthoritativeRngState rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        if (count < 0)
        {
            return BattleDeckTransition.NoOp(this, rng, "invalid_draw_count");
        }

        if (count == 0)
        {
            return BattleDeckTransition.NoOp(this, rng, "zero_draw");
        }

        if (drawPileView.Count == 0 && discardPileView.Count == 0)
        {
            return BattleDeckTransition.NoOp(this, rng, "no_drawable_cards");
        }

        var draw = drawPileView.ToList();
        var hand = handView.ToList();
        var discard = discardPileView.ToList();
        var rngAfter = rng;

        for (var drawn = 0; drawn < count; drawn++)
        {
            if (draw.Count == 0)
            {
                if (discard.Count == 0)
                {
                    break;
                }

                var reshuffled = ShuffleDescending(discard.ToArray(), rngAfter);
                draw.AddRange(reshuffled.Cards);
                rngAfter = reshuffled.RngAfter;
                discard.Clear();
            }

            hand.Add(draw[0]);
            draw.RemoveAt(0);
        }

        return BattleDeckTransition.Applied(
            Create(draw, hand, resolvingView, discard, exhaustPileView),
            rngAfter);
    }

    public BattleDeckTransition BeginResolution(
        string? instanceId,
        AuthoritativeRngState rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        if (resolvingView.Any(card => card.IsActive))
        {
            return BattleDeckTransition.NoOp(this, rng, "active_resolution_exists");
        }

        var handIndex = FindCard(handView, instanceId);
        if (handIndex < 0)
        {
            return BattleDeckTransition.NoOp(this, rng, "card_not_in_hand");
        }

        var hand = handView.ToList();
        var card = hand[handIndex];
        hand.RemoveAt(handIndex);
        var resolving = resolvingView.ToList();
        resolving.Add(new ResolvingBattleCard(card, BattleCardResolutionStatus.Active));
        return BattleDeckTransition.Applied(
            Create(drawPileView, hand, resolving, discardPileView, exhaustPileView),
            rng);
    }

    public BattleDeckTransition CompleteResolution(
        string? instanceId,
        AuthoritativeRngState rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        var resolvingIndex = FindResolvingCard(instanceId, requireActive: true);
        if (resolvingIndex < 0)
        {
            return BattleDeckTransition.NoOp(this, rng, "active_card_not_found");
        }

        var resolving = resolvingView.ToArray();
        resolving[resolvingIndex] = new ResolvingBattleCard(
            resolving[resolvingIndex].Card,
            BattleCardResolutionStatus.Resolved);
        return BattleDeckTransition.Applied(
            Create(drawPileView, handView, resolving, discardPileView, exhaustPileView),
            rng);
    }

    public BattleDeckTransition EndTurn(AuthoritativeRngState rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        if (resolvingView.Any(card => card.IsActive))
        {
            return BattleDeckTransition.NoOp(this, rng, "active_resolution_exists");
        }

        if (handView.Count == 0 && resolvingView.Count == 0)
        {
            return BattleDeckTransition.NoOp(this, rng, "nothing_to_discard");
        }

        var discard = discardPileView.ToList();
        discard.AddRange(handView);
        discard.AddRange(resolvingView.Select(card => card.Card));
        return BattleDeckTransition.Applied(
            Create(drawPileView, [], [], discard, exhaustPileView),
            rng);
    }

    public BattleDeckTransition Exhaust(
        string? instanceId,
        AuthoritativeRngState rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        if (FindCard(exhaustPileView, instanceId) >= 0)
        {
            return BattleDeckTransition.NoOp(this, rng, "card_already_exhausted");
        }

        var draw = drawPileView.ToList();
        var hand = handView.ToList();
        var resolving = resolvingView.ToList();
        var discard = discardPileView.ToList();
        BattleCardInstance? card = null;

        var index = FindCard(draw, instanceId);
        if (index >= 0)
        {
            card = draw[index];
            draw.RemoveAt(index);
        }
        else if ((index = FindCard(hand, instanceId)) >= 0)
        {
            card = hand[index];
            hand.RemoveAt(index);
        }
        else if ((index = FindResolvingCard(resolving, instanceId)) >= 0)
        {
            card = resolving[index].Card;
            resolving.RemoveAt(index);
        }
        else if ((index = FindCard(discard, instanceId)) >= 0)
        {
            card = discard[index];
            discard.RemoveAt(index);
        }

        if (card is null)
        {
            return BattleDeckTransition.NoOp(this, rng, "card_not_found");
        }

        var exhaust = exhaustPileView.ToList();
        exhaust.Add(card);
        return BattleDeckTransition.Applied(
            Create(draw, hand, resolving, discard, exhaust),
            rng);
    }

    public string ToCanonicalText() => CanonicalText;

    private static BattleDeckState Create(
        IEnumerable<BattleCardInstance> drawPile,
        IEnumerable<BattleCardInstance> hand,
        IEnumerable<ResolvingBattleCard> resolving,
        IEnumerable<BattleCardInstance> discardPile,
        IEnumerable<BattleCardInstance> exhaustPile) =>
        new(
            drawPile.ToArray(),
            hand.ToArray(),
            resolving.ToArray(),
            discardPile.ToArray(),
            exhaustPile.ToArray());

    private string CreateCanonicalText()
    {
        var lines = new List<string>
        {
            EncodingVersion,
            $"draw_top={DrawPileTopConvention}",
        };
        AppendZone(lines, "draw", drawPileView, null);
        AppendZone(lines, "hand", handView, null);
        AppendZone(
            lines,
            "resolving",
            resolvingView.Select(card => card.Card),
            resolvingView.Select(card => ResolutionStatusId(card.Status)).ToArray());
        AppendZone(lines, "discard", discardPileView, null);
        AppendZone(lines, "exhaust", exhaustPileView, null);
        return string.Join('\n', lines);
    }

    private static void AppendZone(
        ICollection<string> lines,
        string zone,
        IEnumerable<BattleCardInstance> cards,
        IReadOnlyList<string>? statuses)
    {
        var cardArray = cards.ToArray();
        lines.Add($"{zone}_count={cardArray.Length.ToString(CultureInfo.InvariantCulture)}");
        for (var index = 0; index < cardArray.Length; index++)
        {
            var prefix = $"{zone}_{index.ToString(CultureInfo.InvariantCulture)}";
            lines.Add($"{prefix}_instance={EncodeStableText(cardArray[index].InstanceId)}");
            lines.Add($"{prefix}_content={EncodeStableText(cardArray[index].ContentId)}");
            if (statuses is not null)
            {
                lines.Add($"{prefix}_status={statuses[index]}");
            }
        }
    }

    private static void ValidateZones(
        BattleCardInstance[] drawPile,
        BattleCardInstance[] hand,
        ResolvingBattleCard[] resolving,
        BattleCardInstance[] discardPile,
        BattleCardInstance[] exhaustPile)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        ValidateZone(drawPile, seen);
        ValidateZone(hand, seen);
        foreach (var entry in resolving)
        {
            ArgumentNullException.ThrowIfNull(entry);
            AddUnique(entry.Card, seen);
        }

        if (resolving.Count(entry => entry.IsActive) > 1)
        {
            throw new ArgumentException("A deck may contain at most one active resolving card.");
        }

        ValidateZone(discardPile, seen);
        ValidateZone(exhaustPile, seen);
    }

    private static void ValidateZone(
        IEnumerable<BattleCardInstance> cards,
        ISet<string> seen)
    {
        foreach (var card in cards)
        {
            ArgumentNullException.ThrowIfNull(card);
            AddUnique(card, seen);
        }
    }

    private static void AddUnique(BattleCardInstance card, ISet<string> seen)
    {
        if (!seen.Add(card.InstanceId))
        {
            throw new ArgumentException(
                $"Card instance {card.InstanceId} occurs more than once across battle zones.");
        }
    }

    private static void RejectDuplicateInstanceIds(
        IReadOnlyList<BattleCardInstance> cards,
        string parameterName)
    {
        for (var index = 1; index < cards.Count; index++)
        {
            if (StringComparer.Ordinal.Equals(
                    cards[index - 1].InstanceId,
                    cards[index].InstanceId))
            {
                throw new ArgumentException(
                    $"Battle recipe contains duplicate instance ID {cards[index].InstanceId}.",
                    parameterName);
            }
        }
    }

    private static ShuffleResult ShuffleDescending(
        BattleCardInstance[] source,
        AuthoritativeRngState rng)
    {
        var cards = (BattleCardInstance[])source.Clone();
        var rngAfter = rng;
        for (var index = cards.Length - 1; index > 0; index--)
        {
            var draw = rngAfter.NextGameplayIndex(index + 1);
            (cards[index], cards[draw.Value]) = (cards[draw.Value], cards[index]);
            rngAfter = draw.NextState;
        }

        return new ShuffleResult(cards, rngAfter);
    }

    private int FindResolvingCard(string? instanceId, bool requireActive = false) =>
        FindResolvingCard(resolvingView, instanceId, requireActive);

    private static int FindResolvingCard(
        IReadOnlyList<ResolvingBattleCard> cards,
        string? instanceId,
        bool requireActive = false)
    {
        for (var index = 0; index < cards.Count; index++)
        {
            if (StringComparer.Ordinal.Equals(cards[index].Card.InstanceId, instanceId) &&
                (!requireActive || cards[index].IsActive))
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindCard(
        IReadOnlyList<BattleCardInstance> cards,
        string? instanceId)
    {
        for (var index = 0; index < cards.Count; index++)
        {
            if (StringComparer.Ordinal.Equals(cards[index].InstanceId, instanceId))
            {
                return index;
            }
        }

        return -1;
    }

    private static ReadOnlyCollection<T> ReadOnlyCopy<T>(T[] source) =>
        Array.AsReadOnly((T[])source.Clone());

    private static string EncodeStableText(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static string ResolutionStatusId(BattleCardResolutionStatus status) =>
        status switch
        {
            BattleCardResolutionStatus.Active => "active",
            BattleCardResolutionStatus.Resolved => "resolved",
            _ => throw new InvalidOperationException("Unknown card resolution status."),
        };

    private sealed record ShuffleResult(
        BattleCardInstance[] Cards,
        AuthoritativeRngState RngAfter);
}
