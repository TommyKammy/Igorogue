using Igorogue.Application.Battle;
using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

public sealed class CoreDuelCardTurnStateTests
{
    private static readonly BoardGeometry Geometry =
        BoardGeometry.Create(BoardGeometry.AcceptedSize);

    [Fact]
    public void StartBattleCanonicalizesInjectedRecipeBeforeDeterministicShuffle()
    {
        var recipe = Recipe(8);
        var policy = CoreDuelSystemPolicy.Create(4, 3);
        var resources = EmptyResources();
        var flags = Flags(("turn.combo", true), ("turn.surge", false));

        var forward = CoreDuelCardTurnKernel.StartBattle(
            recipe,
            AuthoritativeRngState.Create(90210),
            policy,
            resources,
            flags);
        var reversed = CoreDuelCardTurnKernel.StartBattle(
            recipe.Reverse(),
            AuthoritativeRngState.Create(90210),
            policy,
            resources,
            flags.Reverse());

        Assert.Equal(Ids(forward.Deck.DrawPile), Ids(reversed.Deck.DrawPile));
        Assert.Equal(forward.Deck.CanonicalText, reversed.Deck.CanonicalText);
        Assert.Equal(forward.RngState, reversed.RngState);
        Assert.Equal(
            forward.RngState.ToCanonicalText(),
            reversed.RngState.ToCanonicalText());
        Assert.Equal(forward.CanonicalText, reversed.CanonicalText);
        Assert.Equal(forward.Checksum, reversed.Checksum);
    }

    [Fact]
    public void StartPlayerTurnUsesExactStageOrderAndResetsOnlyTurnScopedState()
    {
        var board = BlackTerritoryBoard();
        var facilities = FacilityState.Create(board, [], 1);
        var choices = new[]
        {
            new DeferredPlayerChoice("source.alpha", "choice.alpha", 1),
            new DeferredPlayerChoice("source.beta", "choice.beta", 2),
        };
        var resources = ClosedWindowResourceState.Create(
            turnReservedDraw: 1,
            turnReservedQi: 5,
            soul: 7,
            standardCaptureRewardsClaimed: 8,
            deferredChoices: choices,
            firstUseFlags: Flags(("first.alpha", true), ("first.beta", false)),
            nextDeferredChoiceSequence: 3);
        var state = CoreDuelCardTurnKernel.StartBattle(
            Recipe(8),
            AuthoritativeRngState.Create(73),
            CoreDuelSystemPolicy.Create(baseQi: 4, baseDraw: 2),
            resources,
            Flags(("turn.combo", true), ("turn.surge", true)));

        var transition = CoreDuelCardTurnKernel.StartPlayerTurn(
            state,
            board,
            facilities,
            FacilityPolicy(territoryIncomeDivisor: 3),
            [
                new AuthorizedDeferredChoiceOutcome(
                    "source.alpha",
                    "choice.alpha",
                    createdSequence: 1,
                    qiDelta: 2,
                    drawDelta: 1),
                new AuthorizedDeferredChoiceOutcome(
                    "source.beta",
                    "choice.beta",
                    createdSequence: 2,
                    qiDelta: 3,
                    drawDelta: 0),
            ]);

        Assert.True(transition.Accepted);
        Assert.False(transition.IsExactNoOp);
        Assert.Equal(
            new[]
            {
                CoreDuelCardTurnStage.ResetTurnScopedFlags,
                CoreDuelCardTurnStage.ResolveDeferredPlayerChoices,
                CoreDuelCardTurnStage.RecalculateTerritory,
                CoreDuelCardTurnStage.ApplyQi,
                CoreDuelCardTurnStage.DrawCards,
            },
            transition.Stages);
        Assert.Equal(16, transition.TerritoryIncome);
        Assert.Equal(30, transition.AppliedQi);
        Assert.Equal(4, transition.RequestedDrawCount);

        var after = transition.StateAfter;
        Assert.Equal(30, after.Qi);
        Assert.Equal(4, after.Deck.Hand.Count);
        Assert.Equal(0, after.ClosedWindowResources.TurnReservedQi);
        Assert.Equal(0, after.ClosedWindowResources.TurnReservedDraw);
        Assert.Empty(after.ClosedWindowResources.DeferredPlayerChoices);
        Assert.Equal(7, after.ClosedWindowResources.Soul);
        Assert.Equal(8, after.ClosedWindowResources.StandardCaptureRewardsClaimed);
        Assert.Equal(3, after.ClosedWindowResources.NextDeferredChoiceSequence);
        Assert.True(after.ClosedWindowResources.FirstUseFlags["first.alpha"]);
        Assert.False(after.ClosedWindowResources.FirstUseFlags["first.beta"]);
        Assert.False(after.TurnScopedFlags["turn.combo"]);
        Assert.False(after.TurnScopedFlags["turn.surge"]);
    }

