namespace Igorogue.Application.Tests;

public sealed class GoldenBoardFixtureTests
{
    private const string ContentHash =
        "sha256:b411ddf2dfb8e876370d11f2259368b7d898fcfebe8a4e4fb24c30802968ee06";

    private static readonly ExpectedCaseMapping[] RequiredMappings =
    [
        new("CORE-INITIAL-01", "unit_case:tests/Igorogue.Domain.Tests/InitialPositionFixtureTests.cs#Coord10StandardPositionMatchesDataAndRoleAwarePointSymmetry", "exact_unit_case", "initial_state", "exact"),
        new("CORE-SIMULTANEOUS-CAPTURE-01", "unit_case:tests/Igorogue.Domain.Tests/HypotheticalPlacementResolverTests.cs#TwoOpponentGroupsAreRemovedTogetherInAnchorAndStoneOrder", "exact_unit_case", "canonical_replay", "exact"),
        new("CORE-TERMINAL-CAPTURE-01", "unit_case:tests/Igorogue.Application.Tests/HeadlessBattleStateMachineTests.cs#WhiteKingCaptureEndsInVictoryAndSuppressesPostTerminalBenefits", "exact_unit_case", "canonical_replay", "semantic_equivalent"),
        new("KO-01", "fixture:repetition:KO-01", "exact_fixture", "canonical_replay", "exact"),
        new("KO-02", "fixture:repetition:KO-02", "exact_fixture", "canonical_replay", "exact"),
        new("KO-03", "fixture:repetition:KO-03", "exact_fixture", "canonical_replay", "metadata_normalized"),
        new("KO-04", "fixture:repetition:KO-04", "exact_fixture", "canonical_replay", "metadata_normalized"),
        new("KO-05", "fixture:repetition:KO-05", "exact_fixture", "canonical_replay", "exact"),
        new("KO-06", "fixture:repetition:KO-06", "exact_fixture", "canonical_replay", "exact"),
        new("KO-07", "fixture:repetition:KO-07", "exact_fixture", "adapter_plus_replay", "silent_filter"),
        new("FAC-01", "fixture:facility:FAC-01", "exact_fixture", "initial_state", "exact"),
        new("FAC-02", "fixture:facility:FAC-02", "exact_fixture", "initial_state", "exact"),
        new("FAC-03", "fixture:facility:FAC-03", "exact_fixture", "canonical_replay", "exact"),
        new("FAC-04", "fixture:facility:FAC-04", "exact_fixture", "canonical_replay", "exact"),
        new("FAC-05", "fixture:facility:FAC-05", "exact_fixture", "canonical_replay", "semantic_equivalent"),
        new("FAC-06", "fixture:facility:FAC-06", "exact_fixture", "initial_state", "exact"),
        new("FAC-07", "fixture:facility:FAC-07", "exact_fixture", "initial_state", "exact"),
        new("FAC-08", "fixture:facility:FAC-08", "exact_fixture", "canonical_replay", "exact"),
        new("FAC-09", "fixture:facility:FAC-09", "exact_fixture", "canonical_replay", "exact"),
    ];

