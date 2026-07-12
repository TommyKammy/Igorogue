using Igorogue.Application.Battle;
using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Facilities;

namespace Igorogue.Application.Tests;

public sealed class BattleAuthoritativeInitialSnapshotTests
{
    private static readonly BoardGeometry Geometry =
        BoardGeometry.Create(BoardGeometry.AcceptedSize);

    [Fact]
    public void CreateRejectsTemporaryLibertiesFromAValueEquivalentForeignRuntime()
    {
        var stones = Runtime(Board(Stone(StoneColor.Black, 1, 1)));
        var foreignStones = Runtime(Board(Stone(StoneColor.Black, 1, 1)));

        var exception = Assert.Throws<ArgumentException>(() => CreateSnapshot(
            stones,
            temporary: TemporaryLibertyState.Create(foreignStones, [], 1)));

        Assert.Equal("stoneRuntimeState", exception.ParamName);
        Assert.Contains("exact initial stone runtime", exception.Message);
    }

    [Fact]
    public void CreateRejectsContinuousLibertiesFromAValueEquivalentForeignRuntime()
    {
        var stones = Runtime(Board(Stone(StoneColor.Black, 1, 1)));
        var foreignStones = Runtime(Board(Stone(StoneColor.Black, 1, 1)));

        var exception = Assert.Throws<ArgumentException>(() => CreateSnapshot(
            stones,
            continuous: ContinuousLibertySnapshot.Empty(foreignStones)));

        Assert.Equal("stoneRuntimeState", exception.ParamName);
        Assert.Contains("exact initial stone runtime", exception.Message);
    }

    [Fact]
    public void CreateRejectsFacilitiesFromAValueEquivalentForeignBoard()
    {
        var stones = Runtime(Board(Stone(StoneColor.Black, 1, 1)));
        var foreignBoard = Board(Stone(StoneColor.Black, 1, 1));

        var exception = Assert.Throws<ArgumentException>(() => CreateSnapshot(
            stones,
            facilities: FacilityState.Create(foreignBoard, [], 1)));

        Assert.Equal("facilityState", exception.ParamName);
        Assert.Contains("exact runtime board", exception.Message);
    }

    [Fact]
    public void CreateRejectsHistoryWhoseCurrentTopologyDoesNotMatchRuntimeBoard()
    {
        var stones = Runtime(Board(Stone(StoneColor.Black, 1, 1)));
        var mismatchedHistory = BattleRepetitionHistory.Start(
            Board(Stone(StoneColor.Black, 2, 2)));

        var exception = Assert.Throws<ArgumentException>(() => CreateSnapshot(
            stones,
            history: mismatchedHistory));

        Assert.Equal("repetitionHistory", exception.ParamName);
        Assert.Contains("must end at the runtime board topology", exception.Message);
    }

    [Fact]
    public void CreateRejectsCapturedStoneSelfTriggerWhoseStoneIsNotLive()
    {
        var stones = Runtime(Board(Stone(StoneColor.Black, 1, 1)));
        var plan = CaptureBenefitTriggerPlan.CreateConditional(
        [
            new CaptureBenefitTriggerPlanEntry(
                Trigger(CaptureBenefitSource.CapturedStoneSelf("stone.missing")),
                CaptureBenefitTriggerCondition.CapturedSourceStone),
        ]);

        var exception = Assert.Throws<ArgumentException>(() => CreateSnapshot(
            stones,
            triggerPlan: plan));

        Assert.Equal("plan", exception.ParamName);
        Assert.Contains("stone.missing is not present", exception.Message);
    }

    [Fact]
    public void CreateRejectsFacilityTriggerWhoseFacilityIsMissing()
    {
        var stones = Runtime(Board(Stone(StoneColor.Black, 1, 1)));
        var plan = CaptureBenefitTriggerPlan.Create(
        [
            Trigger(CaptureBenefitSource.Facility("facility.missing", C(2, 2))),
        ]);

        var exception = Assert.Throws<ArgumentException>(() => CreateSnapshot(
            stones,
            triggerPlan: plan));

        Assert.Equal("plan", exception.ParamName);
        Assert.Contains("facility.missing is not exact-bound", exception.Message);
    }

