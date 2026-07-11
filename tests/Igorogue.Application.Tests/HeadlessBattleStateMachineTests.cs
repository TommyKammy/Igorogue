using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

public sealed class HeadlessBattleStateMachineTests
{
    private const string ContentHash =
        "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string OtherContentHash =
        "sha256:abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";

    private static readonly BoardGeometry Geometry =
        BoardGeometry.Create(BoardGeometry.AcceptedSize);

    [Fact]
    public void StartBindsExactSnapshotsAndProducesCanonicalChecksums()
    {
        var board = Board(
            Stone(StoneColor.Black, 7, 7, isKing: true),
            Stone(StoneColor.White, 1, 7, isKing: true));
        var facility = Facility("start-facility", StoneColor.Black, 4, 4, 1);
        var facilityState = FacilityState.Create(board, [facility], 2);
        var policy = RuntimePolicy(playerTurnLimit: 20);
        var metadata = Metadata(seed: -42);

        var session = HeadlessBattleStateMachine.Start(
            board,
            facilityState,
            policy,
            metadata);

        Assert.Same(board, session.State.Board);
        Assert.Same(facilityState, session.State.FacilityState);
        Assert.Same(policy, session.State.RuntimePolicy);
        Assert.Same(board, session.State.TerritoryAnalysis.SourceBoard);
        Assert.Same(
            facilityState,
            session.State.FacilityRuntimeAnalysis.FacilityState);
        Assert.Same(
            session.State.TerritoryAnalysis,
            session.State.FacilityRuntimeAnalysis.TerritoryAnalysis);
        Assert.Same(
            policy.FacilityPolicy,
            session.State.FacilityRuntimeAnalysis.Policy);
        Assert.Equal(
            StoneTopologyKey.FromBoard(board),
            session.State.RepetitionHistory.Current);
        Assert.Equal(1, session.State.RepetitionHistory.ObservationCount);
        Assert.Equal(-42, session.State.RngState.InitialSeed);
        Assert.Equal(1, session.State.PlayerTurnIndex);
        Assert.Equal(BattlePhase.PlayerAction, session.State.Phase);
        Assert.Equal(BattleOutcome.Ongoing, session.State.Outcome);
        Assert.Equal(BattleEndReason.None, session.State.EndReason);
        Assert.False(session.State.IsTerminal);
        Assert.Equal(
            DeterministicChecksum.Sha256Hex(session.State.CanonicalText),
            session.State.Checksum);
        Assert.Equal(session.State.CanonicalText, session.State.ToCanonicalText());
        Assert.Matches("^[0-9a-f]{64}$", session.State.Checksum);
        Assert.Same(metadata, session.CommandLog.Metadata);
        Assert.Empty(session.CommandLog.Entries);
        Assert.Matches("^[0-9a-f]{64}$", session.CommandLog.CurrentChecksum);
        var equivalentForeignBoard = Board(
            Stone(StoneColor.Black, 7, 7, isKing: true),
            Stone(StoneColor.White, 1, 7, isKing: true));
        var foreignFacilityState = FacilityState.Create(
            equivalentForeignBoard,
            [Facility("foreign", StoneColor.Black, 1, 1, 1)],
            2);

        Assert.Throws<ArgumentException>(() => HeadlessBattleStateMachine.Start(
            board,
            foreignFacilityState,
            RuntimePolicy(),
            Metadata()));
    }

