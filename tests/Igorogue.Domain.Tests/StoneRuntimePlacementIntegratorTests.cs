using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Tests;

public sealed class StoneRuntimePlacementIntegratorTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Create(7);

    [Fact]
    public void LegalPlacementAtomicallyCreatesRuntimeStoneAndRemovesCapturedCarrier()
    {
        var sourceBoard = Board(
            Stone(StoneColor.White, 2, 2),
            Stone(StoneColor.Black, 1, 2),
            Stone(StoneColor.Black, 2, 1),
            Stone(StoneColor.Black, 3, 2));
        var sourceStones = Runtime(sourceBoard);
        var carrier = sourceStones.InstanceAt(C(2, 2))!;
        var protectedByTimedLiberty = new TemporaryLibertyEffect(
            "effect.carrier",
            1,
            StoneColor.White,
            carrier.InstanceId,
            "card.reinforce",
            1,
            9);
        var sourceTemporary = TemporaryLibertyState.Create(
            sourceStones,
            [protectedByTimedLiberty],
            2);
        var history = BattleRepetitionHistory.Start(sourceBoard);
        var placedStone = Stone(StoneColor.Black, 2, 3);
        Assert.True(HypotheticalPlacementResolver.TryCreate(
            sourceBoard,
            placedStone,
            out var placement));
        Assert.NotNull(placement);

        // The accepted placement is evaluated with the same timed-liberty effect.
        // A -1 active continuous modifier cancels the +1 timed grant, so the carrier
        // reaches effective liberty zero on the exact post-placement snapshot.
        var provisionalPlacedInstance = new StoneRuntimeInstance(
            "stone.placed",
            placement.PlacedStone,
            "standard",
            sourceStones.NextCreatedSequence,
            ["effect.on_place"]);
        var provisionalStones = StoneRuntimeState.Create(
            placement.BoardAfterPlacement,
            sourceStones.Instances.Append(provisionalPlacedInstance),
            checked(sourceStones.NextCreatedSequence + 1L));
        var provisionalTemporary = TemporaryLibertyState.Create(
            provisionalStones,
            sourceTemporary.Effects,
            sourceTemporary.NextCreatedSequence);
        var provisionalContinuous = ContinuousLibertySnapshot.Create(
            provisionalStones,
            [
                new ContinuousLibertyModifier(
                    "modifier.cancel",
                    -1,
                    StoneColor.White,
                    carrier.InstanceId,
                    "test.condition"),
            ]);
        var effectiveAfterPlacement = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            provisionalStones,
            provisionalTemporary,
            provisionalContinuous,
            placement.GroupsAfterPlacement);
        var carrierGroup = effectiveAfterPlacement.GroupAnalysis.GroupAt(carrier.Point)!;
        Assert.Equal(0, effectiveAfterPlacement.BreakdownFor(carrierGroup).EffectiveLibertyCount);

        var candidate = HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            effectiveAfterPlacement.EffectiveLiberties);
        var effectiveAfterCapture = PostCaptureAnalysis(
            sourceStones,
            sourceTemporary,
            provisionalPlacedInstance,
            candidate);
        var legality = PlacementLegalityEvaluator.Evaluate(
            candidate,
            effectiveAfterCapture.EffectiveLiberties,
            history,
            PlacementAccessMode.Normal);
        Assert.True(legality.IsLegal);
        var acceptedPlacement = history.CommitLegalPlacement(legality);

        var commit = StoneRuntimePlacementIntegrator.Apply(
            sourceStones,
            sourceTemporary,
            acceptedPlacement,
            new StoneRuntimePlacementDescriptor(
                "stone.placed",
                "standard",
                ["effect.on_place"]),
            effectiveAfterPlacement,
            effectiveAfterCapture);

        Assert.Same(sourceStones, commit.SourceStones);
        Assert.Same(sourceTemporary, commit.SourceTemporaryLiberties);
        Assert.Same(acceptedPlacement.BoardAfterCommit, commit.BoardAfterCommit);
        Assert.Equal(sourceStones.NextCreatedSequence, commit.PlacedStoneInstance.CreatedSequence);
        Assert.Equal("stone.placed", commit.PlacedStoneInstance.InstanceId);
        Assert.Equal(["effect.on_place"], commit.PlacedStoneInstance.OrderedEffectMetadata);
        Assert.Same(
            commit.PlacedStoneInstance,
            commit.StonesAfterCommit.InstanceAt(placedStone.Point));
        Assert.Null(commit.StonesAfterCommit.InstanceById(carrier.InstanceId));
        Assert.Equal(
            checked(sourceStones.NextCreatedSequence + 1L),
            commit.StonesAfterCommit.NextCreatedSequence);
        Assert.Equal(
            "effect.carrier",
            Assert.Single(commit.RemovedCarrierEffects).EffectInstanceId);
        var removalFact = Assert.Single(commit.OrderedRemovalFacts);
        Assert.Equal(TemporaryLibertyRemovalReason.CarrierRemoved, removalFact.Reason);
        Assert.Equal("carrier_removed", removalFact.ReasonId);
        Assert.Empty(commit.TemporaryLibertiesAfterCommit.Effects);
        Assert.Same(
            commit.StonesAfterCommit,
            commit.TemporaryLibertiesAfterCommit.SourceStones);

        var laterSweep = TemporaryLibertyExpiryResolver.Resolve(
            commit.StonesAfterCommit,
            commit.TemporaryLibertiesAfterCommit,
            ContinuousLibertySnapshot.Empty(commit.StonesAfterCommit),
            acceptedPlacement.HistoryAfterCommit,
            9);
        Assert.True(laterSweep.IsExactNoOp);
        Assert.Empty(laterSweep.ExpiredEffects);
        Assert.Empty(laterSweep.OrderedFacts);
    }

    [Fact]
    public void IntegratorRejectsForeignBoardRuntimeAndDuplicatePlacedIdentity()
    {
        var sourceBoard = Board(Stone(StoneColor.Black, 4, 4));
        var sourceStones = Runtime(sourceBoard);
        var sourceTemporary = TemporaryLibertyState.Create(sourceStones, [], 1);
        var history = BattleRepetitionHistory.Start(sourceBoard);
        var placedStone = Stone(StoneColor.White, 1, 1);
        Assert.True(HypotheticalPlacementResolver.TryCreate(
            sourceBoard,
            placedStone,
            out var placement));
        Assert.NotNull(placement);
        var descriptor = new StoneRuntimePlacementDescriptor("new", "standard", []);
        var provisionalPlaced = new StoneRuntimeInstance(
            descriptor.InstanceId,
            placement.PlacedStone,
            descriptor.KindId,
            sourceStones.NextCreatedSequence,
            descriptor.OrderedEffectMetadata);
        var provisionalStones = StoneRuntimeState.Create(
            placement.BoardAfterPlacement,
            sourceStones.Instances.Append(provisionalPlaced),
            checked(sourceStones.NextCreatedSequence + 1L));
        var provisionalTemporary = TemporaryLibertyState.Create(
            provisionalStones,
            sourceTemporary.Effects,
            sourceTemporary.NextCreatedSequence);
        var captureEffectiveLiberties = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            provisionalStones,
            provisionalTemporary,
            ContinuousLibertySnapshot.Empty(provisionalStones),
            placement.GroupsAfterPlacement);
        var candidate = HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            captureEffectiveLiberties.EffectiveLiberties);
        var postCaptureEffectiveLiberties = PostCaptureAnalysis(
            sourceStones,
            sourceTemporary,
            provisionalPlaced,
            candidate);
        var legality = PlacementLegalityEvaluator.Evaluate(
            candidate,
            postCaptureEffectiveLiberties.EffectiveLiberties,
            history,
            PlacementAccessMode.Normal);
        var acceptedPlacement = history.CommitLegalPlacement(legality);

        var foreignBoard = Board(Stone(StoneColor.Black, 4, 4));
        var foreignRuntime = Runtime(foreignBoard);
        var foreignTemporary = TemporaryLibertyState.Create(foreignRuntime, [], 1);
        Assert.Throws<ArgumentException>(() => StoneRuntimePlacementIntegrator.Apply(
            foreignRuntime,
            foreignTemporary,
            acceptedPlacement,
            descriptor,
            captureEffectiveLiberties,
            postCaptureEffectiveLiberties));
        Assert.Throws<ArgumentException>(() => StoneRuntimePlacementIntegrator.Apply(
            sourceStones,
            sourceTemporary,
            acceptedPlacement,
            new StoneRuntimePlacementDescriptor(
                sourceStones.Instances[0].InstanceId,
                "standard",
                []),
            captureEffectiveLiberties,
            postCaptureEffectiveLiberties));
    }

    [Fact]
    public void IntegratorRejectsCommitWhoseCaptureIgnoredTimedLibertyAnalysis()
    {
        var sourceBoard = Board(
            Stone(StoneColor.White, 2, 2),
            Stone(StoneColor.Black, 1, 2),
            Stone(StoneColor.Black, 2, 1),
            Stone(StoneColor.Black, 3, 2));
        var sourceStones = Runtime(sourceBoard);
        var carrier = sourceStones.InstanceAt(C(2, 2))!;
        var sourceTemporary = TemporaryLibertyState.Create(
            sourceStones,
            [
                new TemporaryLibertyEffect(
                    "effect.protection",
                    1,
                    StoneColor.White,
                    carrier.InstanceId,
                    "card.reinforce",
                    1,
                    9),
            ],
            2);
        var history = BattleRepetitionHistory.Start(sourceBoard);
        var placedStone = Stone(StoneColor.Black, 2, 3);
        Assert.True(HypotheticalPlacementResolver.TryCreate(
            sourceBoard,
            placedStone,
            out var placement));
        Assert.NotNull(placement);
        var descriptor = new StoneRuntimePlacementDescriptor("stone.placed", "standard", []);
        var provisionalPlaced = new StoneRuntimeInstance(
            descriptor.InstanceId,
            placement.PlacedStone,
            descriptor.KindId,
            sourceStones.NextCreatedSequence,
            descriptor.OrderedEffectMetadata);
        var provisionalStones = StoneRuntimeState.Create(
            placement.BoardAfterPlacement,
            sourceStones.Instances.Append(provisionalPlaced),
            checked(sourceStones.NextCreatedSequence + 1L));
        var provisionalTemporary = TemporaryLibertyState.Create(
            provisionalStones,
            sourceTemporary.Effects,
            sourceTemporary.NextCreatedSequence);
        var correctCaptureAnalysis = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            provisionalStones,
            provisionalTemporary,
            ContinuousLibertySnapshot.Empty(provisionalStones),
            placement.GroupsAfterPlacement);
        var protectedGroup = correctCaptureAnalysis.GroupAnalysis.GroupAt(carrier.Point)!;
        Assert.Equal(1, correctCaptureAnalysis.BreakdownFor(protectedGroup).EffectiveLibertyCount);

        // This pre-existing low-level seam can construct a candidate which ignores
        // timed protection. The runtime integrator must not accept it as its bound
        // capture analysis.
        var forgedCandidate = HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            RealLiberties(placement.GroupsAfterPlacement));
        Assert.Single(forgedCandidate.CapturedGroups);
        var postCaptureEffectiveLiberties = PostCaptureAnalysis(
            sourceStones,
            sourceTemporary,
            provisionalPlaced,
            forgedCandidate);
        var legality = PlacementLegalityEvaluator.Evaluate(
            forgedCandidate,
            postCaptureEffectiveLiberties.EffectiveLiberties,
            history,
            PlacementAccessMode.Normal);
        Assert.True(legality.IsLegal);
        var forgedCommit = history.CommitLegalPlacement(legality);

        Assert.Throws<ArgumentException>(() => StoneRuntimePlacementIntegrator.Apply(
            sourceStones,
            sourceTemporary,
            forgedCommit,
            descriptor,
            correctCaptureAnalysis,
            postCaptureEffectiveLiberties));
    }

    [Fact]
    public void IntegratorRejectsCommitWhoseLegalityIgnoredPostCaptureSuicide()
    {
        var sourceBoard = Board(
            Stone(StoneColor.White, 1, 2),
            Stone(StoneColor.White, 2, 1),
            Stone(StoneColor.White, 3, 2),
            Stone(StoneColor.White, 2, 3));
        var sourceStones = Runtime(sourceBoard);
        var sourceTemporary = TemporaryLibertyState.Create(sourceStones, [], 1);
        var history = BattleRepetitionHistory.Start(sourceBoard);
        var placedStone = Stone(StoneColor.Black, 2, 2);
        Assert.True(HypotheticalPlacementResolver.TryCreate(
            sourceBoard,
            placedStone,
            out var placement));
        Assert.NotNull(placement);
        var descriptor = new StoneRuntimePlacementDescriptor("stone.suicide", "standard", []);
        var provisionalPlaced = new StoneRuntimeInstance(
            descriptor.InstanceId,
            placement.PlacedStone,
            descriptor.KindId,
            sourceStones.NextCreatedSequence,
            descriptor.OrderedEffectMetadata);
        var provisionalStones = StoneRuntimeState.Create(
            placement.BoardAfterPlacement,
            sourceStones.Instances.Append(provisionalPlaced),
            checked(sourceStones.NextCreatedSequence + 1L));
        var provisionalTemporary = TemporaryLibertyState.Create(
            provisionalStones,
            sourceTemporary.Effects,
            sourceTemporary.NextCreatedSequence);
        var captureEffectiveLiberties = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            provisionalStones,
            provisionalTemporary,
            ContinuousLibertySnapshot.Empty(provisionalStones),
            placement.GroupsAfterPlacement);
        var candidate = HypotheticalPlacementResolver.ResolveCaptures(
            placement,
            captureEffectiveLiberties.EffectiveLiberties);
        Assert.Empty(candidate.CapturedGroups);
        var correctPostCapture = PostCaptureAnalysis(
            sourceStones,
            sourceTemporary,
            provisionalPlaced,
            candidate);
        Assert.Equal(
            0,
            correctPostCapture.EffectiveLiberties.EffectiveLibertiesFor(
                correctPostCapture.GroupAnalysis.GroupAt(placedStone.Point)!));
        var forgedPostCapture = EffectiveLibertySnapshot.Create(
            candidate.GroupsAfterCapture,
            candidate.GroupsAfterCapture.Groups.Select(group => new GroupEffectiveLiberty(
                group,
                group.Anchor == placedStone.Point ? 1 : group.RealLibertyCount)));
        var legality = PlacementLegalityEvaluator.Evaluate(
            candidate,
            forgedPostCapture,
            history,
            PlacementAccessMode.Normal);
        Assert.True(legality.IsLegal);
        var forgedCommit = history.CommitLegalPlacement(legality);

        Assert.Throws<ArgumentException>(() => StoneRuntimePlacementIntegrator.Apply(
            sourceStones,
            sourceTemporary,
            forgedCommit,
            descriptor,
            captureEffectiveLiberties,
            correctPostCapture));
    }

    private static TemporaryLibertyEffectiveLibertyAnalysis PostCaptureAnalysis(
        StoneRuntimeState sourceStones,
        TemporaryLibertyState sourceTemporary,
        StoneRuntimeInstance provisionalPlaced,
        HypotheticalPlacementResolution candidate)
    {
        var retained = sourceStones.Instances.Where(instance =>
            ReferenceEquals(candidate.BoardAfterCapture.StoneAt(instance.Point), instance.Stone));
        var postCaptureStones = StoneRuntimeState.Create(
            candidate.BoardAfterCapture,
            retained.Append(provisionalPlaced),
            checked(sourceStones.NextCreatedSequence + 1L));
        var survivingEffects = sourceTemporary.Effects.Where(effect =>
            postCaptureStones.InstanceById(effect.AnchorStoneInstanceId) is not null);
        var postCaptureTemporary = TemporaryLibertyState.Create(
            postCaptureStones,
            survivingEffects,
            sourceTemporary.NextCreatedSequence,
            sourceTemporary.ExpirySweepStartedForEnemyTurnIndex);
        return TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            postCaptureStones,
            postCaptureTemporary,
            ContinuousLibertySnapshot.Empty(postCaptureStones),
            candidate.GroupsAfterCapture);
    }

    private static EffectiveLibertySnapshot RealLiberties(StoneGroupAnalysis analysis) =>
        EffectiveLibertySnapshot.Create(
            analysis,
            analysis.Groups.Select(group => new GroupEffectiveLiberty(
                group,
                group.RealLibertyCount)));

    private static StoneRuntimeState Runtime(BoardState board)
    {
        var instances = board.OccupiedStones
            .Select((stone, index) => new StoneRuntimeInstance(
                $"stone-{stone.Point.X}-{stone.Point.Y}",
                stone,
                "standard",
                index + 1L,
                []))
            .ToArray();
        return StoneRuntimeState.Create(board, instances, instances.Length + 1L);
    }

    private static BoardState Board(params BoardStone[] stones) =>
        BoardState.Create(Geometry, stones);

    private static BoardStone Stone(StoneColor color, int x, int y) =>
        new(color, false, C(x, y));

    private static CanonicalPoint C(int x, int y) =>
        Geometry.CreateCanonicalPoint(x, y);
}
