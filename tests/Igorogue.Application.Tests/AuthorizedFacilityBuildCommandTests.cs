using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

public sealed class AuthorizedFacilityBuildCommandTests
{
    private const string ContentHash =
        "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private static readonly BoardGeometry Geometry =
        BoardGeometry.Create(BoardGeometry.AcceptedSize);

    [Fact]
    public void CanonicalCommandContractIsVersionedValidatedAndUnambiguous()
    {
        var upperState = new string('A', 64);
        var upperLog = new string('B', 64);

        var command = new AuthorizedFacilityBuildCommand(
            upperState,
            upperLog,
            C(2, 3),
            "development.v1",
            "facility_01");

        Assert.IsAssignableFrom<IBattleCommand>(command);
        Assert.Equal("battle.authorized_facility_build", command.CommandType);
        Assert.Equal(1, command.CommandSchemaVersion);
        Assert.Equal(new string('a', 64), command.ExpectedStateChecksum);
        Assert.Equal(new string('b', 64), command.ExpectedLogChecksum);
        Assert.Equal(C(2, 3), command.Point);
        Assert.Equal("development.v1", command.FacilityContentId);
        Assert.Equal("facility_01", command.InstanceId);
        Assert.Equal(
            "authorized-facility-build-v1\n" +
            $"expected_state_checksum={new string('a', 64)}\n" +
            $"expected_log_checksum={new string('b', 64)}\n" +
            "actor=black\n" +
            "point=2,3\n" +
            "facility_content_id=development.v1\n" +
            "instance_id=facility_01\n",
            command.ToCanonicalPayload());

        Assert.Throws<ArgumentNullException>(() => new AuthorizedFacilityBuildCommand(
            upperState,
            upperLog,
            null!,
            "development",
            "facility"));
        Assert.Throws<ArgumentException>(() => new AuthorizedFacilityBuildCommand(
            upperState,
            upperLog,
            C(1, 1),
            "bad/content",
            "facility"));
        Assert.Throws<ArgumentException>(() => new AuthorizedFacilityBuildCommand(
            upperState,
            upperLog,
            C(1, 1),
            "development",
            "bad instance"));
    }

    [Fact]
    public void LegalBuildUsesDomainFactOrderAndOnlyChangesFacilitySnapshotsAndAcceptedLog()
    {
        var session = Start(BlackControlledBoard(), capacity: 3, typeLimit: 3);
        var source = session.State;

        var result = Build(session, 1, 1, "development", "built-facility");

        Assert.True(result.Accepted);
        Assert.Equal("accepted", result.ReasonId);
        Assert.Same(session, result.SessionBefore);
        Assert.NotSame(session, result.SessionAfter);
        Assert.Equal(
            new[] { typeof(FacilityBuiltFact), typeof(FacilityActivatedFact) },
            result.OrderedFacts.Select(fact => fact.GetType()));
        var built = Assert.IsType<FacilityBuiltFact>(result.OrderedFacts[0]);
        var activated = Assert.IsType<FacilityActivatedFact>(result.OrderedFacts[1]);
        Assert.Same(built.Facility, activated.Facility);
        Assert.Equal("built-facility", built.Facility.InstanceId);
        Assert.Equal("development", built.Facility.ContentId);
        Assert.Equal(StoneColor.Black, built.Facility.Owner);
        Assert.Equal(C(1, 1), built.Facility.Point);
        Assert.Equal(1, built.Facility.BuildSequence);
        Assert.Equal("built_in_controlled_territory", activated.ReasonId);

        var after = result.SessionAfter.State;
        Assert.Same(source.Board, after.Board);
        Assert.Same(source.RepetitionHistory, after.RepetitionHistory);
        Assert.Same(source.TerritoryAnalysis, after.TerritoryAnalysis);
        Assert.Same(source.RngState, after.RngState);
        Assert.Same(source.RuntimePolicy, after.RuntimePolicy);
        Assert.NotSame(source.FacilityState, after.FacilityState);
        Assert.NotSame(source.FacilityRuntimeAnalysis, after.FacilityRuntimeAnalysis);
        Assert.Same(after.FacilityState, after.FacilityRuntimeAnalysis.FacilityState);
        Assert.Same(source.TerritoryAnalysis, after.FacilityRuntimeAnalysis.TerritoryAnalysis);
        Assert.Same(source.RuntimePolicy.FacilityPolicy, after.FacilityRuntimeAnalysis.Policy);
        Assert.Equal(2, after.FacilityState.NextBuildSequence);
        Assert.Same(built.Facility, after.FacilityState.FacilityById("built-facility"));
        Assert.True(after.FacilityRuntimeAnalysis.OperatingStateFor(built.Facility).IsActive);
        Assert.Equal(source.Phase, after.Phase);
        Assert.Equal(source.PlayerTurnIndex, after.PlayerTurnIndex);
        Assert.Equal(source.Outcome, after.Outcome);
        Assert.Equal(source.EndReason, after.EndReason);
        Assert.NotEqual(source.Checksum, after.Checksum);

        Assert.Empty(session.CommandLog.Entries);
        var entry = Assert.Single(result.SessionAfter.CommandLog.Entries);
        Assert.Equal(0, entry.Sequence);
        Assert.Equal("battle.authorized_facility_build", entry.CommandType);
        Assert.Equal(1, entry.CommandSchemaVersion);
        Assert.Equal(result.StateChecksum, entry.ResultChecksum);
        Assert.Equal(result.LogChecksum, entry.LogChecksum);
    }

