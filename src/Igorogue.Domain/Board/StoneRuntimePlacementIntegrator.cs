using System.Collections.ObjectModel;

using Igorogue.Domain.Combat;

namespace Igorogue.Domain.Board;

public sealed class StoneRuntimePlacementDescriptor
{
    private readonly ReadOnlyCollection<string> effectMetadataView;

    public StoneRuntimePlacementDescriptor(
        string instanceId,
        string kindId,
        IEnumerable<string> orderedEffectMetadata)
    {
        InstanceId = StableDomainId.Validate(instanceId, nameof(instanceId));
        KindId = StableDomainId.Validate(kindId, nameof(kindId));
        ArgumentNullException.ThrowIfNull(orderedEffectMetadata);
        effectMetadataView = Array.AsReadOnly(
            orderedEffectMetadata
                .Select(value => StableDomainId.Validate(
                    value,
                    nameof(orderedEffectMetadata)))
                .ToArray());
    }

    public string InstanceId { get; }

    public string KindId { get; }

    public IReadOnlyList<string> OrderedEffectMetadata => effectMetadataView;
}

public sealed class StoneRuntimePlacementCommit
{
    private readonly ReadOnlyCollection<TemporaryLibertyEffect> removedEffectView;
    private readonly ReadOnlyCollection<TemporaryLibertyRemovedFact> removalFactView;

    internal StoneRuntimePlacementCommit(
        StoneRuntimeState sourceStones,
        TemporaryLibertyState sourceTemporaryLiberties,
        LegalPlacementCommit acceptedPlacement,
        StoneRuntimeInstance placedStoneInstance,
        StoneRuntimeState stonesAfterCommit,
        TemporaryLibertyState temporaryLibertiesAfterCommit,
        TemporaryLibertyEffect[] removedEffects,
        TemporaryLibertyRemovedFact[] orderedRemovalFacts)
    {
        SourceStones = sourceStones;
        SourceTemporaryLiberties = sourceTemporaryLiberties;
        AcceptedPlacement = acceptedPlacement;
        PlacedStoneInstance = placedStoneInstance;
        StonesAfterCommit = stonesAfterCommit;
        TemporaryLibertiesAfterCommit = temporaryLibertiesAfterCommit;
        removedEffectView = Array.AsReadOnly(
            (TemporaryLibertyEffect[])removedEffects.Clone());
        removalFactView = Array.AsReadOnly(
            (TemporaryLibertyRemovedFact[])orderedRemovalFacts.Clone());
    }

    public StoneRuntimeState SourceStones { get; }

    public TemporaryLibertyState SourceTemporaryLiberties { get; }

    public StoneRuntimeInstance PlacedStoneInstance { get; }

    public StoneRuntimeState StonesAfterCommit { get; }

    public TemporaryLibertyState TemporaryLibertiesAfterCommit { get; }

    public IReadOnlyList<TemporaryLibertyEffect> RemovedCarrierEffects => removedEffectView;

    public IReadOnlyList<TemporaryLibertyRemovedFact> OrderedRemovalFacts => removalFactView;

    public BoardState BoardAfterCommit => AcceptedPlacement.BoardAfterCommit;

    internal LegalPlacementCommit AcceptedPlacement { get; }
}

