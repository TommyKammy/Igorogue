using Igorogue.Domain.Board;

namespace Igorogue.Domain.Combat;

public static class TemporaryLibertyExpiryResolver
{
    public const string TopologySourceReasonId = "temporary_liberty_expired";

    public static TemporaryLibertyExpiryResolution Resolve(
        StoneRuntimeState sourceStones,
        TemporaryLibertyState sourceTemporaryLiberties,
        ContinuousLibertySnapshot continuousLiberties,
        BattleRepetitionHistory sourceHistory,
        int enemyTurnIndex)
    {
        ArgumentNullException.ThrowIfNull(sourceStones);
        ArgumentNullException.ThrowIfNull(sourceTemporaryLiberties);
        ArgumentNullException.ThrowIfNull(continuousLiberties);
        ArgumentNullException.ThrowIfNull(sourceHistory);
        if (enemyTurnIndex <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(enemyTurnIndex),
                enemyTurnIndex,
                "Enemy-turn index must be positive.");
        }

        if (!ReferenceEquals(sourceStones, sourceTemporaryLiberties.SourceStones))
        {
            throw new ArgumentException(
                "Temporary liberties must belong to the exact source stone runtime.",
                nameof(sourceTemporaryLiberties));
        }

        if (!ReferenceEquals(sourceStones, continuousLiberties.SourceStones))
        {
            throw new ArgumentException(
                "Continuous liberties must belong to the exact source stone runtime.",
                nameof(continuousLiberties));
        }

        var sourceTopology = StoneTopologyKey.FromBoard(sourceStones.SourceBoard);
        if (!sourceHistory.Current.Equals(sourceTopology))
        {
            throw new ArgumentException(
                "Repetition history must end at the exact source board topology.",
                nameof(sourceHistory));
        }

        if (sourceTemporaryLiberties.Effects.Any(
                effect => effect.ExpiresAfterEnemyTurnIndex < enemyTurnIndex))
        {
            throw new InvalidOperationException(
                "Temporary liberty state contains an overdue effect from an earlier boundary.");
        }

        if (sourceTemporaryLiberties.ExpirySweepStartedForEnemyTurnIndex is int sweepMarker &&
            sweepMarker > enemyTurnIndex)
        {
            throw new InvalidOperationException(
                "Enemy-turn expiry boundaries cannot precede the stored completed sweep marker.");
        }

        var dueEffects = sourceTemporaryLiberties.Effects
            .Where(effect => effect.ExpiresAfterEnemyTurnIndex == enemyTurnIndex)
            .ToArray();
        if (dueEffects.Length == 0)
        {
            return CreateNoOp(
                sourceStones,
                sourceTemporaryLiberties,
                continuousLiberties,
                sourceHistory,
                enemyTurnIndex);
        }

        var dueIds = new HashSet<string>(
            dueEffects.Select(effect => effect.EffectInstanceId),
            StringComparer.Ordinal);
        var afterExpiry = sourceTemporaryLiberties.ReplaceEffects(
            sourceStones,
            sourceTemporaryLiberties.Effects.Where(effect =>
                !dueIds.Contains(effect.EffectInstanceId)),
            enemyTurnIndex);
        var effectiveAnalysis = TemporaryLibertyEffectiveLibertyAnalyzer.Analyze(
            sourceStones,
            afterExpiry,
            continuousLiberties);
        var doomedGroups = effectiveAnalysis.GroupAnalysis.Groups
            .Where(group => effectiveAnalysis.EffectiveLiberties.EffectiveLibertiesFor(group) == 0)
            .ToArray();
        var orderedFacts = new List<IBattleFact>
        {
            new TemporaryLibertyExpirySweepStartedFact(enemyTurnIndex),
        };
        orderedFacts.AddRange(dueEffects.Select(effect =>
            (IBattleFact)new TemporaryLibertyExpiredFact(effect)));

        if (doomedGroups.Length == 0)
        {
            var ongoing = KingCaptureResultEvaluator.EvaluateAtomicCapture(doomedGroups);
            orderedFacts.Add(new TemporaryLibertyExpirySweepResolvedFact(
                enemyTurnIndex,
                0,
                false));
            return new TemporaryLibertyExpiryResolution(
                sourceStones,
                sourceTemporaryLiberties,
                continuousLiberties,
                sourceHistory,
                enemyTurnIndex,
                false,
                sourceStones,
                afterExpiry,
                sourceHistory,
                dueEffects,
                [],
                doomedGroups,
                [],
                ongoing,
                TemporaryLibertyCaptureBenefitDisposition.NotApplicable,
                TerritoryAnalyzer.Analyze(sourceStones.SourceBoard),
                effectiveAnalysis,
                null,
                null,
                orderedFacts.ToArray());
        }