    [Fact]
    public void EveryBuildLegalityAndIdentityRejectionIsAnExactNoOp()
    {
        var stoneTarget = Start(
            Board(Stone(StoneColor.Black, 1, 1)),
            capacity: 3,
            typeLimit: 3);
        AssertRejectedNoOp(
            stoneTarget,
            Build(stoneTarget, 1, 1, "development", "stone-target"),
            "facility_target_has_stone");

        var occupiedFacility = Facility("occupied", "market", 1, 1, 1);
        var occupied = Start(
            BlackControlledBoard(),
            [occupiedFacility],
            capacity: 3,
            typeLimit: 3);
        AssertRejectedNoOp(
            occupied,
            Build(occupied, 1, 1, "development", "other"),
            "facility_target_occupied");

        var notOwned = Start(
            Board(Stone(StoneColor.White, 7, 7)),
            capacity: 3,
            typeLimit: 3);
        AssertRejectedNoOp(
            notOwned,
            Build(notOwned, 1, 1, "development", "not-owned"),
            "facility_target_not_owned_territory");

        var capacityFacility = Facility("capacity-one", "market", 2, 2, 1);
        var capacity = Start(
            BlackControlledBoard(),
            [capacityFacility],
            capacity: 1,
            typeLimit: 3);
        AssertRejectedNoOp(
            capacity,
            Build(capacity, 1, 1, "development", "capacity-two"),
            "facility_capacity_full");

        var typeFacility = Facility("type-one", "development", 2, 2, 1);
        var typeLimit = Start(
            BlackControlledBoard(),
            [typeFacility],
            capacity: 3,
            typeLimit: 1);
        AssertRejectedNoOp(
            typeLimit,
            Build(typeLimit, 1, 1, "development", "type-two"),
            "facility_type_limit_reached");

        var duplicate = Start(
            BlackControlledBoard(),
            [typeFacility],
            capacity: 3,
            typeLimit: 3);
        AssertRejectedNoOp(
            duplicate,
            Build(duplicate, 1, 1, "market", "type-one"),
            "facility_instance_exists");
    }

    [Fact]
    public void PhaseStaleSessionAndTerminalGuardsRejectBeforeBuildEvaluation()
    {
        var session = Start(BlackControlledBoard(), capacity: 3, typeLimit: 3);
        var enemyPhase = EndPlayerTurn(session).SessionAfter;
        AssertRejectedNoOp(
            enemyPhase,
            Build(enemyPhase, 1, 1, "development", "wrong-phase"),
            "wrong_phase");

        var staleCommand = Command(session, 1, 1, "development", "first");
        var accepted = HeadlessBattleStateMachine.Execute(session, staleCommand);
        Assert.True(accepted.Accepted);
        AssertRejectedNoOp(
            accepted.SessionAfter,
            HeadlessBattleStateMachine.Execute(accepted.SessionAfter, staleCommand),
            "stale_state");

        var staleLogCommand = new AuthorizedFacilityBuildCommand(
            accepted.SessionAfter.State.Checksum,
            session.CommandLog.CurrentChecksum,
            C(2, 1),
            "development",
            "stale-log");
        AssertRejectedNoOp(
            accepted.SessionAfter,
            HeadlessBattleStateMachine.Execute(accepted.SessionAfter, staleLogCommand),
            "stale_session");

        var terminalStart = Start(
            BlackControlledBoard(),
            capacity: 3,
            typeLimit: 3,
            playerTurnLimit: 1);
        var terminalEnemy = EndPlayerTurn(terminalStart).SessionAfter;
        var terminal = EnemyPass(terminalEnemy).SessionAfter;
        Assert.True(terminal.State.IsTerminal);
        AssertRejectedNoOp(
            terminal,
            Build(terminal, 1, 1, "development", "terminal"),
            "battle_terminal");
    }

