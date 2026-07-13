using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;

namespace Igorogue.Domain.Tests;

public sealed class StarterReinforceCardPlayEvaluatorTests
{
    private static readonly BoardGeometry Geometry =
        BoardGeometry.Create(BoardGeometry.AcceptedSize);

    [Fact]
    public void ShapeProjectionPreservesOrderedOperationsWithoutContentIdSwitching()
    {
        var definition = StarterReinforceCardPlayDefinition.Create(
            ReinforceContent(
                "card_unfamiliar_reinforce",
                cost: 2,
                drawCards: 3,
                temporaryLiberties: 4));
        var differentRuntimeValue = StarterReinforceCardPlayDefinition.Create(
            ReinforceContent(
                "card_unfamiliar_reinforce",
                cost: 2,
                drawCards: 3,
                temporaryLiberties: 2));

        Assert.Equal("card_unfamiliar_reinforce", definition.ContentId);
        Assert.Equal(2, definition.Cost);
        Assert.Equal(CardTargetKind.FriendlyGroup, definition.Target);
        Assert.Equal(3, definition.DrawIfTargetAtari.Cards);
        Assert.Equal(4, definition.TemporaryLiberty.Amount);
        Assert.Equal(
            TemporaryLibertyDurationKind.EnemyTurnEnd,
            definition.TemporaryLiberty.DurationKind);
        Assert.Equal(
            TemporaryLibertyTiming.FirstEnemyTurnEndAtOrAfterGrant,
            definition.TemporaryLiberty.Timing);
        Assert.Equal(
            TemporaryLibertyStacking.AdditivePerEffectInstance,
            definition.TemporaryLiberty.Stacking);
        Assert.Collection(
            definition.Effects,
            effect => Assert.Same(definition.DrawIfTargetAtari, effect),
            effect => Assert.Same(definition.TemporaryLiberty, effect));
        Assert.Contains(
            "effect_0=draw_if_target_atari;cards=3",
            definition.CanonicalText,
            StringComparison.Ordinal);
        Assert.Contains(
            "effect_1=temporary_liberty;amount=4;duration=enemy_turn_end",
            definition.CanonicalText,
            StringComparison.Ordinal);
        Assert.Equal(definition.CanonicalText, definition.ToCanonicalText());
        Assert.Equal(
            DeterministicChecksum.Sha256Hex(definition.CanonicalText),
            definition.Checksum);
        Assert.NotEqual(definition.CanonicalText, differentRuntimeValue.CanonicalText);
        Assert.NotEqual(definition.Checksum, differentRuntimeValue.Checksum);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<CardOperationDefinition>)definition.Effects).Add(
                new ReserveDrawOperationDefinition(1)));
    }

    [Fact]
    public void ProjectionFailsClosedOnReversedAndInvalidShapes()
    {
        var draw = new DrawIfTargetAtariOperationDefinition(1);
        var temporary = TemporaryOperation(1);
        var invalid = new[]
        {
            RawContent(rarity: CardRarity.Common, effects: [draw, temporary]),
            RawContent(type: CardContentType.Stone, effects: [draw, temporary]),
            RawContent(target: CardTargetKind.None, effects: [draw, temporary]),
            RawContent(tags: [CardPlacementTag.Frontline], effects: [draw, temporary]),
            RawContent(effects: [temporary, draw]),
            RawContent(
                effects:
                [
                    new DrawIfRealLibertiesAtLeastOperationDefinition(1, 1),
                    temporary,
                ]),
            RawContent(
                effects:
                [
                    draw,
                    new ReserveDrawOperationDefinition(1),
                ]),
            RawContent(effects: [draw]),
            RawContent(
                effects: [draw, temporary],
                onCaptured: [new ReserveDrawOperationDefinition(1)]),
        };

        foreach (var content in invalid)
        {
            Assert.Throws<ArgumentException>(() =>
                StarterReinforceCardPlayDefinition.Create(content));
        }
    }

    [Fact]
    public void FriendlyGroupBindsCanonicalRuntimeAnchorAndEffectiveLibertiesAtSelection()
    {
        var definition = Definition();
        var deck = ReadyDeck(Card("reinforce-1", definition.ContentId));
        var board = AtariBlackGroupBoard();
        var stones = Runtime(board, reverseInput: true);
        var target = C(3, 2);
        var targetRuntime = stones.InstanceAt(target)!;
        var canonicalAnchor = stones.InstanceAt(C(2, 2))!;
        var temporary = TemporaryLibertyState.Create(
            stones,
            [new TemporaryLibertyEffect(
                "effect.existing",
                2,
                StoneColor.Black,
                targetRuntime.InstanceId,
                "test.timed",
                1,
                3)],
            2);
        var continuous = ContinuousLibertySnapshot.Create(
            stones,
            [new ContinuousLibertyModifier(
                "modifier.existing",
                -2,
                StoneColor.Black,
                canonicalAnchor.InstanceId,
                "test.continuous")]);

        var evaluation = StarterReinforceCardPlayEvaluator.Evaluate(
            definition,
            deck,
            1,
            "reinforce-1",
            stones,
            temporary,
            continuous,
            target);

        Assert.True(evaluation.IsAuthorized);
        Assert.Equal(StarterReinforceCardPlayStatus.Authorized, evaluation.Status);
        Assert.Equal("authorized", evaluation.ReasonId);
        Assert.Same(definition, evaluation.SourceDefinition);
        Assert.Same(deck, evaluation.SourceDeck);
        Assert.Same(stones, evaluation.SourceStones);
        Assert.Same(temporary, evaluation.SourceTemporaryLiberties);
        Assert.Same(continuous, evaluation.SourceContinuousLiberties);
        Assert.Same(target, evaluation.Target);
        Assert.Equal(C(2, 2), evaluation.TargetGroupAnchor);
        Assert.Equal(canonicalAnchor.InstanceId, evaluation.TargetAnchorStoneInstanceId);
        Assert.Equal(1, evaluation.TargetEffectiveLibertyCount);

        var analysis = Assert.IsType<TemporaryLibertyEffectiveLibertyAnalysis>(
            evaluation.EffectiveLibertyAnalysis);
        var group = analysis.GroupAnalysis.GroupAt(target)!;
        var breakdown = analysis.BreakdownFor(group);
        Assert.Equal(1, breakdown.RealLibertyCount);
        Assert.Equal(2, breakdown.TimedAmount);
        Assert.Equal(-2, breakdown.ContinuousAmount);
        Assert.Equal(1, breakdown.EffectiveLibertyCount);
        Assert.True(evaluation.IsBoundTo(
            definition,
            deck,
            1,
            "reinforce-1",
            stones,
            temporary,
            continuous,
            target));
    }

    [Fact]
    public void EmptyWhiteAndCardResourceFailuresHaveStableExactSources()
    {
        var definition = Definition();
        var board = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.White, 5, 5));
        var stones = Runtime(board);
        var temporary = TemporaryLibertyState.Create(stones, [], 1);
        var continuous = ContinuousLibertySnapshot.Empty(stones);
        var matchingDeck = ReadyDeck(Card("reinforce-1", definition.ContentId));
        var mismatchedDeck = ReadyDeck(Card("reinforce-1", "card_other_shape"));
        var activeDeckBefore = ReadyDeck(
            Card("reinforce-1", definition.ContentId),
            Card("active-1", definition.ContentId));
        var activeDeck = activeDeckBefore.BeginResolution(
            "active-1",
            AuthoritativeRngState.Create(87)).DeckAfter;

        AssertRejected(
            StarterReinforceCardPlayEvaluator.Evaluate(
                definition,
                matchingDeck,
                1,
                "reinforce-1",
                stones,
                temporary,
                continuous,
                C(4, 4)),
            StarterReinforceCardPlayStatus.TargetEmpty,
            "reinforce_target_empty");
        AssertRejected(
            StarterReinforceCardPlayEvaluator.Evaluate(
                definition,
                matchingDeck,
                1,
                "reinforce-1",
                stones,
                temporary,
                continuous,
                C(5, 5)),
            StarterReinforceCardPlayStatus.ForeignTarget,
            "reinforce_target_foreign");
        AssertRejected(
            StarterReinforceCardPlayEvaluator.Evaluate(
                definition,
                matchingDeck,
                1,
                "missing-1",
                stones,
                temporary,
                continuous,
                C(2, 2)),
            StarterReinforceCardPlayStatus.CardNotInHand,
            "card_not_in_hand");
        AssertRejected(
            StarterReinforceCardPlayEvaluator.Evaluate(
                definition,
                mismatchedDeck,
                1,
                "reinforce-1",
                stones,
                temporary,
                continuous,
                C(2, 2)),
            StarterReinforceCardPlayStatus.CardContentMismatch,
            "card_content_mismatch");
        AssertRejected(
            StarterReinforceCardPlayEvaluator.Evaluate(
                definition,
                matchingDeck,
                0,
                "reinforce-1",
                stones,
                temporary,
                continuous,
                C(2, 2)),
            StarterReinforceCardPlayStatus.InsufficientQi,
            "insufficient_qi");
        AssertRejected(
            StarterReinforceCardPlayEvaluator.Evaluate(
                definition,
                activeDeck,
                1,
                "reinforce-1",
                stones,
                temporary,
                continuous,
                C(2, 2)),
            StarterReinforceCardPlayStatus.ActiveResolutionExists,
            "active_resolution_exists");

        var empty = StarterReinforceCardPlayEvaluator.Evaluate(
            definition,
            matchingDeck,
            1,
            "reinforce-1",
            stones,
            temporary,
            continuous,
            C(4, 4));
        Assert.Same(definition, empty.SourceDefinition);
        Assert.Same(matchingDeck, empty.SourceDeck);
        Assert.Same(stones, empty.SourceStones);
        Assert.Same(temporary, empty.SourceTemporaryLiberties);
        Assert.Same(continuous, empty.SourceContinuousLiberties);
        Assert.Null(empty.TargetGroupAnchor);
        Assert.Null(empty.TargetAnchorStoneInstanceId);
        Assert.Null(empty.TargetEffectiveLibertyCount);
        Assert.Null(empty.EffectiveLibertyAnalysis);
    }

    [Fact]
    public void AuthorizationRemainsBoundToEveryExactImmutableSnapshot()
    {
        var definition = Definition();
        var equivalentDefinition = Definition();
        var deck = ReadyDeck(Card("reinforce-1", definition.ContentId));
        var equivalentDeck = ReadyDeck(Card("reinforce-1", definition.ContentId));
        var board = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.Black, 2, 3));
        var stones = Runtime(board);
        var temporary = TemporaryLibertyState.Create(stones, [], 1);
        var continuous = ContinuousLibertySnapshot.Empty(stones);
        var equivalentBoard = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.Black, 2, 3));
        var equivalentStones = Runtime(equivalentBoard);
        var equivalentTemporary = TemporaryLibertyState.Create(
            equivalentStones,
            [],
            1);
        var equivalentContinuous = ContinuousLibertySnapshot.Empty(equivalentStones);
        var target = C(2, 3);
        var evaluation = StarterReinforceCardPlayEvaluator.Evaluate(
            definition,
            deck,
            1,
            "reinforce-1",
            stones,
            temporary,
            continuous,
            target);

        Assert.True(evaluation.IsBoundTo(
            definition,
            deck,
            1,
            "reinforce-1",
            stones,
            temporary,
            continuous,
            target));
        Assert.False(evaluation.IsBoundTo(
            equivalentDefinition,
            deck,
            1,
            "reinforce-1",
            stones,
            temporary,
            continuous,
            target));
        Assert.False(evaluation.IsBoundTo(
            definition,
            equivalentDeck,
            1,
            "reinforce-1",
            stones,
            temporary,
            continuous,
            target));
        Assert.False(evaluation.IsBoundTo(
            definition,
            deck,
            2,
            "reinforce-1",
            stones,
            temporary,
            continuous,
            target));
        Assert.False(evaluation.IsBoundTo(
            definition,
            deck,
            1,
            "reinforce-other",
            stones,
            temporary,
            continuous,
            target));
        Assert.False(evaluation.IsBoundTo(
            definition,
            deck,
            1,
            "reinforce-1",
            equivalentStones,
            equivalentTemporary,
            equivalentContinuous,
            target));
        Assert.False(evaluation.IsBoundTo(
            definition,
            deck,
            1,
            "reinforce-1",
            stones,
            temporary,
            continuous,
            C(2, 2)));

        Assert.Throws<ArgumentException>(() =>
            StarterReinforceCardPlayEvaluator.Evaluate(
                definition,
                deck,
                1,
                "reinforce-1",
                stones,
                equivalentTemporary,
                continuous,
                target));
        Assert.Throws<ArgumentException>(() =>
            StarterReinforceCardPlayEvaluator.Evaluate(
                definition,
                deck,
                1,
                "reinforce-1",
                stones,
                temporary,
                equivalentContinuous,
                target));
    }

    [Fact]
    public void ReversedBoardRuntimeLibertyAndRecipeInputsProduceTheSameEvaluation()
    {
        var definition = Definition();
        var cardRecipe = new[]
        {
            Card("reinforce-1", definition.ContentId),
            Card("reinforce-2", definition.ContentId),
        };
        var boardStones = AtariBlackGroupStones();
        var firstBoard = Board(boardStones);
        var reversedBoard = Board(boardStones.Reverse().ToArray());
        var firstStones = Runtime(firstBoard);
        var reversedStones = Runtime(reversedBoard, reverseInput: true);
        var firstTemporary = OrderedTemporary(firstStones, reverseInput: false);
        var reversedTemporary = OrderedTemporary(reversedStones, reverseInput: true);
        var firstContinuous = OrderedContinuous(firstStones, reverseInput: false);
        var reversedContinuous = OrderedContinuous(reversedStones, reverseInput: true);
        var firstDeck = ReadyDeck(cardRecipe);
        var reversedDeck = ReadyDeck(cardRecipe.Reverse().ToArray());

        var first = StarterReinforceCardPlayEvaluator.Evaluate(
            definition,
            firstDeck,
            1,
            "reinforce-1",
            firstStones,
            firstTemporary,
            firstContinuous,
            C(3, 2));
        var reversed = StarterReinforceCardPlayEvaluator.Evaluate(
            definition,
            reversedDeck,
            1,
            "reinforce-1",
            reversedStones,
            reversedTemporary,
            reversedContinuous,
            C(3, 2));

        Assert.True(first.IsAuthorized);
        Assert.True(reversed.IsAuthorized);
        Assert.Equal(firstDeck.CanonicalText, reversedDeck.CanonicalText);
        Assert.Equal(firstStones.ToCanonicalText(), reversedStones.ToCanonicalText());
        Assert.Equal(
            firstTemporary.ToCanonicalText(),
            reversedTemporary.ToCanonicalText());
        Assert.Equal(
            firstContinuous.ToCanonicalText(),
            reversedContinuous.ToCanonicalText());
        Assert.Equal(first.TargetGroupAnchor, reversed.TargetGroupAnchor);
        Assert.Equal(
            first.TargetAnchorStoneInstanceId,
            reversed.TargetAnchorStoneInstanceId);
        Assert.Equal(
            first.TargetEffectiveLibertyCount,
            reversed.TargetEffectiveLibertyCount);
        Assert.Equal(
            first.EffectiveLibertyAnalysis!.ToCanonicalText(),
            reversed.EffectiveLibertyAnalysis!.ToCanonicalText());
    }

    private static void AssertRejected(
        StarterReinforceCardPlayEvaluation evaluation,
        StarterReinforceCardPlayStatus expectedStatus,
        string expectedReason)
    {
        Assert.False(evaluation.IsAuthorized);
        Assert.Equal(expectedStatus, evaluation.Status);
        Assert.Equal(expectedReason, evaluation.ReasonId);
    }

    private static StarterReinforceCardPlayDefinition Definition() =>
        StarterReinforceCardPlayDefinition.Create(
            ReinforceContent("card_reinforce_shape"));

    private static CardContentDefinition ReinforceContent(
        string id,
        int cost = 1,
        int drawCards = 1,
        int temporaryLiberties = 1) =>
        RawContent(
            id,
            cost: cost,
            effects:
            [
                new DrawIfTargetAtariOperationDefinition(drawCards),
                TemporaryOperation(temporaryLiberties),
            ]);

    private static TemporaryLibertyOperationDefinition TemporaryOperation(int amount) =>
        new(
            amount,
            TemporaryLibertyDurationKind.EnemyTurnEnd,
            TemporaryLibertyTiming.FirstEnemyTurnEndAtOrAfterGrant,
            TemporaryLibertyStacking.AdditivePerEffectInstance);

    private static CardContentDefinition RawContent(
        string id = "card_invalid_reinforce",
        CardRarity rarity = CardRarity.Starter,
        int cost = 1,
        CardContentType type = CardContentType.Technique,
        CardTargetKind target = CardTargetKind.FriendlyGroup,
        IEnumerable<CardPlacementTag>? tags = null,
        IEnumerable<CardOperationDefinition>? effects = null,
        IEnumerable<CardOperationDefinition>? onCaptured = null) =>
        CardContentDefinition.Create(
            id,
            rarity,
            cost,
            type,
            target,
            tags ?? [],
            effects ??
            [
                new DrawIfTargetAtariOperationDefinition(1),
                TemporaryOperation(1),
            ],
            onCaptured);

    private static BoardState AtariBlackGroupBoard() =>
        Board(AtariBlackGroupStones());

    private static BoardStone[] AtariBlackGroupStones() =>
    [
        Stone(StoneColor.Black, 2, 2),
        Stone(StoneColor.Black, 3, 2),
        Stone(StoneColor.White, 1, 2),
        Stone(StoneColor.White, 2, 1),
        Stone(StoneColor.White, 2, 3),
        Stone(StoneColor.White, 3, 1),
        Stone(StoneColor.White, 3, 3),
    ];

    private static TemporaryLibertyState OrderedTemporary(
        StoneRuntimeState stones,
        bool reverseInput)
    {
        var effects = new[]
        {
            new TemporaryLibertyEffect(
                "effect.a",
                1,
                StoneColor.Black,
                stones.InstanceAt(C(2, 2))!.InstanceId,
                "test.timed.a",
                1,
                3),
            new TemporaryLibertyEffect(
                "effect.b",
                2,
                StoneColor.Black,
                stones.InstanceAt(C(3, 2))!.InstanceId,
                "test.timed.b",
                2,
                3),
        };
        if (reverseInput)
        {
            Array.Reverse(effects);
        }

        return TemporaryLibertyState.Create(stones, effects, 3);
    }

    private static ContinuousLibertySnapshot OrderedContinuous(
        StoneRuntimeState stones,
        bool reverseInput)
    {
        var modifiers = new[]
        {
            new ContinuousLibertyModifier(
                "modifier.a",
                -1,
                StoneColor.Black,
                stones.InstanceAt(C(2, 2))!.InstanceId,
                "test.continuous.a"),
            new ContinuousLibertyModifier(
                "modifier.b",
                1,
                StoneColor.Black,
                stones.InstanceAt(C(3, 2))!.InstanceId,
                "test.continuous.b"),
        };
        if (reverseInput)
        {
            Array.Reverse(modifiers);
        }

        return ContinuousLibertySnapshot.Create(stones, modifiers);
    }

    private static BattleCardInstance Card(string instanceId, string contentId) =>
        new(instanceId, contentId);

    private static BattleDeckState ReadyDeck(params BattleCardInstance[] cards)
    {
        var rng = AuthoritativeRngState.Create(123);
        var shuffled = BattleDeckState.CreateShuffled(cards, rng);
        return shuffled.Deck.Draw(cards.Length, shuffled.RngAfter).DeckAfter;
    }

    private static StoneRuntimeState Runtime(
        BoardState board,
        bool reverseInput = false)
    {
        var instances = board.OccupiedStones
            .Select(stone => new StoneRuntimeInstance(
                StoneInstanceId(stone),
                stone,
                stone.IsKing ? "king" : "basic",
                board.Geometry.ToCanonicalIndex(stone.Point) + 1L,
                []))
            .ToArray();
        if (reverseInput)
        {
            Array.Reverse(instances);
        }

        return StoneRuntimeState.Create(
            board,
            instances,
            board.Geometry.PointCount + 1L);
    }

    private static string StoneInstanceId(BoardStone stone) =>
        $"stone.{ColorId(stone.Color)}.{stone.Point.X}.{stone.Point.Y}";

    private static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Unknown stone color."),
    };

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(StoneColor color, int x, int y) =>
        new(color, false, C(x, y));

    private static CanonicalPoint C(int x, int y) =>
        Geometry.CreateCanonicalPoint(x, y);
}
