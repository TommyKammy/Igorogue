using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

public sealed class AuthoritativeEnemyTurnStateMachineTests
{
    private const string ContentHash =
        "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static readonly BoardGeometry Geometry =
        BoardGeometry.Create(BoardGeometry.AcceptedSize);

    [Fact]
    public void PendingAtTurnStartRequiresExactlyOneBonusActionBeforeBoundary()
    {
        var initial = Start(
            Runtime(Board()),
            counterattack: Counterattack(gaugeUnits: 10, pending: true));
        var enemy = EndTurn(initial).SessionAfter;

        var normal = EnemyPass(enemy);

        Assert.True(normal.Accepted);
        Assert.Equal(BattlePhase.EnemyAction, normal.SessionAfter.State.Phase);
        Assert.Equal(
            EnemyActionStage.CounterattackAction,
            normal.SessionAfter.State.AuthoritativeRuntime!.EnemyActionStage);
        Assert.Equal(
            ["enemy_normal_action"],
            normal.OrderedFacts
                .OfType<EnemyTurnBoundaryStageFact>()
                .Select(fact => fact.StageId));
        Assert.DoesNotContain(
            normal.OrderedFacts,
            fact => fact is CounterattackPendingConsumedFact);

        var bonus = EnemyPass(normal.SessionAfter);

        Assert.True(bonus.Accepted);
        Assert.Equal(BattlePhase.PlayerAction, bonus.SessionAfter.State.Phase);
        Assert.Equal(2, bonus.SessionAfter.State.PlayerTurnIndex);
        Assert.Null(bonus.SessionAfter.State.AuthoritativeRuntime!.EnemyActionStage);
        Assert.False(
            bonus.SessionAfter.State.AuthoritativeRuntime.CounterattackState.Pending);
        Assert.Equal(
            [
                "enemy_counterattack_action",
                "consume_current_pending_and_reprime_overflow",
                "temporary_liberty_expiry_sweep",
                "enemy_turn_end_counterattack_gain",
                "plan_next_intents",
            ],
            bonus.OrderedFacts
                .OfType<EnemyTurnBoundaryStageFact>()
                .Select(fact => fact.StageId));
        Assert.Single(bonus.OrderedFacts.OfType<CounterattackPendingConsumedFact>());
        Assert.Empty(bonus.OrderedFacts.OfType<TemporaryLibertyExpirySweepStartedFact>());
        Assert.Equal(
            22,
            bonus.SessionAfter.State.AuthoritativeRuntime.CounterattackState.GaugeUnits);

        var extra = EnemyPass(bonus.SessionAfter);
        AssertRejectedExactNoOp(bonus.SessionAfter, extra, "wrong_phase");
    }

    [Fact]
    public void RuntimePlacementUsesTimedLibertyUntilFinalExpiryBoundary()
    {
        var board = Board(
            Stone(StoneColor.Black, 4, 4),
            Stone(StoneColor.White, 3, 4),
            Stone(StoneColor.White, 5, 4),
            Stone(StoneColor.White, 4, 3));
        var stones = Runtime(board);
        var anchor = stones.InstanceAt(C(4, 4))!;
        var temporary = TemporaryLibertyState.Create(
            stones,
            [new TemporaryLibertyEffect(
                "effect.guard",
                1,
                StoneColor.Black,
                anchor.InstanceId,
                "test.guard",
                1,
                1)],
            2);
        var initial = Start(stones, temporary);
        var enemy = EndTurn(initial).SessionAfter;

        var result = Place(enemy, 4, 5, "stone.enemy.placed");

        Assert.True(result.Accepted);
        Assert.Equal(BattlePhase.PlayerAction, result.SessionAfter.State.Phase);
        Assert.Null(result.SessionAfter.State.Board.StoneAt(C(4, 4)));
        Assert.NotNull(result.SessionAfter.State.Board.StoneAt(C(4, 5)));
        var placedIndex = result.OrderedFacts
            .Select((fact, index) => (fact, index))
            .Single(item => item.fact is StonePlacedFact)
            .index;
        var expiryCaptureIndex = result.OrderedFacts
            .Select((fact, index) => (fact, index))
            .Single(item => item.fact is TemporaryLibertyGroupCapturedFact)
            .index;
        Assert.True(expiryCaptureIndex > placedIndex);
        Assert.Empty(result.OrderedFacts.OfType<GroupCapturedFact>());
        Assert.Contains(
            result.OrderedFacts,
            fact => fact is TemporaryLibertyExpiredFact);
        Assert.Equal(
            3,
            result.SessionAfter.State.RepetitionHistory.ObservationCount);
        Assert.Equal(
            "stone.enemy.placed",
            result.SessionAfter.State.AuthoritativeRuntime!
                .StoneRuntimeState.InstanceAt(C(4, 5))!.InstanceId);
    }