    [Fact]
    public void AcceptedOnlyBuildScriptIsDeterministicAcrossRejectedAttempts()
    {
        var first = RunScript();
        var second = RunScript();

        Assert.Equal(first.Boundaries, second.Boundaries);
        Assert.Equal(first.FinalSession.State.Checksum, second.FinalSession.State.Checksum);
        Assert.Equal(
            first.FinalSession.CommandLog.CurrentChecksum,
            second.FinalSession.CommandLog.CurrentChecksum);
        Assert.Equal(2, first.FinalSession.CommandLog.Entries.Count);
        Assert.Equal(
            new long[] { 0, 1 },
            first.FinalSession.CommandLog.Entries.Select(entry => entry.Sequence));
        Assert.Equal(
            new[]
            {
                "battle.authorized_facility_build",
                "battle.authorized_facility_build",
            },
            first.FinalSession.CommandLog.Entries.Select(entry => entry.CommandType));
        Assert.Equal(
            StoneTopologyKey.FromBoard(BlackControlledBoard()),
            StoneTopologyKey.FromBoard(first.FinalSession.State.Board));
        Assert.Equal(1, first.FinalSession.State.RepetitionHistory.ObservationCount);
    }

    [Fact]
    public void AcceptedPayloadAndOrderChangesDeterministicallyDiverge()
    {
        var source = Start(BlackControlledBoard(), capacity: 3, typeLimit: 3);
        var point = Command(source, 1, 1, "development", "facility-a");
        var otherPoint = Command(source, 2, 1, "development", "facility-a");
        var otherContent = Command(source, 1, 1, "market", "facility-a");
        var otherInstance = Command(source, 1, 1, "development", "facility-b");

        Assert.Equal(4, new[]
        {
            point.ToCanonicalPayload(),
            otherPoint.ToCanonicalPayload(),
            otherContent.ToCanonicalPayload(),
            otherInstance.ToCanonicalPayload(),
        }.Distinct(StringComparer.Ordinal).Count());

        var results = new[] { point, otherPoint, otherContent, otherInstance }
            .Select(command => HeadlessBattleStateMachine.Execute(
                Start(BlackControlledBoard(), capacity: 3, typeLimit: 3),
                command))
            .ToArray();
        Assert.All(results, result => Assert.True(result.Accepted));
        Assert.Equal(4, results.Select(result => result.LogChecksum).Distinct().Count());

        var forward = RunAcceptedOrder((1, 1, "facility-a"), (2, 1, "facility-b"));
        var reverse = RunAcceptedOrder((2, 1, "facility-b"), (1, 1, "facility-a"));
        Assert.Equal(
            StoneTopologyKey.FromBoard(forward.State.Board),
            StoneTopologyKey.FromBoard(reverse.State.Board));
        Assert.NotEqual(forward.State.Checksum, reverse.State.Checksum);
        Assert.NotEqual(
            forward.CommandLog.CurrentChecksum,
            reverse.CommandLog.CurrentChecksum);
    }

    private static ScriptResult RunScript()
    {
        var session = Start(BlackControlledBoard(), capacity: 3, typeLimit: 3);
        var boundaries = new List<Boundary>();

        var first = Build(session, 1, 1, "development", "facility-a");
        boundaries.Add(Project(first));
        session = first.SessionAfter;

        var rejected = Build(session, 3, 1, "development", "facility-a");
        boundaries.Add(Project(rejected));
        session = rejected.SessionAfter;

        var second = Build(session, 2, 1, "development", "facility-b");
        boundaries.Add(Project(second));
        return new ScriptResult(second.SessionAfter, boundaries.ToArray());
    }

