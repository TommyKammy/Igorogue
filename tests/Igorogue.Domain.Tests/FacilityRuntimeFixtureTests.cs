using Igorogue.Domain.Board;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Tests;

public sealed class FacilityRuntimeFixtureTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, FacilityFixture>> Fixtures =
        new(FacilityFixtureData.LoadFixtures);
    private static readonly Lazy<FacilityRuntimePolicy> Policy =
        new(FacilityFixtureData.LoadRuntimePolicy);

    [Theory]
    [InlineData("FAC-01")]
    [InlineData("FAC-02")]
    [InlineData("FAC-06")]
    [InlineData("FAC-07")]
    public void InspectFixturesMatchProductionFacilityAnalysis(string fixtureId)
    {
        var fixture = RequiredFixture(fixtureId);
        var expected = Assert.IsType<FacilityFixtureTerritory>(fixture.Expected.Territory);
        var queryPoint = Assert.IsType<CanonicalPoint>(fixture.TerritoryPoint);
        var analysis = Analyze(fixture, out _);
        var region = Assert.IsType<FacilityRegionRuntimeAnalysis>(analysis.RegionAt(queryPoint));

        Assert.Equal(expected.Owner, region.Region.Owner);
        Assert.Equal(expected.Size, region.Region.Size);
        Assert.Equal(expected.BasicIncome, region.BasicIncome);
        Assert.Equal(expected.ConstructionCapacity, region.ConstructionCapacity);
        if (expected.InstalledCount is not null)
        {
            Assert.Equal(expected.InstalledCount, region.InstalledCount);
        }

        if (expected.IsOverCapacity is not null)
        {
            Assert.Equal(expected.IsOverCapacity, region.IsOverCapacity);
        }

        AssertFacilityStates(fixture, analysis);
        foreach (var pair in fixture.Expected.FacilityOwners)
        {
            Assert.Equal(pair.Value, analysis.FacilityState.FacilityById(pair.Key)?.Owner);
        }

        if (fixture.Expected.GroupLiberties.Count > 0)
        {
            var groupPoint = Assert.IsType<CanonicalPoint>(fixture.GroupPoint);
            Assert.Equal(
                fixture.Expected.GroupLiberties,
                StoneGroupAnalyzer.Analyze(fixture.Board).GroupAt(groupPoint)?.RealLiberties);
        }
    }

    [Theory]
    [InlineData("FAC-03")]
    [InlineData("FAC-04")]
    public void PlacementFixturesMatchLegalCommitAndFacilityDestruction(string fixtureId)
    {
        var fixture = RequiredFixture(fixtureId);
        var state = FacilityFixtureData.CreateInitialState(fixture);
        var stateBefore = state.ToCanonicalText();
        var history = BattleRepetitionHistory.Start(fixture.Board);
        var candidate = ResolvePlacement(
            fixture.Board,
            Assert.IsType<StoneColor>(fixture.Actor),
            Assert.IsType<CanonicalPoint>(fixture.Point));
        var evaluation = PlacementLegalityEvaluator.Evaluate(
            candidate,
            RealLiberties(candidate.GroupsAfterCapture),
            history,
            PlacementAccessMode.Normal);

        Assert.Equal(fixture.Expected.Legal, evaluation.IsLegal);
        Assert.Equal(fixture.Expected.Reason, evaluation.ReasonId);
        if (!evaluation.IsLegal)
        {
            Assert.Throws<InvalidOperationException>(() =>
                history.CommitLegalPlacement(evaluation));
            Assert.Equal(stateBefore, state.ToCanonicalText());
            Assert.Equal(fixture.Expected.RemainingFacilities, InstanceIds(state));
            Assert.Empty(fixture.Expected.DestroyedFacilities);
            Assert.Empty(fixture.Expected.Events);
            AssertFacilityStates(fixture, FacilityRuntimeAnalyzer.Analyze(
                state,
                TerritoryAnalyzer.Analyze(fixture.Board),
                Policy.Value));
            return;
        }

        var placement = FacilityPlacementIntegrator.Apply(
            state,
            history.CommitLegalPlacement(evaluation));

        Assert.Equal(
            fixture.Expected.DestroyedFacilities,
            placement.DestructionFact is null
                ? []
                : [placement.DestructionFact.Facility.InstanceId]);
        Assert.Equal(
            fixture.Expected.RemainingFacilities,
            InstanceIds(placement.FacilityStateAfterCommit));
        Assert.Equal(fixture.Expected.Events, PlacementEvents(placement.OrderedFacts));
        Assert.Equal(
            StoneTopologyKey.FromBoard(Assert.IsType<BoardState>(fixture.Expected.ResultBoard)),
            StoneTopologyKey.FromBoard(placement.BoardAfterCommit));
    }

    [Fact]
    public void Fac05TransitionSequenceMatchesProductionStateFacts()
    {
        var fixture = RequiredFixture("FAC-05");
        var analysis = Analyze(fixture, out _);
        var events = new List<string>();

        foreach (var nextBoard in fixture.NextBoards)
        {
            var transition = FacilityOperatingTransitionResolver.Reassociate(
                analysis,
                TerritoryAnalyzer.Analyze(nextBoard));
            events.AddRange(transition.OrderedFacts.Select(FacilityEvent));
            analysis = transition.AnalysisAfter;
        }

        Assert.Equal(fixture.Expected.Events, events);
        Assert.Equal(fixture.Expected.RemainingFacilities, InstanceIds(analysis.FacilityState));
        AssertFacilityStates(fixture, analysis);
        var facility = Assert.Single(analysis.FacilityState.InstalledFacilities);
        Assert.Equal(fixture.Expected.Owner, facility.Owner);
        Assert.Equal(fixture.Expected.BuildSequence, facility.BuildSequence);
    }

    [Theory]
    [InlineData("FAC-08")]
    [InlineData("FAC-09")]
    public void BuildFixturesMatchProductionEvaluationAndCommit(string fixtureId)
    {
        var fixture = RequiredFixture(fixtureId);
        var analysis = Analyze(fixture, out var territory);
        var topologyBefore = StoneTopologyKey.FromBoard(fixture.Board);
        var stateBefore = analysis.FacilityState.ToCanonicalText();
        var generatedId = fixture.Expected.RemainingFacilities
            .Except(fixture.Facilities.Select(facility => facility.InstanceId), StringComparer.Ordinal)
            .SingleOrDefault() ?? "candidate_facility";
        var request = new FacilityBuildRequest(
            Assert.IsType<StoneColor>(fixture.Actor),
            Assert.IsType<CanonicalPoint>(fixture.Point),
            Assert.IsType<string>(fixture.FacilityContentId),
            generatedId);

        var evaluation = FacilityBuildEvaluator.Evaluate(analysis, request);

        Assert.Equal(fixture.Expected.Legal, evaluation.IsLegal);
        Assert.Equal(fixture.Expected.Reason, evaluation.ReasonId);
        if (!evaluation.IsLegal)
        {
            Assert.Throws<InvalidOperationException>(() =>
                FacilityBuildEvaluator.Commit(evaluation));
            Assert.Equal(stateBefore, analysis.FacilityState.ToCanonicalText());
            Assert.Equal(
                fixture.Expected.RemainingFacilities,
                InstanceIds(analysis.FacilityState));
            Assert.Empty(fixture.Expected.Events);
            return;
        }

        var commit = FacilityBuildEvaluator.Commit(evaluation);

        Assert.Same(fixture.Board, commit.StateAfterCommit.SourceBoard);
        Assert.Same(territory, commit.AnalysisAfterCommit.TerritoryAnalysis);
        Assert.Equal(fixture.Expected.Events, commit.OrderedFacts.Select(FacilityEvent));
        Assert.Equal(
            fixture.Expected.RemainingFacilities,
            InstanceIds(commit.StateAfterCommit));
        AssertFacilityStates(fixture, commit.AnalysisAfterCommit);
        if (fixture.Expected.TopologyUnchanged == true)
        {
            Assert.Equal(topologyBefore, StoneTopologyKey.FromBoard(commit.StateAfterCommit.SourceBoard));
        }

        var expectedTerritory = Assert.IsType<FacilityFixtureTerritory>(fixture.Expected.Territory);
        var region = Assert.IsType<FacilityRegionRuntimeAnalysis>(
            commit.AnalysisAfterCommit.RegionAt(request.Point));
        Assert.Equal(expectedTerritory.Owner, region.Region.Owner);
        Assert.Equal(expectedTerritory.Size, region.Region.Size);
        Assert.Equal(expectedTerritory.BasicIncome, region.BasicIncome);
        Assert.Equal(expectedTerritory.ConstructionCapacity, region.ConstructionCapacity);
    }

    private static FacilityRuntimeAnalysis Analyze(
        FacilityFixture fixture,
        out TerritoryAnalysis territory)
    {
        var state = FacilityFixtureData.CreateInitialState(fixture);
        territory = TerritoryAnalyzer.Analyze(fixture.Board);
        return FacilityRuntimeAnalyzer.Analyze(state, territory, Policy.Value);
    }

    private static void AssertFacilityStates(
        FacilityFixture fixture,
        FacilityRuntimeAnalysis analysis)
    {
        foreach (var pair in fixture.Expected.FacilityStates)
        {
            var facility = analysis.FacilityState.FacilityById(pair.Key)
                ?? throw new InvalidOperationException($"Missing expected facility {pair.Key}.");
            Assert.Equal(pair.Value, OperatingProjection(analysis.OperatingStateFor(facility)));
        }
    }

    private static string OperatingProjection(FacilityOperatingState state) =>
        state.IsActive ? "active" : $"disabled:{state.ReasonId}";

    private static string[] InstanceIds(FacilityState state) =>
        state.InstalledFacilities.Select(facility => facility.InstanceId).ToArray();

    private static string[] PlacementEvents(
        IEnumerable<ICommittedPlacementFact> facts) =>
        facts.Select(fact => fact switch
            {
                StonePlacedFact placed =>
                    $"StonePlaced:{ColorId(placed.Stone.Color)}:{PointId(placed.Stone.Point)}",
                GroupCapturedFact captured =>
                    $"GroupCaptured:{PointId(captured.CapturedGroup.Anchor)}",
                FacilityDestroyedFact destroyed => FacilityEvent(destroyed),
                _ => null,
            })
            .Where(value => value is not null)
            .Select(value => value!)
            .ToArray();

    private static string FacilityEvent(FacilityFact fact) => fact switch
    {
        FacilityBuiltFact built =>
            $"FacilityBuilt:{built.Facility.InstanceId}:{built.Facility.ContentId}:{PointId(built.Facility.Point)}",
        FacilityActivatedFact activated =>
            $"FacilityActivated:{activated.Facility.InstanceId}:{activated.ReasonId}",
        FacilityDisabledFact disabled =>
            $"FacilityDisabled:{disabled.Facility.InstanceId}:{disabled.ReasonId}",
        FacilityDestroyedFact destroyed =>
            $"FacilityDestroyed:{destroyed.Facility.InstanceId}:{destroyed.ReasonId}",
        _ => throw new InvalidOperationException("Unknown facility fact type."),
    };

    private static string PointId(CanonicalPoint point) => $"{point.X},{point.Y}";

    private static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Unknown fixture stone color."),
    };

    private static HypotheticalPlacementResolution ResolvePlacement(
        BoardState board,
        StoneColor actor,
        CanonicalPoint point)
    {
        if (!HypotheticalPlacementResolver.TryCreate(
                board,
                new BoardStone(actor, false, point),
                out var placement) ||
            placement is null)
        {
            throw new InvalidOperationException("Fixture placement unexpectedly targeted a stone.");
        }

        return HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            RealLiberties(placement.GroupsAfterPlacement));
    }

    private static EffectiveLibertySnapshot RealLiberties(StoneGroupAnalysis analysis) =>
        EffectiveLibertySnapshot.Create(
            analysis,
            analysis.Groups.Select(group => new GroupEffectiveLiberty(
                group,
                group.RealLibertyCount)));

    private static FacilityFixture RequiredFixture(string fixtureId) =>
        Fixtures.Value.TryGetValue(fixtureId, out var fixture)
            ? fixture
            : throw new InvalidOperationException($"Missing facility fixture {fixtureId}.");
}