    [Fact]
    public void ExpiryTerritoryRebindsExistingFacilitiesBeforeSweepResolution()
    {
        var board = Board(
            Stone(StoneColor.White, 4, 4),
            Stone(StoneColor.Black, 4, 3),
            Stone(StoneColor.Black, 3, 4),
            Stone(StoneColor.Black, 5, 4),
            Stone(StoneColor.Black, 4, 5));
        var stones = Runtime(board);
        var guarded = stones.InstanceAt(C(4, 4))!;
        var temporary = TemporaryLibertyState.Create(
            stones,
            [new TemporaryLibertyEffect(
                "effect.territory_guard",
                1,
                StoneColor.White,
                guarded.InstanceId,
                "test.guard",
                1,
                1)],
            2);
        var facility = new FacilityInstance(
            "facility.outer",
            "default",
            StoneColor.Black,
            C(1, 1),
            1);
        var facilities = FacilityState.Create(board, [facility], 2);
        var initial = Start(stones, temporary, facilities: facilities);
        Assert.True(
            initial.State.FacilityRuntimeAnalysis.OperatingStateFor(facility).IsActive);
        var enemy = EndTurn(initial).SessionAfter;

        var result = EnemyPass(enemy);

        Assert.True(result.Accepted);
        Assert.Same(
            facility,
            result.SessionAfter.State.FacilityState.FacilityById(
                facility.InstanceId));
        Assert.Same(
            result.SessionAfter.State.Board,
            result.SessionAfter.State.FacilityState.SourceBoard);
        Assert.True(
            result.SessionAfter.State.FacilityRuntimeAnalysis
                .OperatingStateFor(facility).IsActive);
        Assert.DoesNotContain(
            result.OrderedFacts,
            fact => fact is FacilityActivatedFact or FacilityDisabledFact);
        var facts = result.OrderedFacts.ToArray();
        var territoryIndex = Array.FindIndex(
            facts,
            fact => fact is TerritoryEstablishedFact);
        var resolvedIndex = Array.FindIndex(
            facts,
            fact => fact is TemporaryLibertyExpirySweepResolvedFact);
        Assert.InRange(territoryIndex, 0, resolvedIndex - 1);
    }

    [Fact]
    public void TerminalExpiryOnTurnTwentySuppressesBenefitsAndTurnLimit()
    {
        var board = Board(
            Stone(StoneColor.Black, 4, 4, isKing: true),
            Stone(StoneColor.White, 3, 4),
            Stone(StoneColor.White, 5, 4),
            Stone(StoneColor.White, 4, 3),
            Stone(StoneColor.White, 4, 5));
        var stones = Runtime(board);
        var king = stones.InstanceAt(C(4, 4))!;
        var temporary = TemporaryLibertyState.Create(
            stones,
            [new TemporaryLibertyEffect(
                "effect.king_guard",
                1,
                StoneColor.Black,
                king.InstanceId,
                "test.guard",
                1,
                20)],
            2);
        var trigger = new CaptureBenefitTrigger(
            CaptureBenefitSource.StandardAccounting("terminal.test", 0),
            "terminal.test",
            ["terminal", "test"],
            [new GainSoulCaptureBenefitOperation(99)],
            firstUseFlagId: null);
        var initial = Start(
            stones,
            temporary,
            playerTurnIndex: 20,
            triggerPlan: CaptureBenefitTriggerPlan.Create([trigger]));
        var enemy = EndTurn(initial).SessionAfter;

        var result = EnemyPass(enemy);

        Assert.True(result.Accepted);
        Assert.True(result.SessionAfter.State.IsTerminal);
        Assert.Equal(BattleEndReason.BlackKingCaptured, result.SessionAfter.State.EndReason);
        Assert.DoesNotContain(
            result.OrderedFacts,
            fact => fact is CaptureBatchStartedFact or SoulChangedFact);
        Assert.DoesNotContain(
            result.OrderedFacts.OfType<EnemyTurnBoundaryStageFact>(),
            fact => fact.Stage is EnemyTurnBoundaryStage.EnemyTurnEndCounterattackGain or
                EnemyTurnBoundaryStage.PlanNextIntents);
        Assert.DoesNotContain(
            result.OrderedFacts.OfType<BattleEndedFact>(),
            fact => fact.Reason == BattleEndReason.TurnLimit);
        Assert.Equal(
            0,
            result.SessionAfter.State.AuthoritativeRuntime!.ClosedWindowResources.Soul);
        Assert.Equal(
            0,
            result.SessionAfter.State.AuthoritativeRuntime.CounterattackState.GaugeUnits);
    }