    [Fact]
    public void AcceptedPlacementTramplesFacilityAndCommitsEverySnapshotInTypedOrder()
    {
        var board = Board();
        var facility = Facility("trampled", StoneColor.Black, 4, 4, 1);
        var session = Start(board, [facility]);

        var result = Place(session, StoneColor.Black, 4, 4);

        Assert.True(result.Accepted);
        Assert.Equal("accepted", result.ReasonId);
        Assert.Same(session, result.SessionBefore);
        Assert.NotSame(session, result.SessionAfter);
        Assert.Equal(
            new[]
            {
                typeof(StonePlacedFact),
                typeof(FacilityDestroyedFact),
                typeof(StoneTopologyRegisteredFact),
                typeof(KingCaptureEvaluatedFact),
                typeof(TerritoryEstablishedFact),
            },
            result.OrderedFacts.Select(fact => fact.GetType()));
        var destroyed = Assert.IsType<FacilityDestroyedFact>(result.OrderedFacts[1]);
        Assert.Same(facility, destroyed.Facility);
        Assert.Equal("stone_occupied", destroyed.ReasonId);
        var territory = Assert.IsType<TerritoryEstablishedFact>(result.OrderedFacts[^1]);
        Assert.Equal(StoneColor.Black, territory.SourceActor);
        Assert.Equal(48, territory.ChangedPoints.Count);
        Assert.Empty(result.SessionAfter.State.FacilityState.InstalledFacilities);
        Assert.Same(
            result.SessionAfter.State.Board,
            result.SessionAfter.State.FacilityState.SourceBoard);
        Assert.NotNull(result.SessionAfter.State.Board.StoneAt(C(4, 4)));
        Assert.Equal(2, result.SessionAfter.State.RepetitionHistory.ObservationCount);
        Assert.Equal(BattlePhase.PlayerAction, result.SessionAfter.State.Phase);
        Assert.Equal(1, result.SessionAfter.State.PlayerTurnIndex);
        Assert.Equal(result.SessionAfter.State.Checksum, result.StateChecksum);
        Assert.Equal(result.SessionAfter.CommandLog.CurrentChecksum, result.LogChecksum);
        var entry = Assert.Single(result.SessionAfter.CommandLog.Entries);
        Assert.Equal(0, entry.Sequence);
        Assert.Equal("battle.authorized_stone_placement", entry.CommandType);
        Assert.Equal(result.StateChecksum, entry.ResultChecksum);
        Assert.Equal(result.LogChecksum, entry.LogChecksum);
    }

    [Fact]
    public void TerritoryEstablishedPrecedesFacilityActivationAfterNonterminalCapture()
    {
        var board = Board(
            Stone(StoneColor.Black, 3, 4),
            Stone(StoneColor.Black, 2, 3),
            Stone(StoneColor.Black, 4, 3),
            Stone(StoneColor.White, 3, 3));
        var facility = Facility("reactivated", StoneColor.Black, 1, 1, 1);
        var session = Start(board, [facility]);
        Assert.False(
            session.State.FacilityRuntimeAnalysis.OperatingStateFor(facility).IsActive);

        var result = Place(session, StoneColor.Black, 3, 2);

        Assert.True(result.Accepted);
        Assert.Equal(
            new[]
            {
                typeof(StonePlacedFact),
                typeof(GroupCapturedFact),
                typeof(StoneTopologyRegisteredFact),
                typeof(KingCaptureEvaluatedFact),
                typeof(TerritoryEstablishedFact),
                typeof(FacilityActivatedFact),
            },
            result.OrderedFacts.Select(fact => fact.GetType()));
        var territory = Assert.IsType<TerritoryEstablishedFact>(result.OrderedFacts[4]);
        Assert.Equal(StoneColor.Black, territory.SourceActor);
        Assert.Contains(C(3, 3), territory.ChangedPoints);
        var activated = Assert.IsType<FacilityActivatedFact>(result.OrderedFacts[5]);
        Assert.Same(facility, activated.Facility);
        Assert.Equal("territory_control_restored", activated.ReasonId);
        Assert.True(
            result.SessionAfter.State.FacilityRuntimeAnalysis
                .OperatingStateFor(facility)
                .IsActive);
        Assert.Equal(BattleOutcome.Ongoing, result.SessionAfter.State.Outcome);
    }

    [Fact]
    public void OccupiedSuicideAndUnfulfilledTerminalPlacementsAreExactNoOps()
    {
        var occupiedSession = Start(Board(Stone(StoneColor.Black, 4, 4)));

        var occupied = Place(occupiedSession, StoneColor.Black, 4, 4);

        AssertRejectedNoOp(occupiedSession, occupied, "stone_occupied");
        var suicideSession = Start(Board(
            Stone(StoneColor.White, 2, 1),
            Stone(StoneColor.White, 1, 2),
            Stone(StoneColor.White, 3, 2),
            Stone(StoneColor.White, 2, 3)));

        var suicide = Place(suicideSession, StoneColor.Black, 2, 2);

        AssertRejectedNoOp(suicideSession, suicide, "suicide");
        var terminalSession = Start(Board());
        var noCapture = Place(
            terminalSession,
            StoneColor.Black,
            2,
            2,
            PlacementAccessMode.TerminalCapture);

        AssertRejectedNoOp(
            terminalSession,
            noCapture,
            "terminal_capture_required");
    }