    private static HeadlessBattleSession RunAcceptedOrder(
        (int X, int Y, string Id) first,
        (int X, int Y, string Id) second)
    {
        var session = Start(BlackControlledBoard(), capacity: 3, typeLimit: 3);
        session = Build(
            session,
            first.X,
            first.Y,
            "development",
            first.Id).SessionAfter;
        return Build(
            session,
            second.X,
            second.Y,
            "development",
            second.Id).SessionAfter;
    }

    private static Boundary Project(BattleCommandResult result) =>
        new(
            result.Accepted,
            result.ReasonId,
            result.StateChecksum,
            result.LogChecksum,
            string.Join('|', result.OrderedFacts.Select(fact => fact switch
            {
                FacilityBuiltFact built => $"built:{built.Facility.InstanceId}",
                FacilityActivatedFact activated =>
                    $"activated:{activated.Facility.InstanceId}:{activated.ReasonId}",
                CommandRejectedFact rejected => $"rejected:{rejected.ReasonId}",
                _ => throw new InvalidOperationException(
                    $"Unexpected fact type {fact.GetType().FullName}."),
            })));

    private static BattleCommandResult Build(
        HeadlessBattleSession session,
        int x,
        int y,
        string contentId,
        string instanceId) =>
        HeadlessBattleStateMachine.Execute(
            session,
            Command(session, x, y, contentId, instanceId));

    private static AuthorizedFacilityBuildCommand Command(
        HeadlessBattleSession session,
        int x,
        int y,
        string contentId,
        string instanceId) =>
        new(
            session.State.Checksum,
            session.CommandLog.CurrentChecksum,
            C(x, y),
            contentId,
            instanceId);

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
        Assert.False(result.Accepted);
        Assert.Equal(expectedReason, result.ReasonId);
        Assert.Same(expectedSession, result.SessionBefore);
        Assert.Same(expectedSession, result.SessionAfter);
        Assert.Same(expectedSession.State, result.SessionAfter.State);
        Assert.Same(expectedSession.CommandLog, result.SessionAfter.CommandLog);
        Assert.Equal(expectedSession.State.Checksum, result.StateChecksum);
        Assert.Equal(expectedSession.CommandLog.CurrentChecksum, result.LogChecksum);
        var rejected = Assert.IsType<CommandRejectedFact>(
            Assert.Single(result.OrderedFacts));
        Assert.Equal(expectedReason, rejected.ReasonId);
    }

    private static HeadlessBattleSession Start(
        BoardState board,
        IEnumerable<FacilityInstance>? facilities = null,
        int capacity = 3,
        int typeLimit = 3,
        int playerTurnLimit = 20)
    {
        var installed = facilities?.ToArray() ?? [];
        var nextBuildSequence = installed.Length == 0
            ? 1
            : installed.Max(facility => facility.BuildSequence) + 1;
        var facilityPolicy = FacilityRuntimePolicy.Create(
            territoryIncomeDivisor: 5,
            capacityBands: [new FacilityCapacityBand(1, 49, capacity)],
            slotCap: 5,
            typeLimits:
            [
                new KeyValuePair<string, int>("default", typeLimit),
                new KeyValuePair<string, int>("development", typeLimit),
                new KeyValuePair<string, int>("market", typeLimit),
            ]);
        return HeadlessBattleStateMachine.Start(
            board,
            FacilityState.Create(board, installed, nextBuildSequence),
            new BattleRuntimePolicy(playerTurnLimit, facilityPolicy),
            ReplayMetadata.Create("v0.2.10", ContentHash, initialSeed: 42));
    }

    private static FacilityInstance Facility(
        string instanceId,
        string contentId,
        int x,
        int y,
        long buildSequence) =>
        new(
            instanceId,
            contentId,
            StoneColor.Black,
            C(x, y),
            buildSequence);

    private static BoardState BlackControlledBoard() =>
        Board(Stone(StoneColor.Black, 7, 7));

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(StoneColor color, int x, int y) =>
        new(color, false, C(x, y));

    private static CanonicalPoint C(int x, int y) =>
        Geometry.CreateCanonicalPoint(x, y);

    private sealed record ScriptResult(
        HeadlessBattleSession FinalSession,
        IReadOnlyList<Boundary> Boundaries);

    private sealed record Boundary(
        bool Accepted,
        string Reason,
        string StateChecksum,
        string LogChecksum,
        string Facts);
}