    [Fact]
    public void TerminalNormalActionSuppressesPendingBonusAndEveryLaterBoundaryStage()
    {
        var board = Board(
            Stone(StoneColor.Black, 4, 4, isKing: true),
            Stone(StoneColor.White, 3, 4),
            Stone(StoneColor.White, 5, 4),
            Stone(StoneColor.White, 4, 3));
        var initial = Start(
            Runtime(board),
            counterattack: Counterattack(gaugeUnits: 9, pending: true),
            playerTurnIndex: 20);
        var enemy = EndTurn(initial).SessionAfter;

        var result = Place(enemy, 4, 5, "stone.enemy.execution");

        Assert.True(result.Accepted);
        Assert.True(result.SessionAfter.State.IsTerminal);
        Assert.Equal(BattleEndReason.BlackKingCaptured, result.SessionAfter.State.EndReason);
        Assert.Equal(
            ["enemy_normal_action"],
            result.OrderedFacts
                .OfType<EnemyTurnBoundaryStageFact>()
                .Select(fact => fact.StageId));
        Assert.DoesNotContain(
            result.OrderedFacts,
            fact => fact is CounterattackPendingConsumedFact or
                TemporaryLibertyExpirySweepStartedFact or
                CounterattackAdvancedFact);
        Assert.DoesNotContain(
            result.OrderedFacts.OfType<BattleEndedFact>(),
            fact => fact.Reason == BattleEndReason.TurnLimit);
        Assert.True(
            result.SessionAfter.State.AuthoritativeRuntime!.CounterattackState.Pending);
        Assert.Equal(
            9,
            result.SessionAfter.State.AuthoritativeRuntime.CounterattackState.GaugeUnits);
        AssertTerminalPlacementCaptureSuppressed(result);
    }

    [Fact]
    public void TerminalBonusPlacementSuppressesConsumeExpiryAndNaturalGain()
    {
        var board = Board(
            Stone(StoneColor.Black, 4, 4, isKing: true),
            Stone(StoneColor.White, 3, 4),
            Stone(StoneColor.White, 5, 4),
            Stone(StoneColor.White, 4, 3));
        var initial = Start(
            Runtime(board),
            counterattack: Counterattack(gaugeUnits: 11, pending: true));
        var enemy = EndTurn(initial).SessionAfter;
        var bonusStage = EnemyPass(enemy).SessionAfter;

        var result = Place(bonusStage, 4, 5, "stone.enemy.bonus_execution");

        Assert.True(result.Accepted);
        Assert.True(result.SessionAfter.State.IsTerminal);
        Assert.Equal(BattleEndReason.BlackKingCaptured, result.SessionAfter.State.EndReason);
        Assert.Equal(
            ["enemy_counterattack_action"],
            result.OrderedFacts
                .OfType<EnemyTurnBoundaryStageFact>()
                .Select(fact => fact.StageId));
        Assert.DoesNotContain(
            result.OrderedFacts,
            fact => fact is CounterattackPendingConsumedFact or
                TemporaryLibertyExpirySweepStartedFact or
                CounterattackAdvancedFact);
        Assert.True(
            result.SessionAfter.State.AuthoritativeRuntime!.CounterattackState.Pending);
        Assert.Equal(
            11,
            result.SessionAfter.State.AuthoritativeRuntime.CounterattackState.GaugeUnits);
        AssertTerminalPlacementCaptureSuppressed(result);
    }

