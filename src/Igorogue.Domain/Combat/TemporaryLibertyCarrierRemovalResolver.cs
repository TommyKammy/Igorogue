using System.Collections.ObjectModel;

using Igorogue.Domain.Board;

namespace Igorogue.Domain.Combat;

internal sealed class TemporaryLibertyCarrierRemovalResolution
{
    private readonly ReadOnlyCollection<TemporaryLibertyEffect> removedEffectView;
    private readonly ReadOnlyCollection<TemporaryLibertyRemovedFact> factView;

    internal TemporaryLibertyCarrierRemovalResolution(
        TemporaryLibertyState sourceState,
        StoneRuntimeState resultStones,
        TemporaryLibertyState stateAfterRemoval,
        TemporaryLibertyEffect[] removedEffects,
        TemporaryLibertyRemovedFact[] orderedFacts)
    {
        SourceState = sourceState;
        ResultStones = resultStones;
        StateAfterRemoval = stateAfterRemoval;
        removedEffectView = Array.AsReadOnly(
            (TemporaryLibertyEffect[])removedEffects.Clone());
        factView = Array.AsReadOnly(
            (TemporaryLibertyRemovedFact[])orderedFacts.Clone());
    }

    internal TemporaryLibertyState SourceState { get; }

    internal StoneRuntimeState ResultStones { get; }

    internal TemporaryLibertyState StateAfterRemoval { get; }

    internal IReadOnlyList<TemporaryLibertyEffect> RemovedEffects => removedEffectView;

    internal IReadOnlyList<TemporaryLibertyRemovedFact> OrderedFacts => factView;
}

internal static class TemporaryLibertyCarrierRemovalResolver
{
    internal static TemporaryLibertyCarrierRemovalResolution Resolve(
        TemporaryLibertyState sourceState,
        StoneRuntimeState resultStones)
    {
        ArgumentNullException.ThrowIfNull(sourceState);
        ArgumentNullException.ThrowIfNull(resultStones);
        if (!ReferenceEquals(
                sourceState.SourceStones.SourceBoard.Geometry,
                resultStones.SourceBoard.Geometry))
        {
            throw new ArgumentException(
                "Carrier removal result must use the exact source geometry.",
                nameof(resultStones));
        }

        foreach (var resultInstance in resultStones.Instances)
        {
            var sourceInstance = sourceState.SourceStones.InstanceById(
                resultInstance.InstanceId);
            if (sourceInstance is not null && !ReferenceEquals(sourceInstance, resultInstance))
            {
                throw new ArgumentException(
                    $"Live stone instance {resultInstance.InstanceId} changed identity during carrier removal.",
                    nameof(resultStones));
            }

            var sourceAtPoint = sourceState.SourceStones.InstanceAt(resultInstance.Point);
            if (sourceAtPoint is not null &&
                ReferenceEquals(sourceAtPoint.Stone, resultInstance.Stone) &&
                !ReferenceEquals(sourceAtPoint, resultInstance))
            {
                throw new ArgumentException(
                    $"Surviving stone at {resultInstance.Point} changed runtime identity during carrier removal.",
                    nameof(resultStones));
            }
        }

        if (ReferenceEquals(sourceState.SourceStones, resultStones))
        {
            return new TemporaryLibertyCarrierRemovalResolution(
                sourceState,
                resultStones,
                sourceState,
                [],
                []);
        }

        var removedEffects = sourceState.Effects
            .Where(effect => resultStones.InstanceById(effect.AnchorStoneInstanceId) is null)
            .ToArray();
        var removedIds = new HashSet<string>(
            removedEffects.Select(effect => effect.EffectInstanceId),
            StringComparer.Ordinal);
        var stateAfterRemoval = sourceState.ReplaceEffects(
            resultStones,
            sourceState.Effects.Where(effect => !removedIds.Contains(effect.EffectInstanceId)),
            sourceState.ExpirySweepStartedForEnemyTurnIndex);
        var facts = removedEffects
            .Select(effect => new TemporaryLibertyRemovedFact(
                effect,
                TemporaryLibertyRemovalReason.CarrierRemoved))
            .ToArray();

        return new TemporaryLibertyCarrierRemovalResolution(
            sourceState,
            resultStones,
            stateAfterRemoval,
            removedEffects,
            facts);
    }
}
