using System.Globalization;
using System.Text;
using System.Text.Json;

using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

public sealed class CoreDuelBattleGoldenTests
{
    private const string LossGoldenRelativePath =
        "tests/golden/v3/core_duel_turn_limit_loss.json";
    private const long Seed = CoreDuelBattleTestFixture.Seed;

    [Fact]
    public void TerminalStarterCardCapturesTheWhiteKingAndReplaysAsVictory()
    {
        var first = RunVictory();
        var second = RunVictory();

        AssertRunsEqual(first, second);
        Assert.True(first.IsTerminal);
        Assert.Equal("win", first.OutcomeId);
        Assert.Equal("white_king_captured", first.EndReasonId);
        Assert.Single(first.CommandResults);
        Assert.Contains(
            first.FactProjection,
            fact => fact.Contains(
                typeof(BattleEndedFact).FullName!,
                StringComparison.Ordinal));

        var replayStart = StartVictory();
        using var replayStream = new MemoryStream(first.ReplayBytes, writable: false);
        var document = BattleReplaySerializerV3.Load(replayStream);
        var replay = BattleReplayRunnerV3.Replay(document, replayStart.Session);

        Assert.Equal(first.StateCanonicalText, replay.FinalSession.State.CanonicalText);
        Assert.Equal(first.StateChecksum, replay.FinalSession.State.Checksum);
        Assert.Equal(first.LogChecksum, replay.FinalSession.CommandLog.CurrentChecksum);
        Assert.Equal(
            first.FactProjection,
            ProjectFacts(replayStart, replay.CommandResults));
    }

    [Fact]
    public void TurnLimitLossMatchesTheCommittedReplayGoldenByteForByte()
    {
        var first = RunTurnLimitLoss();
        var second = RunTurnLimitLoss();

        AssertRunsEqual(first, second);
        Assert.True(first.IsTerminal);
        Assert.Equal("loss", first.OutcomeId);
        Assert.Equal("turn_limit", first.EndReasonId);
        Assert.Equal(3, first.CommandResults.Count);

        var root = GoldenBoardFixtureAdapter.FindRepositoryRoot();
        var goldenPath = Path.Combine(
            root.FullName,
            LossGoldenRelativePath);
        var goldenBytes = File.ReadAllBytes(goldenPath);
        Assert.Equal(goldenBytes, first.ReplayBytes);

        var replayStart = CoreDuelBattleTestFixture.Start(playerTurnLimit: 1);
        using var replayStream = new MemoryStream(goldenBytes, writable: false);
        var document = BattleReplaySerializerV3.Load(replayStream);
        var replay = BattleReplayRunnerV3.Replay(document, replayStart.Session);

        Assert.Equal(first.StateCanonicalText, replay.FinalSession.State.CanonicalText);
        Assert.Equal(first.StateChecksum, replay.FinalSession.State.Checksum);
        Assert.Equal(first.LogChecksum, replay.FinalSession.CommandLog.CurrentChecksum);
        Assert.Equal(
            first.FactProjection,
            ProjectFacts(replayStart, replay.CommandResults));
    }

    private static GoldenRun RunVictory()
    {
        var start = StartVictory();
        var session = start.Session;
        var terminalCard = session.State.CardTurnState.Deck.Hand.First(card =>
            card.ContentId is "card_basic_stone" or "card_contact");
        var result = ExecuteAccepted(
            session,
            new PlayCardCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum,
                terminalCard.InstanceId,
                Point(3, 3),
                StoneCardPlacementMode.TerminalCapture));

