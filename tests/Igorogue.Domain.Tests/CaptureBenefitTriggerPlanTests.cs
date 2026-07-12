using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

public sealed class CaptureBenefitTriggerPlanTests
{
    [Fact]
    public void PlanCanonicalizesInputEnumerationByFullTriggerProjection()
    {
        var triggers = new[]
        {
            Trigger(
                CaptureBenefitSource.Style("style_source"),
                "trigger.style",
                "style_event",
                new GainSoulCaptureBenefitOperation(1)),
            Trigger(
                CaptureBenefitSource.StandardAccounting("standard_source", 0),
                "trigger.standard",
                "standard_event",
                new ReserveDrawCaptureBenefitOperation(2)),
        };

        var first = CaptureBenefitTriggerPlan.Create(triggers);
        var reversed = CaptureBenefitTriggerPlan.Create(triggers.Reverse());

        Assert.Equal(
            triggers.OrderBy(trigger => trigger.ToCanonicalText(), StringComparer.Ordinal),
            first.Triggers);
        Assert.Equal(first.Triggers, reversed.Triggers);
        Assert.Equal(first.CanonicalText, first.ToCanonicalText());
        Assert.Equal(first.CanonicalText, reversed.CanonicalText);
        Assert.Equal(first.Checksum, reversed.Checksum);
        Assert.Equal(64, first.Checksum.Length);
        Assert.All(first.Entries, entry =>
        {
            Assert.Equal(CaptureBenefitTriggerCondition.AnyCapture, entry.Condition);
            Assert.Equal(
                CaptureBenefitTriggerMaterializationMode.Fixed,
                entry.MaterializationMode);
        });
        Assert.Equal(
            first.Triggers,
            first.SelectFor(MixedCaptureBatches().MixedBatch));

        var mutableView = Assert.IsAssignableFrom<ICollection<CaptureBenefitTrigger>>(
            first.Triggers);
        Assert.Throws<NotSupportedException>(() => mutableView.Add(triggers[0]));
    }

    [Fact]
    public void PlanCanonicalBindsOperationAndFirstUseInputs()
    {
        var first = CaptureBenefitTriggerPlan.Create(
        [
            new CaptureBenefitTrigger(
                CaptureBenefitSource.Style("source"),
                "trigger",
                ["event"],
                [new ReserveDrawCaptureBenefitOperation(1)],
                "source.first_use"),
        ]);
        var changedOperation = CaptureBenefitTriggerPlan.Create(
        [
            new CaptureBenefitTrigger(
                CaptureBenefitSource.Style("source"),
                "trigger",
                ["event"],
                [new ReserveDrawCaptureBenefitOperation(2)],
                "source.first_use"),
        ]);
        var changedFirstUse = CaptureBenefitTriggerPlan.Create(
        [
            new CaptureBenefitTrigger(
                CaptureBenefitSource.Style("source"),
                "trigger",
                ["event"],
                [new ReserveDrawCaptureBenefitOperation(1)],
                "source.other_first_use"),
        ]);

        Assert.NotEqual(first.CanonicalText, changedOperation.CanonicalText);
        Assert.NotEqual(first.Checksum, changedOperation.Checksum);
        Assert.NotEqual(first.CanonicalText, changedFirstUse.CanonicalText);
        Assert.NotEqual(first.Checksum, changedFirstUse.Checksum);
    }