    [Fact]
    public void DeferredChoiceOutcomeMissingMismatchOrReversalIsExactNoOp()
    {
        var board = BlackTerritoryBoard();
        var state = CoreDuelCardTurnKernel.StartBattle(
            Recipe(5),
            AuthoritativeRngState.Create(41),
            CoreDuelSystemPolicy.Create(4, 2),
            ClosedWindowResourceState.Create(
                turnReservedDraw: 1,
                turnReservedQi: 1,
                soul: 0,
                standardCaptureRewardsClaimed: 0,
                deferredChoices:
                [
                    new DeferredPlayerChoice("source.alpha", "choice.alpha", 1),
                    new DeferredPlayerChoice("source.beta", "choice.beta", 2),
                ],
                firstUseFlags: [],
                nextDeferredChoiceSequence: 3),
            Flags(("turn.combo", true)));
        var first = new AuthorizedDeferredChoiceOutcome(
            "source.alpha",
            "choice.alpha",
            1,
            0,
            0);
        var second = new AuthorizedDeferredChoiceOutcome(
            "source.beta",
            "choice.beta",
            2,
            0,
            0);
        var mismatched = new AuthorizedDeferredChoiceOutcome(
            "source.beta",
            "choice.wrong",
            2,
            0,
            0);

        var transitions = new[]
        {
            StartTurn(state, board, [first]),
            StartTurn(state, board, [first, mismatched]),
            StartTurn(state, board, [second, first]),
        };

        Assert.All(transitions, transition =>
        {
            AssertExactNoOp(state, transition);
            Assert.Equal("deferred_choice_outcomes_mismatch", transition.ReasonId);
        });
    }

    [Fact]
    public void StartPlayerTurnQiOrDrawOverflowIsExactNoOp()
    {
        var board = BlackTerritoryBoard();
        var qiOverflow = CoreDuelCardTurnKernel.StartBattle(
            Recipe(2),
            AuthoritativeRngState.Create(84),
            CoreDuelSystemPolicy.Create(int.MaxValue, 1),
            EmptyResources(),
            Flags(("turn.combo", true)));
        var drawOverflow = CoreDuelCardTurnKernel.StartBattle(
            Recipe(2),
            AuthoritativeRngState.Create(84),
            CoreDuelSystemPolicy.Create(1, int.MaxValue),
            EmptyResources(reservedDraw: 1),
            Flags(("turn.combo", true)));

        var transitions = new[]
        {
            StartTurn(qiOverflow, board, []),
            StartTurn(drawOverflow, board, []),
        };

        AssertExactNoOp(qiOverflow, transitions[0]);
        AssertExactNoOp(drawOverflow, transitions[1]);
        Assert.All(
            transitions,
            transition => Assert.Equal("turn_start_overflow", transition.ReasonId));
    }

    [Fact]
    public void EmptyDeckTurnStartStillAppliesQiAndResetWithoutConsumingRng()
    {
        var board = BlackTerritoryBoard();
        var resources = ClosedWindowResourceState.Create(
            turnReservedDraw: 3,
            turnReservedQi: 2,
            soul: 9,
            standardCaptureRewardsClaimed: 4,
            deferredChoices: [],
            firstUseFlags: Flags(("first.alpha", true)),
            nextDeferredChoiceSequence: 7);
        var state = CoreDuelCardTurnKernel.StartBattle(
            [],
            AuthoritativeRngState.Create(-9),
            CoreDuelSystemPolicy.Create(baseQi: 4, baseDraw: 2),
            resources,
            Flags(("turn.combo", true)));

        var transition = StartTurn(state, board, []);

        Assert.True(transition.Accepted);
        Assert.Equal(16, transition.TerritoryIncome);
        Assert.Equal(22, transition.AppliedQi);
        Assert.Equal(5, transition.RequestedDrawCount);
        Assert.Equal(22, transition.StateAfter.Qi);
        Assert.Empty(transition.StateAfter.Deck.DrawPile);
        Assert.Empty(transition.StateAfter.Deck.Hand);
        Assert.Same(state.Deck, transition.StateAfter.Deck);
        Assert.Same(state.RngState, transition.StateAfter.RngState);
        Assert.Equal(0, transition.StateAfter.ClosedWindowResources.TurnReservedQi);
        Assert.Equal(0, transition.StateAfter.ClosedWindowResources.TurnReservedDraw);
        Assert.Equal(9, transition.StateAfter.ClosedWindowResources.Soul);
        Assert.Equal(4, transition.StateAfter.ClosedWindowResources.StandardCaptureRewardsClaimed);
        Assert.Equal(7, transition.StateAfter.ClosedWindowResources.NextDeferredChoiceSequence);
        Assert.True(transition.StateAfter.ClosedWindowResources.FirstUseFlags["first.alpha"]);
        Assert.False(transition.StateAfter.TurnScopedFlags["turn.combo"]);
    }

