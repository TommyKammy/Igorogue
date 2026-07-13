using System.Text;

using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Tests;

public sealed class BasicStoneCardPlayEvaluatorTests
{
    [Fact]
    public void ShapeProjectionIsCanonicalImmutableAndContentIdAgnostic()
    {
        var originalTags = new[]
        {
            CardPlacementTag.Terminal,
            CardPlacementTag.Frontline,
        };
        var first = BasicStoneCardPlayDefinition.Create(
            Content("card_shape_driven", originalTags));
        originalTags[0] = CardPlacementTag.Contact;
        var reversed = BasicStoneCardPlayDefinition.Create(
            Content(
                "card_shape_driven",
                [CardPlacementTag.Frontline, CardPlacementTag.Terminal]));

        Assert.Equal("card_shape_driven", first.ContentId);
        Assert.Equal(2, first.Cost);
        Assert.Equal(StoneContentKind.Basic, first.StoneKind);
        Assert.Equal(
            [CardPlacementTag.Frontline, CardPlacementTag.Terminal],
            first.PlacementTags);
        Assert.True(first.AllowsTerminalCapture);
        Assert.Equal(first.CanonicalText, first.ToCanonicalText());
        Assert.Equal(first.CanonicalText, reversed.CanonicalText);
        Assert.Equal(first.Checksum, reversed.Checksum);
        Assert.Equal(
            DeterministicChecksum.Sha256Hex(first.CanonicalText),
            first.Checksum);
        Assert.Equal(
            string.Join(
                '\n',
                BasicStoneCardPlayDefinition.EncodingVersion,
                $"content_id={Convert.ToBase64String(Encoding.UTF8.GetBytes("card_shape_driven"))}",
                "cost=2",
                "type=stone",
                "target=none",
                "stone_kind=basic",
                "placement_tag_count=2",
                "placement_tag_0=frontline",
                "placement_tag_1=terminal",
                "on_captured_count=0"),
            first.CanonicalText);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<CardPlacementTag>)first.PlacementTags).Add(CardPlacementTag.Contact));
    }

    [Fact]
    public void InvalidBasicStoneShapesAreRejected()
    {
        var invalid = new[]
        {
            Content(
                type: CardContentType.Technique),
            Content(
                target: CardTargetKind.FriendlyGroup),
            Content(
                effects:
                [
                    new PlaceStoneOperationDefinition(StoneContentKind.Basic),
                    new ReserveDrawOperationDefinition(1),
                ]),
            Content(
                effects: [new ReserveDrawOperationDefinition(1)]),
            Content(
                effects: [new PlaceStoneOperationDefinition(StoneContentKind.Lure)]),
            Content(
                placementTags: [CardPlacementTag.Terminal]),
            Content(
                placementTags:
                [CardPlacementTag.Frontline, CardPlacementTag.Contact]),
            Content(
                onCaptured: [new ReserveDrawOperationDefinition(1)]),
        };

        foreach (var content in invalid)
        {
            Assert.Throws<ArgumentException>(() =>
                BasicStoneCardPlayDefinition.Create(content));
        }
    }

    [Fact]
    public void FrontlineModeAuthorizesOnlyAnEmptyPointAdjacentToBlack()
    {
        var definition = BasicStoneCardPlayDefinition.Create(Content());
        var deck = ReadyDeck(Card("basic-1", definition.ContentId));
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var board = BoardState.Create(
            geometry,
            [new BoardStone(StoneColor.Black, false, geometry.CreateCanonicalPoint(2, 2))]);
        var target = geometry.CreateCanonicalPoint(2, 3);

        var evaluation = BasicStoneCardPlayEvaluator.Evaluate(
            definition,
            deck,
            2,
            "basic-1",
            board,
            target,
            BasicStoneCardPlacementMode.Frontline);

        Assert.True(evaluation.IsAuthorized);
        Assert.Equal(BasicStoneCardPlayStatus.Authorized, evaluation.Status);
        Assert.Equal("authorized", evaluation.ReasonId);
        Assert.Same(definition, evaluation.SourceDefinition);
        Assert.Same(deck, evaluation.SourceDeck);
        Assert.Equal(2, evaluation.SourceQi);
        Assert.Equal("basic-1", evaluation.RequestedInstanceId);
        Assert.Same(deck.Hand.Single(), evaluation.Card);
        Assert.Same(board, evaluation.SourceBoard);
        Assert.Same(target, evaluation.Target);
        Assert.Equal(BasicStoneCardPlacementMode.Frontline, evaluation.RequestedMode);
        Assert.Equal(PlacementAccessMode.Normal, evaluation.AccessMode);
        Assert.True(evaluation.IsBoundTo(
            definition,
            deck,
            2,
            "basic-1",
            board,
            target,
            BasicStoneCardPlacementMode.Frontline,
            PlacementAccessMode.Normal));
    }

    [Fact]
    public void TerminalModePreauthorizesRemoteTargetButLeavesCaptureProofToPlacementLegality()
    {
        var definition = BasicStoneCardPlayDefinition.Create(
            Content(
                placementTags:
                [CardPlacementTag.Terminal, CardPlacementTag.Frontline]));
        var deck = ReadyDeck(Card("basic-1", definition.ContentId));
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var board = BoardState.Create(
            geometry,
            [new BoardStone(StoneColor.Black, false, geometry.CreateCanonicalPoint(2, 2))]);
        var remote = geometry.CreateCanonicalPoint(7, 7);

        var evaluation = BasicStoneCardPlayEvaluator.Evaluate(
            definition,
            deck,
            2,
            "basic-1",
            board,
            remote,
            BasicStoneCardPlacementMode.TerminalCapture);

        Assert.True(evaluation.IsAuthorized);
        Assert.Equal(PlacementAccessMode.TerminalCapture, evaluation.AccessMode);
        Assert.True(evaluation.IsBoundTo(
            definition,
            deck,
            2,
            "basic-1",
            board,
            remote,
            BasicStoneCardPlacementMode.TerminalCapture,
            PlacementAccessMode.TerminalCapture));
    }

    [Fact]
    public void RejectionsHaveStableStatusesAndReasons()
    {
        var terminalDefinition = BasicStoneCardPlayDefinition.Create(
            Content(
                placementTags:
                [CardPlacementTag.Frontline, CardPlacementTag.Terminal]));
        var frontlineOnlyDefinition = BasicStoneCardPlayDefinition.Create(Content());
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var blackPoint = geometry.CreateCanonicalPoint(2, 2);
        var adjacent = geometry.CreateCanonicalPoint(2, 3);
        var remote = geometry.CreateCanonicalPoint(7, 7);
        var board = BoardState.Create(
            geometry,
            [new BoardStone(StoneColor.Black, false, blackPoint)]);
        var matchingDeck = ReadyDeck(Card("basic-1", terminalDefinition.ContentId));
        var mismatchedDeck = ReadyDeck(Card("basic-1", "card_other_shape"));
        var activeDeckBefore = ReadyDeck(
            Card("basic-1", terminalDefinition.ContentId),
            Card("other-1", terminalDefinition.ContentId));
        var activeDeck = activeDeckBefore.BeginResolution(
            "other-1",
            AuthoritativeRngState.Create(77)).DeckAfter;

        AssertRejected(
            BasicStoneCardPlayEvaluator.Evaluate(
                terminalDefinition,
                matchingDeck,
                2,
                "missing-1",
                board,
                adjacent,
                BasicStoneCardPlacementMode.Frontline),
            BasicStoneCardPlayStatus.CardNotInHand,
            "card_not_in_hand");
        AssertRejected(
            BasicStoneCardPlayEvaluator.Evaluate(
                terminalDefinition,
                mismatchedDeck,
                2,
                "basic-1",
                board,
                adjacent,
                BasicStoneCardPlacementMode.Frontline),
            BasicStoneCardPlayStatus.CardContentMismatch,
            "card_content_mismatch");
        AssertRejected(
            BasicStoneCardPlayEvaluator.Evaluate(
                terminalDefinition,
                matchingDeck,
                1,
                "basic-1",
                board,
                adjacent,
                BasicStoneCardPlacementMode.Frontline),
            BasicStoneCardPlayStatus.InsufficientQi,
            "insufficient_qi");
        AssertRejected(
            BasicStoneCardPlayEvaluator.Evaluate(
                terminalDefinition,
                activeDeck,
                2,
                "basic-1",
                board,
                adjacent,
                BasicStoneCardPlacementMode.Frontline),
            BasicStoneCardPlayStatus.ActiveResolutionExists,
            "active_resolution_exists");
        AssertRejected(
            BasicStoneCardPlayEvaluator.Evaluate(
                terminalDefinition,
                matchingDeck,
                2,
                "basic-1",
                board,
                adjacent,
                (BasicStoneCardPlacementMode)255),
            BasicStoneCardPlayStatus.UnsupportedMode,
            "unsupported_placement_mode");
        var frontlineOnlyDeck = ReadyDeck(
            Card("basic-1", frontlineOnlyDefinition.ContentId));
        AssertRejected(
            BasicStoneCardPlayEvaluator.Evaluate(
                frontlineOnlyDefinition,
                frontlineOnlyDeck,
                2,
                "basic-1",
                board,
                adjacent,
                BasicStoneCardPlacementMode.TerminalCapture),
            BasicStoneCardPlayStatus.UnsupportedMode,
            "unsupported_placement_mode");
        AssertRejected(
            BasicStoneCardPlayEvaluator.Evaluate(
                terminalDefinition,
                matchingDeck,
                2,
                "basic-1",
                board,
                blackPoint,
                BasicStoneCardPlacementMode.Frontline),
            BasicStoneCardPlayStatus.TargetOccupied,
            "target_occupied");
        AssertRejected(
            BasicStoneCardPlayEvaluator.Evaluate(
                terminalDefinition,
                matchingDeck,
                2,
                "basic-1",
                board,
                remote,
                BasicStoneCardPlacementMode.Frontline),
            BasicStoneCardPlayStatus.FrontlineAdjacencyRequired,
            "frontline_adjacency_required");
    }

    [Fact]
    public void AuthorizationRemainsBoundToItsExactImmutableSources()
    {
        var definition = BasicStoneCardPlayDefinition.Create(Content());
        var equivalentDefinition = BasicStoneCardPlayDefinition.Create(Content());
        var deck = ReadyDeck(Card("basic-1", definition.ContentId));
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var blackPoint = geometry.CreateCanonicalPoint(2, 2);
        var target = geometry.CreateCanonicalPoint(2, 3);
        var board = BoardState.Create(
            geometry,
            [new BoardStone(StoneColor.Black, false, blackPoint)]);
        var evaluation = BasicStoneCardPlayEvaluator.Evaluate(
            definition,
            deck,
            2,
            "basic-1",
            board,
            target,
            BasicStoneCardPlacementMode.Frontline);
        var deckCanonicalBefore = deck.CanonicalText;
        var boardBefore = board.OccupiedStones.ToArray();
        var nextDeck = deck.BeginResolution(
            "basic-1",
            AuthoritativeRngState.Create(88)).DeckAfter;
        var equivalentBoard = BoardState.Create(geometry, board.OccupiedStones);

        Assert.Equal(deckCanonicalBefore, deck.CanonicalText);
        Assert.Equal(boardBefore, board.OccupiedStones);
        Assert.False(evaluation.IsBoundTo(
            equivalentDefinition,
            deck,
            2,
            "basic-1",
            board,
            target,
            BasicStoneCardPlacementMode.Frontline,
            PlacementAccessMode.Normal));
        Assert.False(evaluation.IsBoundTo(
            definition,
            nextDeck,
            2,
            "basic-1",
            board,
            target,
            BasicStoneCardPlacementMode.Frontline,
            PlacementAccessMode.Normal));
        Assert.False(evaluation.IsBoundTo(
            definition,
            deck,
            3,
            "basic-1",
            board,
            target,
            BasicStoneCardPlacementMode.Frontline,
            PlacementAccessMode.Normal));
        Assert.False(evaluation.IsBoundTo(
            definition,
            deck,
            2,
            "basic-1",
            equivalentBoard,
            target,
            BasicStoneCardPlacementMode.Frontline,
            PlacementAccessMode.Normal));
        Assert.False(evaluation.IsBoundTo(
            definition,
            deck,
            2,
            "basic-1",
            board,
            target,
            BasicStoneCardPlacementMode.Frontline,
            PlacementAccessMode.TerminalCapture));
    }

    private static void AssertRejected(
        BasicStoneCardPlayEvaluation evaluation,
        BasicStoneCardPlayStatus expectedStatus,
        string expectedReason)
    {
        Assert.False(evaluation.IsAuthorized);
        Assert.Equal(expectedStatus, evaluation.Status);
        Assert.Equal(expectedReason, evaluation.ReasonId);
    }

    private static CardContentDefinition Content(
        string id = "card_shape_driven",
        IEnumerable<CardPlacementTag>? placementTags = null,
        CardContentType type = CardContentType.Stone,
        CardTargetKind target = CardTargetKind.None,
        IEnumerable<CardOperationDefinition>? effects = null,
        IEnumerable<CardOperationDefinition>? onCaptured = null) =>
        CardContentDefinition.Create(
            id,
            CardRarity.Starter,
            2,
            type,
            target,
            placementTags ?? [CardPlacementTag.Frontline],
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