    [Fact]
    public void ConditionalPlanSelectsByCaptureAttributionAndMaterializesSoulPerWhiteGroup()
    {
        var (batch, blackOnlyBatch, blackStoneInstanceId) = MixedCaptureBatches();
        var any = Trigger(
            CaptureBenefitSource.SourceOrArmedEffect("any_source", 0),
            "trigger.any",
            "any_event",
            new ReserveDrawCaptureBenefitOperation(1));
        var standardTemplate = Trigger(
            CaptureBenefitSource.StandardAccounting("standard_source", 0),
            "trigger.standard",
            "standard_event",
            new GainSoulCaptureBenefitOperation(1));
        var capturedBlack = Trigger(
            CaptureBenefitSource.Style("black_capture_source"),
            "trigger.black",
            "black_event",
            new ReserveDrawCaptureBenefitOperation(1));
        var capturedSource = Trigger(
            CaptureBenefitSource.CapturedStoneSelf(blackStoneInstanceId),
            "trigger.source_stone",
            "source_stone_event",
            new GainSoulCaptureBenefitOperation(1));
        var foreignSource = Trigger(
            CaptureBenefitSource.CapturedStoneSelf("stone.foreign"),
            "trigger.foreign_stone",
            "foreign_stone_event",
            new GainSoulCaptureBenefitOperation(1));
        var entries = new[]
        {
            new CaptureBenefitTriggerPlanEntry(
                any,
                CaptureBenefitTriggerCondition.AnyCapture),
            new CaptureBenefitTriggerPlanEntry(
                standardTemplate,
                CaptureBenefitTriggerCondition.CapturedWhiteGroup,
                CaptureBenefitTriggerMaterializationMode
                    .GainSoulPerCapturedWhiteGroup),
            new CaptureBenefitTriggerPlanEntry(
                capturedBlack,
                CaptureBenefitTriggerCondition.CapturedNonKingBlackStone),
            new CaptureBenefitTriggerPlanEntry(
                capturedSource,
                CaptureBenefitTriggerCondition.CapturedSourceStone),
            new CaptureBenefitTriggerPlanEntry(
                foreignSource,
                CaptureBenefitTriggerCondition.CapturedSourceStone),
        };

        var plan = CaptureBenefitTriggerPlan.CreateConditional(entries);
        var reversed = CaptureBenefitTriggerPlan.CreateConditional(entries.Reverse());
        var selected = plan.SelectFor(batch);

        Assert.Equal(plan.CanonicalText, reversed.CanonicalText);
        Assert.Equal(plan.Checksum, reversed.Checksum);
        Assert.All(plan.Entries, entry =>
        {
            Assert.Contains("condition=", entry.CanonicalText, StringComparison.Ordinal);
            Assert.Contains(
                "materialization=",
                entry.CanonicalText,
                StringComparison.Ordinal);
        });
        Assert.Equal(
            [
                "trigger.any",
                "trigger.standard",
                "trigger.black",
                "trigger.source_stone",
            ],
            selected.Select(trigger => trigger.TriggerId));
        var materialized = Assert.Single(selected, trigger =>
            trigger.TriggerId == standardTemplate.TriggerId);
        var soul = Assert.IsType<GainSoulCaptureBenefitOperation>(
            Assert.Single(materialized.OrderedOperations));
        Assert.Equal(2, soul.Amount);
        Assert.DoesNotContain(selected, trigger =>
            trigger.TriggerId == foreignSource.TriggerId);

        var blackOnlySelected = plan.SelectFor(blackOnlyBatch);
        Assert.Equal(
            ["trigger.any", "trigger.black", "trigger.source_stone"],
            blackOnlySelected.Select(trigger => trigger.TriggerId));
    }

    [Fact]
    public void ConditionalPlanValidatesSourceStoneAndScalingTemplates()
    {
        var invalidSourceCondition = Trigger(
            CaptureBenefitSource.Style("style_source"),
            "trigger.style",
            "style_event",
            new GainSoulCaptureBenefitOperation(1));
        Assert.Throws<ArgumentException>(() =>
            new CaptureBenefitTriggerPlanEntry(
                invalidSourceCondition,
                CaptureBenefitTriggerCondition.CapturedSourceStone));

        var wrongSource = Trigger(
            CaptureBenefitSource.SourceOrArmedEffect("armed", 0),
            "trigger.armed",
            "armed_event",
            new GainSoulCaptureBenefitOperation(1));
        Assert.Throws<ArgumentException>(() =>
            new CaptureBenefitTriggerPlanEntry(
                wrongSource,
                CaptureBenefitTriggerCondition.CapturedWhiteGroup,
                CaptureBenefitTriggerMaterializationMode
                    .GainSoulPerCapturedWhiteGroup));

        var wrongCondition = Trigger(
            CaptureBenefitSource.StandardAccounting("standard", 0),
            "trigger.standard",
            "standard_event",
            new GainSoulCaptureBenefitOperation(1));
        Assert.Throws<ArgumentException>(() =>
            new CaptureBenefitTriggerPlanEntry(
                wrongCondition,
                CaptureBenefitTriggerCondition.AnyCapture,
                CaptureBenefitTriggerMaterializationMode
                    .GainSoulPerCapturedWhiteGroup));

        var wrongTemplateAmount = Trigger(
            CaptureBenefitSource.StandardAccounting("standard_two", 0),
            "trigger.standard_two",
            "standard_two_event",
            new GainSoulCaptureBenefitOperation(2));
        Assert.Throws<ArgumentException>(() =>
            new CaptureBenefitTriggerPlanEntry(
                wrongTemplateAmount,
                CaptureBenefitTriggerCondition.CapturedWhiteGroup,
                CaptureBenefitTriggerMaterializationMode
                    .GainSoulPerCapturedWhiteGroup));
    }

