using Igorogue.Domain.Board;
using Igorogue.Domain.Content;

namespace Igorogue.Domain.Enemies;

public enum BanditExecutionDecisionKind : byte
{
    Placement = 1,
    Pass = 2,
}

public enum BanditExecutionReason : byte
{
    MandatoryLethalOverride = 1,
    MandatoryDefenseOverride = 2,
    PlannedTarget = 3,
    SameIntentRetarget = 4,
    Fallback = 5,
    Pass = 6,
}

public sealed class BanditExecutionDecision
{
    internal BanditExecutionDecision(
        PlannedEnemyIntent plannedIntent,
        BanditPlacementCandidate? candidate,
        BanditExecutionReason reason,
        int fallbackDepth,
        EnemyTargetReference? targetBefore,
        EnemyTargetReference? targetAfter)
    {
        ArgumentNullException.ThrowIfNull(plannedIntent);
        if (fallbackDepth < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fallbackDepth),
                fallbackDepth,
                "Fallback depth cannot be negative.");
        }

        if ((reason == BanditExecutionReason.Pass) != (candidate is null) ||
            (candidate is null && targetAfter is not null) ||
            (candidate is not null && targetAfter is null))
        {
            throw new ArgumentException("Bandit execution decision shape is inconsistent.");
        }

        PlannedIntent = plannedIntent;
        Candidate = candidate;
        Reason = reason;
        FallbackDepth = fallbackDepth;
        TargetBefore = targetBefore;
        TargetAfter = targetAfter;
    }

    public PlannedEnemyIntent PlannedIntent { get; }

    public BanditExecutionDecisionKind Kind => Candidate is null
        ? BanditExecutionDecisionKind.Pass
        : BanditExecutionDecisionKind.Placement;

    public BanditPlacementCandidate? Candidate { get; }

    public EnemyIntentKind? ExecutedIntentKind => Candidate?.IntentKind;

    public string ExecutedIntentId => ExecutedIntentKind is EnemyIntentKind kind
        ? EnemyIntentKindRules.ToIntentId(kind)
        : "pass";

    public BanditExecutionReason Reason { get; }

    public string ReasonId => Reason switch
    {
        BanditExecutionReason.MandatoryLethalOverride => "mandatory_lethal_override",
        BanditExecutionReason.MandatoryDefenseOverride => "mandatory_defense_override",
        BanditExecutionReason.PlannedTarget => "planned_target",
        BanditExecutionReason.SameIntentRetarget => "same_intent_retarget",
        BanditExecutionReason.Fallback => "fallback",
        BanditExecutionReason.Pass => "pass",
        _ => throw new InvalidOperationException("Unknown Bandit execution reason."),
    };

    public bool Retargeted => Reason is
        BanditExecutionReason.SameIntentRetarget or
        BanditExecutionReason.Fallback;

    public int FallbackDepth { get; }

    public EnemyTargetReference? TargetBefore { get; }

    public EnemyTargetReference? TargetAfter { get; }
}