    [Fact]
    public void CatalogContractIsVersionedLinkedAndExplicitAboutEvidence()
    {
        var catalog = GoldenBoardFixtureAdapter.Load();

        Assert.Equal(GoldenBoardFixtureAdapter.SchemaId, catalog.SchemaId);
        Assert.Equal(GoldenBoardFixtureAdapter.SchemaVersion, catalog.SchemaVersion);
        Assert.Equal(
            GoldenBoardFixtureAdapter.FactProjectionVersion,
            catalog.FactProjection);
        Assert.Equal("v0.2.10", catalog.GameVersion);
        Assert.Equal(ContentHash, catalog.ContentHash);
        Assert.Equal(20, catalog.RuntimePolicy.PlayerTurnLimit);
        Assert.Equal(3, catalog.RuntimePolicy.TerritoryIncomeDivisor);
        Assert.Equal(5, catalog.RuntimePolicy.SlotCap);
        Assert.Equal(
            new[] { "1-3=1", "4-7=2", "8-12=3", "13-49=4" },
            catalog.RuntimePolicy.CapacityBands.Select(band =>
                $"{band.Min}-{band.Max}={band.Slots}"));
        Assert.Equal(
            new[] { "default=1", "development=2", "furnace=2" },
            catalog.RuntimePolicy.TypeLimits
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value}"));

        Assert.Equal(
            RequiredMappings.Select(mapping => mapping.Id).Order(StringComparer.Ordinal),
            catalog.Cases.Select(fixture => fixture.Id).Order(StringComparer.Ordinal));
        Assert.Equal(
            catalog.Cases.Count,
            catalog.Cases.Select(fixture => fixture.Id).Distinct(StringComparer.Ordinal).Count());

        var sources = catalog.SourceCatalogs.ToDictionary(source => source.Id, StringComparer.Ordinal);
        Assert.Equal(new[] { "facility", "repetition" }, sources.Keys.Order(StringComparer.Ordinal));
        foreach (var source in sources.Values)
        {
            Assert.Matches("^[0-9a-f]{64}$", source.Sha256);
            Assert.Equal(
                source.Sha256,
                GoldenBoardFixtureAdapter.Sha256ForRepositoryFile(source.Path));
        }

        var sourceFixtureIds = sources.ToDictionary(
            pair => pair.Key,
            pair => GoldenBoardFixtureAdapter.LoadSourceFixtureIds(pair.Value),
            StringComparer.Ordinal);
        foreach (var fixture in catalog.Cases)
        {
            var mapping = Assert.Single(
                RequiredMappings,
                candidate => candidate.Id == fixture.Id);
            Assert.False(string.IsNullOrWhiteSpace(fixture.Title));
            Assert.Equal(42, fixture.Seed);
            Assert.Contains(fixture.Evidence.Domain, new[] { "exact_fixture", "exact_unit_case" });
            Assert.Contains(
                fixture.Evidence.Application,
                new[] { "initial_state", "canonical_replay", "adapter_plus_replay" });
            Assert.Contains(
                fixture.Evidence.Relation,
                new[] { "exact", "metadata_normalized", "silent_filter", "semantic_equivalent" });
            Assert.Matches("^[0-9a-f]{64}$", fixture.Initial.StateChecksum);
            Assert.Matches("^[0-9a-f]{64}$", fixture.Initial.LogChecksum);

            if (fixture.Source.Kind == "fixture")
            {
                var sourceId = Assert.IsType<string>(fixture.Source.Catalog);
                var fixtureId = Assert.IsType<string>(fixture.Source.FixtureId);
                Assert.Contains(sourceId, sources.Keys);
                Assert.Contains(fixtureId, sourceFixtureIds[sourceId]);
                Assert.Equal(
                    mapping.Source,
                    $"fixture:{sourceId}:{fixtureId}");
            }
            else
            {
                Assert.Equal("unit_case", fixture.Source.Kind);
                var sourcePath = Assert.IsType<string>(fixture.Source.Path);
                var testCase = Assert.IsType<string>(fixture.Source.TestCase);
                Assert.True(
                    GoldenBoardFixtureAdapter.RepositoryFileContains(sourcePath, testCase),
                    $"Golden unit source '{sourcePath}' does not contain '{testCase}'.");
                Assert.Equal(
                    mapping.Source,
                    $"unit_case:{sourcePath}#{testCase}");
            }

            Assert.Equal(mapping.Domain, fixture.Evidence.Domain);
            Assert.Equal(mapping.Application, fixture.Evidence.Application);
            Assert.Equal(mapping.Relation, fixture.Evidence.Relation);

            if (fixture.Evidence.Relation == "exact")
            {
                Assert.Empty(fixture.Evidence.Limitations);
            }
            else
            {
                Assert.NotEmpty(fixture.Evidence.Limitations);
            }

            if (fixture.Evidence.Application == "initial_state")
            {
                Assert.Empty(fixture.Steps);
            }

            foreach (var step in fixture.Steps)
            {
                Assert.Matches("^[0-9a-f]{64}$", step.BeforeStateChecksum);
                Assert.Matches("^[0-9a-f]{64}$", step.BeforeLogChecksum);
                Assert.Matches("^[0-9a-f]{64}$", step.Expected.StateChecksum);
                Assert.Matches("^[0-9a-f]{64}$", step.Expected.LogChecksum);
                if (step.Expected.Accepted == false)
                {
                    Assert.Equal(
                        new[] { $"command_rejected|reason={step.Expected.Reason}" },
                        step.Expected.OrderedFacts);
                }
                else if (step.Expected.Accepted is null)
                {
                    Assert.Equal("adapter.silent_candidate_filter", step.Type);
                    Assert.Empty(step.Expected.OrderedFacts);
                }
            }
        }

        AssertEvidence(
            catalog,
            "KO-03",
            "canonical_replay",
            "metadata_normalized");
        AssertEvidence(
            catalog,
            "KO-04",
            "canonical_replay",
            "metadata_normalized");
        AssertEvidence(
            catalog,
            "KO-07",
            "adapter_plus_replay",
            "silent_filter");
        AssertEvidence(
            catalog,
            "FAC-05",
            "canonical_replay",
            "semantic_equivalent");

        var ko07 = Assert.Single(catalog.Cases, fixture => fixture.Id == "KO-07");
        var silentIndex = ko07.Steps
            .Select((step, index) => (step, index))
            .Single(item => item.step.Type == "adapter.silent_candidate_filter")
            .index;
        var silent = ko07.Steps[silentIndex];
        var chosen = Assert.Single(
            Assert.IsAssignableFrom<IReadOnlyList<GoldenCandidate>>(silent.Candidates),
            candidate => candidate.ExpectedLegal);
        Assert.Equal(silent.ExpectedChosenPoint, chosen.Point);
        var submitted = ko07.Steps[silentIndex + 1];
        Assert.Equal("battle.authorized_stone_placement", submitted.Type);
        Assert.Equal("white", submitted.Actor);
        Assert.Equal(silent.ExpectedChosenPoint, submitted.Point);

        AssertSourceNormalization(catalog, sources, "KO-03", []);
        AssertSourceNormalization(
            catalog,
            sources,
            "KO-04",
            ["brilliant_multiplier", "cards_drawn", "facility_destroyed", "qi_delta"]);
        Assert.All(
            catalog.Cases.Where(fixture =>
                fixture.Id != "KO-03" && fixture.Id != "KO-04"),
            fixture => Assert.Null(fixture.SourceNormalization));
    }

