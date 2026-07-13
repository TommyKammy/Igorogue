using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Content;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Enemies;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

public sealed class BanditEnemyTurnStateMachineTests
{
    private const string ContentHash =
        "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private const string AlternateContentHash =
        "sha256:abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";

    private static readonly BoardGeometry Geometry =
        BoardGeometry.Create(BoardGeometry.AcceptedSize);

    private static readonly EnemyContentDefinition Bandit = CreateBandit();

    [Fact]
    public void StartPlansF0901BeforeTheFirstPlayerTurnAndPublishesThePlan()
    {
        var result = Start(StandardBoard());

        var state = result.Session.State;
        Assert.Equal(BattlePhase.PlayerAction, state.BattleState.Phase);
        Assert.Equal("advance_toward_black_king", state.NormalPlan!.IntentId);
        Assert.Equal(C(6, 4), state.NormalPlan.PrimaryPoint);
        Assert.Equal(
            state.BattleState.Checksum,
            state.NormalPlan.PlannedFromStateChecksum);
        Assert.Equal(
            state.NormalPlan.Checksum,
            Assert.Single(result.OrderedFacts.OfType<EnemyIntentPlannedFact>())
                .PlannedIntent.Checksum);
        Assert.Null(state.BonusPlan);
        Assert.Empty(result.Session.CommandLog.Entries);
        Assert.Empty(state.BattleSession.CommandLog.Entries);
        Assert.Equal(7, state.RuntimeState.StoneRuntimeState.NextCreatedSequence);
        Assert.Equal(6, state.RuntimeState.UsedStoneInstanceIds.Count);
    }

    [Fact]
    public void GeneratedProductionBanditContentPlansAndExecutesF0901()
    {
        var root = FindRepositoryRoot();
        var catalog = new CoreDuelContentCatalogLoader().Load(Path.Combine(
            root.FullName,
            "build",
            "generated_content",
            "content_manifest.json"));
        var metadata = ReplayMetadata.Create("v0.2.10", catalog.ContentHash, 42);
        var started = BanditEnemyTurnStateMachine.Start(
            Snapshot(Runtime(StandardBoard())),
            catalog.Bandit,
            metadata);

        Assert.Equal(C(6, 4), started.Session.State.NormalPlan!.PrimaryPoint);
        var enemy = BanditEnemyTurnStateMachine.Execute(
            started.Session,
            new EndPlayerTurnCommand(
                started.Session.State.Checksum,
                started.Session.CommandLog.CurrentChecksum));
        Assert.True(enemy.Accepted);
        var resolved = Resolve(enemy.SessionAfter);

        Assert.True(resolved.Accepted);
        Assert.NotNull(
            resolved.SessionAfter.State.BattleState.Board.StoneAt(C(6, 4)));
        Assert.Equal(catalog.ContentHash, resolved.SessionAfter.State.ContentHash);
    }

    [Fact]
    public void MandatoryPreviewDoesNotReplaceTheDisplayedPlayerTurnPlan()
    {
        var original = Start(StandardBoard()).Session;
        var displayed = original.State.NormalPlan!;
        var lethalInner = HeadlessBattleStateMachine.Start(
            Snapshot(Runtime(LethalBoard())),
            Metadata());
        var reboundState = BanditEnemyTurnState.Create(
            ContentHash,
            Bandit,
            lethalInner,
            displayed,
            bonusPlan: null);
        var rebound = new BanditEnemyTurnSession(
            reboundState,
            OrderedCommandLog.Create(Metadata()));
        var checksumBefore = rebound.State.Checksum;
        var logBefore = rebound.CommandLog.CurrentChecksum;

        var preview = BanditEnemyTurnStateMachine.PreviewMandatoryOverride(rebound);

        Assert.Equal(BanditMandatoryOverrideKind.Lethal, preview.Kind);
        Assert.Equal(C(2, 3), preview.Point);
        Assert.Equal(
            BanditExecutionReason.MandatoryLethalOverride,
            preview.Decision!.Reason);
        Assert.Same(displayed, rebound.State.NormalPlan);
        Assert.Equal(checksumBefore, rebound.State.Checksum);
        Assert.Equal(logBefore, rebound.CommandLog.CurrentChecksum);
    }

