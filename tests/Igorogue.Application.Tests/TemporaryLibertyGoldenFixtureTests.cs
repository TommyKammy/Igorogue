using Igorogue.Application.Battle;
using Igorogue.Application.Replay;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Application.Tests;

public sealed class TemporaryLibertyGoldenFixtureTests
{
    private static readonly string[] RequiredIds = Enumerable.Range(1, 15)
        .Select(index => $"TLE-{index:00}")
        .ToArray();

    private static readonly string[] FullPhaseOrder =
    [
        "enemy_normal_action",
        "enemy_counterattack_action",
        "consume_current_pending_and_reprime_overflow",
        "temporary_liberty_expiry_sweep",
        "enemy_turn_end_counterattack_gain",
        "plan_next_intents",
    ];

    [Fact]
    public void CatalogMatchesCurrentProductionExecutionOrUpdatesOnlyByOptIn()
    {
        var current = TemporaryLibertyGoldenFixtureAdapter.BuildCurrentCatalog();
        if (StringComparer.Ordinal.Equals(
                Environment.GetEnvironmentVariable("IGOROGUE_UPDATE_TLE_GOLDEN"),
                "1"))
        {
            File.WriteAllText(
                TemporaryLibertyGoldenFixtureAdapter.CatalogPath(),
                TemporaryLibertyGoldenFixtureAdapter.SerializeCatalog(current));
            return;
        }

        var stored = TemporaryLibertyGoldenFixtureAdapter.LoadCatalog();
        Assert.Equal(
            TemporaryLibertyGoldenFixtureAdapter.SerializeCatalog(stored),
            TemporaryLibertyGoldenFixtureAdapter.SerializeCatalog(current));
    }

