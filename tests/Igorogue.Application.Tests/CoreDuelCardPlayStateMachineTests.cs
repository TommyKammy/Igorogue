using System.Globalization;

using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

public sealed class CoreDuelCardPlayStateMachineTests
{
    private const string ContentHash =
        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const long Seed = 340034;

    private static readonly BoardGeometry Geometry =
        BoardGeometry.Create(BoardGeometry.AcceptedSize);

    [Fact]
    public void AcceptedFrontlinePlaySpendsQiResolvesCardAndLogsCompositeChecksum()
    {
        var definition = Definition(cost: 1);
        var board = NeutralFrontlineBoard();
        var session = StartSession(board, definition, baseQi: 3);
        var rngBefore = session.State.CardTurnState.RngState;
        var deckBefore = session.State.CardTurnState.Deck;
        var target = C(2, 3);

        var result = Execute(
            session,
            "play-card",
            target,
            BasicStoneCardPlacementMode.Frontline);

        Assert.True(result.Accepted);
        Assert.Equal("accepted", result.ReasonId);
        Assert.NotSame(session, result.SessionAfter);
        Assert.NotSame(session.State, result.SessionAfter.State);
        Assert.NotEqual(session.State.Checksum, result.StateChecksum);
        Assert.Equal(
            DeterministicChecksum.Sha256Hex(result.SessionAfter.State.CanonicalText),
            result.StateChecksum);
        Assert.Contains(
            "basic_stone_definition=",
            result.SessionAfter.State.CanonicalText,
            StringComparison.Ordinal);

        var qi = Assert.IsType<QiChangedFact>(result.OrderedFacts[0]);
        Assert.Equal(3, qi.OldAmount);
        Assert.Equal(2, qi.NewAmount);
        Assert.Equal(-1, qi.Delta);
        Assert.Equal("card_cost", qi.ReasonId);
        Assert.Equal("play-card", qi.SourceId);
        Assert.IsType<StonePlacedFact>(result.OrderedFacts[1]);
        Assert.Contains(result.OrderedFacts, fact => fact is StoneTopologyRegisteredFact);
        Assert.Contains(result.OrderedFacts, fact => fact is KingCaptureEvaluatedFact);

        var afterTurn = result.SessionAfter.State.CardTurnState;
        Assert.Equal(2, afterTurn.Qi);
        Assert.Empty(afterTurn.Deck.Hand);
        var resolved = Assert.Single(afterTurn.Deck.Resolving);
        Assert.Equal("play-card", resolved.Card.InstanceId);
        Assert.True(resolved.IsResolved);
        Assert.Empty(deckBefore.Resolving);
        Assert.Same(rngBefore, afterTurn.RngState);
        Assert.Same(
            result.SessionAfter.State.BattleState.RngState,
            afterTurn.RngState);
        Assert.Equal(StoneColor.Black, result.SessionAfter.State.BattleState.Board.StoneAt(target)?.Color);

        var entry = Assert.Single(result.SessionAfter.CommandLog.Entries);
        Assert.Equal("battle.play_card", entry.CommandType);
        Assert.Equal(result.StateChecksum, entry.ResultChecksum);
        Assert.Equal(result.LogChecksum, entry.LogChecksum);
        Assert.NotEqual(session.CommandLog.CurrentChecksum, result.LogChecksum);
    }