    [Fact]
    public void NormalAndBonusPlacementShareConditionalBenefitsAndConsumeFirstUseOnce()
    {
        var board = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.White, 2, 1),
            Stone(StoneColor.White, 1, 2),
            Stone(StoneColor.White, 3, 2),
            Stone(StoneColor.Black, 6, 6),
            Stone(StoneColor.White, 6, 5),
            Stone(StoneColor.White, 5, 6),
            Stone(StoneColor.White, 7, 6));
        var stones = Runtime(board);
        var firstBlack = stones.InstanceAt(C(2, 2))!;
        var secondBlack = stones.InstanceAt(C(6, 6))!;
        var plan = CaptureBenefitTriggerPlan.CreateConditional(
        [
            CapturedStoneEntry(firstBlack),
            CapturedStoneEntry(secondBlack),
            new CaptureBenefitTriggerPlanEntry(
                new CaptureBenefitTrigger(
                    CaptureBenefitSource.Style("style.sacrifice"),
                    "style.first_capture",
                    ["style", "first_capture"],
                    [new ReserveDrawCaptureBenefitOperation(1)],
                    "style.first_capture"),
                CaptureBenefitTriggerCondition.CapturedNonKingBlackStone),
            new CaptureBenefitTriggerPlanEntry(
                new CaptureBenefitTrigger(
                    CaptureBenefitSource.Seal("seal.sacrifice", 0),
                    "seal.first_capture",
                    ["seal", "first_capture"],
                    [new ReserveDrawCaptureBenefitOperation(2)],
                    "seal.first_capture"),
                CaptureBenefitTriggerCondition.CapturedNonKingBlackStone),
            new CaptureBenefitTriggerPlanEntry(
                new CaptureBenefitTrigger(
                    CaptureBenefitSource.Sacrifice(),
                    "sacrifice.capture",
                    ["sacrifice", "capture"],
                    [new AdvanceSacrificePressureCaptureBenefitOperation()],
                    null),
                CaptureBenefitTriggerCondition.CapturedNonKingBlackStone),
        ]);
        var resources = ClosedWindowResourceState.Empty(
        [
            new KeyValuePair<string, bool>("style.first_capture", false),
            new KeyValuePair<string, bool>("seal.first_capture", false),
        ]);
        var initial = Start(
            stones,
            counterattack: Counterattack(gaugeUnits: 10, pending: true),
            triggerPlan: plan,
            resources: resources);
        var enemy = EndTurn(initial).SessionAfter;

        var normal = Place(enemy, 2, 3, "stone.enemy.normal_capture");

        Assert.True(normal.Accepted);
        Assert.Equal(
            EnemyActionStage.CounterattackAction,
            normal.SessionAfter.State.AuthoritativeRuntime!.EnemyActionStage);
        AssertPlacementCaptureBatch(normal, "stone.enemy.normal_capture", firstBlack);
        Assert.Equal(2, normal.OrderedFacts.OfType<FirstUseFlagConsumedFact>().Count());
        Assert.Equal(2, normal.OrderedFacts.OfType<TurnReservedDrawChangedFact>().Count());
        Assert.Single(normal.OrderedFacts.OfType<SoulChangedFact>());
        var afterNormal = normal.SessionAfter.State.AuthoritativeRuntime;
        Assert.Equal(3, afterNormal.ClosedWindowResources.TurnReservedDraw);
        Assert.Equal(2, afterNormal.ClosedWindowResources.Soul);
        Assert.True(afterNormal.ClosedWindowResources.IsFirstUseConsumed(
            "style.first_capture"));
        Assert.True(afterNormal.ClosedWindowResources.IsFirstUseConsumed(
            "seal.first_capture"));
        Assert.Equal(1, afterNormal.CounterattackState.SacrificeStoneRemainder);

        var bonus = Place(
            normal.SessionAfter,
            6,
            7,
            "stone.enemy.bonus_capture");

        Assert.True(bonus.Accepted);
        AssertPlacementCaptureBatch(bonus, "stone.enemy.bonus_capture", secondBlack);
        Assert.Empty(bonus.OrderedFacts.OfType<FirstUseFlagConsumedFact>());
        Assert.Empty(bonus.OrderedFacts.OfType<TurnReservedDrawChangedFact>());
        Assert.Single(bonus.OrderedFacts.OfType<SoulChangedFact>());
        var afterBonus = bonus.SessionAfter.State.AuthoritativeRuntime!;
        Assert.Equal(3, afterBonus.ClosedWindowResources.TurnReservedDraw);
        Assert.Equal(4, afterBonus.ClosedWindowResources.Soul);
        Assert.Equal(2, afterBonus.CounterattackState.SacrificeStoneRemainder);
        Assert.Equal(22, afterBonus.CounterattackState.GaugeUnits);
        Assert.Equal(BattlePhase.PlayerAction, bonus.SessionAfter.State.Phase);
    }

    [Fact]
    public void TurnTwentyPendingBoundaryEndsOnlyAfterBonusNaturalGainAndPlanningTrace()
    {
        var initial = Start(
            Runtime(Board()),
            counterattack: Counterattack(gaugeUnits: 5, pending: true),
            playerTurnIndex: 20);
        var enemy = EndTurn(initial).SessionAfter;
        var afterNormal = EnemyPass(enemy).SessionAfter;

        var result = EnemyPass(afterNormal);

        Assert.True(result.Accepted);
        Assert.True(result.SessionAfter.State.IsTerminal);
        Assert.Equal(BattleEndReason.TurnLimit, result.SessionAfter.State.EndReason);
        Assert.Equal(
            17,
            result.SessionAfter.State.AuthoritativeRuntime!.CounterattackState.GaugeUnits);
        Assert.Equal(
            "plan_next_intents",
            result.OrderedFacts.OfType<EnemyTurnBoundaryStageFact>().Last().StageId);
        Assert.IsType<BattleEndedFact>(result.OrderedFacts[^1]);
    }

    [Fact]
    public void StaleRuntimePlacementIsAnExactNoOp()
    {
        var initial = Start(Runtime(Board()));
        var enemy = EndTurn(initial).SessionAfter;
        var stale = RuntimePlacementCommand(
            enemy,
            C(7, 7),
            "stone.stale");
        var boundary = EnemyPass(enemy);

        var result = HeadlessBattleStateMachine.Execute(
            boundary.SessionAfter,
            stale);

        AssertRejectedExactNoOp(boundary.SessionAfter, result, "stale_state");
    }

    [Fact]
    public void AuthoritativeV2RejectsFacilityBuildAsAnExactNoOp()
    {
        var initial = Start(Runtime(Board()));
        var result = HeadlessBattleStateMachine.Execute(
            initial,
            new AuthorizedFacilityBuildCommand(
                initial.State.Checksum,
                initial.CommandLog.CurrentChecksum,
                C(4, 4),
                "facility.test",
                "facility.instance"));

        AssertRejectedExactNoOp(initial, result, "unsupported_command");
    }

    [Fact]
    public void ReversedInitialEnumerationHasTheSameV2ProjectionAndChecksum()
    {
        var forward = SnapshotWithReversibleInputs(reverse: false);
        var reversed = SnapshotWithReversibleInputs(reverse: true);

        Assert.Equal(forward.CanonicalText, reversed.CanonicalText);
        Assert.Equal(forward.Checksum, reversed.Checksum);
        var forwardSession = HeadlessBattleStateMachine.Start(forward, Metadata());
        var reversedSession = HeadlessBattleStateMachine.Start(reversed, Metadata());
        Assert.Equal(
            BattleState.AuthoritativeEncodingVersion,
            forwardSession.State.StateProjectionId);
        Assert.Equal(
            forwardSession.State.CanonicalText,
            reversedSession.State.CanonicalText);
        Assert.Equal(
            forwardSession.State.Checksum,
            reversedSession.State.Checksum);
    }

    private static BattleAuthoritativeInitialSnapshot SnapshotWithReversibleInputs(
        bool reverse)
    {
        var board = Board(
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.White, 6, 6));
        var canonical = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                $"stone.{index + 1}",
                stone,
                "standard",
                index + 1L,
                []))
            .ToArray();
        var stones = StoneRuntimeState.Create(
            board,
            reverse ? canonical.Reverse() : canonical,
            3);
        var effects = new[]
        {
            new TemporaryLibertyEffect(
                "effect.1",
                1,
                StoneColor.Black,
                "stone.1",
                "test",
                1,
                2),
            new TemporaryLibertyEffect(
                "effect.2",
                1,
                StoneColor.White,
                "stone.2",
                "test",
                2,
                2),
        };
        var temporary = TemporaryLibertyState.Create(
            stones,
            reverse ? effects.Reverse() : effects,
            3);
        var triggers = new[]
        {
            new CaptureBenefitTrigger(
                CaptureBenefitSource.StandardAccounting("source.a", 1),
                "trigger.a",
                ["event", "a"],
                [new GainSoulCaptureBenefitOperation(1)],
                null),
            new CaptureBenefitTrigger(
                CaptureBenefitSource.SourceOrArmedEffect("source.b", 2),
                "trigger.b",
                ["event", "b"],
                [new ReserveDrawCaptureBenefitOperation(1)],
                null),
        };
        return Snapshot(
            stones,
            temporary,
            triggerPlan: CaptureBenefitTriggerPlan.Create(
                reverse ? triggers.Reverse() : triggers));
    }

    private static HeadlessBattleSession Start(
        StoneRuntimeState stones,
        TemporaryLibertyState? temporary = null,
        CounterattackBoundaryState? counterattack = null,
        int playerTurnIndex = 1,
        CaptureBenefitTriggerPlan? triggerPlan = null,
        ClosedWindowResourceState? resources = null,
        FacilityState? facilities = null)
    {
        var snapshot = Snapshot(
            stones,
            temporary,
            counterattack,
            playerTurnIndex,
            triggerPlan,
            resources,
            facilities);
        return HeadlessBattleStateMachine.Start(snapshot, Metadata());
    }

    private static BattleAuthoritativeInitialSnapshot Snapshot(
        StoneRuntimeState stones,
        TemporaryLibertyState? temporary = null,
        CounterattackBoundaryState? counterattack = null,
        int playerTurnIndex = 1,
        CaptureBenefitTriggerPlan? triggerPlan = null,
        ClosedWindowResourceState? resources = null,
        FacilityState? facilities = null)
    {
        var policy = RuntimePolicy();
        var counterPolicy = CounterPolicy();
        return BattleAuthoritativeInitialSnapshot.Create(
            stones,
            temporary ?? TemporaryLibertyState.Create(stones, [], 1),
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(stones.SourceBoard),
            facilities ?? FacilityState.Create(stones.SourceBoard, [], 1),
            resources ?? ClosedWindowResourceState.Empty([]),
            triggerPlan ?? CaptureBenefitTriggerPlan.Create([]),
            counterattack ?? CounterattackBoundaryState.Create(0, false, 0, counterPolicy),
            counterPolicy,
            policy,
            playerTurnIndex);
    }

    private static CounterattackBoundaryState Counterattack(
        int gaugeUnits,
        bool pending) =>
        CounterattackBoundaryState.Create(
            gaugeUnits,
            pending,
            0,
            CounterPolicy());

    private static CounterattackBoundaryPolicy CounterPolicy() =>
        new(200, 12, 3, 30);

    private static BattleRuntimePolicy RuntimePolicy() =>
        new(
            20,
            FacilityRuntimePolicy.Create(
                5,
                [new FacilityCapacityBand(1, 49, 1)],
                3,
                [new KeyValuePair<string, int>("default", 1)]));

    private static ReplayMetadata Metadata() =>
        ReplayMetadata.Create("v0.2.10", ContentHash, 42);

    private static BattleCommandResult EndTurn(HeadlessBattleSession session) =>
        HeadlessBattleStateMachine.Execute(
            session,
            new EndPlayerTurnCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));

    private static BattleCommandResult EnemyPass(HeadlessBattleSession session) =>
        HeadlessBattleStateMachine.Execute(
            session,
            new ResolveEnemyPassCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));

    private static BattleCommandResult Place(
        HeadlessBattleSession session,
        int x,
        int y,
        string instanceId) =>
        HeadlessBattleStateMachine.Execute(
            session,
            RuntimePlacementCommand(session, C(x, y), instanceId));

    private static AuthorizedRuntimeStonePlacementCommand RuntimePlacementCommand(
        HeadlessBattleSession session,
        CanonicalPoint point,
        string instanceId) =>
        new(
            session.State.Checksum,
            session.CommandLog.CurrentChecksum,
            StoneColor.White,
            point,
            PlacementAccessMode.Normal,
            instanceId,
            "standard",
            []);

    private static CaptureBenefitTriggerPlanEntry CapturedStoneEntry(
        StoneRuntimeInstance stone) => new(
        new CaptureBenefitTrigger(
            CaptureBenefitSource.CapturedStoneSelf(stone.InstanceId),
            $"captured.{stone.InstanceId}",
            ["captured", stone.InstanceId],
            [new GainSoulCaptureBenefitOperation(2)],
            null),
        CaptureBenefitTriggerCondition.CapturedSourceStone);

    private static void AssertPlacementCaptureBatch(
        BattleCommandResult result,
        string placedStoneInstanceId,
        StoneRuntimeInstance expectedCapturedStone)
    {
        var started = Assert.Single(result.OrderedFacts.OfType<CaptureBatchStartedFact>());
        var batch = started.CaptureBatch;
        Assert.Equal(
            $"enemy_placement_capture_{placedStoneInstanceId}",
            batch.BatchId);
        Assert.Equal("stone_placement", batch.ReasonId);
        Assert.Equal(CaptureBoundary.PlacementResolution, batch.Boundary);
        Assert.Null(batch.BoundaryEnemyTurnIndex);
        Assert.Equal(CapturingWindow.ClosedPlayerWindow, batch.CapturingWindow);
        Assert.Same(expectedCapturedStone, Assert.Single(batch.CapturedStoneInstances));
        Assert.False(batch.ContainsKing);
        Assert.False(Assert.Single(
            result.OrderedFacts.OfType<CaptureBatchResolvedFact>()).BenefitsSuppressed);
    }

    private static void AssertTerminalPlacementCaptureSuppressed(
        BattleCommandResult result)
    {
        var started = Assert.Single(result.OrderedFacts.OfType<CaptureBatchStartedFact>());
        Assert.Equal(CaptureBoundary.PlacementResolution, started.CaptureBatch.Boundary);
        Assert.Equal(CapturingWindow.ClosedPlayerWindow, started.CaptureBatch.CapturingWindow);
        Assert.Equal("stone_placement", started.CaptureBatch.ReasonId);
        Assert.True(started.CaptureBatch.ContainsKing);
        Assert.Equal(
            "terminal_king_capture",
            Assert.Single(result.OrderedFacts.OfType<CaptureBenefitSuppressedFact>())
                .ReasonId);
        Assert.True(Assert.Single(
            result.OrderedFacts.OfType<CaptureBatchResolvedFact>()).BenefitsSuppressed);
        Assert.Empty(result.OrderedFacts.OfType<ICaptureBenefitAppliedFact>());
    }

    private static void AssertRejectedExactNoOp(
        HeadlessBattleSession expected,
        BattleCommandResult result,
        string reason)
    {
        Assert.False(result.Accepted);
        Assert.Equal(reason, result.ReasonId);
        Assert.Same(expected, result.SessionBefore);
        Assert.Same(expected, result.SessionAfter);
        Assert.Equal(expected.State.Checksum, result.StateChecksum);
        Assert.Equal(expected.CommandLog.CurrentChecksum, result.LogChecksum);
        Assert.IsType<CommandRejectedFact>(Assert.Single(result.OrderedFacts));
    }

    private static StoneRuntimeState Runtime(BoardState board)
    {
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                $"stone.{index + 1}",
                stone,
                stone.IsKing ? "king" : "standard",
                index + 1L,
                []))
            .ToArray();
        return StoneRuntimeState.Create(board, instances, instances.Length + 1L);
    }

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
}