        return Capture(start, [result]);
    }

    private static GoldenRun RunTurnLimitLoss()
    {
        var start = CoreDuelBattleTestFixture.Start(playerTurnLimit: 1);
        var results = new List<CoreDuelBattleCommandResult>();
        var session = start.Session;

        var play = ExecuteAccepted(session, FirstStarterPlay(session));
        results.Add(play);
        session = play.SessionAfter;

        var endTurn = ExecuteAccepted(
            session,
            new EndPlayerTurnCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));
        results.Add(endTurn);
        session = endTurn.SessionAfter;

        var enemy = ExecuteAccepted(
            session,
            new ResolveBanditEnemyActionCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum));
        results.Add(enemy);

        return Capture(start, results);
    }

    private static GoldenRun Capture(
        CoreDuelBattleStartResult start,
        IReadOnlyList<CoreDuelBattleCommandResult> results)
    {
        var final = results.Count == 0
            ? start.Session
            : results[^1].SessionAfter;
        var document = BattleReplayDocumentV3.Capture(start.Session, results);
        using var replayStream = new MemoryStream();
        BattleReplaySerializerV3.Save(document, replayStream);

        return new GoldenRun(
            replayStream.ToArray(),
            final.State.CanonicalText,
            final.State.Checksum,
            ProjectFacts(start, results),
            final.CommandLog.Entries.Select(ProjectLogEntry).ToArray(),
            final.CommandLog.CurrentChecksum,
            final.State.IsTerminal,
            final.State.BattleState.OutcomeId,
            final.State.BattleState.EndReasonId,
            results.ToArray());
    }

    private static CoreDuelBattleStartResult StartVictory()
    {
        var catalog = CoreDuelBattleTestFixture.LoadCatalog();
        return CoreDuelBattleStateMachine.Start(
            VictoryInitialSnapshot(),
            catalog,
            ReplayMetadata.Create(
                CoreDuelBattleTestFixture.GameVersion,
                catalog.ContentHash,
                Seed));
    }

    private static BattleAuthoritativeInitialSnapshot VictoryInitialSnapshot()
    {
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var board = BoardState.Create(
            geometry,
            [
                Stone(geometry, StoneColor.Black, isKing: false, 3, 1),
                Stone(geometry, StoneColor.Black, isKing: false, 2, 2),
                Stone(geometry, StoneColor.White, isKing: true, 3, 2),
                Stone(geometry, StoneColor.Black, isKing: false, 4, 2),
                Stone(geometry, StoneColor.Black, isKing: true, 7, 7),
            ]);
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                "initial.golden_win.stone." +
                geometry.ToCanonicalIndex(stone.Point)
                    .ToString("D2", CultureInfo.InvariantCulture),
                stone,
                stone.IsKing ? "king" : "standard",
                index + 1L,
                []))
            .ToArray();
        var stones = StoneRuntimeState.Create(board, instances, instances.Length + 1L);
        var standardPolicy = CoreDuelBattleTestFixture.InitialSnapshot();

        return BattleAuthoritativeInitialSnapshot.Create(
            stones,
            TemporaryLibertyState.Create(stones, [], nextCreatedSequence: 1),
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(board),
            FacilityState.Create(board, [], nextBuildSequence: 1),
            ClosedWindowResourceState.Empty([]),
            CaptureBenefitTriggerPlan.Create([]),
            standardPolicy.CounterattackState,
            standardPolicy.CounterattackPolicy,
            standardPolicy.RuntimePolicy,
            playerTurnIndex: 1);
    }

    private static PlayCardCommand FirstStarterPlay(CoreDuelBattleSession session)
    {
        var hand = session.State.CardTurnState.Deck.Hand;
        var stoneCard = hand.FirstOrDefault(card =>
            StringComparer.Ordinal.Equals(card.ContentId, "card_basic_stone")) ??
            hand.FirstOrDefault(card =>
                StringComparer.Ordinal.Equals(card.ContentId, "card_extend"));
        if (stoneCard is not null)
        {
            return new PlayCardCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum,
                stoneCard.InstanceId,
                Point(1, 2),
                StoneCardPlacementMode.Frontline);
        }

        var reinforce = hand.Single(card =>
            StringComparer.Ordinal.Equals(card.ContentId, "card_reinforce"));
        return new PlayCardCommand(
            session.State.Checksum,
            session.CommandLog.CurrentChecksum,
            reinforce.InstanceId,
            Point(2, 2));
    }

    private static CoreDuelBattleCommandResult ExecuteAccepted(
        CoreDuelBattleSession session,
        IBattleCommand command)
    {
        var result = CoreDuelBattleStateMachine.Execute(session, command);
        Assert.True(result.Accepted, result.ReasonId);
        return result;
    }

    private static string[] ProjectFacts(
        CoreDuelBattleStartResult start,
        IEnumerable<CoreDuelBattleCommandResult> results) =>
        start.OrderedFacts
            .Concat(results.SelectMany(result => result.OrderedFacts))
            .Select(FactFingerprint)
            .ToArray();

    private static string FactFingerprint(IBattleFact fact) =>
        $"{fact.GetType().FullName}|" + JsonSerializer.Serialize(fact, fact.GetType());

    private static string ProjectLogEntry(CommandLogEntry entry) => string.Join(
        '|',
        entry.Sequence.ToString(CultureInfo.InvariantCulture),
        entry.CommandType,
        entry.CommandSchemaVersion.ToString(CultureInfo.InvariantCulture),
        Convert.ToBase64String(Encoding.UTF8.GetBytes(entry.CanonicalPayload)),
        entry.ResultChecksum,
        entry.LogChecksum);

    private static void AssertRunsEqual(GoldenRun expected, GoldenRun actual)
    {
        Assert.Equal(expected.ReplayBytes, actual.ReplayBytes);
        Assert.Equal(expected.StateCanonicalText, actual.StateCanonicalText);
        Assert.Equal(expected.StateChecksum, actual.StateChecksum);
        Assert.Equal(expected.FactProjection, actual.FactProjection);
        Assert.Equal(expected.CommandLogProjection, actual.CommandLogProjection);
        Assert.Equal(expected.LogChecksum, actual.LogChecksum);
        Assert.Equal(expected.IsTerminal, actual.IsTerminal);
        Assert.Equal(expected.OutcomeId, actual.OutcomeId);
        Assert.Equal(expected.EndReasonId, actual.EndReasonId);
    }

    private static CanonicalPoint Point(int x, int y) =>
        BoardGeometry.Create(BoardGeometry.AcceptedSize).CreateCanonicalPoint(x, y);

    private static BoardStone Stone(
        BoardGeometry geometry,
        StoneColor color,
        bool isKing,
        int x,
        int y) =>
        new(color, isKing, geometry.CreateCanonicalPoint(x, y));

    private sealed record GoldenRun(
        byte[] ReplayBytes,
        string StateCanonicalText,
        string StateChecksum,
        string[] FactProjection,
        string[] CommandLogProjection,
        string LogChecksum,
        bool IsTerminal,
        string OutcomeId,
        string EndReasonId,
        IReadOnlyList<CoreDuelBattleCommandResult> CommandResults);
}