    [Fact]
    public void RejectionsPreserveEveryAuthoritativeReferenceAndDoNotAppendLog()
    {
        var definition = Definition(cost: 1, terminal: true);
        var board = NeutralFrontlineBoard();
        var valid = StartSession(board, definition, baseQi: 3);

        var staleState = CoreDuelCardPlayStateMachine.Execute(
            valid,
            Command(
                valid,
                "play-card",
                C(2, 3),
                BasicStoneCardPlacementMode.Frontline,
                expectedStateChecksum: OtherChecksum(valid.State.Checksum)));
        AssertRejectedNoOp(valid, staleState, "stale_state");

        var staleLog = CoreDuelCardPlayStateMachine.Execute(
            valid,
            Command(
                valid,
                "play-card",
                C(2, 3),
                BasicStoneCardPlacementMode.Frontline,
                expectedLogChecksum: OtherChecksum(valid.CommandLog.CurrentChecksum)));
        AssertRejectedNoOp(valid, staleLog, "stale_session");

        AssertRejectedNoOp(
            valid,
            Execute(
                valid,
                "missing-card",
                C(2, 3),
                BasicStoneCardPlacementMode.Frontline),
            "card_not_in_hand");

        var mismatched = StartSession(
            board,
            definition,
            baseQi: 3,
            cards: [new BattleCardInstance("play-card", "card_other_shape")]);
        AssertRejectedNoOp(
            mismatched,
            Execute(
                mismatched,
                "play-card",
                C(2, 3),
                BasicStoneCardPlacementMode.Frontline),
            "card_content_mismatch");

        var expensive = Definition(cost: 9, terminal: true);
        var insufficient = StartSession(board, expensive, baseQi: 1);
        AssertRejectedNoOp(
            insufficient,
            Execute(
                insufficient,
                "play-card",
                C(2, 3),
                BasicStoneCardPlacementMode.Frontline),
            "insufficient_qi");

        var active = StartSession(
            board,
            definition,
            baseQi: 3,
            cards:
            [
                new BattleCardInstance("play-card", definition.ContentId),
                new BattleCardInstance("active-card", definition.ContentId),
            ],
            transformCardTurn: state =>
                CoreDuelCardTurnKernel.BeginResolution(state, "active-card").StateAfter);
        AssertRejectedNoOp(
            active,
            Execute(
                active,
                "play-card",
                C(2, 3),
                BasicStoneCardPlacementMode.Frontline),
            "active_resolution_exists");

        var frontlineOnly = Definition(cost: 1, terminal: false);
        var unsupportedMode = StartSession(board, frontlineOnly, baseQi: 3);
        AssertRejectedNoOp(
            unsupportedMode,
            Execute(
                unsupportedMode,
                "play-card",
                C(2, 3),
                BasicStoneCardPlacementMode.TerminalCapture),
            "unsupported_placement_mode");

        AssertRejectedNoOp(
            valid,
            Execute(
                valid,
                "play-card",
                C(5, 5),
                BasicStoneCardPlacementMode.Frontline),
            "frontline_adjacency_required");
        AssertRejectedNoOp(
            valid,
            Execute(
                valid,
                "play-card",
                C(2, 2),
                BasicStoneCardPlacementMode.Frontline),
            "target_occupied");

        var suicideBoard = Board(
            Stone(StoneColor.Black, 2, 1),
            Stone(StoneColor.White, 1, 1),
            Stone(StoneColor.White, 3, 1),
            Stone(StoneColor.White, 1, 2),
            Stone(StoneColor.White, 3, 2),
            Stone(StoneColor.White, 2, 3));
        var suicide = StartSession(suicideBoard, definition, baseQi: 3);
        AssertRejectedNoOp(
            suicide,
            Execute(
                suicide,
                "play-card",
                C(2, 2),
                BasicStoneCardPlacementMode.Frontline),
            "suicide");

        var terminalWithoutCapture = StartSession(board, definition, baseQi: 3);
        AssertRejectedNoOp(
            terminalWithoutCapture,
            Execute(
                terminalWithoutCapture,
                "play-card",
                C(6, 6),
                BasicStoneCardPlacementMode.TerminalCapture),
            "terminal_capture_required");

        var repetitionTarget = C(2, 3);
        var repetitionHistory = HistoryContainingCandidate(board, repetitionTarget);
        var repetition = StartSession(
            board,
            definition,
            baseQi: 3,
            history: repetitionHistory);
        AssertRejectedNoOp(
            repetition,
            Execute(
                repetition,
                "play-card",
                repetitionTarget,
                BasicStoneCardPlacementMode.Frontline),
            "stone_topology_repetition");
    }