    [Fact]
    public void ResolvedCardRemainsResolvingUntilEndTurnThenFollowsHandIntoDiscard()
    {
        var board = BlackTerritoryBoard();
        var state = CoreDuelCardTurnKernel.StartBattle(
            Recipe(5),
            AuthoritativeRngState.Create(25),
            CoreDuelSystemPolicy.Create(baseQi: 4, baseDraw: 3),
            EmptyResources(),
            []);
        var turn = StartTurn(state, board, []);
        var chosen = turn.StateAfter.Deck.Hand[1];

        var begun = CoreDuelCardTurnKernel.BeginResolution(
            turn.StateAfter,
            chosen.InstanceId);
        Assert.True(begun.Accepted);
        var active = Assert.Single(begun.StateAfter.Deck.Resolving);
        Assert.Same(chosen, active.Card);
        Assert.True(active.IsActive);
        var remainingHand = Ids(begun.StateAfter.Deck.Hand);
        var blockedEnd = CoreDuelCardTurnKernel.EndPlayerTurn(begun.StateAfter);
        AssertExactNoOp(begun.StateAfter, blockedEnd);
        Assert.Equal("active_resolution_exists", blockedEnd.ReasonId);

        var completed = CoreDuelCardTurnKernel.CompleteResolution(
            begun.StateAfter,
            chosen.InstanceId);
        Assert.True(completed.Accepted);
        var resolved = Assert.Single(completed.StateAfter.Deck.Resolving);
        Assert.Same(chosen, resolved.Card);
        Assert.True(resolved.IsResolved);
        Assert.Empty(completed.StateAfter.Deck.DiscardPile);

        var ended = CoreDuelCardTurnKernel.EndPlayerTurn(completed.StateAfter);

        Assert.True(ended.Accepted);
        Assert.Empty(ended.StateAfter.Deck.Hand);
        Assert.Empty(ended.StateAfter.Deck.Resolving);
        Assert.Equal(
            remainingHand.Append(chosen.InstanceId),
            Ids(ended.StateAfter.Deck.DiscardPile));
        Assert.Equal(0, ended.StateAfter.Qi);
        Assert.Same(completed.StateAfter.RngState, ended.StateAfter.RngState);
    }

    [Fact]
    public void ExhaustMovesCardExactlyOnceAndRepeatingIsExactNoOp()
    {
        var state = CoreDuelCardTurnKernel.StartBattle(
            Recipe(3),
            AuthoritativeRngState.Create(62),
            CoreDuelSystemPolicy.Create(4, 2),
            EmptyResources(),
            []);
        var chosen = state.Deck.DrawPile[1];

        var first = CoreDuelCardTurnKernel.Exhaust(state, chosen.InstanceId);

        Assert.True(first.Accepted);
        Assert.Same(chosen, Assert.Single(first.StateAfter.Deck.ExhaustPile));
        Assert.DoesNotContain(
            first.StateAfter.Deck.DrawPile,
            card => card.InstanceId == chosen.InstanceId);

        var repeated = CoreDuelCardTurnKernel.Exhaust(
            first.StateAfter,
            chosen.InstanceId);

        AssertExactNoOp(first.StateAfter, repeated);
        Assert.Equal("card_already_exhausted", repeated.ReasonId);
        Assert.Same(chosen, Assert.Single(repeated.StateAfter.Deck.ExhaustPile));
    }

