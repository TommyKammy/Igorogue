using Igorogue.Domain.Board;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Tests;

public sealed class FacilityRuntimeAnalyzerTests
{
    private static readonly BoardGeometry Geometry = FacilityFixtureData.Geometry;
    private static readonly Lazy<FacilityRuntimePolicy> Policy =
        new(FacilityFixtureData.LoadRuntimePolicy);

    [Fact]
    public void AnalysisDerivesExplicitFirstOperatingStateAndRegionMetrics()
    {
        var board = Board(Stone(StoneColor.Black, 7, 7));
        var active = Facility("active", "development", 2, 1, 1);
        var explicitDisabled = Facility(
            "disabled",
            "development",
            1,
            1,
            2,
            ["effect-z", "effect-a"]);
        var state = FacilityState.Create(board, [active, explicitDisabled], 3);
        var territory = TerritoryAnalyzer.Analyze(board);

        var analysis = FacilityRuntimeAnalyzer.Analyze(state, territory, Policy.Value);

        Assert.Same(state, analysis.FacilityState);
        Assert.Same(territory, analysis.TerritoryAnalysis);
        Assert.Same(Policy.Value, analysis.Policy);
        Assert.Equal(FacilityOperatingKind.Active, analysis.OperatingStateFor(active).Kind);
        Assert.Equal("active", analysis.OperatingStateFor(active).ReasonId);
        Assert.Equal(
            FacilityOperatingKind.ExplicitEffect,
            analysis.OperatingStateFor(explicitDisabled).Kind);
        Assert.Equal("explicit_effect", analysis.OperatingStateFor(explicitDisabled).ReasonId);

        var region = Assert.IsType<FacilityRegionRuntimeAnalysis>(analysis.RegionAt(C(1, 1)));
        Assert.Same(region, analysis.RegionAt(C(2, 1)));
        Assert.Equal(TerritoryOwner.Black, region.Region.Owner);
        Assert.Equal(48, region.Region.Size);
        Assert.Equal(16, region.BasicIncome);
        Assert.Equal(4, region.ConstructionCapacity);
        Assert.Equal(2, region.InstalledCount);
        Assert.Equal(2, region.InstalledCountFor("development"));
        Assert.False(region.IsOverCapacity);
        Assert.Equal(5, region.ConstructionCapacityWithModifier(10));

        var installed = Assert.IsAssignableFrom<ICollection<FacilityInstance>>(
            region.InstalledFacilities);
        Assert.True(installed.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => installed.Add(active));
        var typeCounts = Assert.IsAssignableFrom<IDictionary<string, int>>(
            region.InstalledCountsByType);
        Assert.True(typeCounts.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => typeCounts.Add("market", 1));
    }

    [Fact]
    public void OpponentAndNeutralTerritoryDisableWithoutOwnershipTransfer()
    {
        var fixture = FacilityFixtureData.LoadFixtures()["FAC-06"];
        var state = FacilityFixtureData.CreateInitialState(fixture);
        var facility = Assert.Single(state.InstalledFacilities);

        var analysis = FacilityRuntimeAnalyzer.Analyze(
            state,
            TerritoryAnalyzer.Analyze(fixture.Board),
            Policy.Value);
        var operating = analysis.OperatingStateFor(facility);

        Assert.Equal(StoneColor.Black, facility.Owner);
        Assert.Equal(FacilityOperatingKind.TerritoryControlLost, operating.Kind);
        Assert.Equal("territory_control_lost", operating.ReasonId);
        Assert.False(operating.IsActive);
        Assert.Equal(TerritoryOwner.White, operating.CurrentRegion.Owner);
    }

    [Fact]
    public void AnalysisRejectsEquivalentButDifferentBoardSnapshotsAndForeignInstances()
    {
        var board = Board(Stone(StoneColor.Black, 7, 7));
        var equivalentBoard = Board(Stone(StoneColor.Black, 7, 7));
        var facility = Facility("facility", "development", 1, 1, 1);
        var state = FacilityState.Create(board, [facility], 2);

        Assert.Throws<ArgumentException>(() => FacilityRuntimeAnalyzer.Analyze(
            state,
            TerritoryAnalyzer.Analyze(equivalentBoard),
            Policy.Value));

        var analysis = FacilityRuntimeAnalyzer.Analyze(
            state,
            TerritoryAnalyzer.Analyze(board),
            Policy.Value);
        var equivalentFacility = Facility("facility", "development", 1, 1, 1);

        Assert.Throws<ArgumentException>(() =>
            analysis.OperatingStateFor(equivalentFacility));
    }

    [Fact]
    public void AnalysisCollectionsAreReadOnlyAndCanonicallyOrdered()
    {
        var board = Board(Stone(StoneColor.Black, 7, 7));
        var later = Facility("later", "furnace", 2, 2, 2);
        var earlier = Facility("earlier", "development", 1, 1, 1);
        var analysis = FacilityRuntimeAnalyzer.Analyze(
            FacilityState.Create(board, [later, earlier], 3),
            TerritoryAnalyzer.Analyze(board),
            Policy.Value);

        Assert.Equal(
            new[] { earlier, later },
            analysis.FacilityStates.Select(state => state.Facility));
        var states = Assert.IsAssignableFrom<ICollection<FacilityOperatingState>>(
            analysis.FacilityStates);
        Assert.True(states.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => states.Add(analysis.FacilityStates[0]));
        var regions = Assert.IsAssignableFrom<ICollection<FacilityRegionRuntimeAnalysis>>(
            analysis.Regions);
        Assert.True(regions.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => regions.Add(analysis.Regions[0]));
    }

    private static FacilityInstance Facility(
        string id,
        string contentId,
        int x,
        int y,
        long sequence,
        IEnumerable<string>? explicitSources = null) =>
        new(
            id,
            contentId,
            StoneColor.Black,
            C(x, y),
            sequence,
            explicitSources ?? []);

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(StoneColor color, int x, int y) =>
        new(color, false, C(x, y));

    private static CanonicalPoint C(int x, int y) => Geometry.CreateCanonicalPoint(x, y);
}