    [Fact]
    public void RepetitionRecaptureIsAnExactNoOpAfterAcceptedHistory()
    {
        var initial = Start(KoBeforeCapture());
        var capture = Place(initial, StoneColor.Black, 3, 2);
        Assert.True(capture.Accepted);
        var enemyPhase = EndPlayerTurn(capture.SessionAfter);
        Assert.True(enemyPhase.Accepted);
        Assert.Equal(2, enemyPhase.SessionAfter.State.RepetitionHistory.ObservationCount);

        var recapture = Place(
            enemyPhase.SessionAfter,
            StoneColor.White,
            3,
            3,
            PlacementAccessMode.TerminalCapture);

        AssertRejectedNoOp(
            enemyPhase.SessionAfter,
            recapture,
            "stone_topology_repetition");
        Assert.Equal(2, recapture.SessionAfter.State.RepetitionHistory.ObservationCount);
    }

    [Fact]
    public void PhaseActorAndStaleCommandsAreRejectedWithoutMutation()
    {
        var initial = Start(Board());
        AssertRejectedNoOp(
            initial,
            HeadlessBattleStateMachine.Execute(
                initial,
                new ResolveEnemyPassCommand(
                    initial.State.Checksum,
                    initial.CommandLog.CurrentChecksum)),
            "wrong_phase");
        AssertRejectedNoOp(
            initial,
            Place(initial, StoneColor.White, 1, 1),
            "wrong_actor_for_phase");

        var staleCommand = new AuthorizedStonePlacementCommand(
            initial.State.Checksum,
            initial.CommandLog.CurrentChecksum,
            StoneColor.Black,
            C(1, 1),
            PlacementAccessMode.Normal);
        var accepted = Place(initial, StoneColor.Black, 2, 2);
        Assert.True(accepted.Accepted);
        AssertRejectedNoOp(
            accepted.SessionAfter,
            HeadlessBattleStateMachine.Execute(accepted.SessionAfter, staleCommand),
            "stale_state");

        var enemyPhase = EndPlayerTurn(accepted.SessionAfter);
        Assert.True(enemyPhase.Accepted);
        AssertRejectedNoOp(
            enemyPhase.SessionAfter,
            EndPlayerTurn(enemyPhase.SessionAfter),
            "wrong_phase");
        AssertRejectedNoOp(
            enemyPhase.SessionAfter,
            Place(enemyPhase.SessionAfter, StoneColor.Black, 3, 3),
            "wrong_actor_for_phase");
    }

    [Fact]
    public void CommandFromDifferentContentIdentityIsRejectedAsStaleSession()
    {
        var first = Start(Board(), seed: 7, contentHash: ContentHash);
        var second = Start(Board(), seed: 7, contentHash: OtherContentHash);
        Assert.Equal(first.State.Checksum, second.State.Checksum);
        Assert.NotEqual(
            first.CommandLog.CurrentChecksum,
            second.CommandLog.CurrentChecksum);
        var foreignCommand = new AuthorizedStonePlacementCommand(
            first.State.Checksum,
            first.CommandLog.CurrentChecksum,
            StoneColor.Black,
            C(4, 4),
            PlacementAccessMode.Normal);

        var result = HeadlessBattleStateMachine.Execute(second, foreignCommand);

        AssertRejectedNoOp(second, result, "stale_session");
    }