    [Fact]
    public void EveryGoldenCaseMatchesAllAttemptedBoundariesAndTerminalResult()
    {
        var catalog = GoldenBoardFixtureAdapter.Load();

        foreach (var fixture in catalog.Cases)
        {
            var actual = GoldenBoardFixtureAdapter.Run(
                catalog,
                fixture,
                reverseSetupEnumeration: false);

            AssertMatchesFixture(fixture, actual);
        }
    }

    [Fact]
    public void ReversedSetupEnumerationAndSecondRunProduceIdenticalEvidence()
    {
        var catalog = GoldenBoardFixtureAdapter.Load();

        foreach (var fixture in catalog.Cases)
        {
            var first = GoldenBoardFixtureAdapter.Run(
                catalog,
                fixture,
                reverseSetupEnumeration: false);
            var second = GoldenBoardFixtureAdapter.Run(
                catalog,
                fixture,
                reverseSetupEnumeration: false);
            var reversed = GoldenBoardFixtureAdapter.Run(
                catalog,
                fixture,
                reverseSetupEnumeration: true);

            AssertRunsEqual(first, second);
            AssertRunsEqual(first, reversed);
        }
    }

    private static void AssertMatchesFixture(
        GoldenBoardCase fixture,
        GoldenRunResult actual)
    {
        Assert.Equal(fixture.Initial.StateChecksum, actual.InitialStateChecksum);
        Assert.Equal(fixture.Initial.LogChecksum, actual.InitialLogChecksum);
        Assert.Equal(fixture.Steps.Count, actual.Boundaries.Count);

        for (var index = 0; index < fixture.Steps.Count; index++)
        {
            var step = fixture.Steps[index];
            var expected = step.Expected;
            var boundary = actual.Boundaries[index];

            Assert.Equal(expected.Accepted, boundary.Accepted);
            Assert.Equal(expected.Reason, boundary.Reason);
            Assert.Equal(expected.StateChecksum, boundary.StateChecksum);
            Assert.Equal(expected.LogChecksum, boundary.LogChecksum);
            Assert.Equal(expected.OrderedFacts, boundary.OrderedFacts);

            if (expected.Accepted == true)
            {
                Assert.False(boundary.ExactNoOp);
                Assert.Equal(boundary.LogCountBefore + 1, boundary.LogCountAfter);
            }
            else
            {
                Assert.True(boundary.ExactNoOp);
                Assert.Equal(boundary.LogCountBefore, boundary.LogCountAfter);
            }
        }

        Assert.Equal(fixture.Terminal.IsTerminal, actual.Terminal.IsTerminal);
        Assert.Equal(fixture.Terminal.Outcome, actual.Terminal.Outcome);
        Assert.Equal(fixture.Terminal.EndReason, actual.Terminal.EndReason);
    }

