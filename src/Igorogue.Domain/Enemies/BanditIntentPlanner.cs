using Igorogue.Domain.Board;
using Igorogue.Domain.Content;

namespace Igorogue.Domain.Enemies;

public static class BanditIntentPlanner
{
    public static PlannedEnemyIntent Plan(
        BanditPlanningContext context,
        EnemyContentDefinition enemy,
        string plannedFromStateChecksum,
        StoneRuntimePlacementDescriptor placementDescriptor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentNullException.ThrowIfNull(placementDescriptor);

        var lethal = Candidates(
            context,
            enemy,
            EnemyIntentKind.CaptureBlackKing,
            placementDescriptor);
        if (lethal.Count > 0)
        {
            return CreatePlan(lethal, plannedFromStateChecksum);
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
                return CreatePlan(defense, plannedFromStateChecksum);
            }
        }

        foreach (var intentKind in enemy.PlanPriority)
        {
            var candidates = Candidates(context, enemy, intentKind, placementDescriptor);
            if (candidates.Count > 0)
            {
                return CreatePlan(candidates, plannedFromStateChecksum);
            }
        }

        return PlannedEnemyIntent.Pass(plannedFromStateChecksum);
    }

    private static IReadOnlyList<BanditPlacementCandidate> Candidates(
        BanditPlanningContext context,
        EnemyContentDefinition enemy,
        EnemyIntentKind intentKind,
        StoneRuntimePlacementDescriptor placementDescriptor) =>
        BanditCandidateGenerator.Generate(context, enemy, intentKind, placementDescriptor);

    private static PlannedEnemyIntent CreatePlan(
        IReadOnlyList<BanditPlacementCandidate> candidates,
        string plannedFromStateChecksum)
    {
        var primary = candidates[0];
        return PlannedEnemyIntent.Create(
            primary.IntentKind,
            primary.TargetReference,
            primary.Point,
            candidates.Skip(1).Select(candidate => candidate.Point).Distinct().Take(2),
            retargetable: true,
            plannedFromStateChecksum);
    }
}