    [Fact]
    public void PlanRejectsNullAndDuplicateStaticIdentities()
    {
        var first = Trigger(
            CaptureBenefitSource.StandardAccounting("source_one", 0),
            "trigger.one",
            "event_one",
            new ReserveDrawCaptureBenefitOperation(1));
        var duplicateId = Trigger(
            CaptureBenefitSource.StandardAccounting("source_two", 0),
            first.TriggerId,
            "event_two",
            new ReserveDrawCaptureBenefitOperation(1));
        var duplicateSource = Trigger(
            CaptureBenefitSource.StandardAccounting("source_one", 99),
            "trigger.two",
            "event_two",
            new ReserveDrawCaptureBenefitOperation(1));
        var duplicateEventPath = Trigger(
            CaptureBenefitSource.Style("source_two"),
            "trigger.two",
            "event_one",
            new ReserveDrawCaptureBenefitOperation(1));
        var firstUseOne = new CaptureBenefitTrigger(
            CaptureBenefitSource.Style("first_use_one"),
            "trigger.first_use_one",
            ["first_use_one"],
            [new GainSoulCaptureBenefitOperation(1)],
            "shared.first_use");
        var firstUseTwo = new CaptureBenefitTrigger(
            CaptureBenefitSource.Relic("first_use_two", 0),
            "trigger.first_use_two",
            ["first_use_two"],
            [new GainSoulCaptureBenefitOperation(1)],
            "shared.first_use");

        Assert.Throws<ArgumentNullException>(() =>
            CaptureBenefitTriggerPlan.Create(null!));
        Assert.Throws<ArgumentNullException>(() =>
            CaptureBenefitTriggerPlan.Create([null!]));
        Assert.Throws<ArgumentException>(() =>
            CaptureBenefitTriggerPlan.Create([first, duplicateId]));
        Assert.Throws<ArgumentException>(() =>
            CaptureBenefitTriggerPlan.Create([first, duplicateSource]));
        Assert.Throws<ArgumentException>(() =>
            CaptureBenefitTriggerPlan.Create([first, duplicateEventPath]));
        Assert.Throws<ArgumentException>(() =>
            CaptureBenefitTriggerPlan.Create([firstUseOne, firstUseTwo]));
    }

    private static CaptureBenefitTrigger Trigger(
        CaptureBenefitSource source,
        string triggerId,
        string eventId,
        CaptureBenefitOperation operation) =>
        new(source, triggerId, [eventId], [operation], firstUseFlagId: null);

    private static (
        CaptureBatch MixedBatch,
        CaptureBatch BlackOnlyBatch,
        string BlackStoneInstanceId) MixedCaptureBatches()
    {
        var geometry = BoardGeometry.Create(7);
        var stones = new[]
        {
            new BoardStone(
                StoneColor.White,
                false,
                geometry.CreateCanonicalPoint(1, 1)),
            new BoardStone(
                StoneColor.White,
                false,
                geometry.CreateCanonicalPoint(3, 1)),
            new BoardStone(
                StoneColor.Black,
                false,
                geometry.CreateCanonicalPoint(5, 1)),
        };
        var board = BoardState.Create(geometry, stones);
        var instances = stones
            .Select((stone, index) => new StoneRuntimeInstance(
                $"stone.{index + 1}",
                stone,
                "standard",
                index + 1L,
                []))
            .ToArray();
        var runtime = StoneRuntimeState.Create(board, instances, 4);
        var groups = StoneGroupAnalyzer.Analyze(board).Groups;
        var mixed = CaptureBatch.Create(
            "mixed_capture",
            "test_capture",
            CaptureBoundary.PlacementResolution,
            null,
            CapturingWindow.ClosedPlayerWindow,
            runtime,
            groups);
        var blackGroup = Assert.Single(groups, group => group.Color == StoneColor.Black);
        var blackOnly = CaptureBatch.Create(
            "black_capture",
            "test_capture",
            CaptureBoundary.PlacementResolution,
            null,
            CapturingWindow.ClosedPlayerWindow,
            runtime,
            [blackGroup]);
        return (mixed, blackOnly, runtime.InstanceAt(blackGroup.Anchor)!.InstanceId);
    }
}