    [Fact]
    public void WhiteKingCaptureEndsInVictoryAndSuppressesPostTerminalBenefits()
    {
        var facility = Facility("terminal-trample", StoneColor.Black, 3, 2, 1);
        var session = Start(WhiteKingInAtari(), [facility]);
        Assert.False(
            session.State.FacilityRuntimeAnalysis.OperatingStateFor(facility).IsActive);

        var result = Place(session, StoneColor.Black, 3, 2);

        Assert.True(result.Accepted);
        Assert.Equal(
            new[]
            {
                typeof(StonePlacedFact),
                typeof(GroupCapturedFact),
                typeof(FacilityDestroyedFact),
                typeof(StoneTopologyRegisteredFact),
                typeof(KingCaptureEvaluatedFact),
                typeof(BattleEndedFact),
            },
            result.OrderedFacts.Select(fact => fact.GetType()));
        Assert.True(Assert.IsType<GroupCapturedFact>(result.OrderedFacts[1]).ContainsKing);
        var destroyed = Assert.IsType<FacilityDestroyedFact>(result.OrderedFacts[2]);
        Assert.Same(facility, destroyed.Facility);
        Assert.DoesNotContain(result.OrderedFacts, fact => fact is TerritoryEstablishedFact);
        Assert.DoesNotContain(
            result.OrderedFacts,
            fact => fact is FacilityActivatedFact or FacilityDisabledFact);
        Assert.Empty(result.SessionAfter.State.FacilityState.InstalledFacilities);
        var ended = Assert.IsType<BattleEndedFact>(result.OrderedFacts[^1]);
        Assert.Equal(BattleOutcome.PlayerVictory, ended.Outcome);
        Assert.Equal("white_king_captured", ended.ReasonId);
        Assert.Equal(BattlePhase.Ended, result.SessionAfter.State.Phase);
        Assert.Equal(BattleOutcome.PlayerVictory, result.SessionAfter.State.Outcome);
        Assert.Equal(BattleEndReason.WhiteKingCaptured, result.SessionAfter.State.EndReason);

        var postTerminal = HeadlessBattleStateMachine.Execute(
            result.SessionAfter,
            new EndPlayerTurnCommand(
                result.SessionAfter.State.Checksum,
                result.SessionAfter.CommandLog.CurrentChecksum));
        AssertRejectedNoOp(result.SessionAfter, postTerminal, "battle_terminal");
    }

    [Fact]
    public void BlackKingCaptureEndsInDefeatAndSuppressesPostTerminalBenefits()
    {
        var facility = Facility("white-would-activate", StoneColor.White, 1, 1, 1);
        var session = Start(BlackKingInAtari(), [facility]);
        Assert.False(
            session.State.FacilityRuntimeAnalysis.OperatingStateFor(facility).IsActive);
        var enemyPhase = EndPlayerTurn(session);
        Assert.True(enemyPhase.Accepted);

        var result = Place(enemyPhase.SessionAfter, StoneColor.White, 3, 2);

        Assert.True(result.Accepted);
        Assert.Equal(
            new[]
            {
                typeof(StonePlacedFact),
                typeof(GroupCapturedFact),
                typeof(StoneTopologyRegisteredFact),
                typeof(KingCaptureEvaluatedFact),
                typeof(BattleEndedFact),
            },
            result.OrderedFacts.Select(fact => fact.GetType()));
        Assert.True(Assert.IsType<GroupCapturedFact>(result.OrderedFacts[1]).ContainsKing);
        Assert.DoesNotContain(result.OrderedFacts, fact => fact is TerritoryEstablishedFact);
        Assert.DoesNotContain(result.OrderedFacts, fact => fact is FacilityFact);
        var ended = Assert.IsType<BattleEndedFact>(result.OrderedFacts[^1]);
        Assert.Equal(BattleOutcome.PlayerDefeat, ended.Outcome);
        Assert.Equal("black_king_captured", ended.ReasonId);
        Assert.Equal(BattlePhase.Ended, result.SessionAfter.State.Phase);
        Assert.Equal(BattleOutcome.PlayerDefeat, result.SessionAfter.State.Outcome);
        Assert.Equal(BattleEndReason.BlackKingCaptured, result.SessionAfter.State.EndReason);
    }