    [Fact]
    public void CreateRejectsFacilityTriggerWhosePointDoesNotMatchFacility()
    {
        var stones = Runtime(Board(Stone(StoneColor.Black, 1, 1)));
        var facilities = FacilityState.Create(
            stones.SourceBoard,
            [new FacilityInstance(
                "facility.bound",
                "facility.test",
                StoneColor.Black,
                C(2, 2),
                1)],
            2);
        var plan = CaptureBenefitTriggerPlan.Create(
        [
            Trigger(CaptureBenefitSource.Facility("facility.bound", C(3, 3))),
        ]);

        var exception = Assert.Throws<ArgumentException>(() => CreateSnapshot(
            stones,
            facilities: facilities,
            triggerPlan: plan));

        Assert.Equal("plan", exception.ParamName);
        Assert.Contains("facility.bound is not exact-bound", exception.Message);
    }

    [Fact]
    public void CreateRejectsTriggerWhoseFirstUseFlagIsUndeclared()
    {
        var stones = Runtime(Board(Stone(StoneColor.Black, 1, 1)));
        var plan = CaptureBenefitTriggerPlan.Create(
        [
            Trigger(
                CaptureBenefitSource.Style("style.test"),
                firstUseFlagId: "first_use.unknown"),
        ]);

        var exception = Assert.Throws<KeyNotFoundException>(() => CreateSnapshot(
            stones,
            resources: ClosedWindowResourceState.Empty([]),
            triggerPlan: plan));

        Assert.Contains("does not declare first-use flag first_use.unknown", exception.Message);
    }

    [Fact]
    public void CreateRejectsCounterattackStateUnderADifferentPolicy()
    {
        var stones = Runtime(Board());
        var sourcePolicy = new CounterattackBoundaryPolicy(200, 12, 3, 30);
        var mismatchedPolicy = new CounterattackBoundaryPolicy(100, 12, 3, 30);
        var state = CounterattackBoundaryState.Create(
            gaugeUnits: 150,
            pending: false,
            sacrificeStoneRemainder: 0,
            sourcePolicy);

        var exception = Assert.Throws<ArgumentException>(() => CreateSnapshot(
            stones,
            counterattackState: state,
            counterattackPolicy: mismatchedPolicy));

        Assert.Equal("state", exception.ParamName);
        Assert.Contains("below the injected threshold", exception.Message);
    }

    [Fact]
    public void CreateRejectsTemporaryLibertyAlreadyOverdueAtInitialTurn()
    {
        var stones = Runtime(Board(Stone(StoneColor.Black, 1, 1)));
        var anchor = Assert.Single(stones.Instances);
        var temporary = TemporaryLibertyState.Create(
            stones,
            [new TemporaryLibertyEffect(
                "effect.overdue",
                1,
                StoneColor.Black,
                anchor.InstanceId,
                "source.test",
                1,
                1)],
            2);

        var exception = Assert.Throws<ArgumentException>(() => CreateSnapshot(
            stones,
            temporary: temporary,
            playerTurnIndex: 2));

        Assert.Equal("temporaryLibertyState", exception.ParamName);
        Assert.Contains("cannot contain an overdue effect", exception.Message);
    }

    [Fact]
    public void CreateRejectsSweepMarkerAtOrAfterInitialPlayerTurn()
    {
        var stones = Runtime(Board());
        var temporary = TemporaryLibertyState.Create(
            stones,
            [],
            1,
            expirySweepStartedForEnemyTurnIndex: 2);

        var exception = Assert.Throws<ArgumentException>(() => CreateSnapshot(
            stones,
            temporary: temporary,
            playerTurnIndex: 2));

        Assert.Equal("temporaryLibertyState", exception.ParamName);
        Assert.Contains("must precede the initial player turn", exception.Message);
    }