    [Fact]
    public void NonterminalActionUsesTheStoredPlanThenPlansTheNextWindow()
    {
        var started = Start(StandardBoard()).Session;
        var enemy = EndTurn(started).SessionAfter;

        var result = Resolve(enemy);

        Assert.True(result.Accepted);
        Assert.Equal(BattlePhase.PlayerAction, result.SessionAfter.State.BattleState.Phase);
        Assert.Equal(2, result.SessionAfter.State.BattleState.PlayerTurnIndex);
        Assert.NotNull(result.SessionAfter.State.NormalPlan);
        Assert.Equal(
            result.SessionAfter.State.BattleState.Checksum,
            result.SessionAfter.State.NormalPlan!.PlannedFromStateChecksum);
        Assert.Null(result.SessionAfter.State.BonusPlan);
        Assert.Equal(2, result.SessionAfter.CommandLog.Entries.Count);
        Assert.Equal(2, result.SessionAfter.State.BattleSession.CommandLog.Entries.Count);
        Assert.Equal(
            "battle.resolve_bandit_enemy_action",
            result.SessionAfter.CommandLog.Entries[^1].CommandType);
        Assert.Equal(
            "battle.authorized_runtime_stone_placement",
            result.SessionAfter.State.BattleSession.CommandLog.Entries[^1].CommandType);
        Assert.Equal(
            C(6, 4),
            result.SessionAfter.State.BattleState.Board.OccupiedStones
                .Single(stone => stone.Point.Equals(C(6, 4))).Point);

        var facts = result.OrderedFacts.ToArray();
        var resolvedIndex = Array.FindIndex(
            facts,
            fact => fact is EnemyActionResolvedFact);
        var boundaryIndex = Array.FindIndex(
            facts,
            fact => fact is EnemyTurnBoundaryStageFact stage &&
                stage.Stage == EnemyTurnBoundaryStage.ConsumeCurrentPendingAndReprimeOverflow);
        var nextPlanIndex = Array.FindLastIndex(
            facts,
            fact => fact is EnemyIntentPlannedFact);
        Assert.InRange(resolvedIndex, 1, boundaryIndex - 1);
        Assert.True(nextPlanIndex > boundaryIndex);
        Assert.IsType<EnemyIntentPlannedFact>(facts[^1]);
    }

    [Fact]
    public void BonusPlanExistsOnlyForConfirmedPendingAndSurvivesTheNormalAction()
    {
        var withoutPending = Start(StandardBoard()).Session;
        var withPendingStart = Start(
            StandardBoard(),
            counterattack: Counterattack(gaugeUnits: 10, pending: true));
        var withPending = withPendingStart.Session;

        Assert.Null(withoutPending.State.BonusPlan);
        Assert.NotNull(withPending.State.BonusPlan);
        Assert.Equal(2, withPendingStart.OrderedFacts.OfType<EnemyIntentPlannedFact>().Count());

        var enemy = EndTurn(withPending).SessionAfter;
        var storedBonus = enemy.State.BonusPlan;
        var normal = Resolve(enemy);

        Assert.True(normal.Accepted);
        Assert.Equal(BattlePhase.EnemyAction, normal.SessionAfter.State.BattleState.Phase);
        Assert.Equal(
            EnemyActionStage.CounterattackAction,
            normal.SessionAfter.State.RuntimeState.EnemyActionStage);
        Assert.Null(normal.SessionAfter.State.NormalPlan);
        Assert.Same(storedBonus, normal.SessionAfter.State.BonusPlan);
        Assert.Empty(normal.OrderedFacts.OfType<EnemyIntentPlannedFact>());
        var resolved = Assert.Single(normal.OrderedFacts.OfType<EnemyActionResolvedFact>());
        Assert.Equal(1, resolved.ActionIndex);
        Assert.False(resolved.IsCounterattackAction);

        var bonus = Resolve(normal.SessionAfter);

        Assert.True(bonus.Accepted);
        Assert.Equal(BattlePhase.PlayerAction, bonus.SessionAfter.State.BattleState.Phase);
        Assert.Equal(2, bonus.SessionAfter.State.BattleState.PlayerTurnIndex);
        var bonusResolved = Assert.Single(
            bonus.OrderedFacts.OfType<EnemyActionResolvedFact>());
        Assert.Equal(2, bonusResolved.ActionIndex);
        Assert.True(bonusResolved.IsCounterattackAction);
        Assert.Equal("planned_target", bonusResolved.ReasonId);
        Assert.Equal(C(6, 3), bonusResolved.ExecutedPoint);
        Assert.NotNull(bonus.SessionAfter.State.NormalPlan);
        Assert.Null(bonus.SessionAfter.State.BonusPlan);
    }