    [Fact]
    public void SecondEnemyPassReachesTurnLimitOnlyAfterEnemyBoundary()
    {
        var session = Start(Board(), playerTurnLimit: 2);
        var firstEnemyPhase = EndPlayerTurn(session).SessionAfter;
        var secondPlayerTurn = EnemyPass(firstEnemyPhase).SessionAfter;

        Assert.Equal(2, secondPlayerTurn.State.PlayerTurnIndex);
        Assert.Equal(BattlePhase.PlayerAction, secondPlayerTurn.State.Phase);
        Assert.Equal(BattleOutcome.Ongoing, secondPlayerTurn.State.Outcome);
        var secondEnemyPhase = EndPlayerTurn(secondPlayerTurn).SessionAfter;
        Assert.Equal(BattlePhase.EnemyAction, secondEnemyPhase.State.Phase);
        Assert.Equal(BattleOutcome.Ongoing, secondEnemyPhase.State.Outcome);

        var result = EnemyPass(secondEnemyPhase);

        Assert.True(result.Accepted);
        Assert.Equal(
            new[] { typeof(EnemyPassedFact), typeof(BattleEndedFact) },
            result.OrderedFacts.Select(fact => fact.GetType()));
        Assert.Equal(2, Assert.IsType<EnemyPassedFact>(result.OrderedFacts[0]).PlayerTurnIndex);
        Assert.Equal(BattlePhase.Ended, result.SessionAfter.State.Phase);
        Assert.Equal(BattleOutcome.PlayerDefeat, result.SessionAfter.State.Outcome);
        Assert.Equal(BattleEndReason.TurnLimit, result.SessionAfter.State.EndReason);
        Assert.Equal(2, result.SessionAfter.State.PlayerTurnIndex);
    }

    [Fact]
    public void SecondEnemyPlacementCommitsBeforeTurnLimitDefeat()
    {
        var session = Start(Board(), playerTurnLimit: 2);
        var secondPlayerTurn = EnemyPass(EndPlayerTurn(session).SessionAfter).SessionAfter;
        var secondEnemyPhase = EndPlayerTurn(secondPlayerTurn).SessionAfter;

        var result = Place(secondEnemyPhase, StoneColor.White, 7, 7);

        Assert.True(result.Accepted);
        Assert.Equal(
            new[]
            {
                typeof(StonePlacedFact),
                typeof(StoneTopologyRegisteredFact),
                typeof(KingCaptureEvaluatedFact),
                typeof(BattleEndedFact),
            },
            result.OrderedFacts.Select(fact => fact.GetType()));
        var placed = Assert.IsType<StonePlacedFact>(result.OrderedFacts[0]);
        Assert.Equal(C(7, 7), placed.Stone.Point);
        Assert.Same(
            placed.Stone,
            result.SessionAfter.State.Board.StoneAt(C(7, 7)));
        Assert.Equal(2, result.SessionAfter.State.RepetitionHistory.ObservationCount);
        Assert.Equal(BattleEndReason.TurnLimit, result.SessionAfter.State.EndReason);
        Assert.Equal(BattleOutcome.PlayerDefeat, result.SessionAfter.State.Outcome);
    }

    [Fact]
    public void ProductionTurnLimitEndsOnlyAfterTwentiethEnemyBoundary()
    {
        var session = Start(Board(), playerTurnLimit: 20);
        for (var completedTurn = 1; completedTurn < 20; completedTurn++)
        {
            var enemyPhase = EndPlayerTurn(session);
            Assert.True(enemyPhase.Accepted);
            var nextPlayerTurn = EnemyPass(enemyPhase.SessionAfter);
            Assert.True(nextPlayerTurn.Accepted);
            session = nextPlayerTurn.SessionAfter;
        }

        Assert.Equal(20, session.State.PlayerTurnIndex);
        Assert.Equal(BattlePhase.PlayerAction, session.State.Phase);
        Assert.Equal(BattleOutcome.Ongoing, session.State.Outcome);
        var finalEnemyPhase = EndPlayerTurn(session).SessionAfter;

        var result = EnemyPass(finalEnemyPhase);

        Assert.True(result.Accepted);
        Assert.Equal(BattleEndReason.TurnLimit, result.SessionAfter.State.EndReason);
        Assert.Equal(BattleOutcome.PlayerDefeat, result.SessionAfter.State.Outcome);
        Assert.Equal(20, result.SessionAfter.State.PlayerTurnIndex);
    }