    [Fact]
    public void ConditionalTriggerConditionChangesSnapshotCanonicalTextAndChecksum()
    {
        var stones = Runtime(Board(Stone(StoneColor.Black, 1, 1)));
        var trigger = Trigger(CaptureBenefitSource.StandardAccounting("standard", 0));
        var anyCaptureEntry = new CaptureBenefitTriggerPlanEntry(
            trigger,
            CaptureBenefitTriggerCondition.AnyCapture);
        var whiteCaptureEntry = new CaptureBenefitTriggerPlanEntry(
            trigger,
            CaptureBenefitTriggerCondition.CapturedWhiteGroup);
        var anyCapture = CreateSnapshot(
            stones,
            triggerPlan: CaptureBenefitTriggerPlan.CreateConditional([anyCaptureEntry]));
        var whiteCapture = CreateSnapshot(
            stones,
            triggerPlan: CaptureBenefitTriggerPlan.CreateConditional([whiteCaptureEntry]));

        Assert.Contains("condition=any_capture", anyCaptureEntry.CanonicalText);
        Assert.Contains("condition=captured_white_group", whiteCaptureEntry.CanonicalText);
        Assert.NotEqual(
            anyCapture.CaptureBenefitTriggerPlan.CanonicalText,
            whiteCapture.CaptureBenefitTriggerPlan.CanonicalText);
        Assert.NotEqual(
            anyCapture.CaptureBenefitTriggerPlan.Checksum,
            whiteCapture.CaptureBenefitTriggerPlan.Checksum);
        Assert.NotEqual(anyCapture.CanonicalText, whiteCapture.CanonicalText);
        Assert.NotEqual(anyCapture.Checksum, whiteCapture.Checksum);
    }

    private static BattleAuthoritativeInitialSnapshot CreateSnapshot(
        StoneRuntimeState stones,
        TemporaryLibertyState? temporary = null,
        ContinuousLibertySnapshot? continuous = null,
        BattleRepetitionHistory? history = null,
        FacilityState? facilities = null,
        ClosedWindowResourceState? resources = null,
        CaptureBenefitTriggerPlan? triggerPlan = null,
        CounterattackBoundaryState? counterattackState = null,
        CounterattackBoundaryPolicy? counterattackPolicy = null,
        int playerTurnIndex = 1)
    {
        var policy = counterattackPolicy ?? CounterPolicy();
        return BattleAuthoritativeInitialSnapshot.Create(
            stones,
            temporary ?? TemporaryLibertyState.Create(stones, [], 1),
            continuous ?? ContinuousLibertySnapshot.Empty(stones),
            history ?? BattleRepetitionHistory.Start(stones.SourceBoard),
            facilities ?? FacilityState.Create(stones.SourceBoard, [], 1),
            resources ?? ClosedWindowResourceState.Empty([]),
            triggerPlan ?? CaptureBenefitTriggerPlan.Create([]),
            counterattackState ?? CounterattackBoundaryState.Create(0, false, 0, policy),
            policy,
            RuntimePolicy(),
            playerTurnIndex);
    }

    private static CaptureBenefitTrigger Trigger(
        CaptureBenefitSource source,
        string? firstUseFlagId = null) =>
        new(
            source,
            $"trigger.{source.SourceId}",
            ["test", source.SourceId],
            [new GainSoulCaptureBenefitOperation(1)],
            firstUseFlagId);

    private static CounterattackBoundaryPolicy CounterPolicy() =>
        new(200, 12, 3, 30);

    private static BattleRuntimePolicy RuntimePolicy() =>
        new(
            20,
            FacilityRuntimePolicy.Create(
                5,
                [new FacilityCapacityBand(1, 49, 1)],
                3,
                [new KeyValuePair<string, int>("default", 1)]));

    private static StoneRuntimeState Runtime(BoardState board)
    {
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                $"stone.{index + 1}",
                stone,
                stone.IsKing ? "king" : "standard",
                index + 1L,
                []))
            .ToArray();
        return StoneRuntimeState.Create(board, instances, instances.Length + 1L);
    }

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(
        StoneColor color,
        int x,
        int y,
        bool isKing = false) =>
        new(color, isKing, C(x, y));

    private static CanonicalPoint C(int x, int y) =>
        Geometry.CreateCanonicalPoint(x, y);
}
