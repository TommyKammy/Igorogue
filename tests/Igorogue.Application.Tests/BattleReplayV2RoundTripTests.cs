using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Determinism;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

public sealed class BattleReplayV2RoundTripTests
{
    private const string ContentHash =
        "sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06";

    [Fact]
    public void AuthoritativeRuntimeCommandsRoundTripTwiceWithDeterministicBytes()
    {
        var execution = RuntimeExecution();
        var document = BattleReplayDocumentV2.Capture(
            execution.InitialSession,
            execution.CommandResults);
        var firstBytes = Save(document);
        var loaded = Load(firstBytes);
        var secondBytes = Save(loaded);
        var firstReplay = BattleReplayRunnerV2.Replay(
            loaded,
            execution.InitialSession);
        var secondReplay = BattleReplayRunnerV2.Replay(
            Load(secondBytes),
            execution.InitialSession);

        Assert.Equal(firstBytes, secondBytes);
        Assert.False(firstBytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        Assert.Equal((byte)'\n', firstBytes[^1]);
        Assert.Equal("headless-battle-state-v2", loaded.StateProjection);
        Assert.Equal(2, BattleReplaySerializerV2.SchemaVersion);
        Assert.Equal(3, loaded.Attempts.Count);
        Assert.Equal(
            [
                "battle.end_player_turn",
                "battle.authorized_runtime_stone_placement",
                "battle.resolve_enemy_pass",
            ],
            loaded.Attempts.Select(attempt => attempt.CommandType));
        Assert.Equal([true, true, false], loaded.Attempts.Select(attempt => attempt.Accepted));
        Assert.Contains(
            "effect_metadata_count=2\neffect_metadata=effect.alpha\neffect_metadata=effect.beta\n",
            loaded.Attempts[1].CanonicalPayload,
            StringComparison.Ordinal);
        Assert.Equal(
            execution.FinalSession.State.CanonicalText,
            firstReplay.FinalSession.State.CanonicalText);
        Assert.Equal(
            firstReplay.FinalSession.State.CanonicalText,
            secondReplay.FinalSession.State.CanonicalText);
        Assert.Equal(
            execution.FinalSession.CommandLog.CurrentChecksum,
            firstReplay.FinalSession.CommandLog.CurrentChecksum);
        Assert.Equal(2, firstReplay.FinalSession.CommandLog.Entries.Count);
        Assert.Equal(3, firstReplay.CommandResults.Count);
        Assert.False(firstReplay.CommandResults[^1].Accepted);
        Assert.Same(
            firstReplay.CommandResults[^1].SessionBefore,
            firstReplay.CommandResults[^1].SessionAfter);
    }

    [Fact]
    public void V2IntegrityDomainsBindSchemaAndStateProjection()
    {
        var execution = RuntimeExecution();
        var document = BattleReplayDocumentV2.Capture(
            execution.InitialSession,
            execution.CommandResults);
        var chain = DeterministicChecksum.Combine(
            "igorogue-battle-replay-attempt-chain-v2",
            "igorogue.battle-replay",
            "2",
            "headless-battle-state-v2",
            document.Metadata.ToCanonicalText(),
            document.InitialStateChecksum,
            document.InitialLogChecksum);
        foreach (var attempt in document.Attempts)
        {
            chain = DeterministicChecksum.Combine(
                "igorogue-battle-replay-attempt-v2",
                "igorogue.battle-replay",
                "2",
                "headless-battle-state-v2",
                chain,
                attempt.AttemptSequence.ToString(CultureInfo.InvariantCulture),
                attempt.CommandType,
                attempt.CommandSchemaVersion.ToString(CultureInfo.InvariantCulture),
                attempt.CanonicalPayload,
                attempt.BeforeStateChecksum,
                attempt.BeforeLogChecksum,
                attempt.Accepted ? "1" : "0",
                attempt.ReasonId,
                attempt.StateChecksum,
                attempt.LogChecksum);
            Assert.Equal(chain, attempt.AttemptChecksum);
        }

        Assert.Equal(chain, document.AttemptsChecksum);
        Assert.Equal(
            DeterministicChecksum.Combine(
                "igorogue-battle-replay-document-v2",
                "igorogue.battle-replay",
                "2",
                "headless-battle-state-v2",
                "sha256-length-prefixed-v1",
                document.Metadata.ToCanonicalText(),
                document.InitialStateChecksum,
                document.InitialLogChecksum,
                document.AttemptsChecksum,
                document.FinalStateChecksum,
                document.FinalLogChecksum,
                document.Terminal.IsTerminal ? "1" : "0",
                document.Terminal.OutcomeId,
                document.Terminal.EndReasonId),
            document.DocumentChecksum);
    }

    [Fact]
    public void V1AndV2LoadersAndRunnerRejectCrossVersionInputs()
    {
        var v2Execution = RuntimeExecution();
        var v2Document = BattleReplayDocumentV2.Capture(
            v2Execution.InitialSession,
            v2Execution.CommandResults);
        var v2Bytes = Save(v2Document);
        using (var v1Source = new MemoryStream(v2Bytes, writable: false))
        {
            Assert.Throws<ReplayValidationException>(() =>
                BattleReplaySerializer.Load(v1Source));
        }

        var v1Session = LegacyInitialSession();
        var v1Document = BattleReplayDocument.Capture(v1Session, []);
        byte[] v1Bytes;
        using (var stream = new MemoryStream())
        {
            BattleReplaySerializer.Save(v1Document, stream);
            v1Bytes = stream.ToArray();
        }

        var loadFailure = Assert.Throws<ReplayValidationException>(() =>
        {
            using var source = new MemoryStream(v1Bytes, writable: false);
            BattleReplaySerializerV2.Load(source);
        });
        Assert.Equal("unsupported_replay_schema", loadFailure.ReasonId);

        var captureFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplayDocumentV2.Capture(v1Session, []));
        Assert.Equal("unsupported_state_projection", captureFailure.ReasonId);
        var runnerFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplayRunnerV2.Replay(v2Document, v1Session));
        Assert.Equal("unsupported_state_projection", runnerFailure.ReasonId);