public static class StoneRuntimePlacementIntegrator
{
    public static StoneRuntimePlacementCommit Apply(
        StoneRuntimeState sourceStones,
        TemporaryLibertyState sourceTemporaryLiberties,
        LegalPlacementCommit acceptedPlacement,
        StoneRuntimePlacementDescriptor placedStoneDescriptor,
        TemporaryLibertyEffectiveLibertyAnalysis captureEffectiveLiberties,
        TemporaryLibertyEffectiveLibertyAnalysis postCaptureEffectiveLiberties)
    {
        ArgumentNullException.ThrowIfNull(sourceStones);
        ArgumentNullException.ThrowIfNull(sourceTemporaryLiberties);
        ArgumentNullException.ThrowIfNull(acceptedPlacement);
        ArgumentNullException.ThrowIfNull(placedStoneDescriptor);
        ArgumentNullException.ThrowIfNull(captureEffectiveLiberties);
        ArgumentNullException.ThrowIfNull(postCaptureEffectiveLiberties);
        if (!ReferenceEquals(
                sourceStones.SourceBoard,
                acceptedPlacement.Candidate.SourceBoard))
        {
            throw new ArgumentException(
                "Stone runtime state must belong to the accepted placement's exact source board.",
                nameof(sourceStones));
        }

        if (!ReferenceEquals(sourceStones, sourceTemporaryLiberties.SourceStones))
        {
            throw new ArgumentException(
                "Temporary liberties must belong to the exact source stone runtime.",
                nameof(sourceTemporaryLiberties));
        }

        if (sourceStones.InstanceById(placedStoneDescriptor.InstanceId) is not null)
        {
            throw new ArgumentException(
                $"Placed stone instance ID {placedStoneDescriptor.InstanceId} is already live.",
                nameof(placedStoneDescriptor));
        }

        ValidateCaptureEvaluation(
            sourceStones,
            sourceTemporaryLiberties,
            acceptedPlacement,
            placedStoneDescriptor,
            captureEffectiveLiberties);
        ValidatePostCaptureEvaluation(
            sourceStones,
            sourceTemporaryLiberties,
            acceptedPlacement,
            captureEffectiveLiberties,
            postCaptureEffectiveLiberties);

        var placedStoneInstance = captureEffectiveLiberties.SourceStones.InstanceAt(
            acceptedPlacement.Candidate.PlacedStone.Point)
            ?? throw new InvalidOperationException(
                "Validated capture analysis must retain the placed stone runtime instance.");
        var stonesAfterCommit = postCaptureEffectiveLiberties.SourceStones;
        var carrierRemoval = TemporaryLibertyCarrierRemovalResolver.Resolve(
            sourceTemporaryLiberties,
            stonesAfterCommit);

        return new StoneRuntimePlacementCommit(
            sourceStones,
            sourceTemporaryLiberties,
            acceptedPlacement,
            placedStoneInstance,
            stonesAfterCommit,
            postCaptureEffectiveLiberties.TemporaryLiberties,
            carrierRemoval.RemovedEffects.ToArray(),
            carrierRemoval.OrderedFacts.ToArray());
    }

    private static void ValidateCaptureEvaluation(
        StoneRuntimeState sourceStones,
        TemporaryLibertyState sourceTemporaryLiberties,
        LegalPlacementCommit acceptedPlacement,
        StoneRuntimePlacementDescriptor placedStoneDescriptor,
        TemporaryLibertyEffectiveLibertyAnalysis captureEffectiveLiberties)
    {
        var candidate = acceptedPlacement.Candidate;
        var placement = candidate.SourcePlacement;
        if (!ReferenceEquals(
                candidate.CaptureEffectiveLiberties,
                captureEffectiveLiberties.EffectiveLiberties) ||
            !ReferenceEquals(
                placement.GroupsAfterPlacement,
                captureEffectiveLiberties.GroupAnalysis))
        {
            throw new ArgumentException(
                "Accepted placement capture must use the supplied exact temporary-liberty analysis.",
                nameof(captureEffectiveLiberties));
        }

        var provisionalStones = captureEffectiveLiberties.SourceStones;
        if (!ReferenceEquals(
                provisionalStones.SourceBoard,
                placement.BoardAfterPlacement))
        {
            throw new ArgumentException(
                "Capture analysis must belong to the accepted placement's exact post-placement board.",
                nameof(captureEffectiveLiberties));
        }

        if (provisionalStones.Instances.Count != sourceStones.Instances.Count + 1 ||
            provisionalStones.NextCreatedSequence !=
                checked(sourceStones.NextCreatedSequence + 1L))
        {
            throw new ArgumentException(
                "Capture analysis must contain exactly the source runtime stones plus one placed stone.",
                nameof(captureEffectiveLiberties));
        }

        foreach (var sourceInstance in sourceStones.Instances)
        {
            if (!ReferenceEquals(
                    provisionalStones.InstanceById(sourceInstance.InstanceId),
                    sourceInstance))
            {
                throw new ArgumentException(
                    $"Capture analysis changed source stone runtime identity {sourceInstance.InstanceId}.",
                    nameof(captureEffectiveLiberties));
            }
        }

        var provisionalPlaced = provisionalStones.InstanceAt(placement.PlacedStone.Point);
        if (provisionalPlaced is null ||
            !ReferenceEquals(provisionalPlaced.Stone, placement.PlacedStone) ||
            !StringComparer.Ordinal.Equals(
                provisionalPlaced.InstanceId,
                placedStoneDescriptor.InstanceId) ||
            !StringComparer.Ordinal.Equals(
                provisionalPlaced.KindId,
                placedStoneDescriptor.KindId) ||
            provisionalPlaced.CreatedSequence != sourceStones.NextCreatedSequence ||
            !provisionalPlaced.OrderedEffectMetadata.SequenceEqual(
                placedStoneDescriptor.OrderedEffectMetadata,
                StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "Capture analysis placed-stone runtime does not match the supplied descriptor.",
                nameof(captureEffectiveLiberties));
        }

        var provisionalTemporary = captureEffectiveLiberties.TemporaryLiberties;
        if (provisionalTemporary.Effects.Count != sourceTemporaryLiberties.Effects.Count ||
            provisionalTemporary.NextCreatedSequence !=
                sourceTemporaryLiberties.NextCreatedSequence ||
            provisionalTemporary.ExpirySweepStartedForEnemyTurnIndex !=
                sourceTemporaryLiberties.ExpirySweepStartedForEnemyTurnIndex)
        {
            throw new ArgumentException(
                "Capture analysis temporary liberties do not match the exact source state.",
                nameof(captureEffectiveLiberties));
        }

        foreach (var sourceEffect in sourceTemporaryLiberties.Effects)
        {
            if (!ReferenceEquals(
                    provisionalTemporary.EffectById(sourceEffect.EffectInstanceId),
                    sourceEffect))
            {
                throw new ArgumentException(
                    $"Capture analysis changed temporary liberty effect {sourceEffect.EffectInstanceId}.",
                    nameof(captureEffectiveLiberties));
            }
        }
    }