    [Fact]
    public void KingCaptureOnTwentiethEnemyActionTakesPriorityOverTurnLimitReason()
    {
        var session = Start(BlackKingInAtari(), playerTurnLimit: 20);
        for (var completedTurn = 1; completedTurn < 20; completedTurn++)
        {
            session = EnemyPass(EndPlayerTurn(session).SessionAfter).SessionAfter;
        }

        var finalEnemyPhase = EndPlayerTurn(session).SessionAfter;
        var result = Place(finalEnemyPhase, StoneColor.White, 3, 2);

        Assert.True(result.Accepted);
        Assert.Equal(BattleOutcome.PlayerDefeat, result.SessionAfter.State.Outcome);
        Assert.Equal(
            BattleEndReason.BlackKingCaptured,
            result.SessionAfter.State.EndReason);
        Assert.Equal(
            "black_king_captured",
            Assert.IsType<BattleEndedFact>(result.OrderedFacts[^1]).ReasonId);
        Assert.DoesNotContain(
            result.OrderedFacts.OfType<BattleEndedFact>(),
            fact => fact.Reason == BattleEndReason.TurnLimit);
    }

    [Fact]
    public void SameInitialStateSeedAndCommandsProduceIdenticalStateAndLogChecksums()
    {
        var first = RunDeterministicScript(seed: 123456789);
        var second = RunDeterministicScript(seed: 123456789);

        Assert.Equal(first.Boundaries, second.Boundaries);
        Assert.Equal(
            first.FinalSession.State.CanonicalText,
            second.FinalSession.State.CanonicalText);
        Assert.Equal(
            first.FinalSession.State.Checksum,
            second.FinalSession.State.Checksum);
        Assert.Equal(
            first.FinalSession.CommandLog.CurrentChecksum,
            second.FinalSession.CommandLog.CurrentChecksum);
        Assert.Equal(
            first.FinalSession.CommandLog.Entries.Select(ProjectEntry),
            second.FinalSession.CommandLog.Entries.Select(ProjectEntry));
        Assert.Equal(
            first.FinalSession.State.RngState.ToCanonicalText(),
            second.FinalSession.State.RngState.ToCanonicalText());
        Assert.Equal(
            DeterministicChecksum.Sha256Hex(first.FinalSession.State.CanonicalText),
            first.FinalSession.State.Checksum);
        Assert.All(first.FinalSession.CommandLog.Entries, entry =>
        {
            Assert.Matches("^[0-9a-f]{64}$", entry.ResultChecksum);
            Assert.Matches("^[0-9a-f]{64}$", entry.LogChecksum);
        });
    }

    [Fact]
    public void DifferentAcceptedCommandOrderDivergesDespiteEqualFinalBoard()
    {
        var forward = RunTwoBlackPlacements(C(1, 1), C(2, 1));
        var reversed = RunTwoBlackPlacements(C(2, 1), C(1, 1));

        Assert.Equal(
            StoneTopologyKey.FromBoard(forward.State.Board),
            StoneTopologyKey.FromBoard(reversed.State.Board));
        Assert.Equal(
            forward.State.FacilityState.ToCanonicalText(),
            reversed.State.FacilityState.ToCanonicalText());
        Assert.NotEqual(
            forward.State.RepetitionHistory.ToCanonicalText(),
            reversed.State.RepetitionHistory.ToCanonicalText());
        Assert.NotEqual(forward.State.Checksum, reversed.State.Checksum);
        Assert.NotEqual(forward.CommandLog.CurrentChecksum, reversed.CommandLog.CurrentChecksum);
    }

    private static DeterministicScriptResult RunDeterministicScript(long seed)
    {
        var session = Start(Board(), seed: seed);
        var boundaries = new List<CommandBoundaryProjection>();

        var first = Place(session, StoneColor.Black, 1, 1);
        boundaries.Add(ProjectBoundary(first));
        session = first.SessionAfter;

        var second = Place(session, StoneColor.Black, 2, 1);
        boundaries.Add(ProjectBoundary(second));
        session = second.SessionAfter;

        var endTurn = EndPlayerTurn(session);
        boundaries.Add(ProjectBoundary(endTurn));
        session = endTurn.SessionAfter;

        var enemy = Place(session, StoneColor.White, 7, 7);
        boundaries.Add(ProjectBoundary(enemy));
        return new DeterministicScriptResult(enemy.SessionAfter, boundaries.ToArray());
    }