    [Fact]
    public void ChecksumDetectsRngZoneQiAndReservedResourceDifferences()
    {
        var recipe = Recipe(5);
        var policy = CoreDuelSystemPolicy.Create(4, 2);
        var baseline = CoreDuelCardTurnKernel.StartBattle(
            recipe,
            AuthoritativeRngState.Create(100),
            policy,
            EmptyResources(),
            []);
        var changedRng = CoreDuelCardTurnKernel.StartBattle(
            recipe,
            AuthoritativeRngState.Create(101),
            policy,
            EmptyResources(),
            []);

        Assert.NotEqual(
            baseline.RngState.ToCanonicalText(),
            changedRng.RngState.ToCanonicalText());
        Assert.NotEqual(baseline.Checksum, changedRng.Checksum);

        var reserved = CoreDuelCardTurnKernel.StartBattle(
            recipe,
            AuthoritativeRngState.Create(100),
            policy,
            EmptyResources(reservedQi: 1),
            []);
        Assert.Equal(baseline.Deck.CanonicalText, reserved.Deck.CanonicalText);
        Assert.Equal(
            baseline.RngState.ToCanonicalText(),
            reserved.RngState.ToCanonicalText());
        Assert.NotEqual(
            baseline.ClosedWindowResources.ToCanonicalText(),
            reserved.ClosedWindowResources.ToCanonicalText());
        Assert.NotEqual(baseline.Checksum, reserved.Checksum);

        var board = BlackTerritoryBoard();
        var baselineTurn = StartTurn(baseline, board, []).StateAfter;
        var reservedTurn = StartTurn(reserved, board, []).StateAfter;
        Assert.Equal(baselineTurn.Deck.CanonicalText, reservedTurn.Deck.CanonicalText);
        Assert.Equal(
            baselineTurn.RngState.ToCanonicalText(),
            reservedTurn.RngState.ToCanonicalText());
        Assert.Equal(
            baselineTurn.ClosedWindowResources.ToCanonicalText(),
            reservedTurn.ClosedWindowResources.ToCanonicalText());
        Assert.NotEqual(baselineTurn.Qi, reservedTurn.Qi);
        Assert.NotEqual(baselineTurn.Checksum, reservedTurn.Checksum);

        var movedZone = CoreDuelCardTurnKernel.BeginResolution(
            baselineTurn,
            baselineTurn.Deck.Hand[0].InstanceId).StateAfter;
        Assert.Equal(baselineTurn.Qi, movedZone.Qi);
        Assert.Equal(
            baselineTurn.RngState.ToCanonicalText(),
            movedZone.RngState.ToCanonicalText());
        Assert.Equal(
            baselineTurn.ClosedWindowResources.ToCanonicalText(),
            movedZone.ClosedWindowResources.ToCanonicalText());
        Assert.NotEqual(baselineTurn.Deck.CanonicalText, movedZone.Deck.CanonicalText);
        Assert.NotEqual(baselineTurn.Checksum, movedZone.Checksum);
    }

    [Fact]
    public void AuthorizedDeferredChoiceRejectsNegativeDeltas()
    {
        var qi = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AuthorizedDeferredChoiceOutcome(
                "source.alpha",
                "choice.alpha",
                1,
                qiDelta: -1,
                drawDelta: 0));
        var draw = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AuthorizedDeferredChoiceOutcome(
                "source.alpha",
                "choice.alpha",
                1,
                qiDelta: 0,
                drawDelta: -1));

        Assert.Equal("qiDelta", qi.ParamName);
        Assert.Equal("drawDelta", draw.ParamName);
    }

    private static CoreDuelCardTurnTransition StartTurn(
        CoreDuelCardTurnState state,
        BoardState board,
        IEnumerable<AuthorizedDeferredChoiceOutcome> outcomes) =>
        CoreDuelCardTurnKernel.StartPlayerTurn(
            state,
            board,
            FacilityState.Create(board, [], 1),
            FacilityPolicy(territoryIncomeDivisor: 3),
            outcomes);

    private static void AssertExactNoOp(
        CoreDuelCardTurnState state,
        CoreDuelCardTurnTransition transition)
    {
        Assert.True(transition.IsExactNoOp);
        Assert.False(transition.Accepted);
        Assert.Same(state, transition.StateBefore);
        Assert.Same(state, transition.StateAfter);
        Assert.Same(state.RngState, transition.StateAfter.RngState);
        Assert.Empty(transition.Stages);
    }

    private static BoardState BlackTerritoryBoard() =>
        BoardState.Create(
            Geometry,
            [
                new BoardStone(
                    StoneColor.Black,
                    isKing: false,
                    Geometry.CreateCanonicalPoint(1, 1)),
            ]);

    private static FacilityRuntimePolicy FacilityPolicy(
        int territoryIncomeDivisor) =>
        FacilityRuntimePolicy.Create(
            territoryIncomeDivisor,
            [new FacilityCapacityBand(1, 49, 1)],
            slotCap: 3,
            typeLimits: [new KeyValuePair<string, int>("default", 1)]);

    private static ClosedWindowResourceState EmptyResources(
        int reservedQi = 0,
        int reservedDraw = 0) =>
        ClosedWindowResourceState.Create(
            turnReservedDraw: reservedDraw,
            turnReservedQi: reservedQi,
            soul: 0,
            standardCaptureRewardsClaimed: 0,
            deferredChoices: [],
            firstUseFlags: [],
            nextDeferredChoiceSequence: 1);

    private static BattleCardInstance[] Recipe(int count) =>
        Enumerable.Range(1, count)
            .Select(index => new BattleCardInstance(
                $"instance-{index:D2}",
                $"card-{((index - 1) % 3) + 1}"))
            .ToArray();

    private static KeyValuePair<string, bool>[] Flags(
        params (string Id, bool Value)[] flags) =>
        flags.Select(flag => new KeyValuePair<string, bool>(flag.Id, flag.Value))
            .ToArray();

    private static string[] Ids(IEnumerable<BattleCardInstance> cards) =>
        cards.Select(card => card.InstanceId).ToArray();
}
