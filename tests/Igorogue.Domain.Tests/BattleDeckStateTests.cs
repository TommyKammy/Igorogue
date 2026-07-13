using Igorogue.Domain.Cards;
using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Tests;

public sealed class BattleDeckStateTests
{
    [Fact]
    public void BattleStartShuffleMatchesVersionedGoldenAndChecksum()
    {
        var start = Start(42, Cards("abcdef"));

        Assert.Equal(
            new[]
            {
                "instance-a",
                "instance-e",
                "instance-f",
                "instance-c",
                "instance-b",
                "instance-d",
            },
            Ids(start.Deck.DrawPile));
        Assert.Equal(BattleDeckState.DrawPileTopConvention, "index_zero");
        Assert.Equal(5UL, start.RngAfter.Gameplay.DrawCount);
        Assert.Equal(0UL, start.RngAfter.Reward.DrawCount);
        Assert.Equal(
            "4e16cfedc5996105f7d868f2b05fb5866d659fbf2809c6491f7dbc11be7a58c9",
            start.Deck.Checksum);
        Assert.Equal(
            DeterministicChecksum.Sha256Hex(start.Deck.CanonicalText),
            start.Deck.Checksum);
    }

    [Fact]
    public void ReversedInjectedRecipeHasSameShuffleRngAndChecksum()
    {
        var recipe = Cards("abcdef");
        var first = Start(90210, recipe);
        var reversed = Start(90210, recipe.Reverse());

        Assert.Equal(Ids(first.Deck.DrawPile), Ids(reversed.Deck.DrawPile));
        Assert.Equal(first.Deck.CanonicalText, reversed.Deck.CanonicalText);
        Assert.Equal(first.Deck.Checksum, reversed.Deck.Checksum);
        Assert.Equal(first.RngAfter, reversed.RngAfter);
    }

    [Fact]
    public void ZeroAndOneCardShuffleConsumeNoGameplayRng()
    {
        var rng = AuthoritativeRngState.Create(-12);

        var empty = BattleDeckState.CreateShuffled([], rng);
        var one = BattleDeckState.CreateShuffled([Card('a')], rng);

        Assert.Same(rng, empty.RngAfter);
        Assert.Same(rng, one.RngAfter);
        Assert.Empty(empty.Deck.DrawPile);
        Assert.Equal("instance-a", Assert.Single(one.Deck.DrawPile).InstanceId);
    }

    [Fact]
    public void RecipeAllowsRepeatedContentButRejectsDuplicateInstanceIdentity()
    {
        var rng = AuthoritativeRngState.Create(1);
        var repeatedContent = BattleDeckState.CreateShuffled(
            [
                new BattleCardInstance("one", "shared"),
                new BattleCardInstance("two", "shared"),
            ],
            rng);

        Assert.Equal(2, repeatedContent.Deck.DrawPile.Count);
        Assert.Throws<ArgumentException>(() => BattleDeckState.CreateShuffled(
            [
                new BattleCardInstance("same", "first"),
                new BattleCardInstance("same", "second"),
            ],
            rng));
    }

    [Fact]
    public void DrawOverflowMovesEveryAvailableCardWithoutFurtherRng()
    {
        var start = Start(42, Cards("abc"));

        var draw = start.Deck.Draw(100, start.RngAfter);

        Assert.False(draw.IsExactNoOp);
        Assert.Equal(Ids(start.Deck.DrawPile), Ids(draw.DeckAfter.Hand));
        Assert.Empty(draw.DeckAfter.DrawPile);
        Assert.Empty(draw.DeckAfter.DiscardPile);
        Assert.Same(start.RngAfter, draw.RngAfter);
        Assert.Empty(start.Deck.Hand);
    }

    [Fact]
    public void EmptyAndInvalidDrawsAreExactNoOps()
    {
        var start = Start(42, []);

        AssertExactNoOp(start.Deck.Draw(1, start.RngAfter), start.Deck, start.RngAfter, "no_drawable_cards");
        AssertExactNoOp(start.Deck.Draw(0, start.RngAfter), start.Deck, start.RngAfter, "zero_draw");
        AssertExactNoOp(start.Deck.Draw(-1, start.RngAfter), start.Deck, start.RngAfter, "invalid_draw_count");
    }

