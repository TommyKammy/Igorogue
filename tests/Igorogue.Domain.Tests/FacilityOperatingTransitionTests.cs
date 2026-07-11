using Igorogue.Domain.Board;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Tests;

public sealed class FacilityOperatingTransitionTests
{
    private static readonly BoardGeometry Geometry = FacilityFixtureData.Geometry;
    private static readonly Lazy<FacilityRuntimePolicy> Policy =
        new(FacilityFixtureData.LoadRuntimePolicy);

    [Fact]
    public void NeutralizationAndRestorationEmitOnlyActualStateChanges()
    {
        var fixture = FacilityFixtureData.LoadFixtures()["FAC-05"];
        var initialState = FacilityFixtureData.CreateInitialState(fixture);
        var initialFacility = Assert.Single(initialState.InstalledFacilities);
        var initial = Analyze(initialState);

        var disabled = FacilityOperatingTransitionResolver.Reassociate(
            initial,
            TerritoryAnalyzer.Analyze(fixture.NextBoards[0]));
        var disabledFact = Assert.IsType<FacilityDisabledFact>(
            Assert.Single(disabled.OrderedFacts));

        Assert.Same(initialFacility, disabledFact.Facility);
        Assert.Equal("territory_control_lost", disabledFact.ReasonId);
        Assert.Same(fixture.NextBoards[0], disabled.StateAfterTransition.SourceBoard);
        Assert.Same(
            initialFacility,
            Assert.Single(disabled.StateAfterTransition.InstalledFacilities));
        Assert.Equal(StoneColor.Black, initialFacility.Owner);
        Assert.Equal(7, initialFacility.BuildSequence);

        var restored = FacilityOperatingTransitionResolver.Reassociate(
            disabled.AnalysisAfter,
            TerritoryAnalyzer.Analyze(fixture.NextBoards[1]));
        var activatedFact = Assert.IsType<FacilityActivatedFact>(
            Assert.Single(restored.OrderedFacts));

        Assert.Same(initialFacility, activatedFact.Facility);
        Assert.Equal("territory_control_restored", activatedFact.ReasonId);
        Assert.True(restored.AnalysisAfter.OperatingStateFor(initialFacility).IsActive);

        var unchanged = FacilityOperatingTransitionResolver.Reassociate(
            restored.AnalysisAfter,
            TerritoryAnalyzer.Analyze(CloneBoard(fixture.NextBoards[1])));
        Assert.Empty(unchanged.OrderedFacts);
    }

    [Fact]
    public void ExplicitDisableRemainsDominantAcrossTerritoryChangesWithoutFacts()
    {
        var controlledBoard = Board(Stone(StoneColor.Black, 7, 7));
        var neutralBoard = Board();
        var facility = new FacilityInstance(
            "disabled",
            "development",
            StoneColor.Black,
            C(1, 1),
            1,
            ["effect-lock"]);
        var controlled = Analyze(FacilityState.Create(controlledBoard, [facility], 2));

        var transition = FacilityOperatingTransitionResolver.Reassociate(
            controlled,
            TerritoryAnalyzer.Analyze(neutralBoard));

        Assert.Empty(transition.OrderedFacts);
        Assert.Equal(
            FacilityOperatingKind.ExplicitEffect,
            transition.AnalysisAfter.OperatingStateFor(facility).Kind);
    }

    [Fact]
    public void SplitAndMergeReassociateByPointAndCountOnlyTheCurrentRegion()
    {
        var mergedBoard = Board(Stone(StoneColor.Black, 4, 4));
        var splitBoard = Board(Enumerable.Range(1, 7)
            .Select(y => Stone(StoneColor.Black, 4, y))
            .ToArray());
        var right = Facility("right", 7, 1, 2);
        var left = Facility("left", 1, 1, 1);
        var state = FacilityState.Create(mergedBoard, [right, left], 3);
        var policy = CapacityOnePolicy();
        var merged = FacilityRuntimeAnalyzer.Analyze(
            state,
            TerritoryAnalyzer.Analyze(mergedBoard),
            policy);
        var mergedRegion = Assert.IsType<FacilityRegionRuntimeAnalysis>(merged.RegionAt(C(1, 1)));

        Assert.Same(mergedRegion, merged.RegionAt(C(7, 1)));
        Assert.Equal(2, mergedRegion.InstalledCount);
        Assert.Equal(2, mergedRegion.InstalledCountFor("development"));
        Assert.True(mergedRegion.IsOverCapacity);
        Assert.All(merged.FacilityStates, operating => Assert.True(operating.IsActive));

        var split = FacilityOperatingTransitionResolver.Reassociate(
            merged,
            TerritoryAnalyzer.Analyze(splitBoard));
        var leftRegion = Assert.IsType<FacilityRegionRuntimeAnalysis>(
            split.AnalysisAfter.RegionAt(C(1, 1)));
        var rightRegion = Assert.IsType<FacilityRegionRuntimeAnalysis>(
            split.AnalysisAfter.RegionAt(C(7, 1)));

        Assert.NotSame(leftRegion, rightRegion);
        Assert.Equal(1, leftRegion.InstalledCount);
        Assert.Equal(1, rightRegion.InstalledCount);
        Assert.Equal(1, leftRegion.InstalledCountFor("development"));
        Assert.Equal(1, rightRegion.InstalledCountFor("development"));
        Assert.False(leftRegion.IsOverCapacity);
        Assert.False(rightRegion.IsOverCapacity);
        Assert.Empty(split.OrderedFacts);

        var mergedAgain = FacilityOperatingTransitionResolver.Reassociate(
            split.AnalysisAfter,
            TerritoryAnalyzer.Analyze(mergedBoard));
        Assert.Equal(2, mergedAgain.AnalysisAfter.RegionAt(C(1, 1))?.InstalledCount);
        Assert.Empty(mergedAgain.OrderedFacts);
    }