    [Fact]
    public void FacilityPointPlayDestroysFacilityBetweenStoneAndHistoryFacts()
    {
        var definition = Definition(cost: 1);
        var board = NeutralFrontlineBoard();
        var target = C(2, 3);
        var facility = new FacilityInstance(
            "facility-target",
            "facility-shape",
            StoneColor.Black,
            target,
            buildSequence: 1);
        var facilities = FacilityState.Create(board, [facility], 2);
        var session = StartSession(
            board,
            definition,
            baseQi: 3,
            facilities: facilities);

        var result = Execute(
            session,
            "play-card",
            target,
            BasicStoneCardPlacementMode.Frontline);

        Assert.True(result.Accepted);
        Assert.IsType<QiChangedFact>(result.OrderedFacts[0]);
        var stoneIndex = IndexOf<StonePlacedFact>(result.OrderedFacts);
        var facilityIndex = IndexOf<FacilityDestroyedFact>(result.OrderedFacts);
        var historyIndex = IndexOf<StoneTopologyRegisteredFact>(result.OrderedFacts);
        Assert.Equal(1, stoneIndex);
        Assert.True(stoneIndex < facilityIndex);
        Assert.True(facilityIndex < historyIndex);
        var destroyed = Assert.IsType<FacilityDestroyedFact>(result.OrderedFacts[facilityIndex]);
        Assert.Same(facility, destroyed.Facility);
        Assert.Empty(result.SessionAfter.State.BattleState.FacilityState.InstalledFacilities);
        Assert.Equal(StoneColor.Black, result.SessionAfter.State.BattleState.Board.StoneAt(target)?.Color);
        Assert.Equal(
            session.State.BattleState.RepetitionHistory.ObservationCount + 1,
            result.SessionAfter.State.BattleState.RepetitionHistory.ObservationCount);
    }

    [Fact]
    public void AcceptedPlayCommitsSimultaneousCapturesOnceInAnchorOrder()
    {
        var board = Board(
            Stone(StoneColor.White, 2, 3),
            Stone(StoneColor.White, 2, 4),
            Stone(StoneColor.White, 4, 3),
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.Black, 4, 2),
            Stone(StoneColor.Black, 1, 3),
            Stone(StoneColor.Black, 5, 3),
            Stone(StoneColor.Black, 1, 4),
            Stone(StoneColor.Black, 3, 4),
            Stone(StoneColor.Black, 4, 4),
            Stone(StoneColor.Black, 2, 5));
        var session = StartSession(board, Definition(cost: 1), baseQi: 3);

        var result = Execute(
            session,
            "play-card",
            C(3, 3),
            BasicStoneCardPlacementMode.Frontline);

