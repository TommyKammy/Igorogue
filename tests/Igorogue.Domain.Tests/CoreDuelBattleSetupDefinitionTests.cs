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
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        var position = InitialPositionDefinition.Create(
            geometry,
            "test_position",
            [
                new InitialStonePlacement(
                    StoneColor.White,
                    InitialStoneRole.King,
                    geometry.CreateCanonicalPoint(6, 6)),
                new InitialStonePlacement(
                    StoneColor.Black,
                    InitialStoneRole.King,
                    geometry.CreateCanonicalPoint(2, 2)),
            ]);
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

    private static InitialPositionDefinition Position()
    {
        var geometry = BoardGeometry.Create(BoardGeometry.AcceptedSize);
        return InitialPositionDefinition.Create(
            geometry,
            "test_position",
            [
                new InitialStonePlacement(
                    StoneColor.Black,
                    InitialStoneRole.King,
                    geometry.CreateCanonicalPoint(2, 2)),
                new InitialStonePlacement(
                    StoneColor.White,
                    InitialStoneRole.King,
                    geometry.CreateCanonicalPoint(6, 6)),
            ]);
    }

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
}
