using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Tests;

public sealed class CoreDuelBattleSetupDefinitionTests
{
    [Fact]
    public void SetupCarriesTypedRuntimeInputsAndCanonicalizesInitialStoneOrder()
    {
        var position = Position(placements: StandardPlacements().Reverse());
        var facility = FacilityPolicy();
        var counterattack = CounterattackPolicy();

        var setup = CoreDuelBattleSetupDefinition.Create(
            position,
            playerTurnLimit: 20,
            facility,
            counterattack,
            counterattackStartGaugeUnits: 20);

        Assert.Same(position, setup.InitialPosition);
        Assert.Equal(20, setup.PlayerTurnLimit);
        Assert.Same(facility, setup.FacilityPolicy);
        Assert.Same(counterattack, setup.CounterattackPolicy);
        Assert.Equal(20, setup.CounterattackStartGaugeUnits);
        Assert.StartsWith(
            CoreDuelBattleSetupDefinition.EncodingVersion,
            setup.ToCanonicalText(),
            StringComparison.Ordinal);
        Assert.True(
            setup.ToCanonicalText().IndexOf("initial_stone=black,king,2,2", StringComparison.Ordinal) <
            setup.ToCanonicalText().IndexOf("initial_stone=white,king,6,6", StringComparison.Ordinal));
    }