    [Fact]
    public void MultipleTransitionFactsUseCanonicalPointThenOrdinalIdOrder()
    {
        var controlledBoard = Board(Stone(StoneColor.Black, 4, 4));
        var neutralBoard = Board();
        var later = Facility("later", 7, 1, 2);
        var earlier = Facility("earlier", 1, 1, 1);
        var before = FacilityRuntimeAnalyzer.Analyze(
            FacilityState.Create(controlledBoard, [later, earlier], 3),
            TerritoryAnalyzer.Analyze(controlledBoard),
            CapacityOnePolicy());

        var transition = FacilityOperatingTransitionResolver.Reassociate(
            before,
            TerritoryAnalyzer.Analyze(neutralBoard));

        Assert.Equal(
            new[] { "earlier", "later" },
            transition.OrderedFacts.Select(fact => fact.Facility.InstanceId));
        Assert.All(transition.OrderedFacts, fact =>
        {
            var disabled = Assert.IsType<FacilityDisabledFact>(fact);
            Assert.Equal("territory_control_lost", disabled.ReasonId);
        });
    }

    [Fact]
    public void TransitionRejectsForeignPolicyMetadataMutationAndStoneCoexistence()
    {
        var board = Board(Stone(StoneColor.Black, 7, 7));
        var facility = Facility("facility", 1, 1, 1);
        var before = Analyze(FacilityState.Create(board, [facility], 2));
        var equivalentState = FacilityState.Create(
            board,
            [Facility("facility", 1, 1, 1)],
            2);
        var equivalent = FacilityRuntimeAnalyzer.Analyze(
            equivalentState,
            TerritoryAnalyzer.Analyze(board),
            Policy.Value);

        Assert.Throws<ArgumentException>(() =>
            FacilityOperatingTransitionResolver.Resolve(before, equivalent));
        Assert.Throws<ArgumentException>(() =>
            FacilityOperatingTransitionResolver.Resolve(
                before,
                FacilityRuntimeAnalyzer.Analyze(
                    FacilityState.Create(board, [facility], 2),
                    TerritoryAnalyzer.Analyze(board),
                    CapacityOnePolicy())));
        Assert.Throws<ArgumentException>(() =>
            FacilityOperatingTransitionResolver.Reassociate(
                before,
                TerritoryAnalyzer.Analyze(Board(Stone(StoneColor.Black, 1, 1)))));
    }

    private static FacilityRuntimeAnalysis Analyze(FacilityState state) =>
        FacilityRuntimeAnalyzer.Analyze(
            state,
            TerritoryAnalyzer.Analyze(state.SourceBoard),
            Policy.Value);

    private static FacilityRuntimePolicy CapacityOnePolicy() => FacilityRuntimePolicy.Create(
        3,
        [new FacilityCapacityBand(1, 49, 1)],
        5,
        [
            new KeyValuePair<string, int>("default", 1),
            new KeyValuePair<string, int>("development", 2),
        ]);

    private static FacilityInstance Facility(string id, int x, int y, long sequence) =>
        new(id, "development", StoneColor.Black, C(x, y), sequence);

    private static BoardState CloneBoard(BoardState board) =>
        BoardState.Create(Geometry, board.OccupiedStones.Reverse());

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(StoneColor color, int x, int y) =>
        new(color, false, C(x, y));

    private static CanonicalPoint C(int x, int y) => Geometry.CreateCanonicalPoint(x, y);
}
