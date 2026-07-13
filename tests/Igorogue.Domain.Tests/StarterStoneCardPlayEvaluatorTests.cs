using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Tests;

public sealed class StarterStoneCardPlayEvaluatorTests
{
    [Fact]
    public void ShapeProjectionPreservesTypedOrderedOperationsAndRuntimeValues()
    {
        var definition = StarterStoneCardPlayDefinition.Create(
            Extend(
                "card_shape_driven",
                cost: 2,
                minimumRealLiberties: 4,
                cards: 2));
        var differentRuntimeValue = StarterStoneCardPlayDefinition.Create(
            Extend(
                "card_shape_driven",
                cost: 2,
                minimumRealLiberties: 3,
                cards: 2));

        Assert.Equal("card_shape_driven", definition.ContentId);
        Assert.Equal(2, definition.Cost);
        Assert.Equal(StarterStoneCardProfile.Extend, definition.Profile);
        Assert.Equal(StoneContentKind.Basic, definition.StoneKind);
        Assert.Equal([CardPlacementTag.Frontline], definition.PlacementTags);
        Assert.Collection(
            definition.Effects,
            effect => Assert.Same(definition.Placement, effect),
            effect =>
            {
                var draw = Assert.IsType<DrawIfRealLibertiesAtLeastOperationDefinition>(effect);
                Assert.Equal(4, draw.MinimumRealLiberties);
                Assert.Equal(2, draw.Cards);
            });
        Assert.Empty(definition.OnCaptured);
        Assert.Contains("effect_0=place_stone;stone=basic", definition.CanonicalText);
        Assert.Contains(
            "effect_1=draw_if_real_liberties_at_least;minimum=4;cards=2",
            definition.CanonicalText);
        Assert.Equal(definition.CanonicalText, definition.ToCanonicalText());
        Assert.Equal(
            DeterministicChecksum.Sha256Hex(definition.CanonicalText),
            definition.Checksum);
        Assert.NotEqual(definition.CanonicalText, differentRuntimeValue.CanonicalText);
        Assert.NotEqual(definition.Checksum, differentRuntimeValue.Checksum);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<CardPlacementTag>)definition.PlacementTags).Add(
                CardPlacementTag.Contact));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<CardOperationDefinition>)definition.Effects).Add(
                new ReserveDrawOperationDefinition(1)));
    }

    [Fact]
    public void SupportedProfilesAreSelectedByShapeWithoutContentIdSwitching()
    {
        var definitions = new[]
        {
            StarterStoneCardPlayDefinition.Create(Basic("card_unfamiliar_a")),
            StarterStoneCardPlayDefinition.Create(Extend("card_unfamiliar_b")),
            StarterStoneCardPlayDefinition.Create(Contact("card_unfamiliar_c")),
            StarterStoneCardPlayDefinition.Create(Lure("card_unfamiliar_d")),
        };

        Assert.Equal(
            [
                StarterStoneCardProfile.BasicPlacement,
                StarterStoneCardProfile.Extend,
                StarterStoneCardProfile.Contact,
                StarterStoneCardProfile.Lure,
            ],
            definitions.Select(definition => definition.Profile));
        Assert.Equal(
            [StoneContentKind.Basic, StoneContentKind.Basic, StoneContentKind.Basic, StoneContentKind.Lure],
            definitions.Select(definition => definition.StoneKind));

        var lure = definitions[3];
        Assert.IsType<ReserveDrawOperationDefinition>(lure.Effects[1]);
        Assert.IsType<ReserveDrawOperationDefinition>(Assert.Single(lure.OnCaptured));
    }

    [Fact]
    public void CatalogIsSortedCanonicalAndInvariantToInputEnumerationOrder()
    {
        var contents = SupportedContents();
        var first = StarterStoneCardPlayCatalog.Create(contents);
        var reversed = StarterStoneCardPlayCatalog.Create(contents.Reverse());

        Assert.Equal(
            contents.Select(content => content.Id).Order(StringComparer.Ordinal),
            first.Definitions.Select(definition => definition.ContentId));
        Assert.Equal(first.CanonicalText, reversed.CanonicalText);
        Assert.Equal(first.Checksum, reversed.Checksum);
        Assert.Equal(first.CanonicalText, first.ToCanonicalText());
        Assert.Equal(
            DeterministicChecksum.Sha256Hex(first.CanonicalText),
            first.Checksum);
        Assert.Same(
            first.Definitions.Single(definition =>
                definition.Profile == StarterStoneCardProfile.Contact),
            first.DefinitionFor("card_contact_shape"));
        Assert.True(first.TryDefinition("card_lure_shape", out var lure));
        Assert.Equal(StarterStoneCardProfile.Lure, lure?.Profile);
        Assert.False(first.TryDefinition("card_missing_shape", out var missing));
        Assert.Null(missing);
        Assert.Throws<KeyNotFoundException>(() =>
            first.DefinitionFor("card_missing_shape"));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<StarterStoneCardPlayDefinition>)first.Definitions).Add(
                first.Definitions[0]));
    }

    [Fact]
    public void ProjectionFailsClosedOnUnsupportedShapesAndOperationOrder()
    {
        var invalid = new[]
        {
            Raw(
                rarity: CardRarity.Common,
                tags: [CardPlacementTag.Frontline, CardPlacementTag.Terminal],
                effects: [new PlaceStoneOperationDefinition(StoneContentKind.Basic)]),
            Raw(
                type: CardContentType.Technique,
                tags: [CardPlacementTag.Frontline, CardPlacementTag.Terminal],
                effects: [new PlaceStoneOperationDefinition(StoneContentKind.Basic)]),
            Raw(
                target: CardTargetKind.FriendlyGroup,
                tags: [CardPlacementTag.Frontline, CardPlacementTag.Terminal],
                effects: [new PlaceStoneOperationDefinition(StoneContentKind.Basic)]),
            Raw(
                tags: [CardPlacementTag.Frontline, CardPlacementTag.Terminal, CardPlacementTag.Edge],
                effects: [new PlaceStoneOperationDefinition(StoneContentKind.Basic)]),
            Raw(
                tags: [CardPlacementTag.Frontline, CardPlacementTag.Terminal],
                effects: [new PlaceStoneOperationDefinition(StoneContentKind.Lure)]),
            Raw(
                tags: [CardPlacementTag.Frontline],
                effects:
                [
                    new DrawIfRealLibertiesAtLeastOperationDefinition(3, 1),
                    new PlaceStoneOperationDefinition(StoneContentKind.Basic),
                ]),
            Raw(
                tags: [CardPlacementTag.Frontline],
                effects:
                [
                    new PlaceStoneOperationDefinition(StoneContentKind.Basic),
                    new ReserveDrawOperationDefinition(1),
                ]),
            Raw(
                tags: [CardPlacementTag.Contact, CardPlacementTag.Terminal],
                effects:
                [
                    new PlaceStoneOperationDefinition(StoneContentKind.Basic),
                    new GainQiIfEnemyAtariOperationDefinition(1),
                ],
                onCaptured: [new ReserveDrawOperationDefinition(1)]),
            Raw(
                tags: [CardPlacementTag.Contact],
                effects:
                [
                    new PlaceStoneOperationDefinition(StoneContentKind.Lure),
                    new ReserveDrawOperationDefinition(1),
                ]),
            Raw(
                tags: [CardPlacementTag.Contact],
                effects:
                [
                    new PlaceStoneOperationDefinition(StoneContentKind.Lure),
                    new ReserveDrawOperationDefinition(1),
                ],
                onCaptured: [new GainQiIfEnemyAtariOperationDefinition(1)]),
        };

        foreach (var content in invalid)
        {
            Assert.Throws<ArgumentException>(() =>
                StarterStoneCardPlayDefinition.Create(content));
        }
    }

    [Fact]
    public void CatalogRequiresExactlyOneOfEachSupportedProfile()
    {
        var missing = SupportedContents().Take(3);
        var duplicateProfile = new[]
        {
            Basic("card_basic_a"),
            Basic("card_basic_b"),
            Contact("card_contact_a"),
            Lure("card_lure_a"),
        };
        var duplicateId = new[]
        {
            Basic("card_same"),
            Extend("card_same"),
            Contact("card_contact_a"),
            Lure("card_lure_a"),
        };

        Assert.Throws<ArgumentException>(() =>
            StarterStoneCardPlayCatalog.Create(missing));
        Assert.Throws<ArgumentException>(() =>
            StarterStoneCardPlayCatalog.Create(duplicateProfile));
        Assert.Throws<ArgumentException>(() =>
            StarterStoneCardPlayCatalog.Create(duplicateId));
        Assert.Throws<ArgumentException>(() =>
            StarterStoneCardPlayCatalog.Create(
                [Basic("card_basic_a"), Extend("card_extend_a"), Contact("card_contact_a"), null!]));
    }

    [Fact]
    public void FrontlineModeRequiresAnEmptyPointAdjacentToBlack()
    {
        var catalog = Catalog();
        var definition = Definition(catalog, StarterStoneCardProfile.Extend);
        var deck = ReadyDeck(Card("extend-1", definition.ContentId));
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var board = BoardState.Create(
            geometry,
            [new BoardStone(StoneColor.Black, false, geometry.CreateCanonicalPoint(2, 2))]);
        var target = geometry.CreateCanonicalPoint(2, 3);

        var evaluation = StarterStoneCardPlayEvaluator.Evaluate(
            catalog,
            definition,
            deck,
            1,
            "extend-1",
            board,
            target,
            StoneCardPlacementMode.Frontline);

        Assert.True(evaluation.IsAuthorized);
        Assert.Equal(StarterStoneCardPlayStatus.Authorized, evaluation.Status);
        Assert.Equal("authorized", evaluation.ReasonId);
        Assert.Same(catalog, evaluation.SourceCatalog);
        Assert.Same(definition, evaluation.SourceDefinition);
        Assert.Same(deck, evaluation.SourceDeck);
        Assert.Same(board, evaluation.SourceBoard);
        Assert.Same(target, evaluation.Target);
        Assert.Equal(PlacementAccessMode.Normal, evaluation.AccessMode);
        Assert.True(evaluation.IsBoundTo(
            catalog,
            definition,
            deck,
            1,
            "extend-1",
            board,
            target,
            StoneCardPlacementMode.Frontline,
            PlacementAccessMode.Normal));
    }

    [Fact]
    public void ContactModeRequiresAdjacentBlackAndWhiteStones()
    {
        var catalog = Catalog();
        var definition = Definition(catalog, StarterStoneCardProfile.Contact);
        var deck = ReadyDeck(Card("contact-1", definition.ContentId));
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var target = geometry.CreateCanonicalPoint(3, 3);
        var validBoard = BoardState.Create(
            geometry,
            [
                new BoardStone(StoneColor.Black, false, geometry.CreateCanonicalPoint(2, 3)),
                new BoardStone(StoneColor.White, false, geometry.CreateCanonicalPoint(3, 4)),
            ]);
        var blackOnlyBoard = BoardState.Create(
            geometry,
            [new BoardStone(StoneColor.Black, false, geometry.CreateCanonicalPoint(2, 3))]);

        var accepted = StarterStoneCardPlayEvaluator.Evaluate(
            catalog,
            definition,
            deck,
            1,
            "contact-1",
            validBoard,
            target,
            StoneCardPlacementMode.Contact);
        var rejected = StarterStoneCardPlayEvaluator.Evaluate(
            catalog,
            definition,
            deck,
            1,
            "contact-1",
            blackOnlyBoard,
            target,
            StoneCardPlacementMode.Contact);

        Assert.True(accepted.IsAuthorized);
        Assert.Equal(PlacementAccessMode.Normal, accepted.AccessMode);
        AssertRejected(
            rejected,
            StarterStoneCardPlayStatus.ContactAdjacencyRequired,
            "contact_adjacency_required");
    }

    [Fact]
    public void TerminalModeRequiresPrintedTagAndLeavesCaptureProofToPlacementLegality()
    {
        var catalog = Catalog();
        var terminalDefinition = Definition(catalog, StarterStoneCardProfile.Contact);
        var nonTerminalDefinition = Definition(catalog, StarterStoneCardProfile.Lure);
        var deck = ReadyDeck(
            Card("contact-1", terminalDefinition.ContentId),
            Card("lure-1", nonTerminalDefinition.ContentId));
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var board = BoardState.Create(
            geometry,
            [new BoardStone(StoneColor.Black, false, geometry.CreateCanonicalPoint(2, 2))]);
        var remote = geometry.CreateCanonicalPoint(7, 7);

        var accepted = StarterStoneCardPlayEvaluator.Evaluate(
            catalog,
            terminalDefinition,
            deck,
            1,
            "contact-1",
            board,
            remote,
            StoneCardPlacementMode.TerminalCapture);
        var rejected = StarterStoneCardPlayEvaluator.Evaluate(
            catalog,
            nonTerminalDefinition,
            deck,
            1,
            "lure-1",
            board,
            remote,
            StoneCardPlacementMode.TerminalCapture);

        Assert.True(accepted.IsAuthorized);
        Assert.Equal(PlacementAccessMode.TerminalCapture, accepted.AccessMode);
        AssertRejected(
            rejected,
            StarterStoneCardPlayStatus.UnsupportedMode,
            "unsupported_placement_mode");
    }

    [Fact]
    public void RejectionsHaveStableStatusesReasonsAndExactNoOpSources()
    {
        var catalog = Catalog();
        var definition = Definition(catalog, StarterStoneCardProfile.BasicPlacement);
        var equivalentCatalog = Catalog();
        var equivalentDefinition = Definition(
            equivalentCatalog,
            StarterStoneCardProfile.BasicPlacement);
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var blackPoint = geometry.CreateCanonicalPoint(2, 2);
        var adjacent = geometry.CreateCanonicalPoint(2, 3);
        var remote = geometry.CreateCanonicalPoint(7, 7);
        var board = BoardState.Create(
            geometry,
            [new BoardStone(StoneColor.Black, false, blackPoint)]);
        var matchingDeck = ReadyDeck(Card("basic-1", definition.ContentId));
        var mismatchedDeck = ReadyDeck(Card("basic-1", "card_other_shape"));
        var activeDeckBefore = ReadyDeck(
            Card("basic-1", definition.ContentId),
            Card("other-1", definition.ContentId));
        var activeDeck = activeDeckBefore.BeginResolution(
            "other-1",
            AuthoritativeRngState.Create(77)).DeckAfter;

        AssertRejected(
            StarterStoneCardPlayEvaluator.Evaluate(
                catalog, equivalentDefinition, matchingDeck, 1, "basic-1", board, adjacent,
                StoneCardPlacementMode.Frontline),
            StarterStoneCardPlayStatus.DefinitionNotInCatalog,
            "definition_not_in_catalog");
        AssertRejected(
            StarterStoneCardPlayEvaluator.Evaluate(
                catalog, definition, matchingDeck, 1, "missing-1", board, adjacent,
                StoneCardPlacementMode.Frontline),
            StarterStoneCardPlayStatus.CardNotInHand,
            "card_not_in_hand");
        AssertRejected(
            StarterStoneCardPlayEvaluator.Evaluate(
                catalog, definition, mismatchedDeck, 1, "basic-1", board, adjacent,
                StoneCardPlacementMode.Frontline),
            StarterStoneCardPlayStatus.CardContentMismatch,
            "card_content_mismatch");
        AssertRejected(
            StarterStoneCardPlayEvaluator.Evaluate(
                catalog, definition, matchingDeck, 0, "basic-1", board, adjacent,
                StoneCardPlacementMode.Frontline),
            StarterStoneCardPlayStatus.InsufficientQi,
            "insufficient_qi");
        AssertRejected(
            StarterStoneCardPlayEvaluator.Evaluate(
                catalog, definition, activeDeck, 1, "basic-1", board, adjacent,
                StoneCardPlacementMode.Frontline),
            StarterStoneCardPlayStatus.ActiveResolutionExists,
            "active_resolution_exists");
        AssertRejected(
            StarterStoneCardPlayEvaluator.Evaluate(
                catalog, definition, matchingDeck, 1, "basic-1", board, adjacent,
                (StoneCardPlacementMode)255),
            StarterStoneCardPlayStatus.UnsupportedMode,
            "unsupported_placement_mode");
        AssertRejected(
            StarterStoneCardPlayEvaluator.Evaluate(
                catalog, definition, matchingDeck, 1, "basic-1", board, blackPoint,
                StoneCardPlacementMode.Frontline),
            StarterStoneCardPlayStatus.TargetOccupied,
            "target_occupied");
        var remoteRejection = StarterStoneCardPlayEvaluator.Evaluate(
            catalog, definition, matchingDeck, 1, "basic-1", board, remote,
            StoneCardPlacementMode.Frontline);
        AssertRejected(
            remoteRejection,
            StarterStoneCardPlayStatus.FrontlineAdjacencyRequired,
            "frontline_adjacency_required");

        Assert.Same(catalog, remoteRejection.SourceCatalog);
        Assert.Same(definition, remoteRejection.SourceDefinition);
        Assert.Same(matchingDeck, remoteRejection.SourceDeck);
        Assert.Same(board, remoteRejection.SourceBoard);
        Assert.Equal(matchingDeck.CanonicalText, remoteRejection.SourceDeck.CanonicalText);
        Assert.Equal(board.OccupiedStones, remoteRejection.SourceBoard.OccupiedStones);
    }

    [Fact]
    public void AuthorizationRemainsBoundToEveryExactImmutableSource()
    {
        var catalog = Catalog();
        var definition = Definition(catalog, StarterStoneCardProfile.BasicPlacement);
        var equivalentCatalog = Catalog();
        var equivalentDefinition = Definition(
            equivalentCatalog,
            StarterStoneCardProfile.BasicPlacement);
        var deck = ReadyDeck(Card("basic-1", definition.ContentId));
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var target = geometry.CreateCanonicalPoint(2, 3);
        var board = BoardState.Create(
            geometry,
            [new BoardStone(StoneColor.Black, false, geometry.CreateCanonicalPoint(2, 2))]);
        var evaluation = StarterStoneCardPlayEvaluator.Evaluate(
            catalog,
            definition,
            deck,
            1,
            "basic-1",
            board,
            target,
            StoneCardPlacementMode.Frontline);
        var equivalentBoard = BoardState.Create(geometry, board.OccupiedStones);

        Assert.False(evaluation.IsBoundTo(
            equivalentCatalog, equivalentDefinition, deck, 1, "basic-1", board, target,
            StoneCardPlacementMode.Frontline, PlacementAccessMode.Normal));
        Assert.False(evaluation.IsBoundTo(
            catalog, definition, deck, 2, "basic-1", board, target,
            StoneCardPlacementMode.Frontline, PlacementAccessMode.Normal));
        Assert.False(evaluation.IsBoundTo(
            catalog, definition, deck, 1, "basic-1", equivalentBoard, target,
            StoneCardPlacementMode.Frontline, PlacementAccessMode.Normal));
        Assert.False(evaluation.IsBoundTo(
            catalog, definition, deck, 1, "basic-1", board, target,
            StoneCardPlacementMode.Frontline, PlacementAccessMode.TerminalCapture));
    }

    private static void AssertRejected(
        StarterStoneCardPlayEvaluation evaluation,
        StarterStoneCardPlayStatus expectedStatus,
        string expectedReason)
    {
        Assert.False(evaluation.IsAuthorized);
        Assert.Equal(expectedStatus, evaluation.Status);
        Assert.Equal(expectedReason, evaluation.ReasonId);
    }

    private static StarterStoneCardPlayCatalog Catalog() =>
        StarterStoneCardPlayCatalog.Create(SupportedContents());

    private static StarterStoneCardPlayDefinition Definition(
        StarterStoneCardPlayCatalog catalog,
        StarterStoneCardProfile profile) =>
        catalog.Definitions.Single(definition => definition.Profile == profile);

    private static CardContentDefinition[] SupportedContents() =>
    [
        Basic("card_basic_shape"),
        Extend("card_extend_shape"),
        Contact("card_contact_shape"),
        Lure("card_lure_shape"),
    ];

    private static CardContentDefinition Basic(string id) =>
        Raw(
            id,
            tags: [CardPlacementTag.Terminal, CardPlacementTag.Frontline],
            effects: [new PlaceStoneOperationDefinition(StoneContentKind.Basic)]);

    private static CardContentDefinition Extend(
        string id,
        int cost = 1,
        int minimumRealLiberties = 3,
        int cards = 1) =>
        Raw(
            id,
            cost: cost,
            tags: [CardPlacementTag.Frontline],
            effects:
            [
                new PlaceStoneOperationDefinition(StoneContentKind.Basic),
                new DrawIfRealLibertiesAtLeastOperationDefinition(
                    minimumRealLiberties,
                    cards),
            ]);

    private static CardContentDefinition Contact(string id) =>
        Raw(
            id,
            tags: [CardPlacementTag.Terminal, CardPlacementTag.Contact],
            effects:
            [
                new PlaceStoneOperationDefinition(StoneContentKind.Basic),
                new GainQiIfEnemyAtariOperationDefinition(1),
            ]);

    private static CardContentDefinition Lure(string id) =>
        Raw(
            id,
            cost: 0,
            tags: [CardPlacementTag.Contact],
            effects:
            [
                new PlaceStoneOperationDefinition(StoneContentKind.Lure),
                new ReserveDrawOperationDefinition(1),
            ],
            onCaptured: [new ReserveDrawOperationDefinition(2)]);

    private static CardContentDefinition Raw(
        string id = "card_invalid_shape",
        CardRarity rarity = CardRarity.Starter,
        int cost = 1,
        CardContentType type = CardContentType.Stone,
        CardTargetKind target = CardTargetKind.None,
        IEnumerable<CardPlacementTag>? tags = null,
        IEnumerable<CardOperationDefinition>? effects = null,
        IEnumerable<CardOperationDefinition>? onCaptured = null) =>
        CardContentDefinition.Create(
            id,
            rarity,
            cost,
            type,
            target,
            tags ?? [CardPlacementTag.Frontline],
            effects ?? [new PlaceStoneOperationDefinition(StoneContentKind.Basic)],
            onCaptured);

    private static BattleCardInstance Card(string instanceId, string contentId) =>
        new(instanceId, contentId);

    private static BattleDeckState ReadyDeck(params BattleCardInstance[] cards)
    {
        var rng = AuthoritativeRngState.Create(123);
        var shuffled = BattleDeckState.CreateShuffled(cards, rng);
        return shuffled.Deck.Draw(cards.Length, shuffled.RngAfter).DeckAfter;
    }
}
