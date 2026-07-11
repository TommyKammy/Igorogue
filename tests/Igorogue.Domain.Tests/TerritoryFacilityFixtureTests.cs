using Igorogue.Domain.Board;

namespace Igorogue.Domain.Tests;

public sealed class TerritoryFacilityFixtureTests
{
    private static readonly BoardGeometry Geometry = FacilityFixtureData.Geometry;
    private static readonly Lazy<IReadOnlyDictionary<string, FacilityFixture>> Fixtures =
        new(FacilityFixtureData.LoadFixtures);

    [Fact]
    public void CanonicalFacilityFixtureInventoryIsPresent()
    {
        Assert.Equal(
            Enumerable.Range(1, 9).Select(index => $"FAC-{index:00}"),
            Fixtures.Value.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void SharedAdapterRetainsRuntimeMetadataAndJsonEventOrder()
    {
        var placement = RequiredFixture("FAC-03");
        var placedOver = Assert.Single(placement.Facilities);

        Assert.Equal("legal friendly placement destroys the facility on its point", placement.Title);
        Assert.Equal("place", placement.Operation);
        Assert.Equal(StoneColor.Black, placement.Actor);
        Assert.Equal(C(2, 2), placement.Point);
        Assert.Equal("facility_03", placedOver.InstanceId);
        Assert.Equal("development", placedOver.FacilityContentId);
        Assert.Equal(StoneColor.Black, placedOver.OwnerColor);
        Assert.Equal(C(2, 2), placedOver.Point);
        Assert.Equal(1L, placedOver.BuildSequence);
        Assert.Empty(placedOver.ExplicitDisableSources);
        Assert.Equal(2L, placement.InitialNextBuildSequence);
        Assert.True(placement.Expected.Legal);
        Assert.Equal("legal", placement.Expected.Reason);
        Assert.Equal(
            new[]
            {
                "StonePlaced:black:2,2",
                "FacilityDestroyed:facility_03:stone_occupied",
            },
            placement.Expected.Events);
        Assert.Empty(placement.Expected.RemainingFacilities);
        Assert.Equal(new[] { "facility_03" }, placement.Expected.DestroyedFacilities);
        Assert.NotNull(placement.Expected.ResultBoard);

        var transition = RequiredFixture("FAC-05");
        Assert.Equal(2, transition.NextBoards.Count);
        Assert.Equal(StoneColor.Black, transition.Expected.Owner);
        Assert.Equal(7L, transition.Expected.BuildSequence);
        Assert.Equal("active", transition.Expected.FacilityStates["facility_05"]);

        var opponentTerritory = RequiredFixture("FAC-06");
        Assert.Equal(StoneColor.Black, opponentTerritory.Expected.FacilityOwners["facility_06"]);
        Assert.Equal(TerritoryOwner.White, opponentTerritory.Expected.Territory?.Owner);
        Assert.Equal(1, opponentTerritory.Expected.Territory?.BasicIncome);
        Assert.Equal(1, opponentTerritory.Expected.Territory?.ConstructionCapacity);

        var overCapacity = RequiredFixture("FAC-07");
        Assert.Equal(2, overCapacity.Expected.Territory?.InstalledCount);
        Assert.True(overCapacity.Expected.Territory?.IsOverCapacity);

        var build = RequiredFixture("FAC-09");
        Assert.Equal("development", build.FacilityContentId);
        Assert.True(build.Expected.TopologyUnchanged);
        Assert.Equal(1L, build.InitialNextBuildSequence);
    }

    [Fact]
    public void SharedAdapterCreatesPolicyFromCanonicalSystemData()
    {
        var policy = FacilityFixtureData.LoadRuntimePolicy();

        Assert.Equal(3, policy.TerritoryIncomeDivisor);
        Assert.Equal(5, policy.SlotCap);
        Assert.Collection(
            policy.CapacityBands,
            band => Assert.Equal((1, 3, 1), (band.MinSize, band.MaxSize, band.Slots)),
            band => Assert.Equal((4, 7, 2), (band.MinSize, band.MaxSize, band.Slots)),
            band => Assert.Equal((8, 12, 3), (band.MinSize, band.MaxSize, band.Slots)),
            band => Assert.Equal((13, 49, 4), (band.MinSize, band.MaxSize, band.Slots)));
        Assert.Equal(1, policy.TypeLimits["default"]);
        Assert.Equal(2, policy.TypeLimits["development"]);
        Assert.Equal(2, policy.TypeLimits["furnace"]);
    }

    [Theory]
    [InlineData("FAC-01")]
    [InlineData("FAC-02")]
    [InlineData("FAC-06")]
    [InlineData("FAC-07")]
    [InlineData("FAC-09")]
    public void AcceptedFacilityFixturesUseStoneOnlyTerritoryProjection(string fixtureId)
    {
        var fixture = RequiredFixture(fixtureId);
        var queryPoint = Assert.IsType<CanonicalPoint>(fixture.TerritoryPoint);
        var expected = Assert.IsType<FacilityFixtureTerritory>(fixture.ExpectedTerritory);

        Assert.All(fixture.FacilityPoints, point => Assert.True(fixture.Board.IsEmpty(point)));
        if (fixture.ActionPoint is not null)
        {
            Assert.True(fixture.Board.IsEmpty(fixture.ActionPoint));
        }

        var region = TerritoryAnalyzer.Analyze(fixture.Board).RegionAt(queryPoint);

        Assert.NotNull(region);
        Assert.Equal(expected.Owner, region.Owner);
        Assert.Equal(expected.Size, region.Size);
        Assert.Contains(queryPoint, region.Points);
    }

    [Fact]
    public void Fac01FacilityPointIsBothBlackTerritoryAndRealLiberty()
    {
        var fixture = RequiredFixture("FAC-01");
        var facilityPoint = Assert.Single(fixture.FacilityPoints);
        var groupPoint = Assert.IsType<CanonicalPoint>(fixture.GroupPoint);
        var group = StoneGroupAnalyzer.Analyze(fixture.Board).GroupAt(groupPoint);
        var region = TerritoryAnalyzer.Analyze(fixture.Board).RegionAt(facilityPoint);

        Assert.NotNull(group);
        Assert.Equal(fixture.ExpectedGroupLiberties, group.RealLiberties);
        Assert.Contains(facilityPoint, group.RealLiberties);
        Assert.NotNull(region);
        Assert.Equal(TerritoryOwner.Black, region.Owner);
        Assert.Equal(1, region.Size);
    }

    [Fact]
    public void Fac05NeutralizationAndRestorationRecalculateFromEachStoneBoard()
    {
        var fixture = RequiredFixture("FAC-05");
        var facilityPoint = Assert.Single(fixture.FacilityPoints);
        var boards = new[] { fixture.Board }.Concat(fixture.NextBoards).ToArray();
        var expectedOwners = new[]
        {
            TerritoryOwner.Black,
            TerritoryOwner.Neutral,
            TerritoryOwner.Black,
        };

        Assert.Equal(expectedOwners.Length, boards.Length);
        for (var index = 0; index < boards.Length; index++)
        {
            Assert.True(boards[index].IsEmpty(facilityPoint));
            var region = TerritoryAnalyzer.Analyze(boards[index]).RegionAt(facilityPoint);

            Assert.NotNull(region);
            Assert.Equal(expectedOwners[index], region.Owner);
            Assert.Equal(1, region.Size);
        }
    }

    [Fact]
    public void Fac07SplitRegionHasAcceptedCanonicalPoints()
    {
        var fixture = RequiredFixture("FAC-07");
        var queryPoint = Assert.IsType<CanonicalPoint>(fixture.TerritoryPoint);

        var region = TerritoryAnalyzer.Analyze(fixture.Board).RegionAt(queryPoint);

        Assert.NotNull(region);
        Assert.Equal(TerritoryOwner.Black, region.Owner);
        Assert.Equal(C(4, 1), region.Anchor);
        Assert.Equal(new[] { C(4, 1), C(4, 2), C(4, 3) }, region.Points);
        Assert.All(fixture.FacilityPoints, point => Assert.Contains(point, region.Points));
    }

    [Fact]
    public void Fac02AndFac09FacilityPresenceDifferenceHasIdenticalStoneDerivedResults()
    {
        var withFacility = RequiredFixture("FAC-02");
        var withoutFacility = RequiredFixture("FAC-09");
        var facilityPoint = Assert.Single(withFacility.FacilityPoints);
        var buildPoint = Assert.IsType<CanonicalPoint>(withoutFacility.ActionPoint);

        Assert.Empty(withoutFacility.FacilityPoints);
        Assert.Equal(facilityPoint, buildPoint);
        Assert.NotSame(withFacility.Board, withoutFacility.Board);
        Assert.True(withFacility.Board.IsEmpty(facilityPoint));
        Assert.True(withoutFacility.Board.IsEmpty(facilityPoint));
        Assert.Equal(
            StoneTopologyKey.FromBoard(withFacility.Board),
            StoneTopologyKey.FromBoard(withoutFacility.Board));

        var territoryWithFacility = TerritoryAnalyzer.Analyze(withFacility.Board);
        var territoryWithoutFacility = TerritoryAnalyzer.Analyze(withoutFacility.Board);
        var groupsWithFacility = StoneGroupAnalyzer.Analyze(withFacility.Board);
        var groupsWithoutFacility = StoneGroupAnalyzer.Analyze(withoutFacility.Board);

        Assert.Equal(
            TerritoryProjection(territoryWithFacility),
            TerritoryProjection(territoryWithoutFacility));
        Assert.Equal(
            GroupProjection(groupsWithFacility),
            GroupProjection(groupsWithoutFacility));
        Assert.Equal(TerritoryOwner.Black, territoryWithFacility.RegionAt(facilityPoint)?.Owner);
        Assert.Equal(3, territoryWithFacility.RegionAt(facilityPoint)?.Size);
    }

    private static string[] TerritoryProjection(TerritoryAnalysis analysis) =>
        analysis.Regions
            .Select(region =>
                $"{region.Owner}:{region.Anchor}:{string.Join(";", region.Points)}")
            .ToArray();

    private static string[] GroupProjection(StoneGroupAnalysis analysis) =>
        analysis.Groups
            .Select(group =>
                $"{group.Color}:{group.Anchor}:{string.Join(";", group.RealLiberties)}")
            .ToArray();

    private static FacilityFixture RequiredFixture(string fixtureId) =>
        Fixtures.Value.TryGetValue(fixtureId, out var fixture)
            ? fixture
            : throw new InvalidOperationException($"Missing facility fixture {fixtureId}.");

    private static CanonicalPoint C(int x, int y) => Geometry.CreateCanonicalPoint(x, y);
}