        var capturedPointMask = new bool[sourceStones.SourceBoard.Geometry.PointCount];
        var capturedStoneInstances = new List<StoneRuntimeInstance>();
        foreach (var group in doomedGroups)
        {
            orderedFacts.Add(new TemporaryLibertyGroupCapturedFact(group));
            foreach (var stone in group.Stones)
            {
                capturedStoneInstances.Add(
                    sourceStones.InstanceAt(stone.Point)
                    ?? throw new InvalidOperationException(
                        "Every captured stone must have a runtime instance."));
                capturedPointMask[sourceStones.SourceBoard.Geometry.ToCanonicalIndex(stone.Point)] =
                    true;
            }
        }

        var resultBoard = BoardState.Create(
            sourceStones.SourceBoard.Geometry,
            sourceStones.SourceBoard.OccupiedStones.Where(stone =>
                !capturedPointMask[sourceStones.SourceBoard.Geometry.ToCanonicalIndex(stone.Point)]));
        var resultStones = sourceStones.RebindAfterRemoval(resultBoard);
        var carrierRemoval = TemporaryLibertyCarrierRemovalResolver.Resolve(
            afterExpiry,
            resultStones);
        orderedFacts.AddRange(carrierRemoval.OrderedFacts);

        var mandatoryCommit = sourceHistory.CommitMandatoryMutation(
            sourceStones.SourceBoard,
            resultBoard,
            TopologySourceReasonId);
        orderedFacts.Add(mandatoryCommit.RegistrationFact);

        var kingResult = KingCaptureResultEvaluator.EvaluateAtomicCapture(doomedGroups);
        var disposition = kingResult.IsTerminal
            ? TemporaryLibertyCaptureBenefitDisposition.SuppressedByTerminalKingCapture
            : TemporaryLibertyCaptureBenefitDisposition.PendingNonTerminalPipeline;
        if (kingResult.HasKingCapture)
        {
            orderedFacts.Add(new TemporaryLibertyKingGateFact(kingResult));
        }

        if (kingResult.IsTerminal)
        {
            orderedFacts.Add(new CaptureBenefitSuppressedFact("terminal_king_capture"));
        }

        orderedFacts.Add(new TemporaryLibertyExpirySweepResolvedFact(
            enemyTurnIndex,
            doomedGroups.Length,
            kingResult.IsTerminal));

        return new TemporaryLibertyExpiryResolution(
            sourceStones,
            sourceTemporaryLiberties,
            continuousLiberties,
            sourceHistory,
            enemyTurnIndex,
            false,
            resultStones,
            carrierRemoval.StateAfterRemoval,
            mandatoryCommit.HistoryAfterCommit,
            dueEffects,
            carrierRemoval.RemovedEffects.ToArray(),
            doomedGroups,
            capturedStoneInstances.ToArray(),
            kingResult,
            disposition,
            TerritoryAnalyzer.Analyze(resultBoard),
            effectiveAnalysis,
            mandatoryCommit.FirstSeen,
            mandatoryCommit.RegisteredTopologyKey,
            orderedFacts.ToArray());
    }

    private static TemporaryLibertyExpiryResolution CreateNoOp(
        StoneRuntimeState sourceStones,
        TemporaryLibertyState sourceTemporaryLiberties,
        ContinuousLibertySnapshot continuousLiberties,
        BattleRepetitionHistory sourceHistory,
        int enemyTurnIndex) =>
        new(
            sourceStones,
            sourceTemporaryLiberties,
            continuousLiberties,
            sourceHistory,
            enemyTurnIndex,
            true,
            sourceStones,
            sourceTemporaryLiberties,
            sourceHistory,
            [],
            [],
            [],
            [],
            KingCaptureResultEvaluator.EvaluateAtomicCapture([]),
            TemporaryLibertyCaptureBenefitDisposition.NotApplicable,
            TerritoryAnalyzer.Analyze(sourceStones.SourceBoard),
            null,
            null,
            null,
            []);
}