        Assert.True(result.Accepted);
        Assert.IsType<QiChangedFact>(result.OrderedFacts[0]);
        Assert.IsType<StonePlacedFact>(result.OrderedFacts[1]);
        var captures = result.OrderedFacts.OfType<GroupCapturedFact>().ToArray();
        Assert.Equal(2, captures.Length);
        Assert.Equal([C(2, 3), C(4, 3)], captures.Select(fact => fact.CapturedGroup.Anchor));
        Assert.Equal(3, captures.Sum(fact => fact.CapturedGroup.Stones.Count));
        Assert.True(result.SessionAfter.State.BattleState.Board.IsEmpty(C(2, 3)));
        Assert.True(result.SessionAfter.State.BattleState.Board.IsEmpty(C(2, 4)));
        Assert.True(result.SessionAfter.State.BattleState.Board.IsEmpty(C(4, 3)));
        Assert.NotNull(result.SessionAfter.State.BattleState.Board.StoneAt(C(3, 3)));
        Assert.Single(result.SessionAfter.CommandLog.Entries);
    }

    [Fact]
    public void NonterminalCaptureEmitsTerritoryBeforeFacilityReactivation()
    {
        var board = Board(
            Stone(StoneColor.Black, 3, 4),
            Stone(StoneColor.Black, 2, 3),
            Stone(StoneColor.Black, 4, 3),
            Stone(StoneColor.White, 3, 3));
        var facility = new FacilityInstance(
            "facility-reactivated",
            "facility-shape",
            StoneColor.Black,
            C(1, 1),
            buildSequence: 1);
        var facilities = FacilityState.Create(board, [facility], 2);
        var session = StartSession(
            board,
            Definition(cost: 1, terminal: true),
            baseQi: 3,
            facilities: facilities);
        Assert.False(
            session.State.BattleState.FacilityRuntimeAnalysis
                .OperatingStateFor(facility)
                .IsActive);

        var result = Execute(
            session,
            "play-card",
            C(3, 2),
            BasicStoneCardPlacementMode.TerminalCapture);

        Assert.True(result.Accepted);
        Assert.Equal(
            new[]
            {
                typeof(QiChangedFact),
                typeof(StonePlacedFact),
                typeof(GroupCapturedFact),
                typeof(StoneTopologyRegisteredFact),
                typeof(KingCaptureEvaluatedFact),
                typeof(TerritoryEstablishedFact),
                typeof(FacilityActivatedFact),
            },
            result.OrderedFacts.Select(fact => fact.GetType()));
        var territory = Assert.IsType<TerritoryEstablishedFact>(result.OrderedFacts[5]);
        Assert.Equal(StoneColor.Black, territory.SourceActor);
        Assert.Contains(C(3, 3), territory.ChangedPoints);
        var activated = Assert.IsType<FacilityActivatedFact>(result.OrderedFacts[6]);
        Assert.Same(facility, activated.Facility);
        Assert.Equal("territory_control_restored", activated.ReasonId);
        Assert.True(
            result.SessionAfter.State.BattleState.FacilityRuntimeAnalysis
                .OperatingStateFor(facility)
                .IsActive);
        Assert.Equal(BattleOutcome.Ongoing, result.SessionAfter.State.BattleState.Outcome);
    }

    [Fact]
    public void TerminalKingCaptureEndsBattleAndStillLeavesPlayedCardResolved()
    {
        var definition = Definition(cost: 1, terminal: true);
        var board = Board(
            Stone(StoneColor.White, 3, 2, isKing: true),
            Stone(StoneColor.Black, 3, 1),
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.Black, 4, 2));
        var session = StartSession(board, definition, baseQi: 3);

        var result = Execute(
            session,
            "play-card",
            C(3, 3),
            BasicStoneCardPlacementMode.TerminalCapture);

        Assert.True(result.Accepted);
        Assert.True(result.SessionAfter.State.IsTerminal);
        Assert.Equal(BattlePhase.Ended, result.SessionAfter.State.BattleState.Phase);
        Assert.Equal(BattleOutcome.PlayerVictory, result.SessionAfter.State.BattleState.Outcome);
        Assert.Equal(
            BattleEndReason.WhiteKingCaptured,
            result.SessionAfter.State.BattleState.EndReason);
        Assert.IsType<QiChangedFact>(result.OrderedFacts[0]);
        Assert.Contains(result.OrderedFacts, fact =>
            fact is GroupCapturedFact captured && captured.ContainsKing);
        var ended = Assert.Single(result.OrderedFacts.OfType<BattleEndedFact>());
        Assert.Equal(BattleOutcome.PlayerVictory, ended.Outcome);
        var resolved = Assert.Single(
            result.SessionAfter.State.CardTurnState.Deck.Resolving);
        Assert.Equal("play-card", resolved.Card.InstanceId);
        Assert.True(resolved.IsResolved);
        Assert.Single(result.SessionAfter.CommandLog.Entries);

        var afterTerminal = Execute(
            result.SessionAfter,
            "play-card",
            C(2, 3),
            BasicStoneCardPlacementMode.Frontline);

        AssertRejectedNoOp(result.SessionAfter, afterTerminal, "battle_terminal");
    }

    [Fact]
    public void SameSeedStateAndCommandProduceSameFactsStateAndLogChecksums()
    {
        var definition = Definition(cost: 1);
        var first = StartSession(NeutralFrontlineBoard(), definition, baseQi: 3);
        var second = StartSession(NeutralFrontlineBoard(), definition, baseQi: 3);

        var firstResult = Execute(
            first,
            "play-card",
            C(2, 3),
            BasicStoneCardPlacementMode.Frontline);
        var secondResult = Execute(
            second,
            "play-card",
            C(2, 3),
            BasicStoneCardPlacementMode.Frontline);

        Assert.True(firstResult.Accepted);
        Assert.True(secondResult.Accepted);
        Assert.Equal(
            firstResult.SessionAfter.State.CanonicalText,
            secondResult.SessionAfter.State.CanonicalText);
        Assert.Equal(firstResult.StateChecksum, secondResult.StateChecksum);
        Assert.Equal(firstResult.LogChecksum, secondResult.LogChecksum);
        Assert.Equal(
            firstResult.OrderedFacts.Select(ProjectFact),
            secondResult.OrderedFacts.Select(ProjectFact));
        Assert.Equal(
            firstResult.Command.ToCanonicalPayload(),
            secondResult.Command.ToCanonicalPayload());
    }

    [Fact]
    public void ExistingTurnEndMovesResolvedPlayCardToDiscard()
    {
        var definition = Definition(cost: 1);
        var session = StartSession(NeutralFrontlineBoard(), definition, baseQi: 3);
        var played = Execute(
            session,
            "play-card",
            C(2, 3),
            BasicStoneCardPlacementMode.Frontline);

        var ended = CoreDuelCardTurnKernel.EndPlayerTurn(
            played.SessionAfter.State.CardTurnState);

        Assert.True(ended.Accepted);
        Assert.Empty(ended.StateAfter.Deck.Hand);
        Assert.Empty(ended.StateAfter.Deck.Resolving);
        Assert.Equal(0, ended.StateAfter.Qi);
        Assert.Equal(
            "play-card",
            Assert.Single(ended.StateAfter.Deck.DiscardPile).InstanceId);
        Assert.Same(
            played.SessionAfter.State.CardTurnState.RngState,
            ended.StateAfter.RngState);
    }

    [Fact]
    public void StateBindsTypedDefinitionAndContentIdentityOutsideThePlayerCommand()
    {
        var board = NeutralFrontlineBoard();
        var costOne = StartSession(board, Definition(cost: 1), baseQi: 3);
        var costTwo = StartSession(board, Definition(cost: 2), baseQi: 3);
        var otherContent = StartSession(
            board,
            Definition(cost: 1),
            baseQi: 3,
            contentHash:
                "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

        Assert.NotEqual(costOne.State.Checksum, costTwo.State.Checksum);
        Assert.NotEqual(costOne.State.Checksum, otherContent.State.Checksum);
        Assert.NotEqual(
            costOne.CommandLog.CurrentChecksum,
            otherContent.CommandLog.CurrentChecksum);

        var command = Command(
            costOne,
            "play-card",
            C(2, 3),
            BasicStoneCardPlacementMode.Frontline);
        Assert.DoesNotContain(
            "definition",
            command.ToCanonicalPayload(),
            StringComparison.Ordinal);
    }

    private static CoreDuelCardPlaySession StartSession(
        BoardState board,
        BasicStoneCardPlayDefinition definition,
        int baseQi,
        BattleRepetitionHistory? history = null,
        FacilityState? facilities = null,
        IReadOnlyList<BattleCardInstance>? cards = null,
        Func<CoreDuelCardTurnState, CoreDuelCardTurnState>? transformCardTurn = null,
        string contentHash = ContentHash)
    {
        var recipe = cards ??
            [new BattleCardInstance("play-card", definition.ContentId)];
        var facilityState = facilities ?? FacilityState.Create(board, [], 1);
        var facilityPolicy = FacilityPolicy();
        var initialCardTurn = CoreDuelCardTurnKernel.StartBattle(
            recipe,
            AuthoritativeRngState.Create(Seed),
            CoreDuelSystemPolicy.Create(baseQi, Math.Max(1, recipe.Count)),
            ClosedWindowResourceState.Empty([]),
            []);
        var turnStart = CoreDuelCardTurnKernel.StartPlayerTurn(
            initialCardTurn,
            board,
            facilityState,
            facilityPolicy,
            []);
        Assert.True(turnStart.Accepted);
        var cardTurn = transformCardTurn?.Invoke(turnStart.StateAfter) ??
            turnStart.StateAfter;
        var metadata = ReplayMetadata.Create("test-v1", contentHash, Seed);

        return CoreDuelCardPlayStateMachine.Start(
            board,
            history ?? BattleRepetitionHistory.Start(board),
            facilityState,
            new BattleRuntimePolicy(20, facilityPolicy),
            cardTurn,
            definition,
            metadata);
    }

    private static CoreDuelCardPlayResult Execute(
        CoreDuelCardPlaySession session,
        string cardInstanceId,
        CanonicalPoint target,
        BasicStoneCardPlacementMode mode) =>
        CoreDuelCardPlayStateMachine.Execute(
            session,
            Command(session, cardInstanceId, target, mode));

    private static PlayCardCommand Command(
        CoreDuelCardPlaySession session,
        string cardInstanceId,
        CanonicalPoint target,
        BasicStoneCardPlacementMode mode,
        string? expectedStateChecksum = null,
        string? expectedLogChecksum = null) =>
        new(
            expectedStateChecksum ?? session.State.Checksum,
            expectedLogChecksum ?? session.CommandLog.CurrentChecksum,
            cardInstanceId,
            target,
            mode);

    private static void AssertRejectedNoOp(
        CoreDuelCardPlaySession source,
        CoreDuelCardPlayResult result,
        string reasonId)
    {
        Assert.False(result.Accepted);
        Assert.Equal(reasonId, result.ReasonId);
        Assert.Same(source, result.SessionBefore);
        Assert.Same(source, result.SessionAfter);
        Assert.Same(source.State, result.SessionAfter.State);
        Assert.Same(source.CommandLog, result.SessionAfter.CommandLog);
        Assert.Same(
            source.State.CardTurnState.RngState,
            result.SessionAfter.State.CardTurnState.RngState);
        Assert.Same(
            source.State.CardTurnState.Deck,
            result.SessionAfter.State.CardTurnState.Deck);
        Assert.Equal(
            source.State.CardTurnState.Qi,
            result.SessionAfter.State.CardTurnState.Qi);
        Assert.Same(
            source.State.BattleState.Board,
            result.SessionAfter.State.BattleState.Board);
        Assert.Same(
            source.State.BattleState.RepetitionHistory,
            result.SessionAfter.State.BattleState.RepetitionHistory);
        Assert.Same(
            source.State.BattleState.FacilityState,
            result.SessionAfter.State.BattleState.FacilityState);
        var rejected = Assert.Single(result.OrderedFacts);
        Assert.Equal(reasonId, Assert.IsType<CommandRejectedFact>(rejected).ReasonId);
        Assert.Equal(source.State.Checksum, result.StateChecksum);
        Assert.Equal(source.CommandLog.CurrentChecksum, result.LogChecksum);
    }

    private static string ProjectFact(IBattleFact fact) => fact switch
    {
        QiChangedFact qi =>
            $"qi_changed|old={qi.OldAmount.ToString(CultureInfo.InvariantCulture)}|" +
            $"new={qi.NewAmount.ToString(CultureInfo.InvariantCulture)}|" +
            $"delta={qi.Delta.ToString(CultureInfo.InvariantCulture)}|" +
            $"reason={qi.ReasonId}|source={qi.SourceId}",
        _ => GoldenBoardFixtureAdapter.ProjectFact(fact),
    };

    private static BattleRepetitionHistory HistoryContainingCandidate(
        BoardState source,
        CanonicalPoint target)
    {
        Assert.True(HypotheticalPlacementResolver.TryCreate(
            source,
            new BoardStone(StoneColor.Black, false, target),
            out var hypothetical));
        Assert.NotNull(hypothetical);
        var resolved = HypotheticalPlacementResolver.ResolveCaptures(
            hypothetical,
            RealLiberties(hypothetical.GroupsAfterPlacement));
        return BattleRepetitionHistory.FromObservedBoards(
            [resolved.BoardAfterCapture, source]);
    }

    private static EffectiveLibertySnapshot RealLiberties(
        StoneGroupAnalysis analysis) =>
        EffectiveLibertySnapshot.Create(
            analysis,
            analysis.Groups.Select(group => new GroupEffectiveLiberty(
                group,
                group.RealLibertyCount)));

    private static BasicStoneCardPlayDefinition Definition(
        int cost,
        bool terminal = false,
        string contentId = "card_shape_driven") =>
        BasicStoneCardPlayDefinition.Create(
            CardContentDefinition.Create(
                contentId,
                CardRarity.Starter,
                cost,
                CardContentType.Stone,
                CardTargetKind.None,
                terminal
                    ? [CardPlacementTag.Frontline, CardPlacementTag.Terminal]
                    : [CardPlacementTag.Frontline],
                [new PlaceStoneOperationDefinition(StoneContentKind.Basic)]));

    private static FacilityRuntimePolicy FacilityPolicy() =>
        FacilityRuntimePolicy.Create(
            territoryIncomeDivisor: 3,
            capacityBands: [new FacilityCapacityBand(1, 49, 1)],
            slotCap: 3,
            typeLimits: [new KeyValuePair<string, int>("default", 1)]);

    private static BoardState NeutralFrontlineBoard() => Board(
        Stone(StoneColor.Black, 2, 2),
        Stone(StoneColor.White, 7, 7));

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(
        StoneColor color,
        int x,
        int y,
        bool isKing = false) =>
        new(color, isKing, C(x, y));

    private static CanonicalPoint C(int x, int y) =>
        Geometry.CreateCanonicalPoint(x, y);

    private static int IndexOf<T>(IReadOnlyList<IBattleFact> facts)
        where T : IBattleFact
    {
        for (var index = 0; index < facts.Count; index++)
        {
            if (facts[index] is T)
            {
                return index;
            }
        }

        return -1;
    }

    private static string OtherChecksum(string checksum) =>
        (checksum[0] == '0' ? '1' : '0') + checksum[1..];
}
