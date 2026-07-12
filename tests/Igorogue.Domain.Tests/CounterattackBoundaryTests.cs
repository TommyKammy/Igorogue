using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

public sealed class CounterattackBoundaryTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, TemporaryLibertyFixture>> Fixtures =
        new(TemporaryLibertyFixtureData.LoadFixtures);

    [Fact]
    public void PendingConsumptionReprimesOverflowOnceBeforeNaturalGain()
    {
        var fixtureExecution = TemporaryLibertyCaptureBenefitFixtureData.Execute(
            RequiredFixture("TLE-15"));
        var policy = fixtureExecution.Policy;
        var source = CounterattackBoundaryState.Create(450, pending: true, 0, policy);
        var pendingAtStart = CounterattackBoundaryResolver
            .SnapshotPendingAtEnemyTurnStart(source, policy);

        var consumed = CounterattackBoundaryResolver.ConsumeAndReprimeOnce(
            source,
            pendingAtStart,
            policy);
        Assert.Equal(250, consumed.StateAfterTransition.GaugeUnits);
        Assert.True(consumed.StateAfterTransition.Pending);
        Assert.True(Assert.Single(
            consumed.OrderedFacts.OfType<CounterattackPendingConsumedFact>()).Reprimed);
        Assert.Equal(
            CounterattackPrimeReason.OverflowAfterPendingConsumption,
            Assert.Single(
                consumed.OrderedFacts.OfType<CounterattackPendingPrimedFact>()).Reason);

        Assert.Throws<InvalidOperationException>(() =>
            CounterattackBoundaryResolver.ConsumeAndReprimeOnce(
                consumed.StateAfterTransition,
                pendingAtStart,
                policy));
        var foreign = CounterattackBoundaryState.Create(450, pending: true, 0, policy);
        Assert.Throws<InvalidOperationException>(() =>
            CounterattackBoundaryResolver.ConsumeAndReprimeOnce(
                foreign,
                pendingAtStart,
                policy));

        var afterNatural = CounterattackBoundaryResolver.AdvanceEnemyTurnEnd(
            consumed.StateAfterTransition,
            policy);
        Assert.Equal(262, afterNatural.StateAfterTransition.GaugeUnits);
        Assert.True(afterNatural.StateAfterTransition.Pending);
        Assert.Empty(afterNatural.OrderedFacts.OfType<CounterattackPendingPrimedFact>());
    }

    [Fact]
    public void SnapshotWithoutPendingDoesNotConsumeAStatePrimedLater()
    {
        var fixtureExecution = TemporaryLibertyCaptureBenefitFixtureData.Execute(
            RequiredFixture("TLE-15"));
        var policy = fixtureExecution.Policy;
        var source = CounterattackBoundaryState.Create(199, pending: false, 0, policy);
        var pendingAtStart = CounterattackBoundaryResolver
            .SnapshotPendingAtEnemyTurnStart(source, policy);
        var consume = CounterattackBoundaryResolver.ConsumeAndReprimeOnce(
            source,
            pendingAtStart,
            policy);
        Assert.Same(source, consume.StateAfterTransition);
        Assert.Empty(consume.OrderedFacts);

        var primed = CounterattackBoundaryResolver.AdvanceEnemyTurnEnd(
            consume.StateAfterTransition,
            policy);
        Assert.True(primed.StateAfterTransition.Pending);
    }

    [Fact]
    public void SacrificeRemainderPersistsAndCompletesBatchesFromCaptureBatchStoneCount()
    {
        var execution = TemporaryLibertyCaptureBenefitFixtureData.Execute(
            RequiredFixture("TLE-09"));
        var source = CounterattackBoundaryState.Create(10, pending: false, 2, execution.Policy);

        var transition = CounterattackBoundaryResolver.AdvanceSacrifice(
            source,
            execution.BenefitResolution.CaptureBatch,
            execution.Policy);

        Assert.Equal(40, transition.StateAfterTransition.GaugeUnits);
        Assert.False(transition.StateAfterTransition.Pending);
        Assert.Equal(1, transition.StateAfterTransition.SacrificeStoneRemainder);
        var sacrifice = Assert.Single(
            transition.OrderedFacts.OfType<SacrificeBatchAdvancedFact>());
        Assert.Equal(1, sacrifice.CompletedBatchCount);
        Assert.Equal(30, sacrifice.DeltaUnits);
        Assert.Equal(
            CounterattackAdvanceReason.SacrificeBatch,
            Assert.Single(
                transition.OrderedFacts.OfType<CounterattackAdvancedFact>()).Reason);
    }

    [Fact]
    public void CounterattackStateAndPolicyAreCanonicalAndValidateRemainderBoundary()
    {
        var policy = new CounterattackBoundaryPolicy(200, 12, 3, 30);
        var state = CounterattackBoundaryState.Create(160, false, 2, policy);

        Assert.Equal(
            "counterattack-boundary-state-v1\n" +
            "gauge_units=160\n" +
            "pending=0\n" +
            "sacrifice_stone_remainder=2",
            state.ToCanonicalText());
        Assert.Contains("threshold_units=200", policy.ToCanonicalText(), StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() =>
            CounterattackBoundaryState.Create(0, false, 3, policy));
        Assert.Throws<ArgumentException>(() =>
            CounterattackBoundaryState.Create(200, false, 0, policy));
    }

    private static TemporaryLibertyFixture RequiredFixture(string fixtureId) =>
        Fixtures.Value.TryGetValue(fixtureId, out var fixture)
            ? fixture
            : throw new KeyNotFoundException(fixtureId);
}
