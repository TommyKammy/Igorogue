using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;

namespace Igorogue.Application.Battle;

internal sealed class AuthorizedRuntimeStonePlacementResolution
{
    private AuthorizedRuntimeStonePlacementResolution(
        bool accepted,
        string reasonId,
        LegalPlacementCommit? legalPlacementCommit,
        StoneRuntimePlacementCommit? runtimePlacementCommit,
        TemporaryLibertyEffectiveLibertyAnalysis? captureEffectiveLiberties,
        TemporaryLibertyEffectiveLibertyAnalysis? postCaptureEffectiveLiberties)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonId);
        if ((accepted &&
             (legalPlacementCommit is null ||
              runtimePlacementCommit is null ||
              captureEffectiveLiberties is null ||
              postCaptureEffectiveLiberties is null)) ||
            (!accepted &&
             (legalPlacementCommit is not null ||
              runtimePlacementCommit is not null ||
              captureEffectiveLiberties is not null ||
              postCaptureEffectiveLiberties is not null)))
        {
            throw new ArgumentException(
                "Accepted runtime placement preparation requires both exact commits.",
                nameof(accepted));
        }

        if (accepted &&
            (!ReferenceEquals(
                 legalPlacementCommit!.BoardAfterCommit,
                 runtimePlacementCommit!.BoardAfterCommit) ||
             !ReferenceEquals(
                 runtimePlacementCommit.PlacedStoneInstance,
                 captureEffectiveLiberties!.SourceStones.InstanceAt(
                     legalPlacementCommit.Candidate.PlacedStone.Point)) ||
             !ReferenceEquals(
                 legalPlacementCommit.Candidate.GroupsAfterCapture,
                 postCaptureEffectiveLiberties!.GroupAnalysis) ||
             !ReferenceEquals(
                 runtimePlacementCommit.StonesAfterCommit,
                 postCaptureEffectiveLiberties.SourceStones) ||
             !ReferenceEquals(
                 runtimePlacementCommit.TemporaryLibertiesAfterCommit,
                 postCaptureEffectiveLiberties.TemporaryLiberties)))
        {
            throw new ArgumentException(
                "Runtime placement commits and post-capture liberties must share exact result snapshots.",
                nameof(postCaptureEffectiveLiberties));
        }

        Accepted = accepted;
        ReasonId = reasonId;
        LegalPlacementCommit = legalPlacementCommit;
        RuntimePlacementCommit = runtimePlacementCommit;
        CaptureEffectiveLiberties = captureEffectiveLiberties;
        PostCaptureEffectiveLiberties = postCaptureEffectiveLiberties;
    }

    internal bool Accepted { get; }

    internal string ReasonId { get; }

    internal LegalPlacementCommit? LegalPlacementCommit { get; }

    internal StoneRuntimePlacementCommit? RuntimePlacementCommit { get; }

    internal TemporaryLibertyEffectiveLibertyAnalysis?
        CaptureEffectiveLiberties { get; }

    internal TemporaryLibertyEffectiveLibertyAnalysis?
        PostCaptureEffectiveLiberties { get; }

    internal static AuthorizedRuntimeStonePlacementResolution Accept(
        LegalPlacementCommit legalPlacementCommit,
        StoneRuntimePlacementCommit runtimePlacementCommit,
        TemporaryLibertyEffectiveLibertyAnalysis captureEffectiveLiberties,
        TemporaryLibertyEffectiveLibertyAnalysis postCaptureEffectiveLiberties)
    {
        ArgumentNullException.ThrowIfNull(legalPlacementCommit);
        ArgumentNullException.ThrowIfNull(runtimePlacementCommit);
        ArgumentNullException.ThrowIfNull(captureEffectiveLiberties);
        ArgumentNullException.ThrowIfNull(postCaptureEffectiveLiberties);
        return new(
            true,
            "accepted",
            legalPlacementCommit,
            runtimePlacementCommit,
            captureEffectiveLiberties,
            postCaptureEffectiveLiberties);
    }

    internal static AuthorizedRuntimeStonePlacementResolution Reject(
        string reasonId) =>
        new(false, reasonId, null, null, null, null);
}

internal static class AuthorizedRuntimeStonePlacementPipeline
{
    internal static void InsertCarrierRemovalFacts(
        List<IBattleFact> orderedFacts,
        StoneRuntimePlacementCommit runtimeCommit)
    {
        ArgumentNullException.ThrowIfNull(orderedFacts);
        ArgumentNullException.ThrowIfNull(runtimeCommit);
        if (runtimeCommit.OrderedRemovalFacts.Count == 0)
        {
            return;
        }

        var insertionIndex = orderedFacts.FindLastIndex(fact =>
            fact is GroupCapturedFact);
        if (insertionIndex < 0)
        {
            throw new InvalidOperationException(
                "Carrier removal facts require a captured placement group.");
        }

        orderedFacts.InsertRange(
            insertionIndex + 1,
            runtimeCommit.OrderedRemovalFacts.Cast<IBattleFact>());
    }