    [Fact]
    public void TerminalNormalActionSuppressesTheStoredBonusAction()
    {
        var started = Start(
            LethalBoard(),
            counterattack: Counterattack(gaugeUnits: 10, pending: true));
        Assert.NotNull(started.Session.State.NormalPlan);
        Assert.NotNull(started.Session.State.BonusPlan);
        var enemy = EndTurn(started.Session).SessionAfter;

        var result = Resolve(enemy);

        Assert.True(result.Accepted);
        Assert.True(result.SessionAfter.State.IsTerminal);
        Assert.Equal(
            BattleEndReason.BlackKingCaptured,
            result.SessionAfter.State.BattleState.EndReason);
        Assert.Null(result.SessionAfter.State.NormalPlan);
        Assert.Null(result.SessionAfter.State.BonusPlan);
        Assert.Empty(result.OrderedFacts.OfType<EnemyIntentPlannedFact>());
        var resolved = Assert.Single(result.OrderedFacts.OfType<EnemyActionResolvedFact>());
        Assert.Equal(1, resolved.ActionIndex);
        Assert.False(resolved.IsCounterattackAction);
        Assert.DoesNotContain(
            result.OrderedFacts,
            fact => fact is EnemyTurnBoundaryStageFact stage &&
                stage.Stage == EnemyTurnBoundaryStage.EnemyCounterattackAction);
    }

    [Fact]
    public void TerminalBonusClearsEveryPlanAndSuppressesNextPlanning()
    {
        var metadata = Metadata();
        var inner = HeadlessBattleStateMachine.Start(
            Snapshot(
                Runtime(LethalBoard()),
                counterattack: Counterattack(gaugeUnits: 10, pending: true)),
            metadata);
        inner = InnerEndTurn(inner).SessionAfter;
        inner = InnerPass(inner).SessionAfter;
        Assert.Equal(
            EnemyActionStage.CounterattackAction,
            inner.State.AuthoritativeRuntime!.EnemyActionStage);
        var bonus = Plan(inner);
        var state = BanditEnemyTurnState.Create(
            ContentHash,
            Bandit,
            inner,
            normalPlan: null,
            bonus);
        var session = new BanditEnemyTurnSession(
            state,
            OrderedCommandLog.Create(metadata));

        var result = Resolve(session);

        Assert.True(result.Accepted);
        Assert.True(result.SessionAfter.State.IsTerminal);
        Assert.Equal(
            BattleEndReason.BlackKingCaptured,
            result.SessionAfter.State.BattleState.EndReason);
        Assert.Null(result.SessionAfter.State.NormalPlan);
        Assert.Null(result.SessionAfter.State.BonusPlan);
        Assert.Empty(result.OrderedFacts.OfType<EnemyIntentPlannedFact>());
        var resolved = Assert.Single(result.OrderedFacts.OfType<EnemyActionResolvedFact>());
        Assert.Equal(2, resolved.ActionIndex);
        Assert.True(resolved.IsCounterattackAction);
        var facts = result.OrderedFacts.ToArray();
        Assert.True(
            Array.FindIndex(facts, fact => fact is EnemyActionResolvedFact) <
            Array.FindIndex(facts, fact => fact is BattleEndedFact));
        Assert.DoesNotContain(
            facts,
            fact => fact is EnemyTurnBoundaryStageFact stage &&
                stage.Stage >= EnemyTurnBoundaryStage.ConsumeCurrentPendingAndReprimeOverflow);
    }

