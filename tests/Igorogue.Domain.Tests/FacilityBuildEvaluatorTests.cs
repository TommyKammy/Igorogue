using Igorogue.Domain.Board;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Tests;

public sealed class FacilityBuildEvaluatorTests
{
    private static readonly BoardGeometry Geometry = FacilityFixtureData.Geometry;
    private static readonly Lazy<FacilityRuntimePolicy> Policy =
        new(FacilityFixtureData.LoadRuntimePolicy);

    [Fact]
    public void ValidationStatusesFollowAcceptedPrecedence()
    {
        var stoneBoard = Board(Stone(StoneColor.Black, 1, 1));
        AssertStatus(
            FacilityBuildStatus.TargetHasStone,
            Analyze(stoneBoard, [], 1),
            Request("stone-target", "development", 1, 1));

        var controlledBoard = Board(Stone(StoneColor.Black, 7, 7));
        var occupied = Facility("occupied", "development", 1, 1, 1);
        AssertStatus(
            FacilityBuildStatus.TargetOccupied,
            Analyze(controlledBoard, [occupied], 2),
            Request("other", "development", 1, 1));

        var whiteBoard = Board(Stone(StoneColor.White, 7, 7));
        AssertStatus(
            FacilityBuildStatus.TargetNotOwnedTerritory,
            Analyze(whiteBoard, [], 1),
            Request("not-owned", "development", 1, 1));

        var cornerBoard = Board(
            Stone(StoneColor.Black, 1, 3),
            Stone(StoneColor.Black, 2, 2),
            Stone(StoneColor.Black, 3, 1));
        var market = Facility("market-one", "market", 1, 1, 1);
        var capacityBeforeType = FacilityBuildEvaluator.Evaluate(
            Analyze(cornerBoard, [market], 2),
            Request("market-two", "market", 1, 2));

        Assert.Equal(FacilityBuildStatus.CapacityFull, capacityBeforeType.Status);
        Assert.Equal("facility_capacity_full", capacityBeforeType.ReasonId);

        var typeLimited = FacilityBuildEvaluator.Evaluate(
            Analyze(controlledBoard, [market], 2),
            Request("market-two", "market", 2, 1));

        Assert.Equal(FacilityBuildStatus.TypeLimitReached, typeLimited.Status);
        Assert.Equal("facility_type_limit_reached", typeLimited.ReasonId);
    }

    [Fact]
    public void DisabledFacilitiesConsumeCapacityAndTypeLimit()
    {
        var board = Board(Stone(StoneColor.Black, 7, 7));
        var disabledMarket = new FacilityInstance(
            "disabled-market",
            "market",
            StoneColor.Black,
            C(1, 1),
            1,
            ["effect-lock"]);
        var analysis = Analyze(board, [disabledMarket], 2);

        var evaluation = FacilityBuildEvaluator.Evaluate(
            analysis,
            Request("new-market", "market", 2, 1));

        Assert.Equal(FacilityOperatingKind.ExplicitEffect, analysis.FacilityStates[0].Kind);
        Assert.Equal(FacilityBuildStatus.TypeLimitReached, evaluation.Status);
    }

    [Fact]
    public void TerritoryControlLostFacilitiesAlsoConsumeInstalledAndTypeCounts()
    {
        var board = Board(Stone(StoneColor.Black, 7, 7));
        var opponentMarket = new FacilityInstance(
            "opponent-market",
            "market",
            StoneColor.White,
            C(1, 1),
            1);
        var analysis = Analyze(board, [opponentMarket], 2);
        var region = Assert.IsType<FacilityRegionRuntimeAnalysis>(analysis.RegionAt(C(1, 1)));

        var evaluation = FacilityBuildEvaluator.Evaluate(
            analysis,
            Request("new-market", "market", 2, 1));

        Assert.Equal(
            FacilityOperatingKind.TerritoryControlLost,
            analysis.OperatingStateFor(opponentMarket).Kind);
        Assert.Equal(1, region.InstalledCount);
        Assert.Equal(1, region.InstalledCountFor("market"));
        Assert.Equal(FacilityBuildStatus.TypeLimitReached, evaluation.Status);
    }

    [Fact]
    public void TypeLimitCountsOnlyTheTargetCurrentRegion()
    {
        var splitBoard = Board(Enumerable.Range(1, 7)
            .Select(y => Stone(StoneColor.Black, 4, y))
            .ToArray());
        var leftMarket = Facility("left-market", "market", 1, 1, 1);
        var analysis = Analyze(splitBoard, [leftMarket], 2);

        var evaluation = FacilityBuildEvaluator.Evaluate(
            analysis,
            Request("right-market", "market", 7, 1));

        Assert.True(evaluation.IsLegal);
        Assert.Equal(0, analysis.RegionAt(C(7, 1))?.InstalledCountFor("market"));
        Assert.Equal(1, analysis.RegionAt(C(1, 1))?.InstalledCountFor("market"));
    }