    private static void ValidatePostCaptureEvaluation(
        StoneRuntimeState sourceStones,
        TemporaryLibertyState sourceTemporaryLiberties,
        LegalPlacementCommit acceptedPlacement,
        TemporaryLibertyEffectiveLibertyAnalysis captureEffectiveLiberties,
        TemporaryLibertyEffectiveLibertyAnalysis postCaptureEffectiveLiberties)
    {
        var candidate = acceptedPlacement.Candidate;
        if (!ReferenceEquals(
                acceptedPlacement.EvaluatedEffectiveLiberties,
                postCaptureEffectiveLiberties.EffectiveLiberties) ||
            !ReferenceEquals(
                candidate.GroupsAfterCapture,
                postCaptureEffectiveLiberties.GroupAnalysis))
        {
            throw new ArgumentException(
                "Accepted placement legality must use the supplied exact post-capture temporary-liberty analysis.",
                nameof(postCaptureEffectiveLiberties));
        }

        var postCaptureStones = postCaptureEffectiveLiberties.SourceStones;
        if (!ReferenceEquals(
                postCaptureStones.SourceBoard,
                acceptedPlacement.BoardAfterCommit) ||
            postCaptureStones.NextCreatedSequence !=
                checked(sourceStones.NextCreatedSequence + 1L))
        {
            throw new ArgumentException(
                "Post-capture analysis must belong to the accepted commit's exact result board and sequence.",
                nameof(postCaptureEffectiveLiberties));
        }

        var provisionalPlaced = captureEffectiveLiberties.SourceStones.InstanceAt(
            candidate.PlacedStone.Point)
            ?? throw new InvalidOperationException(
                "Capture analysis lost the placed runtime instance.");
        if (!ReferenceEquals(
                postCaptureStones.InstanceAt(candidate.PlacedStone.Point),
                provisionalPlaced))
        {
            throw new ArgumentException(
                "Post-capture analysis changed the placed stone runtime identity.",
                nameof(postCaptureEffectiveLiberties));
        }

        foreach (var sourceInstance in sourceStones.Instances)
        {
            var survived = ReferenceEquals(
                acceptedPlacement.BoardAfterCommit.StoneAt(sourceInstance.Point),
                sourceInstance.Stone);
            var resultInstance = postCaptureStones.InstanceById(sourceInstance.InstanceId);
            if (survived != ReferenceEquals(resultInstance, sourceInstance))
            {
                throw new ArgumentException(
                    $"Post-capture analysis does not preserve the exact survivor set for {sourceInstance.InstanceId}.",
                    nameof(postCaptureEffectiveLiberties));
            }
        }

        var expectedEffects = sourceTemporaryLiberties.Effects
            .Where(effect =>
                postCaptureStones.InstanceById(effect.AnchorStoneInstanceId) is not null)
            .ToArray();
        var postCaptureTemporary = postCaptureEffectiveLiberties.TemporaryLiberties;
        if (postCaptureTemporary.Effects.Count != expectedEffects.Length ||
            postCaptureTemporary.NextCreatedSequence !=
                sourceTemporaryLiberties.NextCreatedSequence ||
            postCaptureTemporary.ExpirySweepStartedForEnemyTurnIndex !=
                sourceTemporaryLiberties.ExpirySweepStartedForEnemyTurnIndex)
        {
            throw new ArgumentException(
                "Post-capture analysis temporary liberties do not match carrier removal from the source state.",
                nameof(postCaptureEffectiveLiberties));
        }

        foreach (var expectedEffect in expectedEffects)
        {
            if (!ReferenceEquals(
                    postCaptureTemporary.EffectById(expectedEffect.EffectInstanceId),
                    expectedEffect))
            {
                throw new ArgumentException(
                    $"Post-capture analysis changed surviving effect {expectedEffect.EffectInstanceId}.",
                    nameof(postCaptureEffectiveLiberties));
            }
        }
    }
}