    [Fact]
    public void MissingTargetRetargetsBeforeStartedAndMissingIntentFallsBack()
    {
        var retargetInner = HeadlessBattleStateMachine.Start(
            Snapshot(Runtime(NonKingCaptureBoard())),
            Metadata());
        var missingCapturePlan = PlannedEnemyIntent.Create(
            EnemyIntentKind.CaptureNonKing,
            new EnemyTargetReference(StoneColor.Black, C(1, 1)),
            C(1, 2),
            [],
            retargetable: true,
            retargetInner.State.Checksum);
        var retarget = SessionWithNormalPlan(retargetInner, missingCapturePlan);

        var retargetResult = Resolve(EndTurn(retarget).SessionAfter);

        Assert.True(retargetResult.Accepted);
        var retargetFacts = retargetResult.OrderedFacts.ToArray();
        Assert.IsType<EnemyIntentRetargetedFact>(retargetFacts[0]);
        Assert.IsType<EnemyActionStartedFact>(retargetFacts[1]);
        Assert.Equal(
            "same_intent_retarget",
            Assert.Single(retargetResult.OrderedFacts.OfType<EnemyActionResolvedFact>()).ReasonId);
        Assert.Null(retargetResult.SessionAfter.State.BattleState.Board.StoneAt(C(4, 4)));

        var fallbackInner = HeadlessBattleStateMachine.Start(
            Snapshot(Runtime(StandardBoard())),
            Metadata());
        var unavailablePressure = PlannedEnemyIntent.Create(
            EnemyIntentKind.PressureBlackKing,
            new EnemyTargetReference(StoneColor.Black, C(2, 2)),
            C(2, 4),
            [],
            retargetable: true,
            fallbackInner.State.Checksum);
        var fallback = SessionWithNormalPlan(fallbackInner, unavailablePressure);

        var fallbackResult = Resolve(EndTurn(fallback).SessionAfter);

        Assert.True(fallbackResult.Accepted);
        var fallbackFact = Assert.Single(
            fallbackResult.OrderedFacts.OfType<EnemyIntentRetargetedFact>());
        Assert.Equal("fallback", fallbackFact.ReasonId);
        Assert.Equal(1, fallbackFact.FallbackDepth);
        Assert.Equal(
            "advance_toward_black_king",
            Assert.Single(fallbackResult.OrderedFacts.OfType<EnemyActionResolvedFact>())
                .ExecutedIntentId);
    }

    [Fact]
    public void ExhaustedBoardPassesAndStillPlansTheNextPass()
    {
        var started = Start(FullBoard()).Session;
        Assert.True(started.State.NormalPlan!.IsPass);
        var enemy = EndTurn(started).SessionAfter;

        var result = Resolve(enemy);

        Assert.True(result.Accepted);
        Assert.Single(result.OrderedFacts.OfType<EnemyPassedFact>());
        var resolved = Assert.Single(result.OrderedFacts.OfType<EnemyActionResolvedFact>());
        Assert.Equal("pass", resolved.ExecutedIntentId);
        Assert.Equal("pass", resolved.ReasonId);
        Assert.True(result.SessionAfter.State.NormalPlan!.IsPass);
        Assert.IsType<EnemyIntentPlannedFact>(result.OrderedFacts[^1]);
    }

    [Fact]
    public void StaleOuterCommandsPreserveBothLogsAndEveryStateReference()
    {
        var started = Start(StandardBoard()).Session;
        var originalEnd = EndCommand(started);
        var accepted = BanditEnemyTurnStateMachine.Execute(started, originalEnd);
        Assert.True(accepted.Accepted);
        var current = accepted.SessionAfter;

        var staleState = BanditEnemyTurnStateMachine.Execute(current, originalEnd);
        AssertRejectedExactNoOp(current, staleState, "stale_state");

        var staleLog = BanditEnemyTurnStateMachine.Execute(
            current,
            new ResolveBanditEnemyActionCommand(
                current.State.Checksum,
                started.CommandLog.CurrentChecksum));
        AssertRejectedExactNoOp(current, staleLog, "stale_session");
    }