        var inverseCaptureFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplayDocument.Capture(v2Execution.InitialSession, []));
        Assert.Equal("unsupported_state_projection", inverseCaptureFailure.ReasonId);
        var inverseRunnerFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplayRunner.Replay(v1Document, v2Execution.InitialSession));
        Assert.Equal("unsupported_state_projection", inverseRunnerFailure.ReasonId);
    }

    [Fact]
    public void RejectedFacilityBuildAttemptRoundTripsWithoutImplementingTheCommand()
    {
        var initial = AuthoritativeInitialSession();
        var command = new AuthorizedFacilityBuildCommand(
            initial.State.Checksum,
            initial.CommandLog.CurrentChecksum,
            initial.State.Board.Geometry.CreateCanonicalPoint(4, 4),
            "facility.test",
            "facility.instance");
        var rejected = HeadlessBattleStateMachine.Execute(initial, command);

        Assert.False(rejected.Accepted);
        Assert.Equal("unsupported_command", rejected.ReasonId);
        Assert.Same(initial, rejected.SessionAfter);

        var loaded = Load(Save(BattleReplayDocumentV2.Capture(
            initial,
            [rejected])));
        var replay = BattleReplayRunnerV2.Replay(loaded, initial);

        var attempt = Assert.Single(loaded.Attempts);
        Assert.Equal("battle.authorized_facility_build", attempt.CommandType);
        Assert.False(attempt.Accepted);
        Assert.Equal("unsupported_command", attempt.ReasonId);
        var replayed = Assert.Single(replay.CommandResults);
        Assert.False(replayed.Accepted);
        Assert.Equal("unsupported_command", replayed.ReasonId);
        Assert.Same(replayed.SessionBefore, replayed.SessionAfter);
        Assert.Equal(initial.State.Checksum, replay.FinalSession.State.Checksum);
        Assert.Equal(
            initial.CommandLog.CurrentChecksum,
            replay.FinalSession.CommandLog.CurrentChecksum);
    }

    [Fact]
    public void LoaderRejectsSchemaProjectionShapeMetadataCommandAndIntegrityTampering()
    {
        var document = RuntimeDocument();

        AssertLoadFailure(
            Mutate(document, root => root["schema_id"] = "other.replay"),
            "unsupported_replay_schema");
        AssertLoadFailure(
            Mutate(document, root => root["schema_version"] = 1),
            "unsupported_replay_schema");
        AssertLoadFailure(
            Mutate(document, root => root["state_projection"] = "headless-battle-state-v1"),
            "unsupported_state_projection");
        AssertLoadFailure(
            Mutate(document, root => root.Remove("state_projection")),
            "malformed_replay");
        AssertLoadFailure(
            Mutate(document, root => root["integrity_scheme"] = "unknown-v2"),
            "unsupported_integrity_scheme");
        AssertLoadFailure(
            Mutate(document, root =>
                root["metadata"]!["rng_algorithm"] = "unknown-v1"),
            "unsupported_metadata");
        AssertLoadFailure(
            Mutate(document, root => root["unexpected"] = true),
            "malformed_replay");
        AssertLoadFailure(
            Mutate(document, root => root.Remove("document_checksum")),
            "malformed_replay");
        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![1]!["command_type"] =
                    "battle.unknown"),
            "unsupported_command_type");
        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![1]!["command_schema_version"] = 2),
            "unsupported_command_schema");
        AssertLoadFailure(
            Mutate(document, root =>
            {
                var payload = root["attempts"]![1]!["canonical_payload"]!.GetValue<string>();
                root["attempts"]![1]!["canonical_payload"] = payload.Replace(
                    "effect_metadata_count=2\n",
                    "effect_metadata_count=3\n",
                    StringComparison.Ordinal);
            }),
            "malformed_command_payload");
        AssertLoadFailure(
            Mutate(document, root =>
            {
                var payload = root["attempts"]![1]!["canonical_payload"]!.GetValue<string>();
                root["attempts"]![1]!["canonical_payload"] = payload.Replace(
                    "effect_metadata_count=2\n",
                    "effect_metadata_count=02\n",
                    StringComparison.Ordinal);
            }),
            "noncanonical_command_payload");
        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![0]!["attempt_checksum"] = new string('0', 64)),
            "attempt_chain_checksum_mismatch");
        AssertLoadFailure(
            Mutate(document, root => root["attempts_checksum"] = new string('0', 64)),
            "attempt_chain_checksum_mismatch");
        AssertLoadFailure(
            Mutate(document, root => root["document_checksum"] = new string('0', 64)),
            "document_checksum_mismatch");
        AssertLoadFailure(
            Mutate(document, root =>
            {
                root["terminal"]!["is_terminal"] = true;
                root["terminal"]!["outcome"] = "loss";
                root["terminal"]!["end_reason"] = "turn_limit";
            }),
            "document_checksum_mismatch");
    }

    [Fact]
    public void LoaderRejectsDuplicateTrailingDepthSizeAndAttemptCountBeforeMapping()
    {
        var valid = Save(RuntimeDocument());
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

        using (var oversized = new MemoryStream(
                   new byte[BattleReplaySerializerV2.MaxDocumentBytes + 1],
                   writable: false))
        {
            var exception = Assert.Throws<ReplayValidationException>(() =>
                BattleReplaySerializerV2.Load(oversized));
            Assert.Equal("replay_too_large", exception.ReasonId);
        }

        var tooMany = new StringBuilder("{\"attempts\":[{\"duplicate\":1,\"duplicate\":2}");
        for (var index = 1; index <= BattleReplaySerializerV2.MaxAttempts; index++)
        {
            tooMany.Append(",{}");
        }

        tooMany.Append("]}");
        AssertLoadFailure(
            Encoding.UTF8.GetBytes(tooMany.ToString()),
            "replay_too_many_attempts");
    }

    [Fact]
    public void CaptureAndSaveEnforceResourceLimitsWithoutPartialOutput()
    {
        var initial = AuthoritativeInitialSession();
        var rejected = HeadlessBattleStateMachine.Execute(
            initial,
            new ResolveEnemyPassCommand(
                initial.State.Checksum,
                initial.CommandLog.CurrentChecksum));
        Assert.False(rejected.Accepted);
        var attemptFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplayDocumentV2.Capture(
                initial,
                Enumerable.Repeat(
                    rejected,
                    BattleReplaySerializerV2.MaxAttempts + 1)));
        Assert.Equal("replay_too_many_attempts", attemptFailure.ReasonId);

        var oversizedMetadata = new string('a', BattleReplaySerializerV2.MaxDocumentBytes);
        var oversizedCommand = new AuthorizedRuntimeStonePlacementCommand(
            initial.State.Checksum,
            initial.CommandLog.CurrentChecksum,
            StoneColor.White,
            initial.State.Board.Geometry.CreateCanonicalPoint(4, 4),
            PlacementAccessMode.Normal,
            "oversized.instance",
            "standard",
            [oversizedMetadata]);
        var oversizedResult = HeadlessBattleStateMachine.Execute(initial, oversizedCommand);
        Assert.False(oversizedResult.Accepted);
        var oversizedDocument = BattleReplayDocumentV2.Capture(
            initial,
            [oversizedResult]);
        using var destination = new MemoryStream();
        destination.Write([1, 2, 3, 4]);
        var beforeBytes = destination.ToArray();
        var beforePosition = destination.Position;

        var saveFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplaySerializerV2.Save(oversizedDocument, destination));

        Assert.Equal("replay_too_large", saveFailure.ReasonId);
        Assert.Equal(beforeBytes, destination.ToArray());
        Assert.Equal(beforePosition, destination.Position);
    }

    [Fact]
    public void LoaderAcceptsDocumentAtExactUtf8ByteLimit()
    {
        var document = RuntimeDocument();
        var valid = Save(document);
        var exactLimit = Enumerable.Repeat(
            (byte)' ',
            BattleReplaySerializerV2.MaxDocumentBytes).ToArray();
        valid.CopyTo(exactLimit, 0);
        using var source = new MemoryStream(exactLimit, writable: false);

        var loaded = BattleReplaySerializerV2.Load(source);

        Assert.Equal(document.DocumentChecksum, loaded.DocumentChecksum);
        Assert.Equal(document.AttemptsChecksum, loaded.AttemptsChecksum);
    }

    [Fact]
    public void AttemptChainDetectsDeletionOfTrailingRejectedAttempt()
    {
        var document = RuntimeDocument();
        Assert.False(document.Attempts[^1].Accepted);
        Assert.Equal(
            document.Attempts[^1].BeforeStateChecksum,
            document.FinalStateChecksum);
        Assert.Equal(
            document.Attempts[^1].BeforeLogChecksum,
            document.FinalLogChecksum);

        AssertLoadFailure(
            Mutate(document, root => root["attempts"]!.AsArray().RemoveAt(
                root["attempts"]!.AsArray().Count - 1)),
            "attempt_chain_checksum_mismatch");
    }

    [Fact]
    public void ResignedSemanticDivergenceLoadsButRunnerFailsWithoutPartialResult()
    {
        var execution = RuntimeExecution();
        var document = BattleReplayDocumentV2.Capture(
            execution.InitialSession,
            execution.CommandResults);
        var root = JsonNode.Parse(Save(document))!.AsObject();
        var rejected = root["attempts"]!.AsArray()[^1]!.AsObject();
        var expectedState = rejected["before_state_checksum"]!.GetValue<string>();
        var expectedLog = rejected["before_log_checksum"]!.GetValue<string>();
        rejected["command_type"] = "battle.end_player_turn";
        rejected["canonical_payload"] =
            "end-player-turn-v1\n" +
            $"expected_state_checksum={expectedState}\n" +
            $"expected_log_checksum={expectedLog}\n";
        RecomputeReplayIntegrity(root, document.Metadata.ToCanonicalText());
        var forged = Load(Encoding.UTF8.GetBytes(root.ToJsonString()));
        var initialStateBefore = execution.InitialSession.State.Checksum;
        var initialLogBefore = execution.InitialSession.CommandLog.CurrentChecksum;
        BattleReplayResult? returned = null;

        var exception = Assert.Throws<ReplayValidationException>(() =>
        {
            returned = BattleReplayRunnerV2.Replay(
                forged,
                execution.InitialSession);
        });

        Assert.Equal("acceptance_mismatch", exception.ReasonId);
        Assert.Equal(2, exception.AttemptIndex);
        Assert.Null(returned);
        Assert.Equal(initialStateBefore, execution.InitialSession.State.Checksum);
        Assert.Equal(initialLogBefore, execution.InitialSession.CommandLog.CurrentChecksum);
    }

    [Fact]
    public void RunnerRejectsForeignMetadataInitialStateAndResignedTerminalResult()
    {
        var execution = RuntimeExecution();
        var document = BattleReplayDocumentV2.Capture(
            execution.InitialSession,
            execution.CommandResults);
        var foreignMetadata = AuthoritativeInitialSession(
            "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var metadataFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplayRunnerV2.Replay(document, foreignMetadata));
        Assert.Equal("metadata_mismatch", metadataFailure.ReasonId);

        var foreignState = AuthoritativeInitialSession(ContentHash, gaugeUnits: 1);
        var stateFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplayRunnerV2.Replay(document, foreignState));
        Assert.Equal("initial_state_checksum_mismatch", stateFailure.ReasonId);

        var root = JsonNode.Parse(Save(document))!.AsObject();
        root["terminal"]!["is_terminal"] = true;
        root["terminal"]!["outcome"] = "loss";
        root["terminal"]!["end_reason"] = "turn_limit";
        RecomputeReplayIntegrity(root, document.Metadata.ToCanonicalText());
        var terminalDrift = Load(Encoding.UTF8.GetBytes(root.ToJsonString()));
        var terminalFailure = Assert.Throws<ReplayValidationException>(() =>
            BattleReplayRunnerV2.Replay(
                terminalDrift,
                execution.InitialSession));
        Assert.Equal("terminal_mismatch", terminalFailure.ReasonId);
    }

    private static RuntimeReplayExecution RuntimeExecution()
    {
        var initial = AuthoritativeInitialSession();
        var endTurn = HeadlessBattleStateMachine.Execute(
            initial,
            new EndPlayerTurnCommand(
                initial.State.Checksum,
                initial.CommandLog.CurrentChecksum));
        var runtimePlacement = HeadlessBattleStateMachine.Execute(
            endTurn.SessionAfter,
            new AuthorizedRuntimeStonePlacementCommand(
                endTurn.StateChecksum,
                endTurn.LogChecksum,
                StoneColor.White,
                initial.State.Board.Geometry.CreateCanonicalPoint(4, 4),
                PlacementAccessMode.Normal,
                "runtime.white.01",
                "standard",
                ["effect.alpha", "effect.beta"]));
        var rejectedPass = HeadlessBattleStateMachine.Execute(
            runtimePlacement.SessionAfter,
            new ResolveEnemyPassCommand(
                runtimePlacement.StateChecksum,
                runtimePlacement.LogChecksum));
        Assert.True(endTurn.Accepted);
        Assert.True(runtimePlacement.Accepted);
        Assert.False(rejectedPass.Accepted);

        return new RuntimeReplayExecution(
            initial,
            rejectedPass.SessionAfter,
            [endTurn, runtimePlacement, rejectedPass]);
    }

    private static HeadlessBattleSession AuthoritativeInitialSession(
        string contentHash = ContentHash,
        int gaugeUnits = 0)
    {
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var board = BoardState.Create(geometry, []);
        var stones = StoneRuntimeState.Create(board, [], 1);
        var counterattackPolicy = new CounterattackBoundaryPolicy(200, 12, 3, 30);
        var snapshot = BattleAuthoritativeInitialSnapshot.Create(
            stones,
            TemporaryLibertyState.Create(stones, [], 1),
            ContinuousLibertySnapshot.Empty(stones),
            BattleRepetitionHistory.Start(board),
            FacilityState.Create(board, [], 1),
            ClosedWindowResourceState.Empty([]),
            CaptureBenefitTriggerPlan.Create([]),
            CounterattackBoundaryState.Create(
                gaugeUnits,
                false,
                0,
                counterattackPolicy),
            counterattackPolicy,
            RuntimePolicy(),
            playerTurnIndex: 1);
        return HeadlessBattleStateMachine.Start(snapshot, Metadata(contentHash));
    }

    private static HeadlessBattleSession LegacyInitialSession()
    {
        var board = BoardState.Create(
            BoardGeometry.Create(BoardGeometry.AcceptedSize),
            []);
        return HeadlessBattleStateMachine.Start(
            board,
            FacilityState.Create(board, [], 1),
            RuntimePolicy(),
            Metadata());
    }

    private static BattleRuntimePolicy RuntimePolicy() =>
        new(
            20,
            FacilityRuntimePolicy.Create(
                territoryIncomeDivisor: 3,
                capacityBands: [new FacilityCapacityBand(1, 49, 1)],
                slotCap: 5,
                typeLimits: [new KeyValuePair<string, int>("default", 1)]));

    private static ReplayMetadata Metadata(string contentHash = ContentHash) =>
        ReplayMetadata.Create("v0.2.10", contentHash, 42);

    private static BattleReplayDocumentV2 RuntimeDocument()
    {
        var execution = RuntimeExecution();
        return BattleReplayDocumentV2.Capture(
            execution.InitialSession,
            execution.CommandResults);
    }

    private static byte[] Save(BattleReplayDocumentV2 document)
    {
        using var stream = new MemoryStream();
        BattleReplaySerializerV2.Save(document, stream);
        return stream.ToArray();
    }

    private static BattleReplayDocumentV2 Load(byte[] utf8)
    {
        using var stream = new MemoryStream(utf8, writable: false);
        return BattleReplaySerializerV2.Load(stream);
    }

    private static byte[] Mutate(
        BattleReplayDocumentV2 document,
        Action<JsonObject> mutation)
    {
        var root = JsonNode.Parse(Save(document))?.AsObject()
            ?? throw new InvalidOperationException("Serialized replay did not contain a JSON object.");
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
        var chain = DeterministicChecksum.Combine(
            "igorogue-battle-replay-attempt-chain-v2",
            schemaId,
            schemaVersion,
            stateProjection,
            canonicalMetadata,
            initialStateChecksum,
            initialLogChecksum);
        var attempts = root["attempts"]!.AsArray();
        foreach (var node in attempts)
        {
            var attempt = node!.AsObject();
            chain = DeterministicChecksum.Combine(
                "igorogue-battle-replay-attempt-v2",
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
        root["document_checksum"] = DeterministicChecksum.Combine(
            "igorogue-battle-replay-document-v2",
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
        var exception = Assert.Throws<ReplayValidationException>(() => Load(utf8));
        Assert.Equal(reasonId, exception.ReasonId);
    }

    private sealed record RuntimeReplayExecution(
        HeadlessBattleSession InitialSession,
        HeadlessBattleSession FinalSession,
        IReadOnlyList<BattleCommandResult> CommandResults);
}
