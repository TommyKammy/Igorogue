using Igorogue.Domain.Combat;

namespace Igorogue.Application.Battle;

public enum EnemyActionStage : byte
{
    NormalAction = 1,
    CounterattackAction = 2,
}

public enum EnemyTurnBoundaryStage : byte
{
    EnemyNormalAction = 1,
    EnemyCounterattackAction = 2,
    ConsumeCurrentPendingAndReprimeOverflow = 3,
    TemporaryLibertyExpirySweep = 4,
    EnemyTurnEndCounterattackGain = 5,
    PlanNextIntents = 6,
}

public sealed class EnemyTurnBoundaryStageFact : IBattleFact
{
    internal EnemyTurnBoundaryStageFact(
        int enemyTurnIndex,
        EnemyTurnBoundaryStage stage)
    {
        if (enemyTurnIndex <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(enemyTurnIndex),
                enemyTurnIndex,
                "Enemy-turn index must be positive.");
        }

        if (stage is < EnemyTurnBoundaryStage.EnemyNormalAction or
            > EnemyTurnBoundaryStage.PlanNextIntents)
        {
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown boundary stage.");
        }

        EnemyTurnIndex = enemyTurnIndex;
        Stage = stage;
    }

    public int EnemyTurnIndex { get; }

    public EnemyTurnBoundaryStage Stage { get; }

    public string StageId => Stage switch
    {
        EnemyTurnBoundaryStage.EnemyNormalAction => "enemy_normal_action",
        EnemyTurnBoundaryStage.EnemyCounterattackAction =>
            "enemy_counterattack_action",
        EnemyTurnBoundaryStage.ConsumeCurrentPendingAndReprimeOverflow =>
            "consume_current_pending_and_reprime_overflow",
        EnemyTurnBoundaryStage.TemporaryLibertyExpirySweep =>
            "temporary_liberty_expiry_sweep",
        EnemyTurnBoundaryStage.EnemyTurnEndCounterattackGain =>
            "enemy_turn_end_counterattack_gain",
        EnemyTurnBoundaryStage.PlanNextIntents => "plan_next_intents",
        _ => throw new InvalidOperationException("Boundary fact contains an unknown stage."),
    };
}
