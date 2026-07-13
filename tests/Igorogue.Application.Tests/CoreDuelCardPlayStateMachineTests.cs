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
            StoneCardPlacementMode.Frontline);

        Assert.True(result.Accepted);
        Assert.Equal("accepted", result.ReasonId);
        Assert.NotSame(session, result.SessionAfter);
        Assert.NotSame(session.State, result.SessionAfter.State);
        Assert.NotEqual(session.State.Checksum, result.StateChecksum);
        Assert.Equal(
            DeterministicChecksum.Sha256Hex(result.SessionAfter.State.CanonicalText),
            result.StateChecksum);
        Assert.Contains(
            "starter_stone_definitions=",
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
        var definition = Definition(cost: 1);
        var board = NeutralFrontlineBoard();
        var valid = StartSession(board, definition, baseQi: 3);

        var staleState = CoreDuelCardPlayStateMachine.Execute(
            valid,
            Command(
                valid,
                "play-card",
                C(2, 3),
                StoneCardPlacementMode.Frontline,
                expectedStateChecksum: OtherChecksum(valid.State.Checksum)));
        AssertRejectedNoOp(valid, staleState, "stale_state");

        var staleLog = CoreDuelCardPlayStateMachine.Execute(
            valid,
            Command(
                valid,
                "play-card",
                C(2, 3),
                StoneCardPlacementMode.Frontline,
                expectedLogChecksum: OtherChecksum(valid.CommandLog.CurrentChecksum)));
        AssertRejectedNoOp(valid, staleLog, "stale_session");

        AssertRejectedNoOp(
            valid,
            Execute(
                valid,
                "missing-card",
                C(2, 3),
                StoneCardPlacementMode.Frontline),
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
                StoneCardPlacementMode.Frontline),
            "card_content_mismatch");

        var expensive = Definition(cost: 9);
        var insufficient = StartSession(board, expensive, baseQi: 1);
        AssertRejectedNoOp(
            insufficient,
            Execute(
                insufficient,
                "play-card",
                C(2, 3),
                StoneCardPlacementMode.Frontline),
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
                StoneCardPlacementMode.Frontline),
            "active_resolution_exists");

        var frontlineOnly = ExtendDefinition(cost: 1);
        var unsupportedMode = StartSession(board, frontlineOnly, baseQi: 3);
        AssertRejectedNoOp(
            unsupportedMode,
            Execute(
                unsupportedMode,
                "play-card",
                C(2, 3),
                StoneCardPlacementMode.TerminalCapture),
            "unsupported_placement_mode");

        AssertRejectedNoOp(
            valid,
            Execute(
                valid,
                "play-card",
                C(5, 5),
                StoneCardPlacementMode.Frontline),
            "frontline_adjacency_required");
        AssertRejectedNoOp(
            valid,
            Execute(
                valid,
                "play-card",
                C(2, 2),
                StoneCardPlacementMode.Frontline),
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
                StoneCardPlacementMode.Frontline),
            "suicide");

        var terminalWithoutCapture = StartSession(board, definition, baseQi: 3);
        AssertRejectedNoOp(
            terminalWithoutCapture,
            Execute(
                terminalWithoutCapture,
                "play-card",
                C(6, 6),
                StoneCardPlacementMode.TerminalCapture),
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
                StoneCardPlacementMode.Frontline),
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
            StoneCardPlacementMode.Frontline);

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
            StoneCardPlacementMode.Frontline);

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
            Definition(cost: 1),
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
            StoneCardPlacementMode.TerminalCapture);

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
        var definition = Definition(cost: 1);
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
            StoneCardPlacementMode.TerminalCapture);

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
            StoneCardPlacementMode.Frontline);

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
            StoneCardPlacementMode.Frontline);
        var secondResult = Execute(
            second,
            "play-card",
            C(2, 3),
            StoneCardPlacementMode.Frontline);

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
            StoneCardPlacementMode.Frontline);

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
            StoneCardPlacementMode.Frontline);
        Assert.DoesNotContain(
            "definition",
            command.ToCanonicalPayload(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void StartPreservesTheAuthoritativeSnapshotPlayerTurnIndex()
    {
        var board = NeutralFrontlineBoard();
        var definition = Definition(cost: 1);
        var firstTurn = StartSession(
            board,
            definition,
            baseQi: 3,
            playerTurnIndex: 1);
        var laterTurn = StartSession(
            board,
            definition,
            baseQi: 3,
            playerTurnIndex: 7);

        Assert.Equal(1, firstTurn.State.BattleState.PlayerTurnIndex);
        Assert.Equal(7, laterTurn.State.BattleState.PlayerTurnIndex);
        Assert.Null(laterTurn.State.BattleState.AuthoritativeRuntime);
        Assert.NotEqual(firstTurn.State.Checksum, laterTurn.State.Checksum);

        var played = Execute(
            laterTurn,
            "play-card",
            C(2, 3),
            StoneCardPlacementMode.Frontline);

        Assert.True(played.Accepted);
        Assert.Equal(7, played.SessionAfter.State.BattleState.PlayerTurnIndex);
    }

    [Fact]
    public void CapturedTemporaryLibertyCarrierPublishesRemovalFactAfterCapture()
    {
        var board = Board(
            Stone(StoneColor.White, 2, 2),
            Stone(StoneColor.Black, 1, 2),
            Stone(StoneColor.Black, 2, 1),
            Stone(StoneColor.Black, 3, 2),
            Stone(StoneColor.Black, 1, 3));
        var definition = ContactDefinition(cost: 1);
        TemporaryLibertyEffect? carrierEffect = null;
        var session = StartSession(
            board,
            definition,
            baseQi: 3,
            temporaryFactory: runtime =>
            {
                var carrier = runtime.InstanceAt(C(2, 2))!;
                carrierEffect = new TemporaryLibertyEffect(
                    "effect.captured.carrier",
                    1,
                    StoneColor.White,
                    carrier.InstanceId,
                    "test.reinforce",
                    1,
                    9);
                return TemporaryLibertyState.Create(
                    runtime,
                    [carrierEffect],
                    2);
            },
            continuousFactory: runtime =>
            {
                var carrier = runtime.InstanceAt(C(2, 2))!;
                return ContinuousLibertySnapshot.Create(
                    runtime,
                    [new ContinuousLibertyModifier(
                        "modifier.cancel.carrier",
                        -1,
                        StoneColor.White,
                        carrier.InstanceId,
                        "test.cancel")]);
            });

        var result = Execute(
            session,
            "play-card",
            C(2, 3),
            StoneCardPlacementMode.Contact);

        Assert.True(result.Accepted);
        Assert.NotNull(carrierEffect);
        var captures = result.OrderedFacts.OfType<GroupCapturedFact>().ToArray();
        var captured = Assert.Single(captures);
        Assert.Equal(C(2, 2), captured.CapturedGroup.Anchor);
        var removal = Assert.Single(
            result.OrderedFacts.OfType<TemporaryLibertyRemovedFact>());
        Assert.Same(carrierEffect, removal.Effect);
        Assert.Equal("carrier_removed", removal.ReasonId);
        var lastCaptureIndex = result.OrderedFacts
            .Select((fact, index) => (fact, index))
            .Last(pair => pair.fact is GroupCapturedFact)
            .index;
        Assert.Equal(
            lastCaptureIndex + 1,
            IndexOf<TemporaryLibertyRemovedFact>(result.OrderedFacts));
        Assert.Empty(
            result.SessionAfter.State.RuntimeState.TemporaryLibertyState.Effects);
        Assert.Empty(
            result.SessionAfter.State.RuntimeState.ContinuousLibertySnapshot.Modifiers);
    }

    [Fact]
    public void ExtendDrawsExactlyOneOnlyWhenCommittedGroupMeetsRealLibertyThreshold()
    {
        var definition = ExtendDefinition(cost: 1);
        var recipe = new[]
        {
            new BattleCardInstance("extend-a", definition.ContentId),
            new BattleCardInstance("extend-b", definition.ContentId),
            new BattleCardInstance("extend-c", definition.ContentId),
        };
        var open = StartSession(
            NeutralFrontlineBoard(),
            definition,
            baseQi: 3,
            cards: recipe,
            baseDraw: 2);
        var openCard = open.State.CardTurnState.Deck.Hand[0];
        var rngBefore = open.State.CardTurnState.RngState;

        var drawn = Execute(
            open,
            openCard.InstanceId,
            C(2, 3),
            StoneCardPlacementMode.Frontline);

        Assert.True(drawn.Accepted);
        Assert.Equal(2, drawn.SessionAfter.State.CardTurnState.Deck.Hand.Count);
        var drawFact = Assert.Single(drawn.OrderedFacts.OfType<CardDrawnFact>());
        Assert.Equal(openCard.InstanceId, drawFact.SourceId);
        Assert.Equal("card_effect_real_liberties", drawFact.ReasonId);
        Assert.Same(rngBefore, drawn.SessionAfter.State.CardTurnState.RngState);
        Assert.Same(
            drawn.SessionAfter.State.BattleState.RngState,
            drawn.SessionAfter.State.CardTurnState.RngState);

        var constrainedBoard = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.White, 2, 1),
            Stone(StoneColor.White, 1, 2),
            Stone(StoneColor.White, 3, 2),
            Stone(StoneColor.White, 1, 3),
            Stone(StoneColor.White, 3, 3));
        var constrained = StartSession(
            constrainedBoard,
            definition,
            baseQi: 3,
            cards: recipe,
            baseDraw: 2);
        var constrainedCard = constrained.State.CardTurnState.Deck.Hand[0];

        var notDrawn = Execute(
            constrained,
            constrainedCard.InstanceId,
            C(2, 3),
            StoneCardPlacementMode.Frontline);

        Assert.True(notDrawn.Accepted);
        Assert.Single(notDrawn.SessionAfter.State.CardTurnState.Deck.Hand);
        Assert.Empty(notDrawn.OrderedFacts.OfType<CardDrawnFact>());

        var reshuffleBoard = NeutralFrontlineBoard();
        var reshuffleFacilities = FacilityState.Create(reshuffleBoard, [], 1);
        var reshuffle = StartSession(
            reshuffleBoard,
            definition,
            baseQi: 3,
            facilities: reshuffleFacilities,
            cards: recipe,
            baseDraw: 1,
            transformCardTurn: state =>
            {
                for (var cycle = 0; cycle < 2; cycle++)
                {
                    var ended = CoreDuelCardTurnKernel.EndPlayerTurn(state);
                    Assert.True(ended.Accepted);
                    var restarted = CoreDuelCardTurnKernel.StartPlayerTurn(
                        ended.StateAfter,
                        reshuffleBoard,
                        reshuffleFacilities,
                        FacilityPolicy(),
                        []);
                    Assert.True(restarted.Accepted);
                    state = restarted.StateAfter;
                }

                return state;
            });
        Assert.Empty(reshuffle.State.CardTurnState.Deck.DrawPile);
        Assert.Equal(2, reshuffle.State.CardTurnState.Deck.DiscardPile.Count);
        var reshuffleRngBefore = reshuffle.State.CardTurnState.RngState;
        var reshuffled = Execute(
            reshuffle,
            Assert.Single(reshuffle.State.CardTurnState.Deck.Hand).InstanceId,
            C(2, 3),
            StoneCardPlacementMode.Frontline);

        Assert.True(reshuffled.Accepted);
        Assert.NotSame(
            reshuffleRngBefore,
            reshuffled.SessionAfter.State.CardTurnState.RngState);
        Assert.NotEqual(
            reshuffleRngBefore.ToCanonicalText(),
            reshuffled.SessionAfter.State.CardTurnState.RngState.ToCanonicalText());
        Assert.Same(
            reshuffled.SessionAfter.State.BattleState.RngState,
            reshuffled.SessionAfter.State.CardTurnState.RngState);
        Assert.Single(reshuffled.OrderedFacts.OfType<CardDrawnFact>());
    }

    [Fact]
    public void ContactGainsQiOnceOnlyForAnAffectedSurvivingEnemyGroupInAtari()
    {
        var definition = ContactDefinition(cost: 1);
        var establishedAtari = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.Black, 3, 2),
            Stone(StoneColor.Black, 4, 3),
            Stone(StoneColor.White, 3, 3));
        var contact = StartSession(establishedAtari, definition, baseQi: 3);

        var gained = Execute(
            contact,
            "play-card",
            C(2, 3),
            StoneCardPlacementMode.Contact);

        Assert.True(gained.Accepted);
        Assert.Equal(3, gained.SessionAfter.State.CardTurnState.Qi);
        var qiFacts = gained.OrderedFacts.OfType<QiChangedFact>().ToArray();
        Assert.Equal(2, qiFacts.Length);
        Assert.Equal(-1, qiFacts[0].Delta);
        Assert.Equal(1, qiFacts[1].Delta);
        Assert.Equal("card_effect_enemy_atari", qiFacts[1].ReasonId);

        var stillTwoLiberties = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.Black, 3, 2),
            Stone(StoneColor.White, 3, 3));
        var noAtari = StartSession(stillTwoLiberties, definition, baseQi: 3);

        var notGained = Execute(
            noAtari,
            "play-card",
            C(2, 3),
            StoneCardPlacementMode.Contact);

        Assert.True(notGained.Accepted);
        Assert.Equal(2, notGained.SessionAfter.State.CardTurnState.Qi);
        Assert.Single(notGained.OrderedFacts.OfType<QiChangedFact>());
    }

    [Fact]
    public void ContactModeRequiresAdjacentBlackAndWhiteStones()
    {
        var definition = ContactDefinition(cost: 1);
        var cases = new[]
        {
            (Board(Stone(StoneColor.Black, 2, 2)), C(2, 3)),
            (Board(Stone(StoneColor.White, 2, 2)), C(2, 3)),
            (Board(
                Stone(StoneColor.Black, 2, 2),
                Stone(StoneColor.White, 7, 7)), C(5, 5)),
        };

        foreach (var (board, target) in cases)
        {
            var session = StartSession(board, definition, baseQi: 3);
            AssertRejectedNoOp(
                session,
                Execute(
                    session,
                    "play-card",
                    target,
                    StoneCardPlacementMode.Contact),
                "contact_adjacency_required");
        }
    }

    [Fact]
    public void ContactTerminalCaptureSuppressesItsQiFollowUp()
    {
        var definition = ContactDefinition(cost: 1);
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
            StoneCardPlacementMode.TerminalCapture);

        Assert.True(result.Accepted);
        Assert.True(result.SessionAfter.State.IsTerminal);
        Assert.Equal(2, result.SessionAfter.State.CardTurnState.Qi);
        var qi = Assert.Single(result.OrderedFacts.OfType<QiChangedFact>());
        Assert.Equal("card_cost", qi.ReasonId);
    }

    [Fact]
    public void LureReservesOneNowAndItsExactRuntimeStoneReservesTwoWhenCapturedLater()
    {
        var definition = LureDefinition(cost: 1);
        var board = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.White, 3, 3));
        var session = StartSession(board, definition, baseQi: 3);
        var target = C(2, 3);

        var played = Execute(
            session,
            "play-card",
            target,
            StoneCardPlacementMode.Contact);

        Assert.True(played.Accepted);
        var state = played.SessionAfter.State;
        Assert.Equal(1, state.CardTurnState.ClosedWindowResources.TurnReservedDraw);
        Assert.Same(
            state.CardTurnState.ClosedWindowResources,
            state.RuntimeState.ClosedWindowResources);
        var immediate = Assert.Single(
            played.OrderedFacts.OfType<TurnReservedDrawChangedFact>());
        Assert.Equal(1, immediate.Delta);
        var lure = Assert.IsType<StoneRuntimeInstance>(
            state.RuntimeState.StoneRuntimeState.InstanceAt(target));
        Assert.Equal("lure", lure.KindId);
        Assert.Equal(["captured_stone_self"], lure.OrderedEffectMetadata);
        Assert.Contains(lure.InstanceId, state.RuntimeState.UsedStoneInstanceIds);
        var entry = Assert.Single(state.RuntimeState.CaptureBenefitTriggerPlan.Entries);
        Assert.Equal(CaptureBenefitTriggerCondition.CapturedSourceStone, entry.Condition);
        Assert.Equal(lure.InstanceId, entry.Trigger.Source.SourceId);

        var group = StoneGroupAnalyzer
            .Analyze(state.RuntimeState.StoneRuntimeState.SourceBoard)
            .GroupAt(target);
        Assert.NotNull(group);
        var batch = CaptureBatch.Create(
            "later_lure_capture",
            "placement_capture",
            CaptureBoundary.PlacementResolution,
            boundaryEnemyTurnIndex: null,
            CapturingWindow.ClosedPlayerWindow,
            state.RuntimeState.StoneRuntimeState,
            [group]);
        var selected = state.RuntimeState.CaptureBenefitTriggerPlan.SelectFor(batch);
        var selectedLure = Assert.Single(selected);
        Assert.Equal(lure.InstanceId, selectedLure.Source.SourceId);

        var resolution = ClosedWindowCaptureBenefitResolver.ResolvePlacement(
            batch,
            state.RuntimeState.ClosedWindowResources,
            state.RuntimeState.CounterattackState,
            state.RuntimeState.CounterattackPolicy,
            selected);

        Assert.False(resolution.BenefitsSuppressed);
        Assert.Equal(3, resolution.ResourcesAfterResolution.TurnReservedDraw);
        var later = Assert.Single(
            resolution.OrderedFacts.OfType<TurnReservedDrawChangedFact>());
        Assert.Equal(1, later.AmountBefore);
        Assert.Equal(3, later.AmountAfter);
        Assert.Equal(2, later.Delta);
    }

    [Fact]
    public void LureCaptureBenefitIsSuppressedWhenItsCapturedGroupContainsTheBlackKing()
    {
        var definition = LureDefinition(cost: 1);
        var board = Board(
            Stone(StoneColor.Black, 2, 2, isKing: true),
            Stone(StoneColor.White, 3, 3));
        var played = Execute(
            StartSession(board, definition, baseQi: 3),
            "play-card",
            C(2, 3),
            StoneCardPlacementMode.Contact);
        Assert.True(played.Accepted);
        var runtime = played.SessionAfter.State.RuntimeState;
        var group = StoneGroupAnalyzer
            .Analyze(runtime.StoneRuntimeState.SourceBoard)
            .GroupAt(C(2, 3));
        Assert.NotNull(group);
        var batch = CaptureBatch.Create(
            "terminal_lure_capture",
            "placement_capture",
            CaptureBoundary.PlacementResolution,
            boundaryEnemyTurnIndex: null,
            CapturingWindow.PlayerActionWindow,
            runtime.StoneRuntimeState,
            [group]);
        Assert.True(batch.ContainsKing);

        var resolution = ClosedWindowCaptureBenefitResolver.ResolvePlacement(
            batch,
            runtime.ClosedWindowResources,
            runtime.CounterattackState,
            runtime.CounterattackPolicy,
            runtime.CaptureBenefitTriggerPlan.SelectFor(batch));

        Assert.True(resolution.BenefitsSuppressed);
        Assert.Same(runtime.ClosedWindowResources, resolution.ResourcesAfterResolution);
        Assert.Equal(1, resolution.ResourcesAfterResolution.TurnReservedDraw);
        Assert.Empty(resolution.OrderedFacts.OfType<TurnReservedDrawChangedFact>());
        Assert.Single(resolution.OrderedFacts.OfType<CaptureBenefitSuppressedFact>());
    }

    [Fact]
    public void ReinforceAtariDrawsBeforeGrantAndBindsTheCanonicalRuntimeAnchor()
    {
        var reinforce = ReinforceDefinition(cost: 1);
        var board = ReinforceAtariBoard();
        var recipe = ReinforceRecipe(reinforce, reinforceCount: 4, includeFiller: true);
        var session = StartSession(
            board,
            Definition(cost: 1),
            baseQi: 3,
            cards: recipe,
            baseDraw: 4,
            playerTurnIndex: 7,
            reinforceDefinition: reinforce);
        var card = session.State.CardTurnState.Deck.Hand.First(candidate =>
            StringComparer.Ordinal.Equals(candidate.ContentId, reinforce.ContentId));
        var target = C(2, 3);
        var groupAnchor = C(2, 2);
        var anchorInstance = session.State.RuntimeState.StoneRuntimeState
            .InstanceAt(groupAnchor)
            ?? throw new InvalidOperationException("Expected Reinforce anchor stone.");
        var rngBefore = session.State.CardTurnState.RngState;
        var boardBefore = session.State.BattleState.Board;
        var runtimeBefore = session.State.RuntimeState.StoneRuntimeState;

        var result = ExecuteReinforce(session, card.InstanceId, target);

        Assert.True(result.Accepted);
        Assert.Collection(
            result.OrderedFacts,
            fact => Assert.IsType<QiChangedFact>(fact),
            fact =>
            {
                var drawn = Assert.IsType<CardDrawnFact>(fact);
                Assert.Equal("card_effect_target_atari", drawn.ReasonId);
                Assert.Equal(card.InstanceId, drawn.SourceId);
            },
            fact => Assert.IsType<TemporaryLibertyGrantedFact>(fact));
        var granted = Assert.IsType<TemporaryLibertyGrantedFact>(result.OrderedFacts[2]);
        Assert.Equal(groupAnchor, granted.TargetGroupAnchor);
        Assert.Equal(anchorInstance.InstanceId, granted.Effect.AnchorStoneInstanceId);
        Assert.Equal(reinforce.ContentId, granted.Effect.SourceId);
        Assert.Equal(1, granted.Effect.Amount);
        Assert.Equal(1, granted.Effect.CreatedSequence);
        Assert.Equal(7, granted.Effect.ExpiresAfterEnemyTurnIndex);
        Assert.Equal(2, result.SessionAfter.State.CardTurnState.Qi);
        Assert.Single(result.SessionAfter.State.RuntimeState.TemporaryLibertyState.Effects);
        Assert.Same(rngBefore, result.SessionAfter.State.CardTurnState.RngState);
        Assert.Same(
            result.SessionAfter.State.BattleState.RngState,
            result.SessionAfter.State.CardTurnState.RngState);
        Assert.Same(boardBefore, result.SessionAfter.State.BattleState.Board);
        Assert.Same(runtimeBefore, result.SessionAfter.State.RuntimeState.StoneRuntimeState);
        Assert.Single(result.SessionAfter.CommandLog.Entries);
        Assert.Contains("placement_mode=none", result.Command.ToCanonicalPayload());
    }

    [Fact]
    public void ReinforceUsesTimedAndContinuousEffectiveLibertiesBeforeGrant()
    {
        var reinforce = ReinforceDefinition(cost: 1);
        var recipe = ReinforceRecipe(reinforce, reinforceCount: 4, includeFiller: true);
        var timed = StartSession(
            ReinforceAtariBoard(),
            Definition(cost: 1),
            baseQi: 3,
            cards: recipe,
            baseDraw: 4,
            reinforceDefinition: reinforce,
            temporaryFactory: runtime => TemporaryLibertyState.Create(
                runtime,
                [new TemporaryLibertyEffect(
                    "existing-timed",
                    1,
                    StoneColor.Black,
                    runtime.InstanceAt(C(2, 2))!.InstanceId,
                    "existing-source",
                    1,
                    1)],
                2));
        var timedCard = timed.State.CardTurnState.Deck.Hand.First(candidate =>
            StringComparer.Ordinal.Equals(candidate.ContentId, reinforce.ContentId));

        var timedResult = ExecuteReinforce(timed, timedCard.InstanceId, C(2, 3));

        Assert.True(timedResult.Accepted);
        Assert.Empty(timedResult.OrderedFacts.OfType<CardDrawnFact>());
        Assert.Equal(
            2,
            timedResult.SessionAfter.State.RuntimeState.TemporaryLibertyState.Effects.Count);

        var realTwoBoard = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.White, 1, 2),
            Stone(StoneColor.White, 2, 1));
        var continuous = StartSession(
            realTwoBoard,
            Definition(cost: 1),
            baseQi: 3,
            cards: recipe,
            baseDraw: 4,
            reinforceDefinition: reinforce,
            continuousFactory: runtime => ContinuousLibertySnapshot.Create(
                runtime,
                [new ContinuousLibertyModifier(
                    "continuous-minus-one",
                    -1,
                    StoneColor.Black,
                    runtime.InstanceAt(C(2, 2))!.InstanceId,
                    "continuous-source")]));
        var continuousCard = continuous.State.CardTurnState.Deck.Hand.First(candidate =>
            StringComparer.Ordinal.Equals(candidate.ContentId, reinforce.ContentId));

        var continuousResult = ExecuteReinforce(
            continuous,
            continuousCard.InstanceId,
            C(2, 2));

        Assert.True(continuousResult.Accepted);
        Assert.Single(continuousResult.OrderedFacts.OfType<CardDrawnFact>());
        var continuousAnalysis = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            continuousResult.SessionAfter.State.RuntimeState.StoneRuntimeState,
            continuousResult.SessionAfter.State.RuntimeState.TemporaryLibertyState,
            continuousResult.SessionAfter.State.RuntimeState.ContinuousLibertySnapshot);
        var targetGroup = continuousAnalysis.GroupAnalysis.GroupAt(C(2, 2))!;
        Assert.Equal(2, continuousAnalysis.BreakdownFor(targetGroup).EffectiveLibertyCount);
    }

    [Fact]
    public void ReinforceRejectsEmptyForeignAndStoneModeTargetsAsExactNoOps()
    {
        var reinforce = ReinforceDefinition(cost: 1);
        var board = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.White, 5, 5));
        var recipe = ReinforceRecipe(reinforce, reinforceCount: 1, includeFiller: false);
        var session = StartSession(
            board,
            Definition(cost: 1),
            baseQi: 3,
            cards: recipe,
            baseDraw: 1,
            reinforceDefinition: reinforce);
        var card = Assert.Single(session.State.CardTurnState.Deck.Hand);

        AssertRejectedNoOp(
            session,
            ExecuteReinforce(session, card.InstanceId, C(3, 3)),
            "reinforce_target_empty");
        AssertRejectedNoOp(
            session,
            ExecuteReinforce(session, card.InstanceId, C(5, 5)),
            "reinforce_target_foreign");
        AssertRejectedNoOp(
            session,
            Execute(
                session,
                card.InstanceId,
                C(2, 2),
                StoneCardPlacementMode.Frontline),
            "unsupported_placement_mode");
        AssertRejectedNoOp(
            session,
            CoreDuelCardPlayStateMachine.Execute(
                session,
                new PlayCardCommand(
                    OtherChecksum(session.State.Checksum),
                    session.CommandLog.CurrentChecksum,
                    card.InstanceId,
                    C(2, 2))),
            "stale_state");
    }

    [Fact]
    public void PlayCardModeIsRequiredForStoneAndForbiddenForReinforce()
    {
        var basic = Definition(cost: 1);
        var stoneSession = StartSession(
            NeutralFrontlineBoard(),
            basic,
            baseQi: 3);
        var noModeStone = new PlayCardCommand(
            stoneSession.State.Checksum,
            stoneSession.CommandLog.CurrentChecksum,
            "play-card",
            C(2, 3));

        Assert.Equal(1, noModeStone.CommandSchemaVersion);
        Assert.Contains("placement_mode=none", noModeStone.ToCanonicalPayload());
        AssertRejectedNoOp(
            stoneSession,
            CoreDuelCardPlayStateMachine.Execute(stoneSession, noModeStone),
            "unsupported_placement_mode");

        var stoneMode = new PlayCardCommand(
            stoneSession.State.Checksum,
            stoneSession.CommandLog.CurrentChecksum,
            "play-card",
            C(2, 3),
            StoneCardPlacementMode.Frontline);
        Assert.Equal(1, stoneMode.CommandSchemaVersion);
        Assert.Contains("placement_mode=frontline", stoneMode.ToCanonicalPayload());
    }

    [Fact]
    public void TwoReinforcesStackButTheSecondUsesTheUpdatedPreGrantSnapshot()
    {
        var reinforce = ReinforceDefinition(cost: 1);
        var session = StartSession(
            ReinforceAtariBoard(),
            Definition(cost: 1),
            baseQi: 3,
            cards: ReinforceRecipe(reinforce, reinforceCount: 4, includeFiller: true),
            baseDraw: 4,
            reinforceDefinition: reinforce);
        var firstCard = session.State.CardTurnState.Deck.Hand.First(candidate =>
            StringComparer.Ordinal.Equals(candidate.ContentId, reinforce.ContentId));

        var first = ExecuteReinforce(session, firstCard.InstanceId, C(2, 3));
        var secondCard = first.SessionAfter.State.CardTurnState.Deck.Hand.First(candidate =>
            StringComparer.Ordinal.Equals(candidate.ContentId, reinforce.ContentId));
        var second = ExecuteReinforce(
            first.SessionAfter,
            secondCard.InstanceId,
            C(2, 2));

        Assert.True(first.Accepted);
        Assert.True(second.Accepted);
        Assert.Single(first.OrderedFacts.OfType<CardDrawnFact>());
        Assert.Empty(second.OrderedFacts.OfType<CardDrawnFact>());
        var effects = second.SessionAfter.State.RuntimeState.TemporaryLibertyState.Effects;
        Assert.Equal(2, effects.Count);
        Assert.Equal([1L, 2L], effects.Select(effect => effect.CreatedSequence));
        Assert.Equal(2, effects.Sum(effect => effect.Amount));
        Assert.Single(effects.Select(effect => effect.AnchorStoneInstanceId).Distinct());
        Assert.All(effects, effect => Assert.Equal(1, effect.ExpiresAfterEnemyTurnIndex));
    }

    [Fact]
    public void ReinforceUsesTheSnapshotTurnForExpiryAndBridgesToTheExistingSweep()
    {
        var reinforce = ReinforceDefinition(cost: 1);
        var session = StartSession(
            ReinforceAtariBoard(),
            Definition(cost: 1),
            baseQi: 3,
            cards: ReinforceRecipe(reinforce, reinforceCount: 1, includeFiller: false),
            baseDraw: 1,
            playerTurnIndex: 7,
            reinforceDefinition: reinforce);
        var card = Assert.Single(session.State.CardTurnState.Deck.Hand);
        var result = ExecuteReinforce(session, card.InstanceId, C(2, 3));
        var runtime = result.SessionAfter.State.RuntimeState;
        var effect = Assert.Single(runtime.TemporaryLibertyState.Effects);

        var expiry = TemporaryLibertyExpiryResolver.Resolve(
            runtime.StoneRuntimeState,
            runtime.TemporaryLibertyState,
            runtime.ContinuousLibertySnapshot,
            result.SessionAfter.State.BattleState.RepetitionHistory,
            7);

        Assert.Equal(7, effect.ExpiresAfterEnemyTurnIndex);
        Assert.False(expiry.IsExactNoOp);
        Assert.Empty(expiry.TemporaryLibertiesAfterResolution.Effects);
        Assert.Contains(
            expiry.OrderedFacts,
            fact => fact is TemporaryLibertyExpiredFact expired &&
                StringComparer.Ordinal.Equals(
                    expired.Effect.EffectInstanceId,
                    effect.EffectInstanceId));
    }

    [Fact]
    public void ReinforceEffectFollowsItsStableAnchorThroughAStoneGroupMerge()
    {
        var basic = Definition(cost: 1);
        var reinforce = ReinforceDefinition(cost: 1);
        var board = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.Black, 2, 4),
            Stone(StoneColor.White, 7, 7));
        var recipe = new[]
        {
            new BattleCardInstance("basic-card", basic.ContentId),
            new BattleCardInstance("reinforce-card", reinforce.ContentId),
        };
        var session = StartSession(
            board,
            basic,
            baseQi: 3,
            cards: recipe,
            baseDraw: 2,
            reinforceDefinition: reinforce);
        var anchorId = session.State.RuntimeState.StoneRuntimeState
            .InstanceAt(C(2, 2))!.InstanceId;

        var reinforced = ExecuteReinforce(
            session,
            "reinforce-card",
            C(2, 2));
        var merged = Execute(
            reinforced.SessionAfter,
            "basic-card",
            C(2, 3),
            StoneCardPlacementMode.Frontline);

        Assert.True(reinforced.Accepted);
        Assert.True(merged.Accepted);
        var effect = Assert.Single(
            merged.SessionAfter.State.RuntimeState.TemporaryLibertyState.Effects);
        Assert.Equal(anchorId, effect.AnchorStoneInstanceId);
        var analysis = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            merged.SessionAfter.State.RuntimeState.StoneRuntimeState,
            merged.SessionAfter.State.RuntimeState.TemporaryLibertyState,
            merged.SessionAfter.State.RuntimeState.ContinuousLibertySnapshot);
        var group = analysis.GroupAnalysis.GroupAt(C(2, 2))!;
        Assert.Contains(group.Stones, stone => stone.Point.Equals(C(2, 4)));
        Assert.Equal(1, analysis.BreakdownFor(group).TimedAmount);
    }

    [Fact]
    public void ReversedReinforceInputsProduceTheSameCanonicalOutcome()
    {
        var reinforce = ReinforceDefinition(cost: 1);
        var stones = ReinforceAtariBoard().OccupiedStones.ToArray();
        var recipe = ReinforceRecipe(reinforce, reinforceCount: 4, includeFiller: true);
        var first = StartSession(
            Board(stones),
            Definition(cost: 1),
            baseQi: 3,
            cards: recipe,
            baseDraw: 4,
            reinforceDefinition: reinforce);
        var reversed = StartSession(
            Board(stones.Reverse().ToArray()),
            Definition(cost: 1),
            baseQi: 3,
            cards: recipe.Reverse().ToArray(),
            baseDraw: 4,
            reverseCatalogInput: true,
            reverseRuntimeInput: true,
            reinforceDefinition: ReinforceDefinition(cost: 1));
        var cardId = first.State.CardTurnState.Deck.Hand.First(candidate =>
            StringComparer.Ordinal.Equals(candidate.ContentId, reinforce.ContentId)).InstanceId;

        var firstResult = ExecuteReinforce(first, cardId, C(2, 3));
        var reversedResult = ExecuteReinforce(reversed, cardId, C(2, 3));

        Assert.True(firstResult.Accepted);
        Assert.True(reversedResult.Accepted);
        Assert.Equal(first.State.CanonicalText, reversed.State.CanonicalText);
        Assert.Equal(
            firstResult.SessionAfter.State.CanonicalText,
            reversedResult.SessionAfter.State.CanonicalText);
        Assert.Equal(firstResult.StateChecksum, reversedResult.StateChecksum);
        Assert.Equal(firstResult.LogChecksum, reversedResult.LogChecksum);
        Assert.Equal(
            firstResult.OrderedFacts.Select(ProjectFact),
            reversedResult.OrderedFacts.Select(ProjectFact));
    }

    [Fact]
    public void ReversedCatalogRuntimeAndBoardInputsProduceTheSameAcceptedOutcome()
    {
        var definition = LureDefinition(cost: 1);
        var stones = new[]
        {
            Stone(StoneColor.White, 3, 3),
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.Black, 1, 2),
        };
        var first = StartSession(Board(stones), definition, baseQi: 3);
        var reversed = StartSession(
            Board(stones.Reverse().ToArray()),
            definition,
            baseQi: 3,
            reverseCatalogInput: true,
            reverseRuntimeInput: true);

        var firstResult = Execute(
            first,
            "play-card",
            C(2, 3),
            StoneCardPlacementMode.Contact);
        var reversedResult = Execute(
            reversed,
            "play-card",
            C(2, 3),
            StoneCardPlacementMode.Contact);

        Assert.True(firstResult.Accepted);
        Assert.True(reversedResult.Accepted);
        Assert.Equal(first.State.CanonicalText, reversed.State.CanonicalText);
        Assert.Equal(
            firstResult.SessionAfter.State.CanonicalText,
            reversedResult.SessionAfter.State.CanonicalText);
        Assert.Equal(firstResult.StateChecksum, reversedResult.StateChecksum);
        Assert.Equal(firstResult.LogChecksum, reversedResult.LogChecksum);
        Assert.Equal(
            firstResult.OrderedFacts.Select(ProjectFact),
            reversedResult.OrderedFacts.Select(ProjectFact));
    }

    private static CoreDuelCardPlaySession StartSession(
        BoardState board,
        StarterStoneCardPlayDefinition definition,
        int baseQi,
        BattleRepetitionHistory? history = null,
        FacilityState? facilities = null,
        IReadOnlyList<BattleCardInstance>? cards = null,
        Func<CoreDuelCardTurnState, CoreDuelCardTurnState>? transformCardTurn = null,
        string contentHash = ContentHash,
        int? baseDraw = null,
        bool reverseCatalogInput = false,
        bool reverseRuntimeInput = false,
        int playerTurnIndex = 1,
        Func<StoneRuntimeState, TemporaryLibertyState>? temporaryFactory = null,
        Func<StoneRuntimeState, ContinuousLibertySnapshot>? continuousFactory = null,
        StarterReinforceCardPlayDefinition? reinforceDefinition = null)
    {
        var recipe = cards ??
            [new BattleCardInstance("play-card", definition.ContentId)];
        var facilityState = facilities ?? FacilityState.Create(board, [], 1);
        var facilityPolicy = FacilityPolicy();
        var initialCardTurn = CoreDuelCardTurnKernel.StartBattle(
            recipe,
            AuthoritativeRngState.Create(Seed),
            CoreDuelSystemPolicy.Create(
                baseQi,
                baseDraw ?? Math.Max(1, recipe.Count)),
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
        var runtime = Runtime(board, reverseRuntimeInput);
        var counterattackPolicy = new CounterattackBoundaryPolicy(200, 12, 3, 30);
        var initial = BattleAuthoritativeInitialSnapshot.Create(
            runtime,
            temporaryFactory?.Invoke(runtime) ??
                TemporaryLibertyState.Create(runtime, [], 1),
            continuousFactory?.Invoke(runtime) ??
                ContinuousLibertySnapshot.Empty(runtime),
            history ?? BattleRepetitionHistory.Start(board),
            facilityState,
            cardTurn.ClosedWindowResources,
            CaptureBenefitTriggerPlan.Create([]),
            CounterattackBoundaryState.Create(0, false, 0, counterattackPolicy),
            counterattackPolicy,
            new BattleRuntimePolicy(20, facilityPolicy),
            playerTurnIndex);

        return CoreDuelCardPlayStateMachine.Start(
            initial,
            cardTurn,
            Catalog(definition, reverseCatalogInput),
            reinforceDefinition ?? ReinforceDefinition(cost: 1),
            metadata);
    }

    private static CoreDuelCardPlayResult Execute(
        CoreDuelCardPlaySession session,
        string cardInstanceId,
        CanonicalPoint target,
        StoneCardPlacementMode mode) =>
        CoreDuelCardPlayStateMachine.Execute(
            session,
            Command(session, cardInstanceId, target, mode));

    private static CoreDuelCardPlayResult ExecuteReinforce(
        CoreDuelCardPlaySession session,
        string cardInstanceId,
        CanonicalPoint target) =>
        CoreDuelCardPlayStateMachine.Execute(
            session,
            new PlayCardCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum,
                cardInstanceId,
                target));

    private static PlayCardCommand Command(
        CoreDuelCardPlaySession session,
        string cardInstanceId,
        CanonicalPoint target,
        StoneCardPlacementMode mode,
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
        Assert.Same(
            source.State.RuntimeState.StoneRuntimeState,
            result.SessionAfter.State.RuntimeState.StoneRuntimeState);
        Assert.Same(
            source.State.RuntimeState.TemporaryLibertyState,
            result.SessionAfter.State.RuntimeState.TemporaryLibertyState);
        Assert.Same(
            source.State.RuntimeState.ContinuousLibertySnapshot,
            result.SessionAfter.State.RuntimeState.ContinuousLibertySnapshot);
        Assert.Same(
            source.State.RuntimeState.ClosedWindowResources,
            result.SessionAfter.State.RuntimeState.ClosedWindowResources);
        Assert.Same(
            source.State.RuntimeState.CaptureBenefitTriggerPlan,
            result.SessionAfter.State.RuntimeState.CaptureBenefitTriggerPlan);
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
        CardDrawnFact drawn =>
            $"card_drawn|instance={drawn.Card.InstanceId}|content={drawn.Card.ContentId}|" +
            $"reason={drawn.ReasonId}|source={drawn.SourceId}",
        TurnReservedDrawChangedFact reserved =>
            $"reserved_draw|trigger={reserved.TriggerId}|event={reserved.EventId}|" +
            $"before={reserved.AmountBefore.ToString(CultureInfo.InvariantCulture)}|" +
            $"after={reserved.AmountAfter.ToString(CultureInfo.InvariantCulture)}|" +
            $"delta={reserved.Delta.ToString(CultureInfo.InvariantCulture)}",
        TemporaryLibertyGrantedFact granted =>
            "temporary_liberty_granted|effect=" + granted.Effect.EffectInstanceId +
            "|amount=" + granted.Effect.Amount.ToString(CultureInfo.InvariantCulture) +
            "|owner=" + granted.Effect.OwnerColor +
            "|anchor=" + granted.Effect.AnchorStoneInstanceId +
            "|source=" + granted.Effect.SourceId +
            "|sequence=" +
            granted.Effect.CreatedSequence.ToString(CultureInfo.InvariantCulture) +
            "|expiry=" +
            granted.Effect.ExpiresAfterEnemyTurnIndex.ToString(CultureInfo.InvariantCulture) +
            "|group=" + granted.TargetGroupAnchor,
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

    private static StarterStoneCardPlayDefinition Definition(
        int cost,
        string contentId = "card_shape_driven") =>
        StarterStoneCardPlayDefinition.Create(
            CardContentDefinition.Create(
                contentId,
                CardRarity.Starter,
                cost,
                CardContentType.Stone,
                CardTargetKind.None,
                [CardPlacementTag.Frontline, CardPlacementTag.Terminal],
                [new PlaceStoneOperationDefinition(StoneContentKind.Basic)]));

    private static StarterStoneCardPlayDefinition ExtendDefinition(int cost) =>
        StarterStoneCardPlayDefinition.Create(
            CardContentDefinition.Create(
                "card_shape_extend",
                CardRarity.Starter,
                cost,
                CardContentType.Stone,
                CardTargetKind.None,
                [CardPlacementTag.Frontline],
                [
                    new PlaceStoneOperationDefinition(StoneContentKind.Basic),
                    new DrawIfRealLibertiesAtLeastOperationDefinition(3, 1),
                ]));

    private static StarterStoneCardPlayDefinition ContactDefinition(int cost) =>
        StarterStoneCardPlayDefinition.Create(
            CardContentDefinition.Create(
                "card_shape_contact_selected",
                CardRarity.Starter,
                cost,
                CardContentType.Stone,
                CardTargetKind.None,
                [CardPlacementTag.Contact, CardPlacementTag.Terminal],
                [
                    new PlaceStoneOperationDefinition(StoneContentKind.Basic),
                    new GainQiIfEnemyAtariOperationDefinition(1),
                ]));

    private static StarterStoneCardPlayDefinition LureDefinition(int cost) =>
        StarterStoneCardPlayDefinition.Create(
            CardContentDefinition.Create(
                "card_shape_lure_selected",
                CardRarity.Starter,
                cost,
                CardContentType.Stone,
                CardTargetKind.None,
                [CardPlacementTag.Contact],
                [
                    new PlaceStoneOperationDefinition(StoneContentKind.Lure),
                    new ReserveDrawOperationDefinition(1),
                ],
                [new ReserveDrawOperationDefinition(2)]));

    private static StarterReinforceCardPlayDefinition ReinforceDefinition(int cost) =>
        StarterReinforceCardPlayDefinition.Create(
            CardContentDefinition.Create(
                "card_shape_reinforce_selected",
                CardRarity.Starter,
                cost,
                CardContentType.Technique,
                CardTargetKind.FriendlyGroup,
                [],
                [
                    new DrawIfTargetAtariOperationDefinition(1),
                    new TemporaryLibertyOperationDefinition(
                        1,
                        TemporaryLibertyDurationKind.EnemyTurnEnd,
                        TemporaryLibertyTiming.FirstEnemyTurnEndAtOrAfterGrant,
                        TemporaryLibertyStacking.AdditivePerEffectInstance),
                ]));

    private static IReadOnlyList<BattleCardInstance> ReinforceRecipe(
        StarterReinforceCardPlayDefinition definition,
        int reinforceCount,
        bool includeFiller)
    {
        var cards = Enumerable.Range(1, reinforceCount)
            .Select(index => new BattleCardInstance(
                $"reinforce-{index.ToString(CultureInfo.InvariantCulture)}",
                definition.ContentId))
            .ToList();
        if (includeFiller)
        {
            cards.Add(new BattleCardInstance("draw-filler", "card_draw_filler"));
        }

        return cards;
    }

    private static StarterStoneCardPlayCatalog Catalog(
        StarterStoneCardPlayDefinition selected,
        bool reverseInput = false)
    {
        var definitions = StarterContents()
            .Where(content =>
                StarterStoneCardPlayDefinition.Create(content).Profile != selected.Profile)
            .Append(Content(selected))
            .ToArray();
        if (reverseInput)
        {
            Array.Reverse(definitions);
        }

        return StarterStoneCardPlayCatalog.Create(definitions);
    }

    private static IReadOnlyList<CardContentDefinition> StarterContents() =>
    [
        CardContentDefinition.Create(
            "card_shape_basic",
            CardRarity.Starter,
            1,
            CardContentType.Stone,
            CardTargetKind.None,
            [CardPlacementTag.Frontline, CardPlacementTag.Terminal],
            [new PlaceStoneOperationDefinition(StoneContentKind.Basic)]),
        CardContentDefinition.Create(
            "card_shape_extend_default",
            CardRarity.Starter,
            1,
            CardContentType.Stone,
            CardTargetKind.None,
            [CardPlacementTag.Frontline],
            [
                new PlaceStoneOperationDefinition(StoneContentKind.Basic),
                new DrawIfRealLibertiesAtLeastOperationDefinition(3, 1),
            ]),
        CardContentDefinition.Create(
            "card_shape_contact",
            CardRarity.Starter,
            1,
            CardContentType.Stone,
            CardTargetKind.None,
            [CardPlacementTag.Contact, CardPlacementTag.Terminal],
            [
                new PlaceStoneOperationDefinition(StoneContentKind.Basic),
                new GainQiIfEnemyAtariOperationDefinition(1),
            ]),
        CardContentDefinition.Create(
            "card_shape_lure",
            CardRarity.Starter,
            1,
            CardContentType.Stone,
            CardTargetKind.None,
            [CardPlacementTag.Contact],
            [
                new PlaceStoneOperationDefinition(StoneContentKind.Lure),
                new ReserveDrawOperationDefinition(1),
            ],
            [new ReserveDrawOperationDefinition(2)]),
    ];

    private static CardContentDefinition Content(
        StarterStoneCardPlayDefinition definition) =>
        CardContentDefinition.Create(
            definition.ContentId,
            CardRarity.Starter,
            definition.Cost,
            CardContentType.Stone,
            CardTargetKind.None,
            definition.PlacementTags,
            definition.Effects,
            definition.OnCaptured);

    private static StoneRuntimeState Runtime(
        BoardState board,
        bool reverseInput = false)
    {
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                $"initial-stone-{index + 1}",
                stone,
                stone.IsKing ? "king" : "basic",
                index + 1L,
                []))
            .ToArray();
        if (reverseInput)
        {
            Array.Reverse(instances);
        }

        return StoneRuntimeState.Create(board, instances, instances.Length + 1L);
    }

    private static FacilityRuntimePolicy FacilityPolicy() =>
        FacilityRuntimePolicy.Create(
            territoryIncomeDivisor: 3,
            capacityBands: [new FacilityCapacityBand(1, 49, 1)],
            slotCap: 3,
            typeLimits: [new KeyValuePair<string, int>("default", 1)]);

    private static BoardState NeutralFrontlineBoard() => Board(
        Stone(StoneColor.Black, 2, 2),
        Stone(StoneColor.White, 7, 7));

    private static BoardState ReinforceAtariBoard() => Board(
        Stone(StoneColor.Black, 2, 2),
        Stone(StoneColor.Black, 2, 3),
        Stone(StoneColor.White, 2, 1),
        Stone(StoneColor.White, 1, 2),
        Stone(StoneColor.White, 3, 2),
        Stone(StoneColor.White, 1, 3),
        Stone(StoneColor.White, 2, 4));

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