    private static HeadlessBattleSession RunTwoBlackPlacements(
        CanonicalPoint first,
        CanonicalPoint second)
    {
        var session = Start(Board(), seed: 77);
        session = ExecutePlacement(session, StoneColor.Black, first).SessionAfter;
        return ExecutePlacement(session, StoneColor.Black, second).SessionAfter;
    }

    private static HeadlessBattleSession Start(
        BoardState board,
        IEnumerable<FacilityInstance>? facilities = null,
        int playerTurnLimit = 20,
        long seed = 42,
        string gameVersion = "v0.2.10",
        string contentHash = ContentHash)
    {
        var installed = facilities?.ToArray() ?? [];
        var nextBuildSequence = installed.Length == 0
            ? 1
            : installed.Max(facility => facility.BuildSequence) + 1;
        return HeadlessBattleStateMachine.Start(
            board,
            FacilityState.Create(board, installed, nextBuildSequence),
            RuntimePolicy(playerTurnLimit),
            Metadata(seed, gameVersion, contentHash));
    }

    private static BattleRuntimePolicy RuntimePolicy(int playerTurnLimit = 20) =>
        new(
            playerTurnLimit,
            FacilityRuntimePolicy.Create(
                territoryIncomeDivisor: 5,
                capacityBands: [new FacilityCapacityBand(1, 49, 1)],
                slotCap: 3,
                typeLimits: [new KeyValuePair<string, int>("default", 1)]));

    private static ReplayMetadata Metadata(
        long seed = 42,
        string gameVersion = "v0.2.10",
        string contentHash = ContentHash) =>
        ReplayMetadata.Create(gameVersion, contentHash, seed);

    private static BattleCommandResult Place(
        HeadlessBattleSession session,
        StoneColor actor,
        int x,
        int y,
        PlacementAccessMode accessMode = PlacementAccessMode.Normal) =>
        ExecutePlacement(session, actor, C(x, y), accessMode);