    [Fact]
    public void DrawAcrossDiscardReshuffleMatchesGoldenSequence()
    {
        var start = Start(42, Cards("abcd"));
        Assert.Equal(new[] { "instance-b", "instance-c", "instance-a", "instance-d" }, Ids(start.Deck.DrawPile));
        var allInHand = start.Deck.Draw(4, start.RngAfter);
        var discarded = allInHand.DeckAfter.EndTurn(allInHand.RngAfter);

        var reshuffledDraw = discarded.DeckAfter.Draw(2, discarded.RngAfter);

        Assert.Equal(new[] { "instance-b", "instance-d" }, Ids(reshuffledDraw.DeckAfter.Hand));
        Assert.Equal(new[] { "instance-a", "instance-c" }, Ids(reshuffledDraw.DeckAfter.DrawPile));
        Assert.Empty(reshuffledDraw.DeckAfter.DiscardPile);
        Assert.Equal(6UL, reshuffledDraw.RngAfter.Gameplay.DrawCount);
        Assert.Equal(0UL, reshuffledDraw.RngAfter.Reward.DrawCount);
    }

    [Fact]
    public void SingleDiscardReshuffleConsumesNoRng()
    {
        var start = Start(42, [Card('a')]);
        var hand = start.Deck.Draw(1, start.RngAfter);
        var discard = hand.DeckAfter.EndTurn(hand.RngAfter);

        var redrawn = discard.DeckAfter.Draw(1, discard.RngAfter);

        Assert.Equal("instance-a", Assert.Single(redrawn.DeckAfter.Hand).InstanceId);
        Assert.Same(discard.RngAfter, redrawn.RngAfter);
    }

    [Fact]
    public void ResolutionRemainsOrderedAndResolvedUntilTurnEnd()
    {
        var start = Start(17, Cards("abcd"));
        var drawn = start.Deck.Draw(4, start.RngAfter);
        var selected = drawn.DeckAfter.Hand[1];
        var expectedHandOrder = drawn.DeckAfter.Hand
            .Where(card => !ReferenceEquals(card, selected))
            .Select(card => card.InstanceId)
            .ToArray();

        var begun = drawn.DeckAfter.BeginResolution(selected.InstanceId, drawn.RngAfter);
        var completed = begun.DeckAfter.CompleteResolution(selected.InstanceId, begun.RngAfter);

        var resolved = Assert.Single(completed.DeckAfter.Resolving);
        Assert.Same(selected, resolved.Card);
        Assert.True(resolved.IsResolved);
        Assert.Empty(completed.DeckAfter.DiscardPile);

        var ended = completed.DeckAfter.EndTurn(completed.RngAfter);
        Assert.Empty(ended.DeckAfter.Hand);
        Assert.Empty(ended.DeckAfter.Resolving);
        Assert.Equal(expectedHandOrder.Append(selected.InstanceId), Ids(ended.DeckAfter.DiscardPile));
        Assert.Same(start.RngAfter, ended.RngAfter);
    }

    [Fact]
    public void ActiveResolutionRejectsSecondBeginAndTurnEndAsExactNoOps()
    {
        var start = Start(17, Cards("ab"));
        var drawn = start.Deck.Draw(2, start.RngAfter);
        var begun = drawn.DeckAfter.BeginResolution(
            drawn.DeckAfter.Hand[0].InstanceId,
            drawn.RngAfter);

        AssertExactNoOp(
            begun.DeckAfter.BeginResolution(begun.DeckAfter.Hand[0].InstanceId, begun.RngAfter),
            begun.DeckAfter,
            begun.RngAfter,
            "active_resolution_exists");
        AssertExactNoOp(
            begun.DeckAfter.EndTurn(begun.RngAfter),
            begun.DeckAfter,
            begun.RngAfter,
            "active_resolution_exists");
    }

    [Fact]
    public void InvalidResolutionTransitionsPreserveExactDeckAndRng()
    {
        var start = Start(17, Cards("ab"));

        AssertExactNoOp(
            start.Deck.BeginResolution("missing", start.RngAfter),
            start.Deck,
            start.RngAfter,
            "card_not_in_hand");
        AssertExactNoOp(
            start.Deck.CompleteResolution("missing", start.RngAfter),
            start.Deck,
            start.RngAfter,
            "active_card_not_found");
        AssertExactNoOp(
            start.Deck.EndTurn(start.RngAfter),
            start.Deck,
            start.RngAfter,
            "nothing_to_discard");
    }

