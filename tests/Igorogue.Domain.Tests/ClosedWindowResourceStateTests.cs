using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

public sealed class ClosedWindowResourceStateTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, TemporaryLibertyFixture>> Fixtures =
        new(TemporaryLibertyFixtureData.LoadFixtures);

    [Fact]
    public void ResourceStateCanonicalizesChoicesAndFirstUseFlags()
    {
        var choices = new[]
        {
            new DeferredPlayerChoice("source_b", "choice_b", 2),
            new DeferredPlayerChoice("source_a", "choice_a", 1),
        };
        var flags = new[]
        {
            new KeyValuePair<string, bool>("flag_b", true),
            new KeyValuePair<string, bool>("flag_a", false),
        };

        var first = ClosedWindowResourceState.Create(
            2,
            3,
            4,
            2,
            choices,
            flags,
            3);
        var reversed = ClosedWindowResourceState.Create(
            2,
            3,
            4,
            2,
            choices.Reverse(),
            flags.Reverse(),
            3);

        Assert.Equal(first.ToCanonicalText(), reversed.ToCanonicalText());
        Assert.Equal(["source_a:choice_a", "source_b:choice_b"],
            first.DeferredPlayerChoices.Select(choice => choice.Id));
        Assert.Equal(["flag_a", "flag_b"], first.FirstUseFlags.Keys);
        Assert.Equal(2, first.StandardCaptureRewardsClaimed);
        Assert.False(first.IsFirstUseConsumed("flag_a"));
        Assert.True(first.IsFirstUseConsumed("flag_b"));
    }

    [Fact]
    public void FirstUseTriggerAppliesItsWholeEffectArrayOnce()
    {
        var fixtureExecution = TemporaryLibertyCaptureBenefitFixtureData.Execute(
            RequiredFixture("TLE-09"));
        var source = ClosedWindowResourceState.Empty(
        [
            new KeyValuePair<string, bool>("bundle.first_use", false),
        ]);
        var trigger = new CaptureBenefitTrigger(
            CaptureBenefitSource.Style("bundle"),
            "bundle.trigger",
            ["bundle"],
            [
                new ReserveDrawCaptureBenefitOperation(2),
                new GainSoulCaptureBenefitOperation(1),
            ],
            "bundle.first_use");

        var first = ClosedWindowCaptureBenefitResolver.Resolve(
            fixtureExecution.BenefitResolution.CaptureBatch,
            source,
            CounterattackBoundaryState.Create(0, false, 0, fixtureExecution.Policy),
            fixtureExecution.Policy,
            [trigger]);
        Assert.Equal(2, first.ResourcesAfterResolution.TurnReservedDraw);
        Assert.Equal(1, first.ResourcesAfterResolution.Soul);
        Assert.True(first.ResourcesAfterResolution.IsFirstUseConsumed("bundle.first_use"));
        Assert.Equal(
            ["bundle:reserve_draw_2", "bundle:soul_1"],
            first.OrderedFacts
                .OfType<ICaptureBenefitAppliedFact>()
                .Select(fact => fact.EventId));

        var second = ClosedWindowCaptureBenefitResolver.Resolve(
            fixtureExecution.BenefitResolution.CaptureBatch,
            first.ResourcesAfterResolution,
            first.CounterattackAfterResolution,
            fixtureExecution.Policy,
            [trigger]);
        Assert.Same(first.ResourcesAfterResolution, second.ResourcesAfterResolution);
        Assert.Empty(second.OrderedFacts.OfType<ICaptureBenefitAppliedFact>());
        Assert.Empty(second.OrderedFacts.OfType<FirstUseFlagConsumedFact>());
    }

    [Fact]
    public void ResourceStateRejectsNegativeDuplicateAndNonMonotonicInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ClosedWindowResourceState.Create(-1, 0, 0, [], [], 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ClosedWindowResourceState.Create(0, 0, 0, -1, [], [], 1));
        Assert.Throws<ArgumentException>(() =>
            ClosedWindowResourceState.Create(
                0,
                0,
                0,
                [],
                [
                    new KeyValuePair<string, bool>("duplicate", false),
                    new KeyValuePair<string, bool>("duplicate", true),
                ],
                1));
        Assert.Throws<ArgumentException>(() =>
            ClosedWindowResourceState.Create(
                0,
                0,
                0,
                [
                    new DeferredPlayerChoice("one", "choice", 1),
                    new DeferredPlayerChoice("two", "choice", 1),
                ],
                [],
                2));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ClosedWindowResourceState.Create(
                0,
                0,
                0,
                [new DeferredPlayerChoice("one", "choice", 1)],
                [],
                1));
        Assert.Throws<KeyNotFoundException>(() =>
            ClosedWindowResourceState.Empty([]).IsFirstUseConsumed("undeclared"));

        var fixtureExecution = TemporaryLibertyCaptureBenefitFixtureData.Execute(
            RequiredFixture("TLE-09"));
        var undeclaredTrigger = new CaptureBenefitTrigger(
            CaptureBenefitSource.Style("undeclared_style"),
            "undeclared.trigger",
            ["undeclared"],
            [new ReserveDrawCaptureBenefitOperation(1)],
            "undeclared.first_use");
        Assert.Throws<KeyNotFoundException>(() =>
            ClosedWindowCaptureBenefitResolver.Resolve(
                fixtureExecution.BenefitResolution.CaptureBatch,
                ClosedWindowResourceState.Empty([]),
                CounterattackBoundaryState.Create(
                    0,
                    false,
                    0,
                    fixtureExecution.Policy),
                fixtureExecution.Policy,
                [undeclaredTrigger]));
    }

    [Fact]
    public void TriggerIdentityParticipatesInCanonicalResolution()
    {
        var fixtureExecution = TemporaryLibertyCaptureBenefitFixtureData.Execute(
            RequiredFixture("TLE-09"));
        var firstTrigger = new CaptureBenefitTrigger(
            CaptureBenefitSource.StandardAccounting("same_source", 0),
            "trigger.first",
            ["same_event"],
            [new ReserveDrawCaptureBenefitOperation(1)],
            firstUseFlagId: null);
        var secondTrigger = new CaptureBenefitTrigger(
            CaptureBenefitSource.StandardAccounting("same_source", 0),
            "trigger.second",
            ["same_event"],
            [new ReserveDrawCaptureBenefitOperation(1)],
            firstUseFlagId: null);

        var first = Resolve(fixtureExecution, firstTrigger);
        var second = Resolve(fixtureExecution, secondTrigger);

        Assert.Equal(
            "trigger.first",
            Assert.Single(first.OrderedFacts.OfType<ICaptureBenefitAppliedFact>()).TriggerId);
        Assert.Equal(
            "trigger.second",
            Assert.Single(second.OrderedFacts.OfType<ICaptureBenefitAppliedFact>()).TriggerId);
        Assert.NotEqual(first.CanonicalText, second.CanonicalText);
        Assert.NotEqual(first.Checksum, second.Checksum);
    }

    private static ClosedWindowCaptureBenefitResolution Resolve(
        TemporaryLibertyCaptureBenefitFixtureExecution execution,
        CaptureBenefitTrigger trigger) =>
        ClosedWindowCaptureBenefitResolver.Resolve(
            execution.BenefitResolution.CaptureBatch,
            ClosedWindowResourceState.Empty([]),
            CounterattackBoundaryState.Create(0, false, 0, execution.Policy),
            execution.Policy,
            [trigger]);

    private static TemporaryLibertyFixture RequiredFixture(string fixtureId) =>
        Fixtures.Value.TryGetValue(fixtureId, out var fixture)
            ? fixture
            : throw new KeyNotFoundException(fixtureId);
}