    [Fact]
    public void SetupRejectsUnsupportedInitialPositionId()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateSetup(
            Position(id: "future_position")));

        Assert.Contains("standard_v0_2", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupAcceptsDifferentCoordinatesThatPreserveTheAcceptedStructure()
    {
        InitialPlacement[] placements =
        [
            new(StoneColor.Black, InitialStoneRole.King, 2, 5),
            new(StoneColor.Black, InitialStoneRole.Guard, 2, 6),
            new(StoneColor.Black, InitialStoneRole.Guard, 3, 5),
            new(StoneColor.White, InitialStoneRole.Guard, 6, 2),
            new(StoneColor.White, InitialStoneRole.Guard, 5, 3),
            new(StoneColor.White, InitialStoneRole.King, 6, 3),
        ];

        var setup = CreateSetup(Position(placements: placements));

        Assert.Equal(
            placements
                .Select(stone => (stone.Color, stone.Role, stone.X, stone.Y))
                .OrderBy(stone => stone.Y)
                .ThenBy(stone => stone.X),
            setup.InitialPosition.Stones.Select(stone =>
                (stone.Color, stone.Role, stone.Point.X, stone.Point.Y)));
    }

    [Fact]
    public void SetupRejectsMissingSymmetricGuardPair()
    {
        var placements = StandardPlacements()
            .Where(stone => stone is not
                { Color: StoneColor.Black, X: 2, Y: 3 } and not
                { Color: StoneColor.White, X: 6, Y: 5 });

        var exception = Assert.Throws<ArgumentException>(() => CreateSetup(
            Position(placements: placements)));

        Assert.Contains("one king and two guards", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupRejectsSymmetricGuardsReplacedByKings()
    {
        var placements = StandardPlacements().Select(stone =>
            stone is { X: 2, Y: 3 } or { X: 6, Y: 5 }
                ? stone with { Role = InitialStoneRole.King }
                : stone);

        var exception = Assert.Throws<ArgumentException>(() => CreateSetup(
            Position(placements: placements)));

        Assert.Contains("one king and two guards", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupRejectsPositionWithoutRoleAwarePointSymmetry()
    {
        var placements = StandardPlacements().Select(stone =>
            stone is { Color: StoneColor.Black, X: 3, Y: 2 }
                ? stone with { X = 4 }
                : stone);

        var exception = Assert.Throws<ArgumentException>(() => CreateSetup(
            Position(placements: placements)));

        Assert.Contains("point-reflection symmetry", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupRejectsDisconnectedThreeStoneColorShape()
    {
        InitialPlacement[] placements =
        [
            new(StoneColor.Black, InitialStoneRole.King, 2, 2),
            new(StoneColor.Black, InitialStoneRole.Guard, 2, 3),
            new(StoneColor.Black, InitialStoneRole.Guard, 4, 3),
            new(StoneColor.White, InitialStoneRole.Guard, 4, 5),
            new(StoneColor.White, InitialStoneRole.Guard, 6, 5),
            new(StoneColor.White, InitialStoneRole.King, 6, 6),
        ];

        var exception = Assert.Throws<ArgumentException>(() => CreateSetup(
            Position(placements: placements)));

        Assert.Contains("three connected", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupRejectsConnectedKingGroupsWithWrongLibertyCount()
    {
        InitialPlacement[] placements =
        [
            new(StoneColor.Black, InitialStoneRole.King, 1, 1),
            new(StoneColor.Black, InitialStoneRole.Guard, 2, 1),
            new(StoneColor.Black, InitialStoneRole.Guard, 1, 2),
            new(StoneColor.White, InitialStoneRole.Guard, 7, 6),
            new(StoneColor.White, InitialStoneRole.Guard, 6, 7),
            new(StoneColor.White, InitialStoneRole.King, 7, 7),
        ];

        var exception = Assert.Throws<ArgumentException>(() => CreateSetup(
            Position(placements: placements)));

        Assert.Contains("seven real liberties", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupRejectsOccupiedCenterBeforeOtherStructuralChecks()
    {
        var placements = StandardPlacements().Select(stone =>
            stone is { Color: StoneColor.Black, X: 3, Y: 2 }
                ? stone with { X = 4, Y = 4 }
                : stone);

        var exception = Assert.Throws<ArgumentException>(() => CreateSetup(
            Position(placements: placements)));

        Assert.Contains("center point empty", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetupRejectsNonPositiveTurnLimit(int turnLimit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CoreDuelBattleSetupDefinition.Create(
                Position(),
                turnLimit,
                FacilityPolicy(),
                CounterattackPolicy(),
                counterattackStartGaugeUnits: 20));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(200)]
    public void SetupRejectsInvalidNonPendingStartGauge(int gaugeUnits)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            CoreDuelBattleSetupDefinition.Create(
                Position(),
                playerTurnLimit: 20,
                FacilityPolicy(),
                CounterattackPolicy(),
                gaugeUnits));
    }

    private static CoreDuelBattleSetupDefinition CreateSetup(
        InitialPositionDefinition position) =>
        CoreDuelBattleSetupDefinition.Create(
            position,
            playerTurnLimit: 20,
            FacilityPolicy(),
            CounterattackPolicy(),
            counterattackStartGaugeUnits: 20);

    private static InitialPositionDefinition Position(
        string id = "standard_v0_2",
        IEnumerable<InitialPlacement>? placements = null)
    {
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        return InitialPositionDefinition.Create(
            geometry,
            id,
            (placements ?? StandardPlacements()).Select(stone =>
                new InitialStonePlacement(
                    stone.Color,
                    stone.Role,
                    geometry.CreateCanonicalPoint(stone.X, stone.Y))));
    }

    private static InitialPlacement[] StandardPlacements() =>
    [
        new(StoneColor.Black, InitialStoneRole.King, 2, 2),
        new(StoneColor.Black, InitialStoneRole.Guard, 2, 3),
        new(StoneColor.Black, InitialStoneRole.Guard, 3, 2),
        new(StoneColor.White, InitialStoneRole.Guard, 6, 5),
        new(StoneColor.White, InitialStoneRole.Guard, 5, 6),
        new(StoneColor.White, InitialStoneRole.King, 6, 6),
    ];

    private static FacilityRuntimePolicy FacilityPolicy() =>
        FacilityRuntimePolicy.Create(
            territoryIncomeDivisor: 3,
            [new FacilityCapacityBand(1, 49, 1)],
            slotCap: 1,
            [new KeyValuePair<string, int>("default", 1)]);

    private static CounterattackBoundaryPolicy CounterattackPolicy() =>
        new(
            thresholdUnits: 200,
            enemyTurnEndNaturalGainUnits: 12,
            sacrificeStonesPerBatch: 3,
            sacrificeUnitsPerBatch: 30);

    private sealed record InitialPlacement(
        StoneColor Color,
        InitialStoneRole Role,
        int X,
        int Y);
}
