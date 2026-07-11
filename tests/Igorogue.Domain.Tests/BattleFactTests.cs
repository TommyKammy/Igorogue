using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Domain.Tests;

public sealed class BattleFactTests
{
    [Fact]
    public void LifecycleFactsExposeValidatedStablePayloadsForApplication()
    {
        var rejected = new CommandRejectedFact("wrong_phase");
        var passed = new EnemyPassedFact(20);
        var ended = new BattleEndedFact(
            BattleOutcome.PlayerDefeat,
            BattleEndReason.TurnLimit);

        Assert.Equal("wrong_phase", rejected.ReasonId);
        Assert.Equal(20, passed.PlayerTurnIndex);
        Assert.Equal(BattleOutcome.PlayerDefeat, ended.Outcome);
        Assert.Equal(BattleEndReason.TurnLimit, ended.Reason);
        Assert.Equal("turn_limit", ended.ReasonId);
        Assert.All(
            new IBattleFact[] { rejected, passed, ended },
            fact => Assert.IsAssignableFrom<IBattleFact>(fact));
    }

    [Fact]
    public void LifecycleFactsRejectInvalidPayloads()
    {
        Assert.Throws<ArgumentException>(() => new CommandRejectedFact(" "));
        Assert.Throws<ArgumentException>(() => new CommandRejectedFact("bad reason"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyPassedFact(0));
        Assert.Throws<ArgumentException>(() => new BattleEndedFact(
            BattleOutcome.Ongoing,
            BattleEndReason.TurnLimit));
        Assert.Throws<ArgumentException>(() => new BattleEndedFact(
            BattleOutcome.PlayerVictory,
            BattleEndReason.BlackKingCaptured));
        Assert.Throws<ArgumentException>(() => new BattleEndedFact(
            BattleOutcome.PlayerDefeat,
            BattleEndReason.WhiteKingCaptured));
    }

    [Fact]
    public void PlacementAndFacilityFactsShareTheBattleFactMarker()
    {
        Assert.True(typeof(IBattleFact).IsAssignableFrom(
            typeof(ICommittedPlacementFact)));
        Assert.True(typeof(IBattleFact).IsAssignableFrom(
            typeof(FacilityFact)));
    }
}
