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

        if (!source.Board.IsEmpty(point))
        {
            return AuthorizedRuntimeStonePlacementResolution.Reject(
                "stone_occupied");
        }

        if (runtime.HasUsedStoneInstanceId(placementDescriptor.InstanceId))
        {
            return AuthorizedRuntimeStonePlacementResolution.Reject(
                "stone_instance_already_used");
        }

        var evaluation = RuntimeStonePlacementEvaluator.Evaluate(
            runtime.StoneRuntimeState,
            runtime.TemporaryLibertyState,
            runtime.ContinuousLibertySnapshot,
            source.RepetitionHistory,
            new BoardStone(actor, false, point),
            accessMode,
            placementDescriptor);
        if (!evaluation.Accepted)
        {
            return AuthorizedRuntimeStonePlacementResolution.Reject(
                evaluation.ReasonId);
        }

        return AuthorizedRuntimeStonePlacementResolution.Accept(
            evaluation.LegalPlacementCommit
                ?? throw new InvalidOperationException(
                    "Accepted runtime evaluation is missing its legal placement."),
            evaluation.RuntimePlacementCommit
                ?? throw new InvalidOperationException(
                    "Accepted runtime evaluation is missing its runtime placement."),
            evaluation.CaptureEffectiveLiberties
                ?? throw new InvalidOperationException(
                    "Accepted runtime evaluation is missing capture liberties."),
            evaluation.PostCaptureEffectiveLiberties
                ?? throw new InvalidOperationException(
                    "Accepted runtime evaluation is missing post-capture liberties."));
    }
}