    [Fact]
    public void ReversedInputsAreDeterministicAndContentHashChangesTheSidecarIdentity()
    {
        var forward = Start(StandardBoard(), reverseRuntime: false).Session;
        var reversed = Start(StandardBoard(), reverseRuntime: true).Session;

        Assert.Equal(forward.State.CanonicalText, reversed.State.CanonicalText);
        Assert.Equal(forward.State.Checksum, reversed.State.Checksum);
        Assert.Equal(
            forward.State.NormalPlan!.Checksum,
            reversed.State.NormalPlan!.Checksum);

        var forwardAfter = Resolve(EndTurn(forward).SessionAfter).SessionAfter;
        var reversedAfter = Resolve(EndTurn(reversed).SessionAfter).SessionAfter;
        Assert.Equal(
            forwardAfter.State.CanonicalText,
            reversedAfter.State.CanonicalText);
        Assert.Equal(forwardAfter.State.Checksum, reversedAfter.State.Checksum);
        Assert.Equal(
            forwardAfter.CommandLog.CurrentChecksum,
            reversedAfter.CommandLog.CurrentChecksum);
        Assert.Equal(
            forwardAfter.State.BattleSession.CommandLog.CurrentChecksum,
            reversedAfter.State.BattleSession.CommandLog.CurrentChecksum);

        var alternate = BanditEnemyTurnStateMachine.Start(
            Snapshot(Runtime(StandardBoard())),
            Bandit,
            Metadata(AlternateContentHash)).Session;
        Assert.Equal(
            forward.State.BattleState.CanonicalText,
            alternate.State.BattleState.CanonicalText);
        Assert.NotEqual(forward.State.Checksum, alternate.State.Checksum);
        Assert.NotEqual(
            forward.State.BattleSession.CommandLog.CurrentChecksum,
            alternate.State.BattleSession.CommandLog.CurrentChecksum);
    }

    private static BanditEnemyTurnStartResult Start(
        BoardState board,
        CounterattackBoundaryState? counterattack = null,
        bool reverseRuntime = false) =>
        BanditEnemyTurnStateMachine.Start(
            Snapshot(Runtime(board, reverseRuntime), counterattack: counterattack),
            Bandit,
            Metadata());

    private static BanditEnemyTurnResult EndTurn(BanditEnemyTurnSession session) =>
        BanditEnemyTurnStateMachine.Execute(session, EndCommand(session));

    private static EndPlayerTurnCommand EndCommand(BanditEnemyTurnSession session) =>
        new(session.State.Checksum, session.CommandLog.CurrentChecksum);

