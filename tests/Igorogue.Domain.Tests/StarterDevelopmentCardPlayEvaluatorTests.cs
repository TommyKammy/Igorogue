using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Tests;

public sealed class StarterDevelopmentCardPlayEvaluatorTests
{
    private static readonly BoardGeometry Geometry = FacilityFixtureData.Geometry;
    private static readonly Lazy<FacilityRuntimePolicy> Policy =
        new(FacilityFixtureData.LoadRuntimePolicy);

    [Fact]
    public void ShapeProjectionBindsContentAuthoredCostAndFacilityWithoutIdSwitching()
    {
        var definition = StarterDevelopmentCardPlayDefinition.Create(
            DevelopmentContent(
                "card_unfamiliar_development",
                cost: 3,
                facilityContentId: "unfamiliar_facility"));
        var differentRuntimeValue = StarterDevelopmentCardPlayDefinition.Create(
            DevelopmentContent(
                "card_unfamiliar_development",
                cost: 3,
                facilityContentId: "other_facility"));

        Assert.Equal("card_unfamiliar_development", definition.ContentId);
        Assert.Equal(3, definition.Cost);
        Assert.Equal(CardTargetKind.BlackTerritoryEmpty, definition.Target);
        Assert.Equal("unfamiliar_facility", definition.FacilityContentId);
        Assert.Same(definition.BuildFacility, Assert.Single(definition.Effects));
        Assert.Contains("type=territory", definition.CanonicalText, StringComparison.Ordinal);
        Assert.Contains(
            "target=black_territory_empty",
            definition.CanonicalText,
            StringComparison.Ordinal);
        Assert.Contains(
            "effect_0=build_facility;facility=dW5mYW1pbGlhcl9mYWNpbGl0eQ==",
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
                new BuildFacilityOperationDefinition("another_facility")));
    }

    [Fact]
    public void ProjectionFailsClosedOnUnsupportedShapeAndExtraOperations()
    {
        var build = new BuildFacilityOperationDefinition("development");
        var invalid = new[]
        {
            RawContent(rarity: CardRarity.Common, effects: [build]),
            RawContent(type: CardContentType.Technique, effects: [build]),
            RawContent(target: CardTargetKind.FriendlyGroup, effects: [build]),
            RawContent(tags: [CardPlacementTag.Frontline], effects: [build]),
            RawContent(effects: [new ReserveDrawOperationDefinition(1)]),
            RawContent(
                effects:
                [
                    build,
                    new BuildFacilityOperationDefinition("furnace"),
                ]),
            RawContent(
                effects: [build],
                onCaptured: [new ReserveDrawOperationDefinition(1)]),
        };

        foreach (var content in invalid)
        {
            Assert.Throws<ArgumentException>(() =>
                StarterDevelopmentCardPlayDefinition.Create(content));
        }
    }

    [Fact]
    public void AuthorizedPlayExposesOnlyTheSharedLegalBuildEvaluationForCommit()
    {
        var definition = Definition();
        var deck = ReadyDeck(Card("development-1", definition.ContentId));
        var analysis = Analyze(
            Board(Stone(StoneColor.Black, 7, 7)),
            [],
            nextSequence: 1);
        var target = C(1, 1);

        var evaluation = StarterDevelopmentCardPlayEvaluator.Evaluate(
            definition,
            deck,
            qi: 2,
            "development-1",
            analysis,
            target,
            "facility.development.1");

        Assert.True(evaluation.IsAuthorized);
        Assert.Equal(StarterDevelopmentCardPlayStatus.Authorized, evaluation.Status);
        Assert.Equal("authorized", evaluation.ReasonId);
        Assert.Same(definition, evaluation.SourceDefinition);
        Assert.Same(deck, evaluation.SourceDeck);
        Assert.Same(analysis, evaluation.SourceFacilityAnalysis);
        Assert.Same(analysis.FacilityState, evaluation.SourceFacilityState);
        Assert.Same(analysis.Policy, evaluation.SourceFacilityPolicy);
        Assert.Same(target, evaluation.Target);
        Assert.Equal("facility.development.1", evaluation.FacilityInstanceId);
        Assert.Equal(StoneColor.Black, evaluation.BuildRequest.ActorColor);
        Assert.Equal("development", evaluation.BuildRequest.FacilityContentId);

        var legal = evaluation.LegalFacilityBuildEvaluation;
        Assert.Same(evaluation.BuildEvaluation, legal);
        Assert.Same(analysis, legal.Analysis);
        Assert.Same(evaluation.BuildRequest, legal.Request);
        Assert.True(evaluation.IsBoundTo(
            definition,
            deck,
            2,
            "development-1",
            analysis,
            target,
            "facility.development.1"));

        var commit = FacilityBuildEvaluator.Commit(legal);
        Assert.Equal("development", commit.BuiltFacility.ContentId);
        Assert.Equal("facility.development.1", commit.BuiltFacility.InstanceId);
        Assert.Equal(target, commit.BuiltFacility.Point);
        Assert.Same(analysis.FacilityState.SourceBoard, commit.StateAfterCommit.SourceBoard);
        Assert.Empty(analysis.FacilityState.InstalledFacilities);
        Assert.Single(commit.StateAfterCommit.InstalledFacilities);
    }

    [Fact]
    public void SharedFacilityKernelOwnsEveryFacilityLegalityReasonAndPrecedence()
    {
        var definition = Definition();
        var deck = ReadyDeck(Card("development-1", definition.ContentId));
        var cornerBoard = Board(
            Stone(StoneColor.Black, 1, 3),
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.Black, 3, 1));
        var controlledBoard = Board(Stone(StoneColor.Black, 7, 7));
        var scenarios = new[]
        {
            new RejectionScenario(
                StarterDevelopmentCardPlayStatus.TargetHasStone,
                "facility_target_has_stone",
                Analyze(Board(Stone(StoneColor.Black, 1, 1)), [], 1),
                C(1, 1),
                "facility.stone"),
            new RejectionScenario(
                StarterDevelopmentCardPlayStatus.TargetOccupied,
                "facility_target_occupied",
                Analyze(
                    controlledBoard,
                    [Facility("existing", "development", 1, 1, 1)],
                    2),
                C(1, 1),
                "facility.occupied"),
            new RejectionScenario(
                StarterDevelopmentCardPlayStatus.TargetNotOwnedTerritory,
                "facility_target_not_owned_territory",
                Analyze(Board(Stone(StoneColor.White, 7, 7)), [], 1),
                C(1, 1),
                "facility.not-owned"),
            new RejectionScenario(
                StarterDevelopmentCardPlayStatus.CapacityFull,
                "facility_capacity_full",
                Analyze(
                    cornerBoard,
                    [Facility("market-one", "market", 1, 1, 1)],
                    2),
                C(1, 2),
                "facility.capacity"),
            new RejectionScenario(
                StarterDevelopmentCardPlayStatus.TypeLimitReached,
                "facility_type_limit_reached",
                Analyze(
                    controlledBoard,
                    [
                        Facility("development-one", "development", 1, 1, 1),
                        Facility("development-two", "development", 2, 1, 2),
                    ],
                    3),
                C(3, 1),
                "facility.type-limit"),
        };

        foreach (var scenario in scenarios)
        {
            var facilityBefore = scenario.Analysis.FacilityState.ToCanonicalText();
            var deckBefore = deck.ToCanonicalText();
            var evaluation = StarterDevelopmentCardPlayEvaluator.Evaluate(
                definition,
                deck,
                qi: 2,
                "development-1",
                scenario.Analysis,
                scenario.Target,
                scenario.FacilityInstanceId);

            Assert.False(evaluation.IsAuthorized);
            Assert.Equal(scenario.Status, evaluation.Status);
            Assert.Equal(scenario.ReasonId, evaluation.ReasonId);
            var shared = Assert.IsType<FacilityBuildEvaluation>(
                evaluation.BuildEvaluation);
            Assert.False(shared.IsLegal);
            Assert.Equal(scenario.ReasonId, shared.ReasonId);
            Assert.Same(scenario.Analysis, shared.Analysis);
            Assert.Same(evaluation.BuildRequest, shared.Request);
            Assert.Throws<InvalidOperationException>(() =>
                FacilityBuildEvaluator.Commit(shared));
            Assert.Throws<InvalidOperationException>(() =>
                _ = evaluation.LegalFacilityBuildEvaluation);
            Assert.Equal(facilityBefore, scenario.Analysis.FacilityState.ToCanonicalText());
            Assert.Equal(deckBefore, deck.ToCanonicalText());
        }
    }

    [Fact]
    public void CardAndResourceRejectionsDoNotInvokeOrMutateFacilityKernel()
    {
        var definition = Definition();
        var analysis = Analyze(
            Board(Stone(StoneColor.Black, 7, 7)),
            [],
            nextSequence: 1);
        var matching = ReadyDeck(Card("development-1", definition.ContentId));
        var mismatched = ReadyDeck(Card("development-1", "card_other"));
        var activeBefore = ReadyDeck(
            Card("development-1", definition.ContentId),
            Card("active-1", definition.ContentId));
        var active = activeBefore.BeginResolution(
            "active-1",
            AuthoritativeRngState.Create(44)).DeckAfter;
        var target = C(1, 1);
        var stateBefore = analysis.FacilityState.ToCanonicalText();

        AssertEarlyRejection(
            StarterDevelopmentCardPlayEvaluator.Evaluate(
                definition,
                matching,
                2,
                "missing-1",
                analysis,
                target,
                "facility.missing"),
            StarterDevelopmentCardPlayStatus.CardNotInHand,
            "card_not_in_hand");
        AssertEarlyRejection(
            StarterDevelopmentCardPlayEvaluator.Evaluate(
                definition,
                mismatched,
                2,
                "development-1",
                analysis,
                target,
                "facility.mismatch"),
            StarterDevelopmentCardPlayStatus.CardContentMismatch,
            "card_content_mismatch");
        AssertEarlyRejection(
            StarterDevelopmentCardPlayEvaluator.Evaluate(
                definition,
                matching,
                1,
                "development-1",
                analysis,
                target,
                "facility.no-qi"),
            StarterDevelopmentCardPlayStatus.InsufficientQi,
            "insufficient_qi");
        AssertEarlyRejection(
            StarterDevelopmentCardPlayEvaluator.Evaluate(
                definition,
                active,
                2,
                "development-1",
                analysis,
                target,
                "facility.active"),
            StarterDevelopmentCardPlayStatus.ActiveResolutionExists,
            "active_resolution_exists");

        Assert.Equal(stateBefore, analysis.FacilityState.ToCanonicalText());
        Assert.Empty(analysis.FacilityState.InstalledFacilities);
    }

    [Fact]
    public void AuthorizationRejectsEquivalentButNonidenticalSources()
    {
        var definition = Definition();
        var equivalentDefinition = Definition();
        var deck = ReadyDeck(Card("development-1", definition.ContentId));
        var equivalentDeck = ReadyDeck(Card("development-1", definition.ContentId));
        var board = Board(Stone(StoneColor.Black, 7, 7));
        var state = FacilityState.Create(board, [], 1);
        var territory = TerritoryAnalyzer.Analyze(board);
        var analysis = FacilityRuntimeAnalyzer.Analyze(state, territory, Policy.Value);
        var equivalentPolicyAnalysis = FacilityRuntimeAnalyzer.Analyze(
            state,
            territory,
            FacilityFixtureData.LoadRuntimePolicy());
        var equivalentStateAnalysis = Analyze(
            Board(Stone(StoneColor.Black, 7, 7)),
            [],
            1);
        var target = C(1, 1);
        var evaluation = StarterDevelopmentCardPlayEvaluator.Evaluate(
            definition,
            deck,
            2,
            "development-1",
            analysis,
            target,
            "facility.development.1");

        Assert.Same(state, evaluation.SourceFacilityState);
        Assert.Same(Policy.Value, evaluation.SourceFacilityPolicy);
        Assert.True(evaluation.IsBoundTo(
            definition,
            deck,
            2,
            "development-1",
            analysis,
            target,
            "facility.development.1"));
        Assert.False(evaluation.IsBoundTo(
            equivalentDefinition,
            deck,
            2,
            "development-1",
            analysis,
            target,
            "facility.development.1"));
        Assert.False(evaluation.IsBoundTo(
            definition,
            equivalentDeck,
            2,
            "development-1",
            analysis,
            target,
            "facility.development.1"));
        Assert.False(evaluation.IsBoundTo(
            definition,
            deck,
            3,
            "development-1",
            analysis,
            target,
            "facility.development.1"));
        Assert.False(evaluation.IsBoundTo(
            definition,
            deck,
            2,
            "other-card",
            analysis,
            target,
            "facility.development.1"));
        Assert.False(evaluation.IsBoundTo(
            definition,
            deck,
            2,
            "development-1",
            equivalentPolicyAnalysis,
            target,
            "facility.development.1"));
        Assert.False(evaluation.IsBoundTo(
            definition,
            deck,
            2,
            "development-1",
            equivalentStateAnalysis,
            target,
            "facility.development.1"));
        Assert.False(evaluation.IsBoundTo(
            definition,
            deck,
            2,
            "development-1",
            analysis,
            C(2, 1),
            "facility.development.1"));
        Assert.False(evaluation.IsBoundTo(
            definition,
            deck,
            2,
            "development-1",
            analysis,
            target,
            "facility.development.2"));
    }

    [Fact]
    public void ReversedRecipeAndFacilityInputsProduceCanonicalEquivalentResults()
    {
        var firstDefinition = Definition();
        var secondDefinition = Definition();
        var cards = new[]
        {
            Card("development-1", firstDefinition.ContentId),
            Card("development-2", firstDefinition.ContentId),
        };
        var facilities = new[]
        {
            Facility("development-one", "development", 1, 1, 1),
            Facility("development-two", "development", 2, 1, 2),
        };
        var firstDeck = ReadyDeck(cards);
        var reversedDeck = ReadyDeck(cards.Reverse().ToArray());
        var board = Board(Stone(StoneColor.Black, 7, 7));
        var firstAnalysis = Analyze(board, facilities, 3);
        var reversedAnalysis = Analyze(board, facilities.Reverse(), 3);

        var first = StarterDevelopmentCardPlayEvaluator.Evaluate(
            firstDefinition,
            firstDeck,
            2,
            "development-1",
            firstAnalysis,
            C(3, 1),
            "facility.next");
        var reversed = StarterDevelopmentCardPlayEvaluator.Evaluate(
            secondDefinition,
            reversedDeck,
            2,
            "development-1",
            reversedAnalysis,
            C(3, 1),
            "facility.next");

        Assert.Equal(firstDefinition.CanonicalText, secondDefinition.CanonicalText);
        Assert.Equal(firstDefinition.Checksum, secondDefinition.Checksum);
        Assert.Equal(firstDeck.CanonicalText, reversedDeck.CanonicalText);
        Assert.Equal(
            firstAnalysis.FacilityState.ToCanonicalText(),
            reversedAnalysis.FacilityState.ToCanonicalText());
        Assert.Equal(first.Status, reversed.Status);
        Assert.Equal(first.ReasonId, reversed.ReasonId);
        Assert.Equal(StarterDevelopmentCardPlayStatus.TypeLimitReached, first.Status);
        Assert.Equal(
            first.BuildEvaluation?.Status,
            reversed.BuildEvaluation?.Status);
        Assert.Equal(first.BuildRequest.ActorColor, reversed.BuildRequest.ActorColor);
        Assert.Equal(
            first.BuildRequest.FacilityContentId,
            reversed.BuildRequest.FacilityContentId);
        Assert.Equal(first.BuildRequest.Point, reversed.BuildRequest.Point);
    }

    private static void AssertEarlyRejection(
        StarterDevelopmentCardPlayEvaluation evaluation,
        StarterDevelopmentCardPlayStatus expectedStatus,
        string expectedReason)
    {
        Assert.False(evaluation.IsAuthorized);
        Assert.Equal(expectedStatus, evaluation.Status);
        Assert.Equal(expectedReason, evaluation.ReasonId);
        Assert.Null(evaluation.BuildEvaluation);
        Assert.Throws<InvalidOperationException>(() =>
            _ = evaluation.LegalFacilityBuildEvaluation);
    }

    private static StarterDevelopmentCardPlayDefinition Definition() =>
        StarterDevelopmentCardPlayDefinition.Create(
            DevelopmentContent("card_development"));

    private static CardContentDefinition DevelopmentContent(
        string id,
        int cost = 2,
        string facilityContentId = "development") =>
        RawContent(
            id,
            cost: cost,
            effects: [new BuildFacilityOperationDefinition(facilityContentId)]);

    private static CardContentDefinition RawContent(
        string id = "card_invalid_development",
        CardRarity rarity = CardRarity.Starter,
        int cost = 2,
        CardContentType type = CardContentType.Territory,
        CardTargetKind target = CardTargetKind.BlackTerritoryEmpty,
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
            effects ?? [new BuildFacilityOperationDefinition("development")],
            onCaptured);

    private static FacilityRuntimeAnalysis Analyze(
        BoardState board,
        IEnumerable<FacilityInstance> facilities,
        long nextSequence) =>
        FacilityRuntimeAnalyzer.Analyze(
            FacilityState.Create(board, facilities, nextSequence),
            TerritoryAnalyzer.Analyze(board),
            Policy.Value);

    private static FacilityInstance Facility(
        string id,
        string contentId,
        int x,
        int y,
        long sequence) =>
        new(id, contentId, StoneColor.Black, C(x, y), sequence);

    private static BattleCardInstance Card(string instanceId, string contentId) =>
        new(instanceId, contentId);

    private static BattleDeckState ReadyDeck(params BattleCardInstance[] cards)
    {
        var rng = AuthoritativeRngState.Create(123);
        var shuffled = BattleDeckState.CreateShuffled(cards, rng);
        return shuffled.Deck.Draw(cards.Length, shuffled.RngAfter).DeckAfter;
    }

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(StoneColor color, int x, int y) =>
        new(color, false, C(x, y));

    private static CanonicalPoint C(int x, int y) =>
        Geometry.CreateCanonicalPoint(x, y);

    private sealed record RejectionScenario(
        StarterDevelopmentCardPlayStatus Status,
        string ReasonId,
        FacilityRuntimeAnalysis Analysis,
        CanonicalPoint Target,
        string FacilityInstanceId);
}
