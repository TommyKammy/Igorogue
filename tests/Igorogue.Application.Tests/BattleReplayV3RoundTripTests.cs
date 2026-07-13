using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;

using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Cards;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

public sealed class BattleReplayV3RoundTripTests
{
    [Fact]
    public void FixedCoreDuelRoundTripsTwiceWithDeterministicBytesFactsAndRestart()
    {
        var execution = FixedExecution();
        var document = BattleReplayDocumentV3.Capture(
            execution.InitialSession,
            execution.CommandResults);
        var firstBytes = Save(document);
        var loaded = Load(firstBytes);
        var secondBytes = Save(loaded);
        var firstReplay = BattleReplayRunnerV3.Replay(
            loaded,
            execution.InitialSession);
        var secondReplay = BattleReplayRunnerV3.Replay(
            Load(secondBytes),
            execution.InitialSession);
        var repeatedExecution = FixedExecution();
        var repeatedBytes = Save(BattleReplayDocumentV3.Capture(
            repeatedExecution.InitialSession,
            repeatedExecution.CommandResults));

        Assert.Equal(firstBytes, secondBytes);
        Assert.Equal(firstBytes, repeatedBytes);
        Assert.False(firstBytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        Assert.Equal((byte)'\n', firstBytes[^1]);
        Assert.Equal(3, BattleReplaySerializerV3.SchemaVersion);
        Assert.Equal("headless-core-duel-state-v1", loaded.StateProjection);
        Assert.Equal(
            [
                "battle.play_card",
                "battle.end_player_turn",
                "battle.resolve_bandit_enemy_action",
                "battle.restart",
            ],
            loaded.Attempts.Select(attempt => attempt.CommandType));
        Assert.All(loaded.Attempts, attempt => Assert.True(attempt.Accepted));
        Assert.False(loaded.Terminal.IsTerminal);
        Assert.Equal("ongoing", loaded.Terminal.OutcomeId);
        Assert.Equal("none", loaded.Terminal.EndReasonId);
        Assert.Contains(
            execution.CommandResults,
            result => result.SessionAfter.State.IsTerminal &&
                result.SessionAfter.State.BattleState.OutcomeId == "loss");
        Assert.Equal(1, execution.FinalSession.State.RestartCount);
        Assert.Equal(
            execution.FinalSession.State.CanonicalText,
            firstReplay.FinalSession.State.CanonicalText);
        Assert.Equal(
            firstReplay.FinalSession.State.CanonicalText,
            secondReplay.FinalSession.State.CanonicalText);
        Assert.Equal(
            execution.FinalSession.CommandLog.CurrentChecksum,
            firstReplay.FinalSession.CommandLog.CurrentChecksum);
        Assert.Equal(
            ProjectFacts(execution.CommandResults),
            ProjectFacts(firstReplay.CommandResults));
        Assert.Equal(
            ProjectFacts(firstReplay.CommandResults),
            ProjectFacts(secondReplay.CommandResults));
    }

    [Fact]
    public void V1V2AndV3LoadersFailClosedAcrossSchemaVersions()
    {
        var execution = FixedExecution();
        var v3Bytes = Save(BattleReplayDocumentV3.Capture(
            execution.InitialSession,
            execution.CommandResults));
        Assert.Throws<ReplayValidationException>(() => LoadV1(v3Bytes));
        Assert.Throws<ReplayValidationException>(() => LoadV2(v3Bytes));

        var metadata = execution.InitialSession.CommandLog.Metadata;
        var snapshot = CoreDuelBattleTestFixture.InitialSnapshot();
        var v2Session = HeadlessBattleStateMachine.Start(snapshot, metadata);
        var v2Document = BattleReplayDocumentV2.Capture(v2Session, []);
        var v2Bytes = SaveV2(v2Document);
        var v2Failure = Assert.Throws<ReplayValidationException>(() => Load(v2Bytes));
        Assert.Equal("unsupported_replay_schema", v2Failure.ReasonId);

        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var board = BoardState.Create(geometry, []);
        var v1Session = HeadlessBattleStateMachine.Start(
            board,
            FacilityState.Create(board, [], nextBuildSequence: 1),
            snapshot.RuntimePolicy,
            metadata);
        var v1Document = BattleReplayDocument.Capture(v1Session, []);
        var v1Bytes = SaveV1(v1Document);
        var v1Failure = Assert.Throws<ReplayValidationException>(() => Load(v1Bytes));
        Assert.Equal("unsupported_replay_schema", v1Failure.ReasonId);
    }

    [Fact]
    public void LoaderRejectsSchemaProjectionPayloadIntegrityAndJsonShapeTampering()
    {
        var document = RuntimeDocument();

        AssertLoadFailure(
            Mutate(document, root => root["schema_id"] = "other.replay"),
            "unsupported_replay_schema");
        AssertLoadFailure(
            Mutate(document, root => root["schema_version"] = 2),
            "unsupported_replay_schema");
        AssertLoadFailure(
            Mutate(document, root => root["state_projection"] = "headless-battle-state-v2"),
            "unsupported_state_projection");
        AssertLoadFailure(
            Mutate(document, root => root.Remove("state_projection")),
            "malformed_replay");
        AssertLoadFailure(
            Mutate(document, root => root["integrity_scheme"] = "unknown-v1"),
            "unsupported_integrity_scheme");
        AssertLoadFailure(
            Mutate(document, root => root["unexpected"] = true),
            "malformed_replay");
        AssertLoadFailure(
            Mutate(document, root =>
            {
                var payload = root["attempts"]![0]!["canonical_payload"]!.GetValue<string>();
                var marker = "placement_mode=";
                var start = payload.IndexOf(marker, StringComparison.Ordinal);
                var end = payload.IndexOf('\n', start);
                root["attempts"]![0]!["canonical_payload"] =
                    payload[..start] + marker + "unknown" + payload[end..];
            }),
            "malformed_command_payload");
        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![0]!["command_type"] =
                    "battle.authorized_runtime_stone_placement"),
            "unsupported_command_type");
        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![0]!["attempt_checksum"] = new string('0', 64)),
            "attempt_chain_checksum_mismatch");
        AssertLoadFailure(
            Mutate(document, root => root["document_checksum"] = new string('0', 64)),
            "document_checksum_mismatch");

        var valid = Save(document);
        var validText = Encoding.UTF8.GetString(valid);
        var duplicate = validText.Insert(
            validText.IndexOf('{') + 1,
            "\n  \"schema_id\": \"igorogue.battle-replay\",");
        AssertLoadFailure(
            Encoding.UTF8.GetBytes(duplicate),
            "duplicate_json_property");
        AssertLoadFailure(
            Encoding.UTF8.GetBytes(validText + "{}"),
            "malformed_replay");

        var deep = new StringBuilder();
        for (var index = 0; index < 65; index++)
        {
            deep.Append("{\"nested\":");
        }

        deep.Append('0').Append('}', 65);
        AssertLoadFailure(
            Encoding.UTF8.GetBytes(deep.ToString()),
            "malformed_replay");
    }

    [Fact]
    public void ResignedSemanticTamperLoadsButRunnerFailsWithoutPartialResult()
    {
        var initial = CoreDuelBattleTestFixture.Start().Session;
        var rejected = CoreDuelBattleStateMachine.Execute(
            initial,
            new RestartBattleCommand(
                initial.State.Checksum,
                initial.CommandLog.CurrentChecksum));
        Assert.False(rejected.Accepted);
        var document = BattleReplayDocumentV3.Capture(initial, [rejected]);
        var root = JsonNode.Parse(Save(document))!.AsObject();
        var attempt = root["attempts"]![0]!.AsObject();
        var expectedState = attempt["before_state_checksum"]!.GetValue<string>();
        var expectedLog = attempt["before_log_checksum"]!.GetValue<string>();
        attempt["command_type"] = "battle.end_player_turn";
        attempt["canonical_payload"] =
            "end-player-turn-v1\n" +
            $"expected_state_checksum={expectedState}\n" +
            $"expected_log_checksum={expectedLog}\n";
        RecomputeReplayIntegrity(root, document.Metadata.ToCanonicalText());
        var forged = Load(Encoding.UTF8.GetBytes(root.ToJsonString()));
        var stateBefore = initial.State.Checksum;
        var logBefore = initial.CommandLog.CurrentChecksum;
        CoreDuelBattleReplayResult? returned = null;

        var failure = Assert.Throws<ReplayValidationException>(() =>
        {
            returned = BattleReplayRunnerV3.Replay(forged, initial);
        });

        Assert.Equal("acceptance_mismatch", failure.ReasonId);
        Assert.Equal(0, failure.AttemptIndex);
        Assert.Null(returned);
        Assert.Equal(stateBefore, initial.State.Checksum);
        Assert.Equal(logBefore, initial.CommandLog.CurrentChecksum);
    }

    [Fact]
    public void RunnerRejectsForeignMetadataInitialStateAndResignedTerminalResult()
    {
        var execution = FixedExecution();
        var document = BattleReplayDocumentV3.Capture(
            execution.InitialSession,
            execution.CommandResults);
        var foreignMetadata = CoreDuelBattleTestFixture.Start(
            seed: CoreDuelBattleTestFixture.Seed + 1,
            playerTurnLimit: 1).Session;
        var metadataFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplayRunnerV3.Replay(document, foreignMetadata));
        Assert.Equal("metadata_mismatch", metadataFailure.ReasonId);

        var foreignInitialState = CoreDuelBattleTestFixture.Start(
            seed: CoreDuelBattleTestFixture.Seed,
            playerTurnLimit: 2).Session;
        var stateFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplayRunnerV3.Replay(document, foreignInitialState));
        Assert.Equal("initial_state_checksum_mismatch", stateFailure.ReasonId);

        var root = JsonNode.Parse(Save(document))!.AsObject();
        root["terminal"]!["is_terminal"] = true;
        root["terminal"]!["outcome"] = "loss";
        root["terminal"]!["end_reason"] = "turn_limit";
        RecomputeReplayIntegrity(root, document.Metadata.ToCanonicalText());
        var terminalDrift = Load(Encoding.UTF8.GetBytes(root.ToJsonString()));
        var terminalFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplayRunnerV3.Replay(
                terminalDrift,
                execution.InitialSession));
        Assert.Equal("terminal_mismatch", terminalFailure.ReasonId);
    }

    [Fact]
    public void SerializerAndCaptureEnforceByteAndAttemptLimitsAtomically()
    {
        using (var oversized = new MemoryStream(
                   new byte[BattleReplaySerializerV3.MaxDocumentBytes + 1],
                   writable: false))
        {
            var failure = Assert.Throws<ReplayValidationException>(() =>
                BattleReplaySerializerV3.Load(oversized));
            Assert.Equal("replay_too_large", failure.ReasonId);
        }

        var validDocument = RuntimeDocument();
        var valid = Save(validDocument);
        var exactLimit = Enumerable.Repeat(
            (byte)' ',
            BattleReplaySerializerV3.MaxDocumentBytes).ToArray();
        valid.CopyTo(exactLimit, 0);
        var exact = Load(exactLimit);
        Assert.Equal(validDocument.DocumentChecksum, exact.DocumentChecksum);

        var initial = CoreDuelBattleTestFixture.Start().Session;
        var rejected = CoreDuelBattleStateMachine.Execute(
            initial,
            new RestartBattleCommand(
                initial.State.Checksum,
                initial.CommandLog.CurrentChecksum));
        Assert.False(rejected.Accepted);
        Assert.Same(initial, rejected.SessionAfter);
        var atLimit = BattleReplayDocumentV3.Capture(
            initial,
            Enumerable.Repeat(rejected, BattleReplaySerializerV3.MaxAttempts));
        Assert.Equal(BattleReplaySerializerV3.MaxAttempts, atLimit.Attempts.Count);
        var countFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplayDocumentV3.Capture(
                initial,
                Enumerable.Repeat(
                    rejected,
                    BattleReplaySerializerV3.MaxAttempts + 1)));
        Assert.Equal("replay_too_many_attempts", countFailure.ReasonId);

        var tooMany = new StringBuilder("{\"attempts\":[{\"duplicate\":1,\"duplicate\":2}");
        for (var index = 1; index <= BattleReplaySerializerV3.MaxAttempts; index++)
        {
            tooMany.Append(",{}");
        }

        tooMany.Append("]}");
        AssertLoadFailure(
            Encoding.UTF8.GetBytes(tooMany.ToString()),
            "replay_too_many_attempts");

        var oversizedCardId = new string('a', BattleReplaySerializerV3.MaxDocumentBytes);
        var oversizedCommand = new PlayCardCommand(
            initial.State.Checksum,
            initial.CommandLog.CurrentChecksum,
            oversizedCardId,
            CoreDuelBattleTestFixture.Point(1, 2),
            StoneCardPlacementMode.Frontline);
        var oversizedResult = CoreDuelBattleStateMachine.Execute(initial, oversizedCommand);
        Assert.False(oversizedResult.Accepted);
        var oversizedDocument = BattleReplayDocumentV3.Capture(
            initial,
            [oversizedResult]);
        using var destination = new MemoryStream();
        destination.Write([1, 2, 3, 4]);
        var beforeBytes = destination.ToArray();
        var beforePosition = destination.Position;

        var saveFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplaySerializerV3.Save(oversizedDocument, destination));

        Assert.Equal("replay_too_large", saveFailure.ReasonId);
        Assert.Equal(beforeBytes, destination.ToArray());
        Assert.Equal(beforePosition, destination.Position);
    }

    [Fact]
    public void RejectedRestartRoundTripsAsAnExactNoOp()
    {
        var initial = CoreDuelBattleTestFixture.Start().Session;
        var stateBefore = initial.State.CanonicalText;
        var rngBefore = initial.State.BattleState.RngState.ToCanonicalText();
        var logBefore = initial.CommandLog.CurrentChecksum;
        var rejected = CoreDuelBattleStateMachine.Execute(
            initial,
            new RestartBattleCommand(
                initial.State.Checksum,
                initial.CommandLog.CurrentChecksum));

        Assert.False(rejected.Accepted);
        Assert.Equal("battle_not_terminal", rejected.ReasonId);
        Assert.Same(initial, rejected.SessionAfter);
        Assert.Equal(stateBefore, rejected.SessionAfter.State.CanonicalText);
        Assert.Equal(rngBefore, rejected.SessionAfter.State.BattleState.RngState.ToCanonicalText());
        Assert.Equal(logBefore, rejected.LogChecksum);

        var document = BattleReplayDocumentV3.Capture(initial, [rejected]);
        var replay = BattleReplayRunnerV3.Replay(Load(Save(document)), initial);
        var replayed = Assert.Single(replay.CommandResults);
        Assert.False(replayed.Accepted);
        Assert.Equal("battle_not_terminal", replayed.ReasonId);
        Assert.Same(replayed.SessionBefore, replayed.SessionAfter);
        Assert.Equal(stateBefore, replay.FinalSession.State.CanonicalText);
        Assert.Equal(logBefore, replay.FinalSession.CommandLog.CurrentChecksum);
    }

    private static FixedReplayExecution FixedExecution()
    {
        var start = CoreDuelBattleTestFixture.Start(playerTurnLimit: 1);
        var initial = start.Session;
        var session = initial;
        var results = new List<CoreDuelBattleCommandResult>();

        Execute(FirstPlayableCardCommand(session));
        while (!session.State.IsTerminal)
        {
            IBattleCommand command = session.State.BattleState.Phase switch
            {
                BattlePhase.PlayerAction => new EndPlayerTurnCommand(
                    session.State.Checksum,
                    session.CommandLog.CurrentChecksum),
                BattlePhase.EnemyAction => new ResolveBanditEnemyActionCommand(
                    session.State.Checksum,
                    session.CommandLog.CurrentChecksum),
                _ => throw new InvalidOperationException(
                    "Ongoing Core Duel entered an unsupported phase."),
            };
            Execute(command);
        }

        Execute(new RestartBattleCommand(
            session.State.Checksum,
            session.CommandLog.CurrentChecksum));
        return new FixedReplayExecution(initial, session, results);

        void Execute(IBattleCommand command)
        {
            var result = CoreDuelBattleStateMachine.Execute(session, command);
            Assert.True(result.Accepted, result.ReasonId);
            results.Add(result);
            session = result.SessionAfter;
        }
    }

    private static PlayCardCommand FirstPlayableCardCommand(CoreDuelBattleSession session)
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
                CoreDuelBattleTestFixture.Point(1, 2),
                StoneCardPlacementMode.Frontline);
        }

        var reinforce = hand.SingleOrDefault(card =>
            StringComparer.Ordinal.Equals(card.ContentId, "card_reinforce"))
            ?? throw new InvalidOperationException(
                "Fixed starter hand has no supported deterministic opening play.");
        return new PlayCardCommand(
            session.State.Checksum,
            session.CommandLog.CurrentChecksum,
            reinforce.InstanceId,
            CoreDuelBattleTestFixture.Point(2, 2));
    }

    private static BattleReplayDocumentV3 RuntimeDocument()
    {
        var execution = FixedExecution();
        return BattleReplayDocumentV3.Capture(
            execution.InitialSession,
            execution.CommandResults);
    }

    private static string ProjectFacts(
        IEnumerable<CoreDuelBattleCommandResult> commandResults) =>
        string.Join(
            '\u001e',
            commandResults.Select((result, index) =>
                $"boundary={index}|accepted={(result.Accepted ? "1" : "0")}|" +
                $"reason={result.ReasonId}|" +
                string.Join('\u001f', result.OrderedFacts.Select(FactFingerprint))));

    private static string FactFingerprint(IBattleFact fact) =>
        $"{fact.GetType().FullName}|" + JsonSerializer.Serialize(fact, fact.GetType());

    private static byte[] Save(BattleReplayDocumentV3 document)
    {
        using var stream = new MemoryStream();
        BattleReplaySerializerV3.Save(document, stream);
        return stream.ToArray();
    }

    private static BattleReplayDocumentV3 Load(byte[] utf8)
    {
        using var stream = new MemoryStream(utf8, writable: false);
        return BattleReplaySerializerV3.Load(stream);
    }

    private static byte[] SaveV1(BattleReplayDocument document)
    {
        using var stream = new MemoryStream();
        BattleReplaySerializer.Save(document, stream);
        return stream.ToArray();
    }

    private static BattleReplayDocument LoadV1(byte[] utf8)
    {
        using var stream = new MemoryStream(utf8, writable: false);
        return BattleReplaySerializer.Load(stream);
    }

    private static byte[] SaveV2(BattleReplayDocumentV2 document)
    {
        using var stream = new MemoryStream();
        BattleReplaySerializerV2.Save(document, stream);
        return stream.ToArray();
    }

    private static BattleReplayDocumentV2 LoadV2(byte[] utf8)
    {
        using var stream = new MemoryStream(utf8, writable: false);
        return BattleReplaySerializerV2.Load(stream);
    }

    private static byte[] Mutate(
        BattleReplayDocumentV3 document,
        Action<JsonObject> mutation)
    {
        var root = JsonNode.Parse(Save(document))?.AsObject()
            ?? throw new InvalidOperationException(
                "Serialized replay did not contain a JSON object.");
        mutation(root);
        return Encoding.UTF8.GetBytes(root.ToJsonString());
    }

    private static void RecomputeReplayIntegrity(
        JsonObject root,
        string canonicalMetadata)
    {
        var schemaId = RequiredString(root, "schema_id");
        var schemaVersion = root["schema_version"]!.GetValue<int>()
            .ToString(CultureInfo.InvariantCulture);
        var stateProjection = RequiredString(root, "state_projection");
        var initialStateChecksum = RequiredString(root, "initial_state_checksum");
        var initialLogChecksum = RequiredString(root, "initial_log_checksum");
        var chain = Igorogue.Domain.Determinism.DeterministicChecksum.Combine(
            "igorogue-battle-replay-attempt-chain-v3",
            schemaId,
            schemaVersion,
            stateProjection,
            canonicalMetadata,
            initialStateChecksum,
            initialLogChecksum);
        foreach (var node in root["attempts"]!.AsArray())
        {
            var attempt = node!.AsObject();
            chain = Igorogue.Domain.Determinism.DeterministicChecksum.Combine(
                "igorogue-battle-replay-attempt-v3",
                schemaId,
                schemaVersion,
                stateProjection,
                chain,
                attempt["attempt_sequence"]!.GetValue<int>()
                    .ToString(CultureInfo.InvariantCulture),
                RequiredString(attempt, "command_type"),
                attempt["command_schema_version"]!.GetValue<int>()
                    .ToString(CultureInfo.InvariantCulture),
                RequiredString(attempt, "canonical_payload"),
                RequiredString(attempt, "before_state_checksum"),
                RequiredString(attempt, "before_log_checksum"),
                attempt["accepted"]!.GetValue<bool>() ? "1" : "0",
                RequiredString(attempt, "reason_id"),
                RequiredString(attempt, "state_checksum"),
                RequiredString(attempt, "log_checksum"));
            attempt["attempt_checksum"] = chain;
        }

        root["attempts_checksum"] = chain;
        var terminal = root["terminal"]!.AsObject();
        root["document_checksum"] =
            Igorogue.Domain.Determinism.DeterministicChecksum.Combine(
                "igorogue-battle-replay-document-v3",
                schemaId,
                schemaVersion,
                stateProjection,
                RequiredString(root, "integrity_scheme"),
                canonicalMetadata,
                initialStateChecksum,
                initialLogChecksum,
                chain,
                RequiredString(root, "final_state_checksum"),
                RequiredString(root, "final_log_checksum"),
                terminal["is_terminal"]!.GetValue<bool>() ? "1" : "0",
                RequiredString(terminal, "outcome"),
                RequiredString(terminal, "end_reason"));
    }

    private static string RequiredString(JsonObject value, string propertyName) =>
        value[propertyName]!.GetValue<string>();

    private static void AssertLoadFailure(byte[] utf8, string reasonId)
    {
        var failure = Assert.Throws<ReplayValidationException>(() => Load(utf8));
        Assert.Equal(reasonId, failure.ReasonId);
    }

    private sealed record FixedReplayExecution(
        CoreDuelBattleSession InitialSession,
        CoreDuelBattleSession FinalSession,
        IReadOnlyList<CoreDuelBattleCommandResult> CommandResults);
}