    private static BattleCommandResult ExecutePlacement(
        HeadlessBattleSession session,
        StoneColor actor,
        CanonicalPoint point,
        PlacementAccessMode accessMode = PlacementAccessMode.Normal) =>
        HeadlessBattleStateMachine.Execute(
            session,
            new AuthorizedStonePlacementCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum,
                actor,
                point,
                accessMode));

    private static BattleCommandResult EndPlayerTurn(HeadlessBattleSession session) =>
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

    private static void AssertRejectedNoOp(
        HeadlessBattleSession expectedSession,
        BattleCommandResult result,
        string expectedReason)
    {
        var stateCanonicalBefore = expectedSession.State.CanonicalText;
        var stateChecksumBefore = expectedSession.State.Checksum;
        var logChecksumBefore = expectedSession.CommandLog.CurrentChecksum;
        var logCountBefore = expectedSession.CommandLog.Entries.Count;

        Assert.False(result.Accepted);
        Assert.Equal(expectedReason, result.ReasonId);
        Assert.Same(expectedSession, result.SessionBefore);
        Assert.Same(expectedSession, result.SessionAfter);
        Assert.Same(expectedSession.State, result.SessionAfter.State);
        Assert.Same(expectedSession.CommandLog, result.SessionAfter.CommandLog);
        Assert.Equal(stateCanonicalBefore, result.SessionAfter.State.CanonicalText);
        Assert.Equal(stateChecksumBefore, result.StateChecksum);
        Assert.Equal(logChecksumBefore, result.LogChecksum);
        Assert.Equal(logCountBefore, result.SessionAfter.CommandLog.Entries.Count);
        var rejected = Assert.IsType<CommandRejectedFact>(
            Assert.Single(result.OrderedFacts));
        Assert.Equal(expectedReason, rejected.ReasonId);
    }

    private static CommandBoundaryProjection ProjectBoundary(
        BattleCommandResult result) =>
        new(
            result.Accepted,
            result.ReasonId,
            result.StateChecksum,
            result.LogChecksum,
            string.Join('\u001e', result.OrderedFacts.Select(ProjectFact)));

    private static string ProjectFact(IBattleFact fact) => fact switch
    {
        StonePlacedFact placed =>
            $"stone_placed|{ColorId(placed.Stone.Color)}|{PointId(placed.Stone.Point)}|king={placed.Stone.IsKing}",
        GroupCapturedFact captured =>
            $"group_captured|by={ColorId(captured.CapturingColor)}|" +
            $"anchor={PointId(captured.CapturedGroup.Anchor)}|" +
            $"stones={string.Join(';', captured.CapturedGroup.Stones.Select(stone => PointId(stone.Point)))}|" +
            $"king={captured.ContainsKing}",
        FacilityDestroyedFact destroyed =>
            $"facility_destroyed|{destroyed.Facility.InstanceId}|{destroyed.ReasonId}",
        FacilityActivatedFact activated =>
            $"facility_activated|{activated.Facility.InstanceId}|{activated.ReasonId}|" +
            PointId(activated.Region.Anchor),
        FacilityDisabledFact disabled =>
            $"facility_disabled|{disabled.Facility.InstanceId}|{disabled.ReasonId}|" +
            PointId(disabled.Region.Anchor),
        FacilityBuiltFact built => $"facility_built|{built.Facility.InstanceId}",
        StoneTopologyRegisteredFact topology =>
            $"topology_registered|{topology.RegisteredTopologyKey.ToCanonicalText()}|" +
            $"observations={topology.HistoryAfterRegistration.ObservationCount}",
        KingCaptureEvaluatedFact king =>
            $"king_capture_evaluated|{king.Result.ToCanonicalText()}|{king.Result.EndReasonId}",
        TerritoryEstablishedFact territory =>
            $"territory_established|{ColorId(territory.SourceActor)}|" +
            string.Join(';', territory.ChangedPoints.Select(PointId)),
        EnemyPassedFact passed => $"enemy_passed|turn={passed.PlayerTurnIndex}",
        BattleEndedFact ended =>
            $"battle_ended|{ended.Outcome}|{ended.ReasonId}",
        CommandRejectedFact rejected => $"command_rejected|{rejected.ReasonId}",
        _ => throw new InvalidOperationException(
            $"Unhandled battle fact projection type {fact.GetType().FullName}."),
    };

    private static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Unknown test stone color."),
    };

    private static string PointId(CanonicalPoint point) => $"{point.X},{point.Y}";

    private static string ProjectEntry(CommandLogEntry entry) =>
        $"{entry.Sequence}|{entry.CommandType}|{entry.CommandSchemaVersion}|" +
        $"{entry.CanonicalPayload}|{entry.ResultChecksum}|{entry.LogChecksum}";

    private sealed record DeterministicScriptResult(
        HeadlessBattleSession FinalSession,
        IReadOnlyList<CommandBoundaryProjection> Boundaries);

    private sealed record CommandBoundaryProjection(
        bool Accepted,
        string ReasonId,
        string StateChecksum,
        string LogChecksum,
        string OrderedFacts);

    private static BoardState KoBeforeCapture() => Board(
        Stone(StoneColor.Black, 3, 4),
        Stone(StoneColor.Black, 2, 3),
        Stone(StoneColor.Black, 4, 3),
        Stone(StoneColor.White, 3, 3),
        Stone(StoneColor.White, 2, 2),
        Stone(StoneColor.White, 4, 2),
        Stone(StoneColor.White, 3, 1));

    private static BoardState WhiteKingInAtari() => Board(
        Stone(StoneColor.Black, 3, 4),
        Stone(StoneColor.Black, 2, 3),
        Stone(StoneColor.Black, 4, 3),
        Stone(StoneColor.Black, 7, 7, isKing: true),
        Stone(StoneColor.White, 3, 3, isKing: true));

    private static BoardState BlackKingInAtari() => Board(
        Stone(StoneColor.White, 3, 4),
        Stone(StoneColor.White, 2, 3),
        Stone(StoneColor.White, 4, 3),
        Stone(StoneColor.White, 7, 7, isKing: true),
        Stone(StoneColor.Black, 3, 3, isKing: true));

    private static FacilityInstance Facility(
        string instanceId,
        StoneColor owner,
        int x,
        int y,
        long buildSequence) =>
        new(instanceId, "development", owner, C(x, y), buildSequence);

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