    private static BanditEnemyTurnResult Resolve(BanditEnemyTurnSession session) =>
        BanditEnemyTurnStateMachine.Execute(
            session,
            new ResolveBanditEnemyActionCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));

    private static BattleCommandResult InnerEndTurn(HeadlessBattleSession session) =>
        HeadlessBattleStateMachine.Execute(
            session,
            new EndPlayerTurnCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));

    private static BattleCommandResult InnerPass(HeadlessBattleSession session) =>
        HeadlessBattleStateMachine.Execute(
            session,
            new ResolveEnemyPassCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));

    private static PlannedEnemyIntent Plan(HeadlessBattleSession session)
    {
        var state = session.State;
        var runtime = state.AuthoritativeRuntime!;
        return BanditIntentPlanner.Plan(
            new BanditPlanningContext(
                runtime.StoneRuntimeState,
                runtime.TemporaryLibertyState,
                runtime.ContinuousLibertySnapshot,
                state.RepetitionHistory,
                state.FacilityRuntimeAnalysis),
            Bandit,
            state.Checksum,
            new StoneRuntimePlacementDescriptor(
                "test.bandit.probe",
                "standard",
                []));
    }

    private static BanditEnemyTurnSession SessionWithNormalPlan(
        HeadlessBattleSession inner,
        PlannedEnemyIntent normalPlan)
    {
        var state = BanditEnemyTurnState.Create(
            ContentHash,
            Bandit,
            inner,
            normalPlan,
            bonusPlan: null);
        return new BanditEnemyTurnSession(
            state,
            OrderedCommandLog.Create(Metadata()));
    }

    private static void AssertRejectedExactNoOp(
        BanditEnemyTurnSession expected,
        BanditEnemyTurnResult result,
        string reasonId)
    {
        Assert.False(result.Accepted);
        Assert.Equal(reasonId, result.ReasonId);
        Assert.Same(expected, result.SessionBefore);
        Assert.Same(expected, result.SessionAfter);
        Assert.Same(expected.State, result.SessionAfter.State);
        Assert.Same(expected.State.BattleSession, result.SessionAfter.State.BattleSession);
        Assert.Same(expected.CommandLog, result.SessionAfter.CommandLog);
        Assert.Equal(expected.State.Checksum, result.StateChecksum);
        Assert.Equal(expected.CommandLog.CurrentChecksum, result.LogChecksum);
        Assert.IsType<CommandRejectedFact>(Assert.Single(result.OrderedFacts));
    }

    private static BattleAuthoritativeInitialSnapshot Snapshot(
        StoneRuntimeState stones,
        CounterattackBoundaryState? counterattack = null)
    {
        var counterPolicy = CounterPolicy();
        return BattleAuthoritativeInitialSnapshot.Create(
            stones,
            TemporaryLibertyState.Create(stones, [], 1),
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(stones.SourceBoard),
            FacilityState.Create(stones.SourceBoard, [], 1),
            ClosedWindowResourceState.Empty([]),
            CaptureBenefitTriggerPlan.Create([]),
            counterattack ?? CounterattackBoundaryState.Create(0, false, 0, counterPolicy),
            counterPolicy,
            RuntimePolicy(),
            playerTurnIndex: 1);
    }

    private static CounterattackBoundaryState Counterattack(
        int gaugeUnits,
        bool pending) =>
        CounterattackBoundaryState.Create(
            gaugeUnits,
            pending,
            sacrificeStoneRemainder: 0,
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

    private static ReplayMetadata Metadata(string contentHash = ContentHash) =>
        ReplayMetadata.Create("v0.2.10", contentHash, 42);

    private static StoneRuntimeState Runtime(BoardState board, bool reverse = false)
    {
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                $"stone.{index + 1}",
                stone,
                stone.IsKing ? "king" : "standard",
                index + 1L,
                []))
            .ToArray();
        return StoneRuntimeState.Create(
            board,
            reverse ? instances.Reverse() : instances,
            instances.Length + 1L);
    }

    private static BoardState StandardBoard() => Board(
        Stone(StoneColor.Black, 2, 2, isKing: true),
        Stone(StoneColor.Black, 2, 3),
        Stone(StoneColor.Black, 3, 2),
        Stone(StoneColor.White, 6, 6, isKing: true),
        Stone(StoneColor.White, 5, 6),
        Stone(StoneColor.White, 6, 5));

    private static BoardState LethalBoard() => Board(
        Stone(StoneColor.Black, 2, 2, isKing: true),
        Stone(StoneColor.White, 1, 2),
        Stone(StoneColor.White, 2, 1),
        Stone(StoneColor.White, 3, 2),
        Stone(StoneColor.White, 6, 6, isKing: true));

    private static BoardState NonKingCaptureBoard() => Board(
        Stone(StoneColor.Black, 2, 2, isKing: true),
        Stone(StoneColor.Black, 4, 4),
        Stone(StoneColor.White, 3, 4),
        Stone(StoneColor.White, 4, 3),
        Stone(StoneColor.White, 5, 4),
        Stone(StoneColor.White, 6, 6, isKing: true));

    private static BoardState FullBoard()
    {
        var stones = new List<BoardStone>(Geometry.PointCount);
        for (var y = 1; y <= BoardGeometry.AcceptedSize; y++)
        {
            for (var x = 1; x <= BoardGeometry.AcceptedSize; x++)
            {
                var color = (x + y) % 2 == 0
                    ? StoneColor.Black
                    : StoneColor.White;
                var isKing = (x, y) is (1, 1) or (6, 7);
                stones.Add(Stone(color, x, y, isKing));
            }
        }

        return Board(stones.ToArray());
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

    private static EnemyContentDefinition CreateBandit()
    {
        var captureBlackKing = EnemyIntentDefinition.Create(
            EnemyIntentKind.CaptureBlackKing,
            EnemyIntentKind.CaptureBlackKing,
            [
                EnemyPlacementMode.WhiteTerminal,
                EnemyPlacementMode.WhiteFrontline,
                EnemyPlacementMode.WhiteContact,
            ],
            EnemyScoreProfile.KingExecution,
            []);
        var defendWhiteKing = EnemyIntentDefinition.Create(
            EnemyIntentKind.DefendWhiteKing,
            EnemyIntentKind.DefendWhiteKing,
            [
                EnemyPlacementMode.WhiteFrontline,
                EnemyPlacementMode.WhiteContact,
                EnemyPlacementMode.WhiteTerminal,
            ],
            EnemyScoreProfile.KingDefense,
            [
                EnemyIntentKind.CaptureNonKing,
                EnemyIntentKind.PressureBlackKing,
                EnemyIntentKind.AdvanceTowardBlackKing,
            ]);
        var captureNonKing = EnemyIntentDefinition.Create(
            EnemyIntentKind.CaptureNonKing,
            EnemyIntentKind.CaptureNonKing,
            [
                EnemyPlacementMode.WhiteTerminal,
                EnemyPlacementMode.WhiteFrontline,
                EnemyPlacementMode.WhiteContact,
            ],
            EnemyScoreProfile.CaptureValue,
            [
                EnemyIntentKind.PressureBlackKing,
                EnemyIntentKind.AdvanceTowardBlackKing,
            ]);
        var pressureBlackKing = EnemyIntentDefinition.Create(
            EnemyIntentKind.PressureBlackKing,
            EnemyIntentKind.PressureBlackKing,
            [
                EnemyPlacementMode.WhiteFrontline,
                EnemyPlacementMode.WhiteContact,
            ],
            EnemyScoreProfile.KingPressure,
            [EnemyIntentKind.AdvanceTowardBlackKing]);
        var advanceTowardBlackKing = EnemyIntentDefinition.Create(
            EnemyIntentKind.AdvanceTowardBlackKing,
            EnemyIntentKind.AdvanceTowardBlackKing,
            [EnemyPlacementMode.WhiteFrontline],
            EnemyScoreProfile.KingAdvance,
            []);

        return EnemyContentDefinition.Create(
            "enemy_bandit",
            "FEAT-009",
            "1.0.0",
            new EnemyActionBudget(1, 1, 2),
            [
                EnemyPlacementMode.WhiteFrontline,
                EnemyPlacementMode.WhiteContact,
                EnemyPlacementMode.WhiteTerminal,
            ],
            new BanditParameters(2, 1),
            [
                EnemyIntentKind.CaptureBlackKing,
                EnemyIntentKind.DefendWhiteKing,
            ],
            [
                EnemyIntentKind.CaptureNonKing,
                EnemyIntentKind.PressureBlackKing,
                EnemyIntentKind.AdvanceTowardBlackKing,
            ],
            [
                EnemyIntentKind.CaptureNonKing,
                EnemyIntentKind.PressureBlackKing,
                EnemyIntentKind.AdvanceTowardBlackKing,
            ],
            [
                captureBlackKing,
                defendWhiteKing,
                captureNonKing,
                pressureBlackKing,
                advanceTowardBlackKing,
            ],
            EnemyTieBreak.CanonicalYThenX);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null &&
               !File.Exists(Path.Combine(current.FullName, "Igorogue.sln")))
        {
            current = current.Parent;
        }

        return current ??
            throw new DirectoryNotFoundException("Repository root not found.");
    }
}