    internal static AuthorizedRuntimeStonePlacementResolution Resolve(
        BattleState source,
        BattleAuthoritativeRuntimeState runtime,
        StoneColor actor,
        CanonicalPoint point,
        PlacementAccessMode accessMode,
        StoneRuntimePlacementDescriptor placementDescriptor)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(point);
        ArgumentNullException.ThrowIfNull(placementDescriptor);
        if (!ReferenceEquals(runtime.StoneRuntimeState.SourceBoard, source.Board))
        {
            throw new ArgumentException(
                "Runtime placement preparation requires a runtime bound to the source's exact board snapshot.",
                nameof(runtime));
        }

        var proposedStone = new BoardStone(actor, false, point);
        if (!HypotheticalPlacementResolver.TryCreate(
                source.Board,
                proposedStone,
                out var hypothetical) ||
            hypothetical is null)
        {
            return AuthorizedRuntimeStonePlacementResolution.Reject(
                "stone_occupied");
        }

        if (runtime.HasUsedStoneInstanceId(placementDescriptor.InstanceId))
        {
            return AuthorizedRuntimeStonePlacementResolution.Reject(
                "stone_instance_already_used");
        }

        var provisionalPlaced = new StoneRuntimeInstance(
            placementDescriptor.InstanceId,
            hypothetical.PlacedStone,
            placementDescriptor.KindId,
            runtime.StoneRuntimeState.NextCreatedSequence,
            placementDescriptor.OrderedEffectMetadata);
        var provisionalStones = StoneRuntimeState.Create(
            hypothetical.BoardAfterPlacement,
            runtime.StoneRuntimeState.Instances.Append(provisionalPlaced),
            checked(runtime.StoneRuntimeState.NextCreatedSequence + 1L));
        var provisionalTemporary = TemporaryLibertyState.Create(
            provisionalStones,
            runtime.TemporaryLibertyState.Effects,
            runtime.TemporaryLibertyState.NextCreatedSequence,
            runtime.TemporaryLibertyState.ExpirySweepStartedForEnemyTurnIndex);
        var provisionalContinuous = runtime.ContinuousLibertySnapshot.Rebind(
            provisionalStones);
        var captureEffective = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            provisionalStones,
            provisionalTemporary,
            provisionalContinuous,
            hypothetical.GroupsAfterPlacement);
        var resolved = HypotheticalPlacementResolver.ResolveCaptures(
            hypothetical,
            captureEffective.EffectiveLiberties);
        var postCapture = CreatePostCaptureAnalysis(
            runtime,
            provisionalPlaced,
            resolved);
        var legality = PlacementLegalityEvaluator.Evaluate(
            resolved,
            postCapture.EffectiveLiberties,
            source.RepetitionHistory,
            accessMode);
        if (!legality.IsLegal)
        {
            return AuthorizedRuntimeStonePlacementResolution.Reject(
                legality.ReasonId);
        }

        var legalPlacement = source.RepetitionHistory.CommitLegalPlacement(legality);
        var runtimeCommit = StoneRuntimePlacementIntegrator.Apply(
            runtime.StoneRuntimeState,
            runtime.TemporaryLibertyState,
            legalPlacement,
            placementDescriptor,
            captureEffective,
            postCapture);
        return AuthorizedRuntimeStonePlacementResolution.Accept(
            legalPlacement,
            runtimeCommit,
            captureEffective,
            postCapture);
    }

    private static TemporaryLibertyEffectiveLibertyAnalysis
        CreatePostCaptureAnalysis(
            BattleAuthoritativeRuntimeState runtime,
            StoneRuntimeInstance provisionalPlaced,
            HypotheticalPlacementResolution candidate)
    {
        var retained = runtime.StoneRuntimeState.Instances.Where(instance =>
            ReferenceEquals(
                candidate.BoardAfterCapture.StoneAt(instance.Point),
                instance.Stone));
        var postCaptureStones = StoneRuntimeState.Create(
            candidate.BoardAfterCapture,
            retained.Append(provisionalPlaced),
            checked(runtime.StoneRuntimeState.NextCreatedSequence + 1L));
        var survivingEffects = runtime.TemporaryLibertyState.Effects.Where(effect =>
            postCaptureStones.InstanceById(effect.AnchorStoneInstanceId) is not null);
        var postCaptureTemporary = TemporaryLibertyState.Create(
            postCaptureStones,
            survivingEffects,
            runtime.TemporaryLibertyState.NextCreatedSequence,
            runtime.TemporaryLibertyState.ExpirySweepStartedForEnemyTurnIndex);
        var postCaptureContinuous = runtime.ContinuousLibertySnapshot.Rebind(
            postCaptureStones);
        return TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            postCaptureStones,
            postCaptureTemporary,
            postCaptureContinuous,
            candidate.GroupsAfterCapture);
    }
}