    [Fact]
    public void LegalCommitUsesCallerIdAndNextSequenceWithoutChangingStoneState()
    {
        var fixture = FacilityFixtureData.LoadFixtures()["FAC-09"];
        var state = FacilityFixtureData.CreateInitialState(fixture);
        var territory = TerritoryAnalyzer.Analyze(fixture.Board);
        var analysis = FacilityRuntimeAnalyzer.Analyze(state, territory, Policy.Value);
        var history = BattleRepetitionHistory.Start(fixture.Board);
        var historyBefore = history.ToCanonicalText();
        var topologyBefore = StoneTopologyKey.FromBoard(fixture.Board);
        var request = new FacilityBuildRequest(
            Assert.IsType<StoneColor>(fixture.Actor),
            Assert.IsType<CanonicalPoint>(fixture.Point),
            Assert.IsType<string>(fixture.FacilityContentId),
            "generated_facility");

        var evaluation = FacilityBuildEvaluator.Evaluate(analysis, request);
        var commit = FacilityBuildEvaluator.Commit(evaluation);

        Assert.True(evaluation.IsLegal);
        Assert.Equal("legal", evaluation.ReasonId);
        Assert.Same(analysis, commit.AnalysisBeforeCommit);
        Assert.Same(fixture.Board, commit.StateAfterCommit.SourceBoard);
        Assert.Same(territory, commit.AnalysisAfterCommit.TerritoryAnalysis);
        Assert.Same(Policy.Value, commit.AnalysisAfterCommit.Policy);
        Assert.Empty(state.InstalledFacilities);
        Assert.Equal(1, state.NextBuildSequence);
        Assert.Equal("generated_facility", commit.BuiltFacility.InstanceId);
        Assert.Equal("development", commit.BuiltFacility.ContentId);
        Assert.Equal(StoneColor.Black, commit.BuiltFacility.Owner);
        Assert.Equal(C(1, 1), commit.BuiltFacility.Point);
        Assert.Equal(1, commit.BuiltFacility.BuildSequence);
        Assert.Equal(2, commit.StateAfterCommit.NextBuildSequence);
        Assert.Same(
            commit.BuiltFacility,
            commit.StateAfterCommit.FacilityAt(C(1, 1)));
        Assert.True(commit.AnalysisAfterCommit.OperatingStateFor(commit.BuiltFacility).IsActive);
        Assert.IsType<FacilityBuiltFact>(commit.OrderedFacts[0]);
        var activated = Assert.IsType<FacilityActivatedFact>(commit.OrderedFacts[1]);
        Assert.Equal("built_in_controlled_territory", activated.ReasonId);
        Assert.Equal(topologyBefore, StoneTopologyKey.FromBoard(commit.StateAfterCommit.SourceBoard));
        Assert.Equal(historyBefore, history.ToCanonicalText());
        Assert.Equal(1, history.ObservationCount);

        var facts = Assert.IsAssignableFrom<ICollection<FacilityFact>>(commit.OrderedFacts);
        Assert.True(facts.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => facts.Add(commit.OrderedFacts[0]));
    }

    [Fact]
    public void IllegalEvaluationCannotCommitAndDuplicateCallerIdIsRejected()
    {
        var board = Board(Stone(StoneColor.Black, 7, 7));
        var existing = Facility("existing", "market", 1, 1, 1);
        var analysis = Analyze(board, [existing], 2);
        var illegal = FacilityBuildEvaluator.Evaluate(
            analysis,
            Request("other", "market", 2, 1));

        Assert.False(illegal.IsLegal);
        Assert.Throws<InvalidOperationException>(() => FacilityBuildEvaluator.Commit(illegal));
        Assert.Throws<ArgumentException>(() => FacilityBuildEvaluator.Evaluate(
            analysis,
            Request("existing", "development", 2, 1)));
        Assert.Single(analysis.FacilityState.InstalledFacilities);
        Assert.Equal(2, analysis.FacilityState.NextBuildSequence);
    }

    [Fact]
    public void CommitNeverReusesASequenceMissingFromTheInstalledSet()
    {
        var board = Board(Stone(StoneColor.Black, 7, 7));
        var state = FacilityState.Create(board, [], 10);
        var analysis = FacilityRuntimeAnalyzer.Analyze(
            state,
            TerritoryAnalyzer.Analyze(board),
            Policy.Value);
        var evaluation = FacilityBuildEvaluator.Evaluate(
            analysis,
            Request("late-build", "development", 1, 1));

        var commit = FacilityBuildEvaluator.Commit(evaluation);

        Assert.Equal(10, commit.BuiltFacility.BuildSequence);
        Assert.Equal(11, commit.StateAfterCommit.NextBuildSequence);
    }

    private static void AssertStatus(
        FacilityBuildStatus expected,
        FacilityRuntimeAnalysis analysis,
        FacilityBuildRequest request)
    {
        var evaluation = FacilityBuildEvaluator.Evaluate(analysis, request);
        Assert.Equal(expected, evaluation.Status);
        Assert.False(evaluation.IsLegal);
    }

    private static FacilityRuntimeAnalysis Analyze(
        BoardState board,
        IEnumerable<FacilityInstance> facilities,
        long nextSequence) =>
        FacilityRuntimeAnalyzer.Analyze(
            FacilityState.Create(board, facilities, nextSequence),
            TerritoryAnalyzer.Analyze(board),
            Policy.Value);

    private static FacilityBuildRequest Request(
        string id,
        string content,
        int x,
        int y) =>
        new(StoneColor.Black, C(x, y), content, id);

    private static FacilityInstance Facility(
        string id,
        string content,
        int x,
        int y,
        long sequence) =>
        new(id, content, StoneColor.Black, C(x, y), sequence);

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(StoneColor color, int x, int y) =>
        new(color, false, C(x, y));

    private static CanonicalPoint C(int x, int y) => Geometry.CreateCanonicalPoint(x, y);
}
