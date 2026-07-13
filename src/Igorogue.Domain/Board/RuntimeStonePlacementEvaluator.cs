using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Board;

public sealed class RuntimeStonePlacementEvaluation
{
    private RuntimeStonePlacementEvaluation(
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
                "Accepted runtime placement evaluation requires every exact projection.",
                nameof(accepted));
        }

        Accepted = accepted;
        ReasonId = reasonId;
        LegalPlacementCommit = legalPlacementCommit;
        RuntimePlacementCommit = runtimePlacementCommit;
        CaptureEffectiveLiberties = captureEffectiveLiberties;
        PostCaptureEffectiveLiberties = postCaptureEffectiveLiberties;
    }

    public bool Accepted { get; }

    public string ReasonId { get; }

    public LegalPlacementCommit? LegalPlacementCommit { get; }

    public StoneRuntimePlacementCommit? RuntimePlacementCommit { get; }

    public TemporaryLibertyEffectiveLibertyAnalysis? CaptureEffectiveLiberties { get; }

    public TemporaryLibertyEffectiveLibertyAnalysis? PostCaptureEffectiveLiberties { get; }

    internal static RuntimeStonePlacementEvaluation Accept(
        LegalPlacementCommit legalPlacementCommit,
        StoneRuntimePlacementCommit runtimePlacementCommit,
        TemporaryLibertyEffectiveLibertyAnalysis captureEffectiveLiberties,
        TemporaryLibertyEffectiveLibertyAnalysis postCaptureEffectiveLiberties) =>
        new(
            true,
            "accepted",
            legalPlacementCommit,
            runtimePlacementCommit,
            captureEffectiveLiberties,
            postCaptureEffectiveLiberties);

    internal static RuntimeStonePlacementEvaluation Reject(string reasonId) =>
        new(false, reasonId, null, null, null, null);
}

public static class RuntimeStonePlacementEvaluator
{
    public static RuntimeStonePlacementEvaluation Evaluate(
        StoneRuntimeState sourceStones,
        TemporaryLibertyState sourceTemporaryLiberties,
        ContinuousLibertySnapshot sourceContinuousLiberties,
        BattleRepetitionHistory repetitionHistory,
        BoardStone proposedStone,
        PlacementAccessMode accessMode,
        StoneRuntimePlacementDescriptor placementDescriptor)
    {
        ArgumentNullException.ThrowIfNull(sourceStones);
        ArgumentNullException.ThrowIfNull(sourceTemporaryLiberties);
        ArgumentNullException.ThrowIfNull(sourceContinuousLiberties);
        ArgumentNullException.ThrowIfNull(repetitionHistory);
        ArgumentNullException.ThrowIfNull(proposedStone);
        ArgumentNullException.ThrowIfNull(placementDescriptor);
        if (!ReferenceEquals(sourceTemporaryLiberties.SourceStones, sourceStones))
        {
            throw new ArgumentException(
                "Temporary liberties must belong to the exact source stone runtime.",
                nameof(sourceTemporaryLiberties));
        }

        if (!ReferenceEquals(sourceContinuousLiberties.SourceStones, sourceStones))
        {
            throw new ArgumentException(
                "Continuous liberties must belong to the exact source stone runtime.",
                nameof(sourceContinuousLiberties));
        }

        if (!repetitionHistory.Current.Equals(
                StoneTopologyKey.FromBoard(sourceStones.SourceBoard)))
        {
            throw new ArgumentException(
                "Repetition history must end at the exact source board topology.",
                nameof(repetitionHistory));
        }

        if (!HypotheticalPlacementResolver.TryCreate(
                sourceStones.SourceBoard,
                proposedStone,
                out var hypothetical) ||
            hypothetical is null)
        {
            return RuntimeStonePlacementEvaluation.Reject("stone_occupied");
        }

        if (sourceStones.InstanceById(placementDescriptor.InstanceId) is not null)
        {
            return RuntimeStonePlacementEvaluation.Reject("stone_instance_already_live");
        }

        var provisionalPlaced = new StoneRuntimeInstance(
            placementDescriptor.InstanceId,
            hypothetical.PlacedStone,
            placementDescriptor.KindId,
            sourceStones.NextCreatedSequence,
            placementDescriptor.OrderedEffectMetadata);
        var provisionalStones = StoneRuntimeState.Create(
            hypothetical.BoardAfterPlacement,
            sourceStones.Instances.Append(provisionalPlaced),
            checked(sourceStones.NextCreatedSequence + 1L));
        var provisionalTemporary = TemporaryLibertyState.Create(
            provisionalStones,
            sourceTemporaryLiberties.Effects,
            sourceTemporaryLiberties.NextCreatedSequence,
            sourceTemporaryLiberties.ExpirySweepStartedForEnemyTurnIndex);
        var provisionalContinuous = sourceContinuousLiberties.Rebind(provisionalStones);
        var captureEffective = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            provisionalStones,
            provisionalTemporary,
            provisionalContinuous,
            hypothetical.GroupsAfterPlacement);
        var resolved = HypotheticalPlacementResolver.ResolveCaptures(
            hypothetical,
            captureEffective.EffectiveLiberties);
        var postCapture = CreatePostCaptureAnalysis(
            sourceStones,
            sourceTemporaryLiberties,
            sourceContinuousLiberties,
            provisionalPlaced,
            resolved);
        var legality = PlacementLegalityEvaluator.Evaluate(
            resolved,
            postCapture.EffectiveLiberties,
            repetitionHistory,
            accessMode);
        if (!legality.IsLegal)
        {
            return RuntimeStonePlacementEvaluation.Reject(legality.ReasonId);
        }

        var legalPlacement = repetitionHistory.CommitLegalPlacement(legality);
        var runtimeCommit = StoneRuntimePlacementIntegrator.Apply(
            sourceStones,
            sourceTemporaryLiberties,
            legalPlacement,
            placementDescriptor,
            captureEffective,
            postCapture);
        return RuntimeStonePlacementEvaluation.Accept(
            legalPlacement,
            runtimeCommit,
            captureEffective,
            postCapture);
    }

    private static TemporaryLibertyEffectiveLibertyAnalysis CreatePostCaptureAnalysis(
        StoneRuntimeState sourceStones,
        TemporaryLibertyState sourceTemporaryLiberties,
        ContinuousLibertySnapshot sourceContinuousLiberties,
        StoneRuntimeInstance provisionalPlaced,
        HypotheticalPlacementResolution candidate)
    {
        var retained = sourceStones.Instances.Where(instance =>
            ReferenceEquals(candidate.BoardAfterCapture.StoneAt(instance.Point), instance.Stone));
        var postCaptureStones = StoneRuntimeState.Create(
            candidate.BoardAfterCapture,
            retained.Append(provisionalPlaced),
            checked(sourceStones.NextCreatedSequence + 1L));
        var survivingEffects = sourceTemporaryLiberties.Effects.Where(effect =>
            postCaptureStones.InstanceById(effect.AnchorStoneInstanceId) is not null);
        var postCaptureTemporary = TemporaryLibertyState.Create(
            postCaptureStones,
            survivingEffects,
            sourceTemporaryLiberties.NextCreatedSequence,
            sourceTemporaryLiberties.ExpirySweepStartedForEnemyTurnIndex);
        var postCaptureContinuous = sourceContinuousLiberties.Rebind(postCaptureStones);
        return TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            postCaptureStones,
            postCaptureTemporary,
            postCaptureContinuous,
            candidate.GroupsAfterCapture);
    }
}