public static class BanditIntentExecutor
{
    public static BanditExecutionDecision Decide(
        BanditPlanningContext context,
        EnemyContentDefinition enemy,
        PlannedEnemyIntent plannedIntent,
        StoneRuntimePlacementDescriptor placementDescriptor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentNullException.ThrowIfNull(plannedIntent);
        ArgumentNullException.ThrowIfNull(placementDescriptor);

        var lethal = Candidates(
            context,
            enemy,
            EnemyIntentKind.CaptureBlackKing,
            placementDescriptor);
        if (lethal.Count > 0)
        {
            return PlacementDecision(
                plannedIntent,
                lethal[0],
                BanditExecutionReason.MandatoryLethalOverride,
                fallbackDepth: 0,
                plannedIntent.TargetReference,
                lethal[0].TargetReference);
        }

        if (context.EffectiveLibertiesFor(context.WhiteKingGroup) <=
            enemy.Parameters.DefenseThreshold)
        {
            var defense = Candidates(
                context,
                enemy,
                EnemyIntentKind.DefendWhiteKing,
                placementDescriptor);
            if (defense.Count > 0)
            {
                return PlacementDecision(
                    plannedIntent,
                    defense[0],
                    BanditExecutionReason.MandatoryDefenseOverride,
                    fallbackDepth: 0,
                    plannedIntent.TargetReference,
                    defense[0].TargetReference);
            }
        }

        if (plannedIntent.IntentKind is not EnemyIntentKind plannedKind)
        {
            return PassDecision(plannedIntent, fallbackDepth: 0);
        }

        var sameIntent = Candidates(context, enemy, plannedKind, placementDescriptor);
        var resolvedPlannedTarget = ResolvePlannedTarget(context, plannedIntent.TargetReference);
        if (resolvedPlannedTarget is not null)
        {
            var plannedTargetCandidate = sameIntent.FirstOrDefault(candidate =>
                ReferenceEquals(candidate.TargetGroup, resolvedPlannedTarget));
            if (plannedTargetCandidate is not null)
            {
                return PlacementDecision(
                    plannedIntent,
                    plannedTargetCandidate,
                    BanditExecutionReason.PlannedTarget,
                    fallbackDepth: 0,
                    plannedIntent.TargetReference,
                    plannedIntent.TargetReference
                        ?? throw new InvalidOperationException(
                            "Resolved planned target requires a target reference."));
            }
        }

        if (sameIntent.Count > 0)
        {
            return PlacementDecision(
                plannedIntent,
                sameIntent[0],
                BanditExecutionReason.SameIntentRetarget,
                fallbackDepth: 0,
                plannedIntent.TargetReference,
                sameIntent[0].TargetReference);
        }

        var definition = enemy.Intents.SingleOrDefault(intent => intent.Kind == plannedKind)
            ?? throw new ArgumentException(
                $"Enemy definition does not contain planned intent {plannedKind}.",
                nameof(enemy));
        for (var index = 0; index < definition.Fallback.Count; index++)
        {
            var fallback = Candidates(
                context,
                enemy,
                definition.Fallback[index],
                placementDescriptor);
            if (fallback.Count > 0)
            {
                return PlacementDecision(
                    plannedIntent,
                    fallback[0],
                    BanditExecutionReason.Fallback,
                    fallbackDepth: index + 1,
                    plannedIntent.TargetReference,
                    fallback[0].TargetReference);
            }
        }

        return PassDecision(plannedIntent, definition.Fallback.Count);
    }

    private static StoneGroup? ResolvePlannedTarget(
        BanditPlanningContext context,
        EnemyTargetReference? targetReference)
    {
        if (targetReference is null ||
            context.Board.StoneAt(targetReference.Anchor) is not { } anchorStone ||
            anchorStone.Color != targetReference.Color)
        {
            return null;
        }

        return context.GroupAnalysis.GroupAt(targetReference.Anchor);
    }

    private static IReadOnlyList<BanditPlacementCandidate> Candidates(
        BanditPlanningContext context,
        EnemyContentDefinition enemy,
        EnemyIntentKind intentKind,
        StoneRuntimePlacementDescriptor placementDescriptor) =>
        BanditCandidateGenerator.Generate(context, enemy, intentKind, placementDescriptor);

    private static BanditExecutionDecision PlacementDecision(
        PlannedEnemyIntent plannedIntent,
        BanditPlacementCandidate candidate,
        BanditExecutionReason reason,
        int fallbackDepth,
        EnemyTargetReference? targetBefore,
        EnemyTargetReference targetAfter) =>
        new(
            plannedIntent,
            candidate,
            reason,
            fallbackDepth,
            targetBefore,
            targetAfter);

    private static BanditExecutionDecision PassDecision(
        PlannedEnemyIntent plannedIntent,
        int fallbackDepth) =>
        new(
            plannedIntent,
            candidate: null,
            BanditExecutionReason.Pass,
            fallbackDepth,
            plannedIntent.TargetReference,
            targetAfter: null);
}
