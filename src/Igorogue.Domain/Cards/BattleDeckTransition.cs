using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Cards;

public sealed record BattleDeckInitialization(
    BattleDeckState Deck,
    AuthoritativeRngState RngAfter);

public sealed record BattleDeckTransition(
    BattleDeckState DeckAfter,
    AuthoritativeRngState RngAfter,
    bool IsExactNoOp,
    string? NoOpReason)
{
    internal static BattleDeckTransition Applied(
        BattleDeckState deckAfter,
        AuthoritativeRngState rngAfter) =>
        new(deckAfter, rngAfter, false, null);

    internal static BattleDeckTransition NoOp(
        BattleDeckState sourceDeck,
        AuthoritativeRngState sourceRng,
        string reason) =>
        new(sourceDeck, sourceRng, true, reason);
}