    [Fact]
    public void CatalogIsVersionedSourceBoundAndExplicitAboutExcludedClaims()
    {
        var catalog = TemporaryLibertyGoldenFixtureAdapter.LoadCatalog();
        var sources = TemporaryLibertyGoldenFixtureAdapter.LoadSourceCases();

        Assert.Equal(TemporaryLibertyGoldenFixtureAdapter.SchemaId, catalog.SchemaId);
        Assert.Equal(TemporaryLibertyGoldenFixtureAdapter.SchemaVersion, catalog.SchemaVersion);
        Assert.Equal(
            TemporaryLibertyGoldenFixtureAdapter.StateProjectionId,
            catalog.StateProjection);
        Assert.Equal(
            TemporaryLibertyGoldenFixtureAdapter.FactProjectionId,
            catalog.FactProjection);
        Assert.Equal(TemporaryLibertyGoldenFixtureAdapter.GameVersion, catalog.GameVersion);
        Assert.Equal(TemporaryLibertyGoldenFixtureAdapter.ContentHash, catalog.ContentHash);
        Assert.Equal(0, catalog.Claims.MomentumEventCount);
        Assert.Equal(0, catalog.Claims.BrilliantEventCount);
        Assert.Equal(0, catalog.Claims.CounterattackFixtureCoverageClaimCount);

        Assert.Equal(RequiredIds, sources.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(RequiredIds, catalog.Cases.Select(item => item.Id));
        Assert.Equal(
            catalog.SourceCatalogs.Count,
            catalog.SourceCatalogs.Select(item => item.Path)
                .Distinct(StringComparer.Ordinal).Count());
        foreach (var source in catalog.SourceCatalogs)
        {
            Assert.Matches("^[0-9a-f]{64}$", source.Sha256);
            Assert.Equal(
                source.Sha256,
                TemporaryLibertyGoldenFixtureAdapter.Sha256ForRepositoryFile(source.Path));
        }

        Assert.Contains(
            catalog.SourceCatalogs,
            source => source.Path ==
                TemporaryLibertyGoldenFixtureAdapter.RelativeSourcePath);
        foreach (var fixture in catalog.Cases)
        {
            Assert.Equal(fixture.Id, fixture.SourceFixtureId);
            Assert.Equal(TemporaryLibertyGoldenFixtureAdapter.Seed, fixture.Seed);
            Assert.Equal(0, fixture.MomentumEventCount);
            Assert.Equal(0, fixture.BrilliantEventCount);
            Assert.Equal(0, fixture.CounterattackFixtureCoverageClaimCount);
            Assert.InRange(fixture.Final.StandardCaptureRewardsClaimed, 0, 3);
            Assert.Matches("^[0-9a-f]{64}$", fixture.Initial.SnapshotChecksum);
            Assert.Matches("^[0-9a-f]{64}$", fixture.Initial.StateChecksum);
            Assert.Matches("^[0-9a-f]{64}$", fixture.Initial.LogChecksum);
            Assert.Equal("battle.end_player_turn", fixture.Commands[0].Type);
            Assert.All(fixture.Commands, command =>
            {
                Assert.Equal(1, command.SchemaVersion);
                Assert.True(command.Accepted);
                Assert.Equal("accepted", command.Reason);
                Assert.Matches("^[0-9a-f]{64}$", command.StateChecksum);
                Assert.Matches("^[0-9a-f]{64}$", command.LogChecksum);
                Assert.Contains(
                    $"expected_state_checksum=",
                    command.CanonicalPayload,
                    StringComparison.Ordinal);
            });

            Assert.Equal(0, fixture.MomentumEventCount);
            Assert.DoesNotContain(
                fixture.Commands.SelectMany(command => command.OrderedFacts),
                fact => fact.StartsWith(
                    "momentum_",
                    StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(
                fixture.Commands.SelectMany(command => command.OrderedFacts),
                fact => fact.StartsWith(
                    "brilliant_",
                    StringComparison.OrdinalIgnoreCase));
        }

        var phaseOnly = Assert.Single(catalog.Cases, item => item.Id == "TLE-14");
        Assert.Equal("phase_boundary_adapter", phaseOnly.Adapter);
        Assert.Equal(FullPhaseOrder, phaseOnly.StageTrace);
        Assert.Equal(3, phaseOnly.Commands.Count);
        Assert.All(
            catalog.Cases.Where(item => item.Id != "TLE-14"),
            item => Assert.Equal("expiry_sweep_adapter", item.Adapter));
    }

    [Fact]
    public void SameRunTwiceAndReversedSetupProduceIdenticalEvidence()
    {
        foreach (var fixtureId in RequiredIds)
        {
            var first = TemporaryLibertyGoldenFixtureAdapter.ExecuteCase(fixtureId);
            var second = TemporaryLibertyGoldenFixtureAdapter.ExecuteCase(fixtureId);
            var reversed = TemporaryLibertyGoldenFixtureAdapter.ExecuteCase(
                fixtureId,
                reverseSetupEnumeration: true);

            AssertRunsEqual(first, second);
            AssertRunsEqual(first, reversed);
        }
    }

    [Fact]
    public void EveryTleCaseRoundTripsThroughReplayV2Twice()
    {
        foreach (var fixtureId in RequiredIds)
        {
            var run = TemporaryLibertyGoldenFixtureAdapter.ExecuteCase(fixtureId);
            var document = BattleReplayDocumentV2.Capture(
                run.InitialSession,
                run.CommandResults);
            byte[] firstBytes;
            using (var destination = new MemoryStream())
            {
                BattleReplaySerializerV2.Save(document, destination);
                firstBytes = destination.ToArray();
            }

            BattleReplayDocumentV2 loaded;
            using (var source = new MemoryStream(firstBytes, writable: false))
            {
                loaded = BattleReplaySerializerV2.Load(source);
            }

            byte[] secondBytes;
            using (var destination = new MemoryStream())
            {
                BattleReplaySerializerV2.Save(loaded, destination);
                secondBytes = destination.ToArray();
            }

            var firstReplay = BattleReplayRunnerV2.Replay(
                loaded,
                run.InitialSession);
            BattleReplayDocumentV2 reloaded;
            using (var source = new MemoryStream(secondBytes, writable: false))
            {
                reloaded = BattleReplaySerializerV2.Load(source);
            }

            var secondReplay = BattleReplayRunnerV2.Replay(
                reloaded,
                run.InitialSession);

            Assert.Equal(firstBytes, secondBytes);
            Assert.Equal(
                run.FinalSession.State.CanonicalText,
                firstReplay.FinalSession.State.CanonicalText);
            Assert.Equal(
                firstReplay.FinalSession.State.CanonicalText,
                secondReplay.FinalSession.State.CanonicalText);
            Assert.Equal(
                run.FinalSession.CommandLog.CurrentChecksum,
                firstReplay.FinalSession.CommandLog.CurrentChecksum);
            Assert.Equal(run.Boundaries.Count, firstReplay.CommandResults.Count);
            for (var index = 0; index < run.Boundaries.Count; index++)
            {
                Assert.Equal(
                    run.Boundaries[index].OrderedFacts,
                    firstReplay.CommandResults[index].OrderedFacts
                        .Select(TemporaryLibertyGoldenFixtureAdapter.ProjectFact));
            }
        }
    }

    [Fact]
    public void Tle11HistoryComesFromObservedBoardsAndMandatoryCaptureIsNotBlocked()
    {
        var run = TemporaryLibertyGoldenFixtureAdapter.ExecuteCase("TLE-11");

        Assert.Equal(2, run.InitialSession.State.RepetitionHistory.ObservationCount);
        Assert.Equal(3, run.FinalSession.State.RepetitionHistory.ObservationCount);
        Assert.Contains(
            run.ProjectedFacts,
            fact => fact.StartsWith(
                "topology_registered|",
                StringComparison.Ordinal) &&
                fact.Contains("first_seen=0", StringComparison.Ordinal));
        Assert.All(run.CommandResults, result => Assert.True(result.Accepted));
    }

    [Fact]
    public void Tle13HasBoundaryTraceButNoDomainSweepEvents()
    {
        var run = TemporaryLibertyGoldenFixtureAdapter.ExecuteCase("TLE-13");

        Assert.DoesNotContain(
            run.CommandResults.SelectMany(result => result.OrderedFacts),
            fact => fact is TemporaryLibertyExpirySweepStartedFact or
                TemporaryLibertyExpiredFact or
                TemporaryLibertyExpirySweepResolvedFact);
        Assert.Contains("temporary_liberty_expiry_sweep", run.StageTrace);
    }

    [Fact]
    public void Tle12PublishesIncomeTerritoryAsMomentumIneligibleExpiryEvidence()
    {
        var run = TemporaryLibertyGoldenFixtureAdapter.ExecuteCase("TLE-12");
        var point = run.FinalSession.State.Board.Geometry.CreateCanonicalPoint(4, 4);
        var territory = Assert.Single(
            run.CommandResults.SelectMany(result => result.OrderedFacts)
                .OfType<TerritoryEstablishedFact>());

        Assert.Equal(StoneColor.White, territory.SourceActor);
        Assert.Equal(
            TerritoryEstablishmentSourceKind.TemporaryLibertyExpiry,
            territory.SourceKind);
        Assert.Equal("temporary_liberty_expired", territory.SourceReasonId);
        Assert.False(territory.ImplicitMomentumEligible);
        Assert.Equal([point], territory.ChangedPoints);

        var region = Assert.IsType<TerritoryRegion>(
            run.FinalSession.State.TerritoryAnalysis.RegionAt(point));
        Assert.Equal(TerritoryOwner.Black, region.Owner);
        Assert.Equal(1, region.Size);
        Assert.Equal(
            1,
            run.FinalSession.State.FacilityRuntimeAnalysis.RegionAt(point)!.BasicIncome);

        var facts = run.CommandResults.SelectMany(result => result.OrderedFacts).ToArray();
        var territoryIndex = Array.FindIndex(
            facts,
            fact => ReferenceEquals(fact, territory));
        var resolvedIndex = Array.FindIndex(
            facts,
            fact => fact is TemporaryLibertyExpirySweepResolvedFact);
        Assert.InRange(territoryIndex, 0, resolvedIndex - 1);
        Assert.DoesNotContain(
            facts,
            fact => fact.GetType().Name.Contains("Momentum", StringComparison.Ordinal) ||
                fact.GetType().Name.Contains("Brilliant", StringComparison.Ordinal));
    }

    [Fact]
    public void Tle14UsesOnlyPassCommandsAndEmitsExactTypedStageOrder()
    {
        var run = TemporaryLibertyGoldenFixtureAdapter.ExecuteCase("TLE-14");

        Assert.Equal(FullPhaseOrder, run.StageTrace);
        Assert.Equal(
            [
                "battle.end_player_turn",
                "battle.resolve_enemy_pass",
                "battle.resolve_enemy_pass",
            ],
            run.CommandResults.Select(result => result.Command.CommandType));
        Assert.DoesNotContain(
            run.CommandResults.SelectMany(result => result.OrderedFacts),
            fact => fact.GetType().Name.Contains(
                "IntentPlanned",
                StringComparison.Ordinal));
    }

    [Fact]
    public void ClosedWindowAndCounterattackCasesReachAcceptedExactValues()
    {
        var tle09 = RuntimeAfter("TLE-09");
        Assert.Equal(7, tle09.ClosedWindowResources.TurnReservedDraw);
        Assert.Equal(1, tle09.ClosedWindowResources.Soul);
        Assert.Equal(2, tle09.CounterattackState.SacrificeStoneRemainder);
        Assert.Equal(42, tle09.CounterattackState.GaugeUnits);

        var tle10 = RuntimeAfter("TLE-10");
        Assert.Equal(2, tle10.ClosedWindowResources.TurnReservedDraw);
        Assert.Equal(3, tle10.ClosedWindowResources.TurnReservedQi);
        Assert.Equal(1, tle10.ClosedWindowResources.Soul);
        Assert.Equal(
            ["seal_bone:qi_or_draw"],
            tle10.ClosedWindowResources.DeferredPlayerChoices.Select(choice => choice.Id));
        Assert.True(tle10.ClosedWindowResources.IsFirstUseConsumed(
            "capture_chain.armed"));
        Assert.Equal(52, tle10.CounterattackState.GaugeUnits);

        var tle15Run = TemporaryLibertyGoldenFixtureAdapter.ExecuteCase("TLE-15");
        var tle15 = Assert.IsType<BattleAuthoritativeRuntimeState>(
            tle15Run.FinalSession.State.AuthoritativeRuntime);
        Assert.Equal(2, tle15.CounterattackState.GaugeUnits);
        Assert.True(tle15.CounterattackState.Pending);
        Assert.Equal(0, tle15.CounterattackState.SacrificeStoneRemainder);
        var advanceReasons = tle15Run.CommandResults
            .SelectMany(result => result.OrderedFacts)
            .OfType<CounterattackAdvancedFact>()
            .Select(fact => fact.ReasonId)
            .ToArray();
        Assert.Equal(["sacrifice_batch", "enemy_turn_end"], advanceReasons);
    }

    [Theory]
    [InlineData("TLE-07", "black_king_captured")]
    [InlineData("TLE-08", "both_kings_captured")]
    public void TerminalExpirySuppressesBenefitsAndNaturalGain(
        string fixtureId,
        string expectedReason)
    {
        var run = TemporaryLibertyGoldenFixtureAdapter.ExecuteCase(fixtureId);

        Assert.True(run.FinalSession.State.IsTerminal);
        Assert.Equal("loss", run.FinalSession.State.OutcomeId);
        Assert.Equal(expectedReason, run.FinalSession.State.EndReasonId);
        Assert.Empty(
            run.CommandResults.SelectMany(result => result.OrderedFacts)
                .OfType<ICaptureBenefitAppliedFact>());
        Assert.Empty(
            run.CommandResults.SelectMany(result => result.OrderedFacts)
                .OfType<CounterattackAdvancedFact>());
    }

    private static BattleAuthoritativeRuntimeState RuntimeAfter(string fixtureId) =>
        Assert.IsType<BattleAuthoritativeRuntimeState>(
            TemporaryLibertyGoldenFixtureAdapter.ExecuteCase(fixtureId)
                .FinalSession.State.AuthoritativeRuntime);

    private static void AssertRunsEqual(
        TemporaryLibertyGoldenRunResult expected,
        TemporaryLibertyGoldenRunResult actual)
    {
        Assert.Equal(expected.InitialSnapshot.CanonicalText, actual.InitialSnapshot.CanonicalText);
        Assert.Equal(expected.InitialSnapshot.Checksum, actual.InitialSnapshot.Checksum);
        Assert.Equal(
            expected.InitialSession.State.Checksum,
            actual.InitialSession.State.Checksum);
        Assert.Equal(
            expected.InitialSession.CommandLog.CurrentChecksum,
            actual.InitialSession.CommandLog.CurrentChecksum);
        Assert.Equal(expected.Boundaries.Count, actual.Boundaries.Count);
        for (var index = 0; index < expected.Boundaries.Count; index++)
        {
            var expectedBoundary = expected.Boundaries[index];
            var actualBoundary = actual.Boundaries[index];
            Assert.Equal(expectedBoundary.CommandType, actualBoundary.CommandType);
            Assert.Equal(
                expectedBoundary.CommandSchemaVersion,
                actualBoundary.CommandSchemaVersion);
            Assert.Equal(expectedBoundary.CanonicalPayload, actualBoundary.CanonicalPayload);
            Assert.Equal(expectedBoundary.Accepted, actualBoundary.Accepted);
            Assert.Equal(expectedBoundary.Reason, actualBoundary.Reason);
            Assert.Equal(expectedBoundary.StateChecksum, actualBoundary.StateChecksum);
            Assert.Equal(expectedBoundary.LogChecksum, actualBoundary.LogChecksum);
            Assert.Equal(expectedBoundary.OrderedFacts, actualBoundary.OrderedFacts);
        }

        Assert.Equal(expected.ProjectedFacts, actual.ProjectedFacts);
        Assert.Equal(expected.StageTrace, actual.StageTrace);
        Assert.Equal(
            expected.FinalSession.State.CanonicalText,
            actual.FinalSession.State.CanonicalText);
        Assert.Equal(
            expected.FinalSession.CommandLog.CurrentChecksum,
            actual.FinalSession.CommandLog.CurrentChecksum);
    }
}