    private static void AssertRunsEqual(GoldenRunResult expected, GoldenRunResult actual)
    {
        Assert.Equal(expected.InitialStateChecksum, actual.InitialStateChecksum);
        Assert.Equal(expected.InitialLogChecksum, actual.InitialLogChecksum);
        Assert.Equal(expected.Boundaries.Count, actual.Boundaries.Count);
        for (var index = 0; index < expected.Boundaries.Count; index++)
        {
            var expectedBoundary = expected.Boundaries[index];
            var actualBoundary = actual.Boundaries[index];
            Assert.Equal(expectedBoundary.Accepted, actualBoundary.Accepted);
            Assert.Equal(expectedBoundary.Reason, actualBoundary.Reason);
            Assert.Equal(expectedBoundary.StateChecksum, actualBoundary.StateChecksum);
            Assert.Equal(expectedBoundary.LogChecksum, actualBoundary.LogChecksum);
            Assert.Equal(expectedBoundary.OrderedFacts, actualBoundary.OrderedFacts);
            Assert.Equal(expectedBoundary.ExactNoOp, actualBoundary.ExactNoOp);
            Assert.Equal(expectedBoundary.LogCountBefore, actualBoundary.LogCountBefore);
            Assert.Equal(expectedBoundary.LogCountAfter, actualBoundary.LogCountAfter);
        }

        Assert.Equal(expected.Terminal.IsTerminal, actual.Terminal.IsTerminal);
        Assert.Equal(expected.Terminal.Outcome, actual.Terminal.Outcome);
        Assert.Equal(expected.Terminal.EndReason, actual.Terminal.EndReason);
        Assert.Equal(
            expected.FinalSession.State.Checksum,
            actual.FinalSession.State.Checksum);
        Assert.Equal(
            expected.FinalSession.CommandLog.CurrentChecksum,
            actual.FinalSession.CommandLog.CurrentChecksum);
    }

    private static void AssertEvidence(
        GoldenBoardCatalog catalog,
        string fixtureId,
        string application,
        string relation)
    {
        var fixture = Assert.Single(catalog.Cases, item => item.Id == fixtureId);
        Assert.Equal(application, fixture.Evidence.Application);
        Assert.Equal(relation, fixture.Evidence.Relation);
        Assert.NotEmpty(fixture.Evidence.Limitations);
    }

    private static void AssertSourceNormalization(
        GoldenBoardCatalog catalog,
        IReadOnlyDictionary<string, GoldenSourceCatalog> sources,
        string fixtureId,
        IReadOnlyList<string> expectedOmittedFields)
    {
        var fixture = Assert.Single(catalog.Cases, item => item.Id == fixtureId);
        var normalization = Assert.IsType<GoldenSourceNormalization>(
            fixture.SourceNormalization);
        Assert.Equal(
            "canonical_basic_stone_placement",
            normalization.ApplicationProjection);
        Assert.Equal(expectedOmittedFields, normalization.OmittedFields);

        var sourceId = Assert.IsType<string>(fixture.Source.Catalog);
        var sourceFixtureId = Assert.IsType<string>(fixture.Source.FixtureId);
        var source = GoldenBoardFixtureAdapter.LoadSourceFixture(
            sources[sourceId],
            sourceFixtureId);
        Assert.Equal(
            source.GetProperty("stone_kind").GetString(),
            normalization.SourceStoneKind);

        var sourceMetadata = source.TryGetProperty(
            "non_stone_state_changes",
            out var metadata)
            ? metadata.EnumerateObject().ToDictionary(
                property => property.Name,
                property => property.Value.GetRawText(),
                StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        Assert.Equal(
            sourceMetadata.Keys.Order(StringComparer.Ordinal),
            normalization.SourceNonTopologyMetadata.Keys.Order(StringComparer.Ordinal));
        foreach (var pair in sourceMetadata)
        {
            Assert.Equal(
                pair.Value,
                normalization.SourceNonTopologyMetadata[pair.Key].GetRawText());
        }
    }

    private sealed record ExpectedCaseMapping(
        string Id,
        string Source,
        string Domain,
        string Application,
        string Relation);
}