    [Fact]
    public void ExhaustMovesAnInstanceExactlyOnceAndPreservesOtherOrder()
    {
        var start = Start(11, Cards("abc"));
        var target = start.Deck.DrawPile[1];

        var exhausted = start.Deck.Exhaust(target.InstanceId, start.RngAfter);

        Assert.Equal(
            start.Deck.DrawPile.Where(card => !ReferenceEquals(card, target)).Select(card => card.InstanceId),
            Ids(exhausted.DeckAfter.DrawPile));
        Assert.Same(target, Assert.Single(exhausted.DeckAfter.ExhaustPile));
        Assert.Same(start.RngAfter, exhausted.RngAfter);
        AssertExactNoOp(
            exhausted.DeckAfter.Exhaust(target.InstanceId, exhausted.RngAfter),
            exhausted.DeckAfter,
            exhausted.RngAfter,
            "card_already_exhausted");
        AssertExactNoOp(
            exhausted.DeckAfter.Exhaust("missing", exhausted.RngAfter),
            exhausted.DeckAfter,
            exhausted.RngAfter,
            "card_not_found");
    }

    [Fact]
    public void ExhaustingActiveResolutionClearsTheResolutionWindow()
    {
        var start = Start(19, Cards("ab"));
        var drawn = start.Deck.Draw(1, start.RngAfter);
        var card = Assert.Single(drawn.DeckAfter.Hand);
        var begun = drawn.DeckAfter.BeginResolution(card.InstanceId, drawn.RngAfter);

        var exhausted = begun.DeckAfter.Exhaust(card.InstanceId, begun.RngAfter);

        Assert.Empty(exhausted.DeckAfter.Resolving);
        Assert.Same(card, Assert.Single(exhausted.DeckAfter.ExhaustPile));
        Assert.False(exhausted.DeckAfter.EndTurn(exhausted.RngAfter).NoOpReason == "active_resolution_exists");
    }

    [Fact]
    public void ZoneCollectionsAreReadOnlySnapshotsAndChecksumsTrackOrderedState()
    {
        var recipe = Cards("abc");
        var start = Start(5, recipe);
        var canonicalBefore = start.Deck.CanonicalText;
        recipe[0] = Card('z');

        var drawCollection = Assert.IsAssignableFrom<ICollection<BattleCardInstance>>(
            start.Deck.DrawPile);
        Assert.True(drawCollection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => drawCollection.Add(Card('x')));

        var drawn = start.Deck.Draw(1, start.RngAfter);
        Assert.Equal(canonicalBefore, start.Deck.CanonicalText);
        Assert.NotEqual(start.Deck.CanonicalText, drawn.DeckAfter.CanonicalText);
        Assert.NotEqual(start.Deck.Checksum, drawn.DeckAfter.Checksum);
    }

    [Fact]
    public void CardIdentityRejectsMalformedStableIds()
    {
        Assert.Throws<ArgumentException>(() => new BattleCardInstance(" ", "content"));
        Assert.Throws<ArgumentException>(() => new BattleCardInstance("instance", "bad:id"));
    }

    private static BattleDeckInitialization Start(
        long seed,
        IEnumerable<BattleCardInstance> recipe) =>
        BattleDeckState.CreateShuffled(recipe, AuthoritativeRngState.Create(seed));

    private static BattleCardInstance[] Cards(string suffixes) =>
        suffixes.Select(Card).ToArray();

    private static BattleCardInstance Card(char suffix) =>
        new($"instance-{suffix}", $"content-{suffix}");

    private static string[] Ids(IEnumerable<BattleCardInstance> cards) =>
        cards.Select(card => card.InstanceId).ToArray();

    private static void AssertExactNoOp(
        BattleDeckTransition transition,
        BattleDeckState expectedDeck,
        AuthoritativeRngState expectedRng,
        string expectedReason)
    {
        Assert.True(transition.IsExactNoOp);
        Assert.Equal(expectedReason, transition.NoOpReason);
        Assert.Same(expectedDeck, transition.DeckAfter);
        Assert.Same(expectedRng, transition.RngAfter);
    }
}
