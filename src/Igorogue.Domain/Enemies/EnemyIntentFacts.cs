using Igorogue.Domain.Board;
using Igorogue.Domain.Combat;
using Igorogue.Domain.Content;

namespace Igorogue.Domain.Enemies;

public sealed class EnemyIntentPlannedFact : IBattleFact
{
    public EnemyIntentPlannedFact(
        PlannedEnemyIntent plannedIntent,
        bool isCounterattackAction)
    {
        ArgumentNullException.ThrowIfNull(plannedIntent);
        PlannedIntent = plannedIntent;
        IsCounterattackAction = isCounterattackAction;
    }

    public PlannedEnemyIntent PlannedIntent { get; }

    public bool IsCounterattackAction { get; }
}

public sealed class EnemyIntentRetargetedFact : IBattleFact
{
    public EnemyIntentRetargetedFact(BanditExecutionDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (!decision.Retargeted ||
            decision.ExecutedIntentKind is not EnemyIntentKind executedIntentKind ||
            decision.TargetAfter is null)
        {
            throw new ArgumentException(
                "Enemy retarget fact requires a retargeted placement decision.",
                nameof(decision));
        }

        PlannedIntentKind = decision.PlannedIntent.IntentKind;
        ExecutedIntentKind = executedIntentKind;
        TargetBefore = decision.TargetBefore;
        TargetAfter = decision.TargetAfter;
        FallbackDepth = decision.FallbackDepth;
        ReasonId = decision.ReasonId;
    }

    public EnemyIntentKind? PlannedIntentKind { get; }

    public EnemyIntentKind ExecutedIntentKind { get; }

    public EnemyTargetReference? TargetBefore { get; }

    public EnemyTargetReference TargetAfter { get; }

    public int FallbackDepth { get; }

    public string ReasonId { get; }
}

public sealed class EnemyActionStartedFact : IBattleFact
{
    public EnemyActionStartedFact(
        int enemyTurnIndex,
        int actionIndex,
        bool isCounterattackAction,
        PlannedEnemyIntent plannedIntent)
    {
        ValidateActionCoordinates(enemyTurnIndex, actionIndex);
        ArgumentNullException.ThrowIfNull(plannedIntent);
        EnemyTurnIndex = enemyTurnIndex;
        ActionIndex = actionIndex;
        IsCounterattackAction = isCounterattackAction;
        PlannedIntent = plannedIntent;
    }

    public int EnemyTurnIndex { get; }

    public int ActionIndex { get; }

    public bool IsCounterattackAction { get; }

    public PlannedEnemyIntent PlannedIntent { get; }

    internal static void ValidateActionCoordinates(int enemyTurnIndex, int actionIndex)
    {
        if (enemyTurnIndex <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(enemyTurnIndex),
                enemyTurnIndex,
                "Enemy-turn index must be positive.");
        }

        if (actionIndex <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionIndex),
                actionIndex,
                "Enemy action index must be positive.");
        }
    }
}

public sealed class EnemyActionResolvedFact : IBattleFact
{
    public EnemyActionResolvedFact(
        int enemyTurnIndex,
        int actionIndex,
        bool isCounterattackAction,
        BanditExecutionDecision decision)
    {
        EnemyActionStartedFact.ValidateActionCoordinates(enemyTurnIndex, actionIndex);
        ArgumentNullException.ThrowIfNull(decision);
        EnemyTurnIndex = enemyTurnIndex;
        ActionIndex = actionIndex;
        IsCounterattackAction = isCounterattackAction;
        PlannedIntentId = decision.PlannedIntent.IntentId;
        ExecutedIntentId = decision.ExecutedIntentId;
        ReasonId = decision.ReasonId;
        ExecutedPoint = decision.Candidate?.Point;
        TargetBefore = decision.TargetBefore;
        TargetAfter = decision.TargetAfter;
        FallbackDepth = decision.FallbackDepth;
    }

    public int EnemyTurnIndex { get; }

    public int ActionIndex { get; }

    public bool IsCounterattackAction { get; }

    public string PlannedIntentId { get; }

    public string ExecutedIntentId { get; }

    public string ReasonId { get; }

    public CanonicalPoint? ExecutedPoint { get; }

    public EnemyTargetReference? TargetBefore { get; }

    public EnemyTargetReference? TargetAfter { get; }

    public int FallbackDepth { get; }
}
