using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

public sealed class TemporaryLibertyCaptureBenefitFixtureTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, TemporaryLibertyFixture>> Fixtures =
        new(TemporaryLibertyFixtureData.LoadFixtures);

    [Theory]
    [InlineData("TLE-09", 9)]
    [InlineData("TLE-10", 10)]
    [InlineData("TLE-15", 15)]
    public void ExpiryResolutionExposesCanonicalCommonCaptureBatch(
        string fixtureId,
        int enemyTurnIndex)
    {
        var resolution = TemporaryLibertyFixtureData.Execute(
            RequiredFixture(fixtureId)).Resolution;
        var batch = Assert.IsType<CaptureBatch>(resolution.CaptureBatch);

        Assert.Equal($"temporary_liberty_expiry_{enemyTurnIndex}", batch.BatchId);
        Assert.Equal("temporary_liberty_expired", batch.ReasonId);
        Assert.Equal(
            CaptureBoundary.EnemyTurnTemporaryLibertyExpirySweep,
            batch.Boundary);
        Assert.Equal(enemyTurnIndex, batch.BoundaryEnemyTurnIndex);
        Assert.Equal(CapturingWindow.ClosedPlayerWindow, batch.CapturingWindow);
        Assert.Equal(
            batch.CapturedGroups.OrderBy(group => group.GroupAnchor),
            batch.CapturedGroups);
        Assert.Equal(
            resolution.CapturedGroups.Select(group => group.Anchor),
            batch.CapturedGroups.Select(group => group.GroupAnchor));
        Assert.Equal(
            resolution.CapturedStoneInstances.Select(instance => instance.InstanceId),
            batch.CapturedStoneInstances.Select(instance => instance.InstanceId));
        Assert.All(batch.CapturedGroups, group =>
            Assert.Equal(
                group.StoneInstances.OrderBy(instance => instance.Point),
                group.StoneInstances));
        Assert.False(string.IsNullOrWhiteSpace(batch.CanonicalText));
    }

    [Fact]
    public void CaptureBatchRejectsForeignOrDuplicateGroupsAndNoCaptureHasNoBatch()
    {
        var execution = TemporaryLibertyFixtureData.Execute(RequiredFixture("TLE-09"));
        var validGroup = Assert.Single(execution.Resolution.CapturedGroups);
        var sourceBoard = execution.SourceStones.SourceBoard;
        var foreignBoard = BoardState.Create(
            sourceBoard.Geometry,
            sourceBoard.OccupiedStones.Select(stone => new BoardStone(
                stone.Color,
                stone.IsKing,
                stone.Point)));
        var foreignGroup = Assert.IsType<StoneGroup>(
            StoneGroupAnalyzer.Analyze(foreignBoard).GroupAt(validGroup.Anchor));

        Assert.Throws<ArgumentException>(() => CaptureBatch.Create(
            "foreign_group",
            "test_capture",
            CaptureBoundary.EnemyTurnTemporaryLibertyExpirySweep,
            9,
            CapturingWindow.ClosedPlayerWindow,
            execution.SourceStones,
            [foreignGroup]));
        Assert.Throws<ArgumentException>(() => CaptureBatch.Create(
            "duplicate_group",
            "test_capture",
            CaptureBoundary.EnemyTurnTemporaryLibertyExpirySweep,
            9,
            CapturingWindow.ClosedPlayerWindow,
            execution.SourceStones,
            [validGroup, validGroup]));
        Assert.Null(TemporaryLibertyFixtureData.Execute(
            RequiredFixture("TLE-13")).Resolution.CaptureBatch);
    }

    [Fact]
    public void MultiGroupCaptureBatchCanonicalizesReversedGroupEnumeration()
    {
        var execution = TemporaryLibertyFixtureData.Execute(RequiredFixture("TLE-03"));
        var first = Assert.IsType<CaptureBatch>(execution.Resolution.CaptureBatch);
        var reversed = CaptureBatch.Create(
            first.BatchId,
            first.ReasonId,
            first.Boundary,
            first.BoundaryEnemyTurnIndex,
            first.CapturingWindow,
            execution.SourceStones,
            execution.Resolution.CapturedGroups.Reverse());

        Assert.Equal(2, first.CapturedGroups.Count);
        Assert.Equal(first.CanonicalText, reversed.CanonicalText);
        Assert.Equal(
            first.CapturedGroups.Select(group => group.GroupAnchor),
            reversed.CapturedGroups.Select(group => group.GroupAnchor));
    }

    [Fact]
    public void CaptureBatchCanonicalIncludesFullPublicStoneRuntimeProjection()
    {
        var execution = TemporaryLibertyFixtureData.Execute(RequiredFixture("TLE-09"));
        var source = execution.SourceStones;
        var capturedGroup = Assert.Single(execution.Resolution.CapturedGroups);
        var changedInstances = source.Instances.Select(instance =>
            instance.Point == capturedGroup.Anchor
                ? new StoneRuntimeInstance(
                    instance.InstanceId,
                    instance.Stone,
                    instance.KindId,
                    100,
                    ["effect.changed"])
                : new StoneRuntimeInstance(
                    instance.InstanceId,
                    instance.Stone,
                    instance.KindId,
                    instance.CreatedSequence,
                    instance.OrderedEffectMetadata)).ToArray();
        var changedRuntime = StoneRuntimeState.Create(
            source.SourceBoard,
            changedInstances,
            101);
        var first = Assert.IsType<CaptureBatch>(execution.Resolution.CaptureBatch);
        var changed = CaptureBatch.Create(
            first.BatchId,
            first.ReasonId,
            first.Boundary,
            first.BoundaryEnemyTurnIndex,
            first.CapturingWindow,
            changedRuntime,
            [capturedGroup]);

        Assert.NotEqual(first.CanonicalText, changed.CanonicalText);
        Assert.Equal(
            ["effect.changed"],
            changed.CapturedStoneInstances[0].OrderedEffectMetadata);
        Assert.Equal(100, changed.CapturedStoneInstances[0].CreatedSequence);
    }

    [Fact]
    public void Tle09AppliesStoneStyleSealAndSacrificeRemainderExactly()
    {
        var fixture = RequiredFixture("TLE-09");
        var execution = TemporaryLibertyCaptureBenefitFixtureData.Execute(fixture);
        var batch = execution.BenefitResolution.CaptureBatch;
        var resources = execution.BenefitResolution.ResourcesAfterResolution;
        var counterattack = execution.BenefitResolution.CounterattackAfterResolution;

        var group = Assert.Single(batch.CapturedGroups);
        Assert.Equal(StoneColor.Black, group.Color);
        Assert.Equal(StoneColor.White, group.CapturingColor);
        Assert.Equal(
            ["stone_lure", "stone_blood"],
            group.StoneInstances.Select(stone => stone.InstanceId));
        Assert.Equal(["lure", "blood"], group.StoneInstances.Select(stone => stone.KindId));
        Assert.Equal(2, batch.NonKingBlackStoneCount);
        Assert.False(batch.ContainsKing);

        Assert.Equal(Assert.IsType<int>(fixture.Expected.ReservedDrawDelta), resources.TurnReservedDraw);
        Assert.Equal(Assert.IsType<int>(fixture.Expected.SoulDelta), resources.Soul);
        Assert.Equal(0, resources.TurnReservedQi);
        Assert.Empty(resources.DeferredPlayerChoices);
        Assert.True(resources.IsFirstUseConsumed("style_sacrifice.first_capture"));
        Assert.True(resources.IsFirstUseConsumed("seal_sacrifice.first_capture"));
        Assert.Equal(
            Assert.IsType<int>(fixture.Expected.SacrificeRemainder),
            counterattack.SacrificeStoneRemainder);
        Assert.Equal(0, counterattack.GaugeUnits);
        Assert.False(counterattack.Pending);
        Assert.Empty(execution.BenefitResolution.OrderedFacts.OfType<CounterattackAdvancedFact>());
        Assert.Equal(
            Assert.IsType<int>(fixture.Expected.CounterattackDeltaUnits),
            execution.BenefitResolution.OrderedFacts
                .OfType<CounterattackAdvancedFact>()
                .Sum(fact => fact.DeltaUnits));
        Assert.Empty(
            execution.BenefitResolution.OrderedFacts.OfType<SacrificeBatchAdvancedFact>());
        var remainder = Assert.Single(
            execution.BenefitResolution.OrderedFacts.OfType<SacrificeRemainderChangedFact>());
        Assert.Equal(0, remainder.RemainderBefore);
        Assert.Equal(2, remainder.RemainderAfter);

        Assert.NotNull(fixture.Expected.BenefitEventOrder);
        Assert.Equal(
            fixture.Expected.BenefitEventOrder,
            execution.BenefitResolution.OrderedFacts
                .OfType<ICaptureBenefitAppliedFact>()
                .Select(fact => fact.EventId));
    }

    [Fact]
    public void Tle10CreditsBlackAndDefersClosedWindowRewardsExactly()
    {
        var fixture = RequiredFixture("TLE-10");
        var execution = TemporaryLibertyCaptureBenefitFixtureData.Execute(fixture);
        var batch = execution.BenefitResolution.CaptureBatch;
        var resources = execution.BenefitResolution.ResourcesAfterResolution;
        var group = Assert.Single(batch.CapturedGroups);

        Assert.Equal(StoneColor.White, group.Color);
        Assert.Equal(StoneColor.Black, group.CapturingColor);
        Assert.NotNull(fixture.Expected.CapturingColors);
        Assert.Equal(
            fixture.Expected.CapturingColors,
            batch.CapturedGroups.Select(group => ColorId(group.CapturingColor)));
        Assert.Equal(Assert.IsType<int>(fixture.Expected.SoulDelta), resources.Soul);
        Assert.Equal(Assert.IsType<int>(fixture.Expected.ReservedDrawDelta), resources.TurnReservedDraw);
        Assert.Equal(Assert.IsType<int>(fixture.Expected.ReservedQiDelta), resources.TurnReservedQi);
        Assert.NotNull(fixture.Expected.DeferredChoices);
        Assert.Equal(
            fixture.Expected.DeferredChoices,
            resources.DeferredPlayerChoices.Select(choice => choice.Id));
        Assert.True(resources.IsFirstUseConsumed("seal_bone.first_capture"));

        Assert.Equal(
            [
                "standard_capture:soul_1",
                "capture_chain:reserve_qi_1",
                "capture_chain:reserve_draw_2",
                "seal_bone:deferred_choice",
                "relic_hungry_furnace:reserve_qi_2",
            ],
            execution.BenefitResolution.OrderedFacts
                .OfType<ICaptureBenefitAppliedFact>()
                .Select(fact => fact.EventId));
    }

    [Fact]
    public void Tle15SacrificeAdvancePrecedesNaturalGainAndPrimesExactly()
    {
        var fixture = RequiredFixture("TLE-15");
        var execution = TemporaryLibertyCaptureBenefitFixtureData.Execute(fixture);
        var afterSacrifice = execution.BenefitResolution.CounterattackAfterResolution;

        Assert.Equal(190, afterSacrifice.GaugeUnits);
        Assert.False(afterSacrifice.Pending);
        Assert.Equal(0, afterSacrifice.SacrificeStoneRemainder);
        Assert.Empty(execution.BenefitResolution.OrderedFacts.OfType<ICaptureBenefitAppliedFact>());
        Assert.True(
            execution.BenefitResolution.ResourcesAfterResolution
                .IsFirstUseConsumed("style_sacrifice.first_capture"));

        var natural = CounterattackBoundaryResolver.AdvanceEnemyTurnEnd(
            afterSacrifice,
            execution.Policy);
        Assert.Equal(
            Assert.IsType<int>(fixture.Expected.EndCounterattackUnits),
            natural.StateAfterTransition.GaugeUnits);
        Assert.Equal(
            Assert.IsType<bool>(fixture.Expected.Pending),
            natural.StateAfterTransition.Pending);
        Assert.Equal(
            Assert.IsType<int>(fixture.Expected.SacrificeRemainder),
            natural.StateAfterTransition.SacrificeStoneRemainder);

        var advances = execution.BenefitResolution.OrderedFacts
            .Concat(natural.OrderedFacts)
            .OfType<CounterattackAdvancedFact>()
            .Select(fact => new TemporaryLibertyFixtureCounterattackAdvance(
                fact.ReasonId,
                fact.DeltaUnits));
        Assert.NotNull(fixture.Expected.CounterattackAdvances);
        Assert.Equal(fixture.Expected.CounterattackAdvances, advances);
    }

    [Theory]
    [InlineData("TLE-07")]
    [InlineData("TLE-08")]
    public void TerminalExpiryCaptureSuppressesEveryBenefitOperation(string fixtureId)
    {
        var fixture = RequiredFixture(fixtureId);
        var execution = TemporaryLibertyCaptureBenefitFixtureData.Execute(fixture);
        var resolution = execution.BenefitResolution;

        Assert.True(resolution.CaptureBatch.ContainsKing);
        Assert.True(resolution.BenefitsSuppressed);
        Assert.Same(resolution.SourceResources, resolution.ResourcesAfterResolution);
        Assert.Same(
            resolution.SourceCounterattack,
            resolution.CounterattackAfterResolution);
        Assert.Empty(resolution.OrderedFacts.OfType<ICaptureBenefitAppliedFact>());
        Assert.Empty(resolution.OrderedFacts.OfType<SacrificeBatchAdvancedFact>());
        Assert.Empty(resolution.OrderedFacts.OfType<CounterattackAdvancedFact>());
        Assert.Empty(resolution.OrderedFacts.OfType<CaptureBenefitSuppressedFact>());
        Assert.Single(
            execution.ExpiryExecution.Resolution.OrderedFacts
                .OfType<CaptureBenefitSuppressedFact>());
        Assert.Collection(
            resolution.OrderedFacts,
            fact => Assert.IsType<CaptureBatchStartedFact>(fact),
            fact => Assert.True(
                Assert.IsType<CaptureBatchResolvedFact>(fact).BenefitsSuppressed));

        var guarded = ClosedWindowCaptureBenefitResolver.Resolve(
            resolution.CaptureBatch,
            resolution.SourceResources,
            resolution.SourceCounterattack,
            resolution.Policy,
            new ThrowingEnumerable<CaptureBenefitTrigger>());
        Assert.True(guarded.BenefitsSuppressed);
        Assert.Empty(guarded.OrderedTriggers);
    }

    [Fact]
    public void TriggerSourcesRejectForeignAndDuplicateCapturedStoneOrSacrificeUse()
    {
        var execution = TemporaryLibertyCaptureBenefitFixtureData.Execute(
            RequiredFixture("TLE-09"));
        var batch = execution.BenefitResolution.CaptureBatch;
        var sourceStoneId = batch.CapturedStoneInstances[0].InstanceId;
        var firstStone = new CaptureBenefitTrigger(
            CaptureBenefitSource.CapturedStoneSelf(sourceStoneId),
            "stone.first",
            ["stone_first"],
            [new ReserveDrawCaptureBenefitOperation(1)],
            firstUseFlagId: null);
        var duplicateStone = new CaptureBenefitTrigger(
            CaptureBenefitSource.CapturedStoneSelf(sourceStoneId),
            "stone.duplicate",
            ["stone_duplicate"],
            [new ReserveDrawCaptureBenefitOperation(1)],
            firstUseFlagId: null);
        var foreignStone = new CaptureBenefitTrigger(
            CaptureBenefitSource.CapturedStoneSelf("foreign_stone"),
            "stone.foreign",
            ["stone_foreign"],
            [new ReserveDrawCaptureBenefitOperation(1)],
            firstUseFlagId: null);
        var sacrificeOne = new CaptureBenefitTrigger(
            CaptureBenefitSource.Sacrifice(),
            "sacrifice.one",
            ["sacrifice_one"],
            [new AdvanceSacrificePressureCaptureBenefitOperation()],
            firstUseFlagId: null);
        var sacrificeTwo = new CaptureBenefitTrigger(
            CaptureBenefitSource.Sacrifice(),
            "sacrifice.two",
            ["sacrifice_two"],
            [new AdvanceSacrificePressureCaptureBenefitOperation()],
            firstUseFlagId: null);
        var reorderedSourceOne = new CaptureBenefitTrigger(
            CaptureBenefitSource.StandardAccounting("same_logical_source", 0),
            "source.one",
            ["source_one"],
            [new ReserveDrawCaptureBenefitOperation(1)],
            firstUseFlagId: null);
        var reorderedSourceTwo = new CaptureBenefitTrigger(
            CaptureBenefitSource.StandardAccounting("same_logical_source", 99),
            "source.two",
            ["source_two"],
            [new ReserveDrawCaptureBenefitOperation(1)],
            firstUseFlagId: null);

        Assert.Throws<ArgumentException>(() => Resolve(
            execution,
            [firstStone, duplicateStone]));
        Assert.Throws<ArgumentException>(() => Resolve(execution, [foreignStone]));
        Assert.Throws<ArgumentException>(() => Resolve(
            execution,
            [sacrificeOne, sacrificeTwo]));
        Assert.Throws<ArgumentException>(() => Resolve(
            execution,
            [reorderedSourceOne, reorderedSourceTwo]));
    }

    [Theory]
    [InlineData("TLE-09")]
    [InlineData("TLE-10")]
    [InlineData("TLE-15")]
    public void SameAndReversedTriggerSourcesProduceIdenticalCanonicalStateAndFacts(
        string fixtureId)
    {
        var fixture = RequiredFixture(fixtureId);
        var first = TemporaryLibertyCaptureBenefitFixtureData.Execute(fixture);
        var second = TemporaryLibertyCaptureBenefitFixtureData.Execute(fixture);
        var reversed = TemporaryLibertyCaptureBenefitFixtureData.Execute(
            fixture,
            reverseEnumeration: true);

        Assert.Equal(
            first.BenefitResolution.CaptureBatch.CanonicalText,
            reversed.BenefitResolution.CaptureBatch.CanonicalText);
        Assert.Equal(
            first.BenefitResolution.CanonicalText,
            second.BenefitResolution.CanonicalText);
        Assert.Equal(
            first.BenefitResolution.CanonicalText,
            reversed.BenefitResolution.CanonicalText);
        Assert.Equal(first.BenefitResolution.Checksum, second.BenefitResolution.Checksum);
        Assert.Equal(first.BenefitResolution.Checksum, reversed.BenefitResolution.Checksum);
        Assert.Equal(
            first.BenefitResolution.ResourcesAfterResolution.ToCanonicalText(),
            reversed.BenefitResolution.ResourcesAfterResolution.ToCanonicalText());
        Assert.Equal(
            first.BenefitResolution.CounterattackAfterResolution.ToCanonicalText(),
            reversed.BenefitResolution.CounterattackAfterResolution.ToCanonicalText());
    }

    [Fact]
    public void AcceptedStageOrderOverridesInputEnumerationOrder()
    {
        var fixtureExecution = TemporaryLibertyCaptureBenefitFixtureData.Execute(
            RequiredFixture("TLE-15"));
        var batch = fixtureExecution.BenefitResolution.CaptureBatch;
        var resourceStages = new[]
        {
            CaptureBenefitStage.StandardAccounting,
            CaptureBenefitStage.SourceOrArmedEffect,
            CaptureBenefitStage.CapturedStoneSelf,
            CaptureBenefitStage.StyleOrSeal,
            CaptureBenefitStage.Relic,
            CaptureBenefitStage.Facility,
            CaptureBenefitStage.EnemyPassive,
            CaptureBenefitStage.ScoreOrTelemetry,
        };
        var triggers = resourceStages
            .Select((stage, index) => new CaptureBenefitTrigger(
                SourceForStage(stage, index, batch),
                $"stage.{index}",
                [$"stage_{index}"],
                [new ReserveDrawCaptureBenefitOperation(1)],
                firstUseFlagId: null))
            .Append(new CaptureBenefitTrigger(
                CaptureBenefitSource.Sacrifice(),
                "stage.sacrifice",
                ["stage_sacrifice"],
                [new AdvanceSacrificePressureCaptureBenefitOperation()],
                firstUseFlagId: null))
            .Reverse()
            .ToArray();

        var resolution = ClosedWindowCaptureBenefitResolver.Resolve(
            batch,
            ClosedWindowResourceState.Empty([]),
            CounterattackBoundaryState.Create(0, false, 0, fixtureExecution.Policy),
            fixtureExecution.Policy,
            triggers);
        var facts = resolution.OrderedFacts.ToArray();

        Assert.IsType<CaptureBatchStartedFact>(facts[0]);
        Assert.Equal(
            Enumerable.Range(0, 7).Select(index => $"stage_{index}:reserve_draw_1"),
            facts.Skip(1).Take(7).Cast<ICaptureBenefitAppliedFact>()
                .Select(fact => fact.EventId));
        Assert.IsType<SacrificeRemainderChangedFact>(facts[8]);
        Assert.IsType<SacrificeBatchAdvancedFact>(facts[9]);
        Assert.IsType<CounterattackAdvancedFact>(facts[10]);
        Assert.Equal(
            "stage_7:reserve_draw_1",
            Assert.IsAssignableFrom<ICaptureBenefitAppliedFact>(facts[11]).EventId);
        Assert.IsType<CaptureBatchResolvedFact>(facts[12]);
    }

    private static TemporaryLibertyFixture RequiredFixture(string fixtureId) =>
        Fixtures.Value.TryGetValue(fixtureId, out var fixture)
            ? fixture
            : throw new KeyNotFoundException(fixtureId);

    private static ClosedWindowCaptureBenefitResolution Resolve(
        TemporaryLibertyCaptureBenefitFixtureExecution execution,
        IEnumerable<CaptureBenefitTrigger> triggers) =>
        ClosedWindowCaptureBenefitResolver.Resolve(
            execution.BenefitResolution.CaptureBatch,
            ClosedWindowResourceState.Empty([]),
            CounterattackBoundaryState.Create(0, false, 0, execution.Policy),
            execution.Policy,
            triggers);

    private static CaptureBenefitSource SourceForStage(
        CaptureBenefitStage stage,
        int index,
        CaptureBatch batch) => stage switch
    {
        CaptureBenefitStage.StandardAccounting =>
            CaptureBenefitSource.StandardAccounting($"stage_{index}", index),
        CaptureBenefitStage.SourceOrArmedEffect =>
            CaptureBenefitSource.SourceOrArmedEffect($"stage_{index}", index),
        CaptureBenefitStage.CapturedStoneSelf =>
            CaptureBenefitSource.CapturedStoneSelf(
                batch.CapturedStoneInstances[0].InstanceId),
        CaptureBenefitStage.StyleOrSeal =>
            CaptureBenefitSource.Style($"stage_{index}"),
        CaptureBenefitStage.Relic =>
            CaptureBenefitSource.Relic($"stage_{index}", index),
        CaptureBenefitStage.Facility =>
            CaptureBenefitSource.Facility(
                $"stage_{index}",
                batch.CapturedGroups[0].GroupAnchor),
        CaptureBenefitStage.EnemyPassive =>
            CaptureBenefitSource.EnemyPassive($"stage_{index}"),
        CaptureBenefitStage.ScoreOrTelemetry =>
            CaptureBenefitSource.ScoreOrTelemetry($"stage_{index}", index),
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unexpected stage."),
    };

    private static string ColorId(StoneColor color) => color switch
    {
        StoneColor.Black => "black",
        StoneColor.White => "white",
        _ => throw new InvalidOperationException("Unknown stone color."),
    };

    private sealed class ThrowingEnumerable<T> : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() =>
            throw new InvalidOperationException("Terminal gate enumerated benefit triggers.");

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
