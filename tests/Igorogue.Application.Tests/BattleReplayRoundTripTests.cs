using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Determinism;

namespace Igorogue.Application.Tests;

public sealed class BattleReplayRoundTripTests
{
    private const string OtherContentHash =
        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void EveryGoldenApplicationCommandRoundTripsWithFactsAndDeterministicBytes()
    {
        var catalog = GoldenBoardFixtureAdapter.Load();
        var totalAttempts = 0;
        var totalAccepted = 0;
        var totalRejected = 0;
        var zeroCommandCases = 0;

        foreach (var fixture in catalog.Cases)
        {
            var source = GoldenBoardFixtureAdapter.Run(
                catalog,
                fixture,
                reverseSetupEnumeration: false);
            var document = BattleReplayDocument.Capture(
                source.InitialSession,
                source.CommandResults);
            var firstBytes = Save(document);
            var loaded = Load(firstBytes);
            var secondBytes = Save(loaded);
            var replay = BattleReplayRunner.Replay(loaded, source.InitialSession);
            var secondReplay = BattleReplayRunner.Replay(
                Load(secondBytes),
                source.InitialSession);
            var reversedSource = GoldenBoardFixtureAdapter.Run(
                catalog,
                fixture,
                reverseSetupEnumeration: true);
            var reversedReplay = BattleReplayRunner.Replay(
                loaded,
                reversedSource.InitialSession);
            var expectedSteps = fixture.Steps
                .Where(step => step.Type.StartsWith("battle.", StringComparison.Ordinal))
                .ToArray();

            Assert.Equal(firstBytes, secondBytes);
            Assert.False(firstBytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
            Assert.Equal((byte)'\n', firstBytes[^1]);
            Assert.DoesNotContain(
                "ordered_facts",
                Encoding.UTF8.GetString(firstBytes),
                StringComparison.Ordinal);
            Assert.Equal(expectedSteps.Length, document.Attempts.Count);
            Assert.Equal(expectedSteps.Length, replay.CommandResults.Count);
            Assert.Equal(expectedSteps.Length, reversedReplay.CommandResults.Count);
            Assert.Equal(source.FinalSession.State.CanonicalText, replay.FinalSession.State.CanonicalText);
            Assert.Equal(replay.FinalSession.State.CanonicalText, secondReplay.FinalSession.State.CanonicalText);
            Assert.Equal(source.FinalSession.State.Checksum, replay.FinalSession.State.Checksum);
            Assert.Equal(
                source.FinalSession.CommandLog.CurrentChecksum,
                replay.FinalSession.CommandLog.CurrentChecksum);
            Assert.Equal(
                replay.FinalSession.CommandLog.CurrentChecksum,
                secondReplay.FinalSession.CommandLog.CurrentChecksum);
            Assert.Equal(replay.FinalSession.State.Checksum, reversedReplay.FinalSession.State.Checksum);
            Assert.Equal(
                replay.FinalSession.CommandLog.CurrentChecksum,
                reversedReplay.FinalSession.CommandLog.CurrentChecksum);
            Assert.Equal(source.Terminal.IsTerminal, replay.FinalSession.State.IsTerminal);
            Assert.Equal(source.Terminal.Outcome, replay.FinalSession.State.OutcomeId);
            Assert.Equal(source.Terminal.EndReason, replay.FinalSession.State.EndReasonId);

            for (var index = 0; index < expectedSteps.Length; index++)
            {
                var expected = expectedSteps[index].Expected;
                var actual = replay.CommandResults[index];
                var second = secondReplay.CommandResults[index];
                var reversed = reversedReplay.CommandResults[index];
                Assert.Equal(expected.Accepted, actual.Accepted);
                Assert.Equal(expected.Reason, actual.ReasonId);
                Assert.Equal(expected.StateChecksum, actual.StateChecksum);
                Assert.Equal(expected.LogChecksum, actual.LogChecksum);
                Assert.Equal(
                    expected.OrderedFacts,
                    actual.OrderedFacts.Select(GoldenBoardFixtureAdapter.ProjectFact));
                Assert.Equal(actual.Accepted, second.Accepted);
                Assert.Equal(actual.ReasonId, second.ReasonId);
                Assert.Equal(actual.StateChecksum, second.StateChecksum);
                Assert.Equal(actual.LogChecksum, second.LogChecksum);
                Assert.Equal(
                    actual.OrderedFacts.Select(GoldenBoardFixtureAdapter.ProjectFact),
                    second.OrderedFacts.Select(GoldenBoardFixtureAdapter.ProjectFact));
                Assert.Equal(actual.Accepted, reversed.Accepted);
                Assert.Equal(actual.ReasonId, reversed.ReasonId);
                Assert.Equal(actual.StateChecksum, reversed.StateChecksum);
                Assert.Equal(actual.LogChecksum, reversed.LogChecksum);
                Assert.Equal(
                    actual.OrderedFacts.Select(GoldenBoardFixtureAdapter.ProjectFact),
                    reversed.OrderedFacts.Select(GoldenBoardFixtureAdapter.ProjectFact));
            }

            Assert.Equal(
                document.Attempts.Count(attempt => attempt.Accepted),
                replay.FinalSession.CommandLog.Entries.Count);
            Assert.Matches("^[0-9a-f]{64}$", document.AttemptsChecksum);
            Assert.Matches("^[0-9a-f]{64}$", document.DocumentChecksum);
            if (document.Attempts.Count > 0)
            {
                Assert.Equal(document.Attempts[^1].AttemptChecksum, document.AttemptsChecksum);
            }
            else
            {
                zeroCommandCases++;
            }

            totalAttempts += document.Attempts.Count;
            totalAccepted += document.Attempts.Count(attempt => attempt.Accepted);
            totalRejected += document.Attempts.Count(attempt => !attempt.Accepted);
        }

        Assert.Equal(19, catalog.Cases.Count);
        Assert.Equal(34, totalAttempts);
        Assert.Equal(28, totalAccepted);
        Assert.Equal(6, totalRejected);
        Assert.Equal(5, zeroCommandCases);
    }

    [Fact]
    public void Ko07SilentFilterIsNotSerializedAsAnApplicationCommand()
    {
        var catalog = GoldenBoardFixtureAdapter.Load();
        var fixture = Assert.Single(catalog.Cases, fixture => fixture.Id == "KO-07");
        var source = GoldenBoardFixtureAdapter.Run(
            catalog,
            fixture,
            reverseSetupEnumeration: false);
        var document = BattleReplayDocument.Capture(
            source.InitialSession,
            source.CommandResults);

        Assert.Equal(4, fixture.Steps.Count);
        var silent = Assert.Single(
            fixture.Steps.Select((step, index) => (step, index)),
            item => item.step.Type == "adapter.silent_candidate_filter");
        Assert.Equal(3, document.Attempts.Count);
        Assert.DoesNotContain(
            document.Attempts,
            attempt => attempt.CommandType.StartsWith("adapter.", StringComparison.Ordinal));
        var submitted = fixture.Steps[silent.index + 1];
        Assert.Equal("battle.authorized_stone_placement", submitted.Type);
        Assert.Equal("white", submitted.Actor);
        Assert.Equal(silent.step.ExpectedChosenPoint, submitted.Point);
        var submittedAttemptIndex = fixture.Steps
            .Take(silent.index + 2)
            .Count(step => step.Type.StartsWith("battle.", StringComparison.Ordinal)) - 1;
        var selectedPoint = Assert.IsAssignableFrom<IReadOnlyList<int>>(
            silent.step.ExpectedChosenPoint);
        Assert.Contains(
            $"actor=white\npoint={selectedPoint[0]},{selectedPoint[1]}\n",
            document.Attempts[submittedAttemptIndex].CanonicalPayload,
            StringComparison.Ordinal);

        var replay = BattleReplayRunner.Replay(Load(Save(document)), source.InitialSession);
        Assert.Equal(source.FinalSession.State.Checksum, replay.FinalSession.State.Checksum);
    }

    [Fact]
    public void AttemptSequenceRemainsDistinctFromAcceptedLogSequenceAcrossRejection()
    {
        var initial = InitialSession("CORE-INITIAL-01");
        var geometry = initial.State.Board.Geometry;
        var first = ExecuteStone(initial, geometry.CreateCanonicalPoint(1, 1));
        var rejected = ExecuteStone(first.SessionAfter, geometry.CreateCanonicalPoint(1, 1));
        var third = ExecuteStone(rejected.SessionAfter, geometry.CreateCanonicalPoint(2, 1));

        Assert.True(first.Accepted);
        Assert.False(rejected.Accepted);
        Assert.Equal("stone_occupied", rejected.ReasonId);
        Assert.True(third.Accepted);

        var document = BattleReplayDocument.Capture(initial, [first, rejected, third]);
        var loaded = Load(Save(document));
        var replay = BattleReplayRunner.Replay(loaded, initial);

        Assert.Equal([0, 1, 2], loaded.Attempts.Select(attempt => attempt.AttemptSequence));
        Assert.Equal([true, false, true], loaded.Attempts.Select(attempt => attempt.Accepted));
        Assert.Equal([0L, 1L], replay.FinalSession.CommandLog.Entries.Select(entry => entry.Sequence));
        Assert.Equal(3, replay.CommandResults.Count);
        Assert.False(replay.CommandResults[1].Accepted);
        Assert.Same(
            replay.CommandResults[1].SessionBefore,
            replay.CommandResults[1].SessionAfter);
    }

    [Fact]
    public void ForgedSemanticDivergenceLoadsButReplayFailsWithoutReturningPartialResult()
    {
        var initial = InitialSession("CORE-INITIAL-01");
        var geometry = initial.State.Board.Geometry;
        var first = ExecuteStone(initial, geometry.CreateCanonicalPoint(1, 1));
        var rejected = ExecuteStone(first.SessionAfter, geometry.CreateCanonicalPoint(1, 1));
        var third = ExecuteStone(rejected.SessionAfter, geometry.CreateCanonicalPoint(2, 1));
        var document = BattleReplayDocument.Capture(initial, [first, rejected, third]);
        var root = JsonNode.Parse(Save(document))!.AsObject();
        var attempts = root["attempts"]!.AsArray();
        var rejectedPayload = attempts[1]!["canonical_payload"]!.GetValue<string>();
        attempts[1]!["canonical_payload"] = rejectedPayload.Replace(
            "point=1,1\n",
            "point=3,1\n",
            StringComparison.Ordinal);
        RecomputeReplayIntegrity(root, document.Metadata.ToCanonicalText());
        var forged = Load(Encoding.UTF8.GetBytes(root.ToJsonString()));
        var stateBefore = initial.State.Checksum;
        var logBefore = initial.CommandLog.CurrentChecksum;
        BattleReplayResult? returned = null;

        var exception = Assert.Throws<ReplayValidationException>(() =>
        {
            returned = BattleReplayRunner.Replay(forged, initial);
        });

        Assert.Equal("acceptance_mismatch", exception.ReasonId);
        Assert.Equal(1, exception.AttemptIndex);
        Assert.Null(returned);
        Assert.Equal(stateBefore, initial.State.Checksum);
        Assert.Equal(logBefore, initial.CommandLog.CurrentChecksum);
    }

    [Fact]
    public void EnemyPassUsesTheFourthStrictCommandCodec()
    {
        var initial = InitialSession("CORE-INITIAL-01");
        var endedTurn = HeadlessBattleStateMachine.Execute(
            initial,
            new EndPlayerTurnCommand(
                initial.State.Checksum,
                initial.CommandLog.CurrentChecksum));
        var enemyPass = HeadlessBattleStateMachine.Execute(
            endedTurn.SessionAfter,
            new ResolveEnemyPassCommand(
                endedTurn.StateChecksum,
                endedTurn.LogChecksum));
        var document = BattleReplayDocument.Capture(initial, [endedTurn, enemyPass]);

        var replay = BattleReplayRunner.Replay(Load(Save(document)), initial);

        Assert.Equal(
            ["battle.end_player_turn", "battle.resolve_enemy_pass"],
            document.Attempts.Select(attempt => attempt.CommandType));
        Assert.Equal(2, replay.CommandResults.Count);
        Assert.Contains(
            replay.CommandResults[1].OrderedFacts,
            fact => GoldenBoardFixtureAdapter.ProjectFact(fact).StartsWith(
                "enemy_passed|",
                StringComparison.Ordinal));
    }

    [Fact]
    public void LoaderReadsShortNonSeekableStreamsAndLeavesStreamsOpen()
    {
        var document = GoldenDocument("KO-02");
        using var destination = new MemoryStream();

        BattleReplaySerializer.Save(document, destination);

        Assert.True(destination.CanWrite);
        destination.WriteByte((byte)' ');
        var bytes = destination.ToArray()[..^1];
        using var source = new ChunkedReadStream(bytes, maxChunk: 3);
        var loaded = BattleReplaySerializer.Load(source);

        Assert.False(source.WasDisposed);
        Assert.False(source.CanSeek);
        Assert.Equal(document.DocumentChecksum, loaded.DocumentChecksum);
        Assert.Equal(document.AttemptsChecksum, loaded.AttemptsChecksum);
    }

    [Fact]
    public void LoaderRejectsSchemaMetadataShapeAndTrailingData()
    {
        var document = GoldenDocument("KO-02");

        AssertLoadFailure(
            Mutate(document, root => root["schema_id"] = "other.replay"),
            "unsupported_replay_schema");
        AssertLoadFailure(
            Mutate(document, root => root["schema_version"] = 2),
            "unsupported_replay_schema");
        AssertLoadFailure(
            Mutate(document, root => root["integrity_scheme"] = "unknown-v1"),
            "unsupported_integrity_scheme");
        AssertLoadFailure(
            Mutate(document, root =>
                root["metadata"]!["rng_algorithm"] = "unknown-v1"),
            "unsupported_metadata");
        AssertLoadFailure(
            Mutate(document, root =>
                root["metadata"]!["command_log_schema"] = 2),
            "unsupported_metadata");
        AssertLoadFailure(
            Mutate(document, root =>
                root["metadata"]!["checksum_scheme"] = "unknown-v1"),
            "unsupported_metadata");
        AssertLoadFailure(
            Mutate(document, root => root["unexpected"] = true),
            "malformed_replay");
        AssertLoadFailure(
            Mutate(document, root => root.Remove("document_checksum")),
            "malformed_replay");
        AssertLoadFailure(
            Mutate(document, root => root["terminal"] = null),
            "malformed_replay");

        var validText = Encoding.UTF8.GetString(Save(document));
        AssertLoadFailure(
            Encoding.UTF8.GetBytes(validText + "{}"),
            "malformed_replay");
        var duplicate = validText.Insert(
            validText.IndexOf('{') + 1,
            "\n  \"schema_id\": \"igorogue.battle-replay\",");
        AssertLoadFailure(
            Encoding.UTF8.GetBytes(duplicate),
            "duplicate_json_property");
    }

    [Fact]
    public void LoaderRejectsDeepLongDuplicatePropertyWithBoundedDiagnostic()
    {
        const int depth = 48;
        var longName = new string('x', 32768);
        var json = new StringBuilder((longName.Length + 5) * depth);
        for (var index = 0; index < depth; index++)
        {
            json.Append("{\"").Append(longName).Append("\":");
        }

        json.Append("{\"duplicate\":1,\"duplicate\":2}");
        json.Append('}', depth);

        var exception = Assert.Throws<ReplayValidationException>(() =>
            Load(Encoding.UTF8.GetBytes(json.ToString())));

        Assert.Equal("duplicate_json_property", exception.ReasonId);
        Assert.True(exception.Message.Length < 200);
    }

    [Fact]
    public void LoaderRejectsNewlineFloodWithoutExpandingCommandLines()
    {
        var document = GoldenDocument("KO-02");
        var newlineFlood = new string('\n', 500000);

        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![0]!["canonical_payload"] = newlineFlood),
            "malformed_command_payload");
    }

    [Fact]
    public void LoaderRejectsCommandCodecAndIntegrityTampering()
    {
        var document = GoldenDocument("KO-02");

        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![1]!["attempt_sequence"] = 7),
            "invalid_attempt_sequence");
        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![0]!["command_type"] = "adapter.silent_candidate_filter"),
            "unsupported_command_type");
        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![0]!["command_schema_version"] = 2),
            "unsupported_command_schema");
        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![0]!["canonical_payload"] = "not-canonical\n"),
            "malformed_command_payload");
        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![0]!["attempt_checksum"] = new string('0', 64)),
            "attempt_chain_checksum_mismatch");
        AssertLoadFailure(
            Mutate(document, root => root["attempts_checksum"] = new string('0', 64)),
            "attempt_chain_checksum_mismatch");
        AssertLoadFailure(
            Mutate(document, root =>
            {
                var attempts = root["attempts"]!.AsArray();
                attempts[attempts.Count - 1]!["accepted"] = true;
            }),
            "acceptance_reason_mismatch");
        AssertLoadFailure(
            Mutate(document, root =>
            {
                var attempts = root["attempts"]!.AsArray();
                attempts[attempts.Count - 1]!["reason_id"] = "different_rejection";
            }),
            "attempt_chain_checksum_mismatch");
        AssertLoadFailure(
            Mutate(document, root => root["initial_state_checksum"] = new string('0', 64)),
            "boundary_chain_mismatch");
        AssertLoadFailure(
            Mutate(document, root => root["initial_log_checksum"] = new string('0', 64)),
            "command_log_checksum_mismatch");
        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![0]!["before_state_checksum"] = new string('0', 64)),
            "boundary_chain_mismatch");
        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![0]!["before_log_checksum"] = new string('0', 64)),
            "boundary_chain_mismatch");
        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![0]!["state_checksum"] = new string('0', 64)),
            "command_log_checksum_mismatch");
        AssertLoadFailure(
            Mutate(document, root =>
                root["attempts"]![0]!["log_checksum"] = new string('0', 64)),
            "command_log_checksum_mismatch");
        AssertLoadFailure(
            Mutate(document, root => root["final_state_checksum"] = new string('0', 64)),
            "final_checksum_mismatch");
        AssertLoadFailure(
            Mutate(document, root => root["final_log_checksum"] = new string('0', 64)),
            "final_checksum_mismatch");
    }

    [Fact]
    public void AttemptChainDetectsDeletionOfTrailingRejectedAttempt()
    {
        var document = GoldenDocument("KO-02");
        Assert.False(document.Attempts[^1].Accepted);
        Assert.Equal(
            document.Attempts[^1].BeforeStateChecksum,
            document.FinalStateChecksum);
        Assert.Equal(
            document.Attempts[^1].BeforeLogChecksum,
            document.FinalLogChecksum);

        var deleted = Mutate(document, root =>
        {
            var attempts = root["attempts"]!.AsArray();
            attempts.RemoveAt(attempts.Count - 1);
        });

        AssertLoadFailure(deleted, "attempt_chain_checksum_mismatch");
    }

    [Fact]
    public void DocumentChecksumProtectsZeroCommandFinalAndTerminalFields()
    {
        var document = GoldenDocument("CORE-INITIAL-01");
        Assert.Empty(document.Attempts);

        AssertLoadFailure(
            Mutate(document, root => root["final_state_checksum"] = new string('0', 64)),
            "final_checksum_mismatch");
        AssertLoadFailure(
            Mutate(document, root =>
            {
                root["terminal"]!["is_terminal"] = true;
                root["terminal"]!["outcome"] = "loss";
                root["terminal"]!["end_reason"] = "turn_limit";
            }),
            "document_checksum_mismatch");
        AssertLoadFailure(
            Mutate(document, root => root["document_checksum"] = new string('0', 64)),
            "document_checksum_mismatch");
    }

    [Fact]
    public void LoaderRejectsDocumentsOverTheUtf8ByteLimit()
    {
        using var oversized = new MemoryStream(
            new byte[BattleReplaySerializer.MaxDocumentBytes + 1],
            writable: false);

        var exception = Assert.Throws<ReplayValidationException>(
            () => BattleReplaySerializer.Load(oversized));

        Assert.Equal("replay_too_large", exception.ReasonId);
    }

    [Fact]
    public void LoaderAcceptsDocumentAtExactUtf8ByteLimit()
    {
        var document = GoldenDocument("CORE-INITIAL-01");
        var valid = Save(document);
        var exactLimit = Enumerable.Repeat(
            (byte)' ',
            BattleReplaySerializer.MaxDocumentBytes).ToArray();
        valid.CopyTo(exactLimit, 0);
        using var source = new MemoryStream(exactLimit, writable: false);

        var loaded = BattleReplaySerializer.Load(source);

        Assert.Equal(document.DocumentChecksum, loaded.DocumentChecksum);
    }

    [Fact]
    public void SaveRejectsOversizedValidDocumentWithoutWritingDestination()
    {
        var initial = InitialSession("CORE-INITIAL-01");
        var endedTurn = HeadlessBattleStateMachine.Execute(
            initial,
            new EndPlayerTurnCommand(
                initial.State.Checksum,
                initial.CommandLog.CurrentChecksum));
        var oversizedId = new string('a', BattleReplaySerializer.MaxDocumentBytes);
        var rejected = HeadlessBattleStateMachine.Execute(
            endedTurn.SessionAfter,
            new AuthorizedFacilityBuildCommand(
                endedTurn.StateChecksum,
                endedTurn.LogChecksum,
                endedTurn.SessionAfter.State.Board.Geometry.CreateCanonicalPoint(1, 1),
                oversizedId,
                "oversized_instance"));
        Assert.False(rejected.Accepted);
        Assert.Equal("wrong_phase", rejected.ReasonId);
        var document = BattleReplayDocument.Capture(initial, [endedTurn, rejected]);
        using var destination = new MemoryStream();
        destination.Write([1, 2, 3, 4]);
        var beforeBytes = destination.ToArray();
        var beforePosition = destination.Position;

        var exception = Assert.Throws<ReplayValidationException>(
            () => BattleReplaySerializer.Save(document, destination));

        Assert.Equal("replay_too_large", exception.ReasonId);
        Assert.Equal(beforeBytes, destination.ToArray());
        Assert.Equal(beforePosition, destination.Position);
    }

    [Fact]
    public void LoaderRejectsAttemptCountAboveResourceLimitBeforeMappingEntries()
    {
        var root = JsonNode.Parse(Save(GoldenDocument("CORE-INITIAL-01")))!.AsObject();
        var attempts = root["attempts"]!.AsArray();
        for (var index = 0; index <= BattleReplaySerializer.MaxAttempts; index++)
        {
            attempts.Add(new JsonObject());
        }

        var exception = Assert.Throws<ReplayValidationException>(
            () => Load(Encoding.UTF8.GetBytes(root.ToJsonString())));

        Assert.Equal("replay_too_many_attempts", exception.ReasonId);
    }

    [Fact]
    public void CaptureRejectsAttemptCountAboveResourceLimit()
    {
        var initial = InitialSession("CORE-INITIAL-01");
        var rejected = HeadlessBattleStateMachine.Execute(
            initial,
            new ResolveEnemyPassCommand(
                initial.State.Checksum,
                initial.CommandLog.CurrentChecksum));
        Assert.False(rejected.Accepted);
        Assert.Same(rejected.SessionBefore, rejected.SessionAfter);

        var exception = Assert.Throws<ReplayValidationException>(() =>
            BattleReplayDocument.Capture(
                initial,
                Enumerable.Repeat(rejected, BattleReplaySerializer.MaxAttempts + 1)));

        Assert.Equal("replay_too_many_attempts", exception.ReasonId);
    }

    [Fact]
    public void RunnerRejectsForeignMetadataAndInitialStateWithoutMutatingCallerSession()
    {
        var canonical = InitialSession("CORE-INITIAL-01");
        var foreign = HeadlessBattleStateMachine.Start(
            canonical.State.Board,
            canonical.State.FacilityState,
            canonical.State.RuntimePolicy,
            ReplayMetadata.Create(
                canonical.CommandLog.Metadata.GameVersion,
                OtherContentHash,
                canonical.CommandLog.Metadata.InitialSeed));
        var foreignDocument = BattleReplayDocument.Capture(foreign, []);
        var stateBefore = canonical.State.Checksum;
        var logBefore = canonical.CommandLog.CurrentChecksum;

        var metadataFailure = Assert.Throws<ReplayValidationException>(
            () => BattleReplayRunner.Replay(foreignDocument, canonical));
        Assert.Equal("metadata_mismatch", metadataFailure.ReasonId);

        var catalog = GoldenBoardFixtureAdapter.Load();
        var otherFixture = catalog.Cases
            .Where(fixture => fixture.Seed == canonical.CommandLog.Metadata.InitialSeed)
            .Select(fixture => GoldenBoardFixtureAdapter.Run(
                catalog,
                fixture,
                reverseSetupEnumeration: false))
            .First(run => run.InitialSession.State.Checksum != canonical.State.Checksum);
        var canonicalDocument = BattleReplayDocument.Capture(canonical, []);
        var stateFailure = Assert.Throws<ReplayValidationException>(
            () => BattleReplayRunner.Replay(canonicalDocument, otherFixture.InitialSession));
        Assert.Equal("initial_state_checksum_mismatch", stateFailure.ReasonId);
        Assert.Equal(stateBefore, canonical.State.Checksum);
        Assert.Equal(logBefore, canonical.CommandLog.CurrentChecksum);
    }

    private static BattleCommandResult ExecuteStone(
        HeadlessBattleSession session,
        CanonicalPoint point) =>
        HeadlessBattleStateMachine.Execute(
            session,
            new AuthorizedStonePlacementCommand(
                session.State.Checksum,
                session.CommandLog.CurrentChecksum,
                StoneColor.Black,
                point,
                PlacementAccessMode.Normal));

    private static HeadlessBattleSession InitialSession(string fixtureId)
    {
        var catalog = GoldenBoardFixtureAdapter.Load();
        var fixture = Assert.Single(catalog.Cases, fixture => fixture.Id == fixtureId);
        return GoldenBoardFixtureAdapter.Run(
            catalog,
            fixture,
            reverseSetupEnumeration: false).InitialSession;
    }

    private static BattleReplayDocument GoldenDocument(string fixtureId)
    {
        var catalog = GoldenBoardFixtureAdapter.Load();
        var fixture = Assert.Single(catalog.Cases, fixture => fixture.Id == fixtureId);
        var source = GoldenBoardFixtureAdapter.Run(
            catalog,
            fixture,
            reverseSetupEnumeration: false);
        return BattleReplayDocument.Capture(source.InitialSession, source.CommandResults);
    }

    private static byte[] Save(BattleReplayDocument document)
    {
        using var stream = new MemoryStream();
        BattleReplaySerializer.Save(document, stream);
        return stream.ToArray();
    }

    private static BattleReplayDocument Load(byte[] utf8)
    {
        using var stream = new MemoryStream(utf8, writable: false);
        return BattleReplaySerializer.Load(stream);
    }

    private static byte[] Mutate(
        BattleReplayDocument document,
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
        var initialStateChecksum = RequiredString(root, "initial_state_checksum");
        var initialLogChecksum = RequiredString(root, "initial_log_checksum");
        var chain = DeterministicChecksum.Combine(
            "igorogue-battle-replay-attempt-chain-v1",
            canonicalMetadata,
            initialStateChecksum,
            initialLogChecksum);
        var attempts = root["attempts"]!.AsArray();
        foreach (var node in attempts)
        {
            var attempt = node!.AsObject();
            chain = DeterministicChecksum.Combine(
                "igorogue-battle-replay-attempt-v1",
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
            "igorogue-battle-replay-document-v1",
            RequiredString(root, "schema_id"),
            root["schema_version"]!.GetValue<int>().ToString(CultureInfo.InvariantCulture),
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

    private sealed class ChunkedReadStream : Stream
    {
        private readonly byte[] bytes;
        private readonly int maxChunk;
        private int position;

        internal ChunkedReadStream(byte[] bytes, int maxChunk)
        {
            this.bytes = bytes;
            this.maxChunk = maxChunk;
        }

        internal bool WasDisposed { get; private set; }

        public override bool CanRead => !WasDisposed;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(WasDisposed, this);
            var remaining = bytes.Length - position;
            var read = Math.Min(Math.Min(count, maxChunk), remaining);
            if (read > 0)
            {
                Array.Copy(bytes, position, buffer, offset, read);
                position += read;
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
