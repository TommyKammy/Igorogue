using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

public sealed class ClosedWindowCaptureBenefitPlacementTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);
    private static readonly CounterattackBoundaryPolicy Policy = new(
        thresholdUnits: 200,
        enemyTurnEndNaturalGainUnits: 12,
        sacrificeStonesPerBatch: 3,
        sacrificeUnitsPerBatch: 30);

    [Fact]
    public void PlacementTerminalGateEmitsOneSuppressionBeforeResolvedWithoutEnumeratingTriggers()
    {
        var batch = PlacementBatch(
            new BoardStone(
                StoneColor.Black,
                true,
                Geometry.CreateCanonicalPoint(1, 1)),
            CapturingWindow.PlayerActionWindow);
        var resources = ClosedWindowResourceState.Empty([]);
        var counterattack = CounterattackBoundaryState.Create(
            0,
            false,
            0,
            Policy);

        var resolution = ClosedWindowCaptureBenefitResolver.ResolvePlacement(
            batch,
            resources,
            counterattack,
            Policy,
            new ThrowingEnumerable<CaptureBenefitTrigger>());

        Assert.True(resolution.BenefitsSuppressed);
        Assert.Same(resources, resolution.ResourcesAfterResolution);
        Assert.Same(counterattack, resolution.CounterattackAfterResolution);
        Assert.Empty(resolution.OrderedTriggers);
        Assert.Collection(
            resolution.OrderedFacts,
            fact => Assert.IsType<CaptureBatchStartedFact>(fact),
            fact => Assert.Equal(
                "terminal_king_capture",
                Assert.IsType<CaptureBenefitSuppressedFact>(fact).ReasonId),
            fact => Assert.True(
                Assert.IsType<CaptureBatchResolvedFact>(fact).BenefitsSuppressed));
        Assert.Single(resolution.OrderedFacts.OfType<CaptureBenefitSuppressedFact>());
    }

    [Fact]
    public void NonterminalClosedWindowPlacementUsesTheExistingBenefitPipeline()
    {
        var batch = PlacementBatch(
            new BoardStone(
                StoneColor.White,
                false,
                Geometry.CreateCanonicalPoint(1, 1)),
            CapturingWindow.ClosedPlayerWindow);
        var trigger = new CaptureBenefitTrigger(
            CaptureBenefitSource.StandardAccounting("standard_capture", 0),
            "standard.capture",
            ["standard_capture"],
            [new GainStandardCaptureSoulOperation(1, 1, 3)],
            firstUseFlagId: null);

        var resolution = ClosedWindowCaptureBenefitResolver.ResolvePlacement(
            batch,
            ClosedWindowResourceState.Empty([]),
            CounterattackBoundaryState.Create(0, false, 0, Policy),
            Policy,
            [trigger]);

        Assert.False(resolution.BenefitsSuppressed);
        Assert.Equal(1, resolution.ResourcesAfterResolution.Soul);
        Assert.Equal(
            1,
            resolution.ResourcesAfterResolution.StandardCaptureRewardsClaimed);
        Assert.Collection(
            resolution.OrderedFacts,
            fact => Assert.IsType<CaptureBatchStartedFact>(fact),
            fact => Assert.Equal(
                1,
                Assert.IsType<SoulChangedFact>(fact).Delta),
            fact => Assert.False(
                Assert.IsType<CaptureBatchResolvedFact>(fact).BenefitsSuppressed));
    }

    [Fact]
    public void PlacementEntryRejectsNonplacementAndOpenNonterminalBatchesBeforeTriggerEnumeration()
    {
        var stone = new BoardStone(
            StoneColor.White,
            false,
            Geometry.CreateCanonicalPoint(1, 1));
        var (runtime, group) = RuntimeAndGroup(stone);
        var expiryBatch = CaptureBatch.Create(
            "expiry_batch",
            "temporary_liberty_expired",
            CaptureBoundary.EnemyTurnTemporaryLibertyExpirySweep,
            1,
            CapturingWindow.ClosedPlayerWindow,
            runtime,
            [group]);
        var openPlacement = CaptureBatch.Create(
            "open_placement",
            "placement_capture",
            CaptureBoundary.PlacementResolution,
            null,
            CapturingWindow.PlayerActionWindow,
            runtime,
            [group]);
        var resources = ClosedWindowResourceState.Empty([]);
        var counterattack = CounterattackBoundaryState.Create(
            0,
            false,
            0,
            Policy);
        var throwing = new ThrowingEnumerable<CaptureBenefitTrigger>();

        Assert.Throws<ArgumentException>(() =>
            ClosedWindowCaptureBenefitResolver.ResolvePlacement(
                expiryBatch,
                resources,
                counterattack,
                Policy,
                throwing));
        Assert.Throws<ArgumentException>(() =>
            ClosedWindowCaptureBenefitResolver.ResolvePlacement(
                openPlacement,
                resources,
                counterattack,
                Policy,
                throwing));
    }

    private static CaptureBatch PlacementBatch(
        BoardStone stone,
        CapturingWindow capturingWindow)
    {
        var (runtime, group) = RuntimeAndGroup(stone);
        return CaptureBatch.Create(
            "placement_batch",
            "placement_capture",
            CaptureBoundary.PlacementResolution,
            null,
            capturingWindow,
            runtime,
            [group]);
    }

    private static (StoneRuntimeState Runtime, StoneGroup Group) RuntimeAndGroup(
        BoardStone stone)
    {
        var board = BoardState.Create(Geometry, [stone]);
        var instance = new StoneRuntimeInstance(
            "stone.captured",
            stone,
            "standard",
            1,
            []);
        var runtime = StoneRuntimeState.Create(board, [instance], 2);
        return (runtime, Assert.Single(StoneGroupAnalyzer.Analyze(board).Groups));
    }

    private sealed class ThrowingEnumerable<T> : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() =>
            throw new InvalidOperationException("Trigger setup was enumerated.");

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
